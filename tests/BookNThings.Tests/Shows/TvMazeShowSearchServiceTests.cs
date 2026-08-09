using System.Net;
using System.Text;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.OpenAi;
using BookNThings.Infrastructure.TvMaze;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class TvMazeShowSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Map_TvMaze_Show_And_Seasons()
    {
        // Arrange
        var tvMazeHandler = new RoutingHandler(request => request.RequestUri?.AbsolutePath switch
        {
            "/search/shows" => JsonResponse("""
            [
              {
                "score": 42.0,
                "show": {
                  "id": 123,
                  "name": "Severance",
                  "genres": ["Drama", "Mystery"],
                  "rating": { "average": 8.7 },
                  "network": { "name": "Apple TV+" },
                  "webChannel": null
                }
              }
            ]
            """),
            "/shows/123/seasons" => JsonResponse("""
            [
              {
                "id": 1,
                "number": 1,
                "name": "Season 1",
                "episodeOrder": 9,
                "premiereDate": "2022-02-18",
                "endDate": "2022-04-08",
                "network": { "name": "Apple TV+" },
                "webChannel": null
              },
              {
                "id": 2,
                "number": 2,
                "name": "Season 2",
                "episodeOrder": 10,
                "premiereDate": "2025-01-17",
                "endDate": "2025-03-21",
                "network": { "name": "Apple TV+" },
                "webChannel": null
              }
            ]
            """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var tvMazeClient = new HttpClient(tvMazeHandler)
        {
            BaseAddress = new Uri("https://api.tvmaze.com/")
        };

        var openAiHandler = new RoutingHandler(_ => throw new InvalidOperationException("OpenAI fallback should not be called."));
        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var openAiService = new OpenAiShowSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiShowSearchService>.Instance);
        var service = new TvMazeShowSearchService(tvMazeClient, openAiService, MockLogger<TvMazeShowSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Severance", CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Select(result => result.Season).Should().Equal(1, 2);
        results.Should().OnlyContain(result => result.Title == "Severance");
        results[0].Network.Should().Be("Apple TV+");
        results[0].Studio.Should().Be("Apple TV+");
        results[0].Rating.Should().Be(8.7m);
        results[0].Genres.Should().Contain("Drama");
    }

    [Fact]
    public async Task SearchAsync_Should_Find_Stylized_Show_Query_With_Requested_Season()
    {
        // Arrange
        string? requestedSearch = null;
        var tvMazeHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/search/shows")
            {
                requestedSearch = request.RequestUri.PathAndQuery;
            }

            return request.RequestUri?.AbsolutePath switch
            {
                "/search/shows" => JsonResponse("""
                [
                  {
                    "score": 42.0,
                    "show": {
                      "id": 321,
                      "name": "Rick and Morty",
                      "genres": ["Comedy", "Science-Fiction"],
                      "rating": { "average": 9.0 },
                      "network": { "name": "Adult Swim" },
                      "webChannel": null
                    }
                  }
                ]
                """),
                "/shows/321/seasons" => JsonResponse("""
                [
                  {
                    "id": 1,
                    "number": 1,
                    "name": "Season 1",
                    "episodeOrder": 11,
                    "premiereDate": "2013-12-02",
                    "endDate": "2014-04-14",
                    "network": { "name": "Adult Swim" },
                    "webChannel": null
                  },
                  {
                    "id": 4,
                    "number": 4,
                    "name": "Season 4",
                    "episodeOrder": 10,
                    "premiereDate": "2019-11-10",
                    "endDate": "2020-05-31",
                    "network": { "name": "Adult Swim" },
                    "webChannel": null
                  }
                ]
                """),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        var tvMazeClient = new HttpClient(tvMazeHandler)
        {
            BaseAddress = new Uri("https://api.tvmaze.com/")
        };

        var openAiHandler = new RoutingHandler(_ => throw new InvalidOperationException("OpenAI fallback should not be called."));
        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var openAiService = new OpenAiShowSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiShowSearchService>.Instance);
        var service = new TvMazeShowSearchService(tvMazeClient, openAiService, MockLogger<TvMazeShowSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Rick & Morty Season 4", CancellationToken.None);

        // Assert
        requestedSearch.Should().Be("/search/shows?q=Rick%20and%20Morty");
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Rick and Morty");
        results[0].Season.Should().Be(4);
        results[0].Network.Should().Be("Adult Swim");
    }

    [Fact]
    public async Task SearchAsync_Should_Fall_Back_To_OpenAi_When_TvMaze_Misses()
    {
        // Arrange
        var tvMazeHandler = new RoutingHandler(request => request.RequestUri?.AbsolutePath switch
        {
            "/search/shows" => new HttpResponseMessage(HttpStatusCode.NotFound),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var tvMazeClient = new HttpClient(tvMazeHandler)
        {
            BaseAddress = new Uri("https://api.tvmaze.com/")
        };

        var openAiHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri != "https://api.openai.com/v1/responses")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return JsonResponse("""
            {
              "output_text": "{\"results\":[{\"title\":\"Severance\",\"network\":\"Apple TV+\",\"studio\":\"Endeavor Content\",\"season\":2,\"rating\":94,\"genres\":[\"Drama\",\"Mystery\"],\"creator\":\"Dan Erickson\"}]}"
            }
            """);
        });

        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var openAiService = new OpenAiShowSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiShowSearchService>.Instance);
        var service = new TvMazeShowSearchService(tvMazeClient, openAiService, MockLogger<TvMazeShowSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Severance", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Severance");
        results[0].Network.Should().Be("Apple TV+");
        results[0].Season.Should().Be(2);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class MockLogger<T> : ILogger<T>
    {
        public static readonly MockLogger<T> Instance = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
