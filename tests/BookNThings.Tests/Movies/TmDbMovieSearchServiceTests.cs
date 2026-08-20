using System.Net;
using System.Text;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.OpenAi;
using BookNThings.Infrastructure.TmDb;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class TmDbMovieSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Map_TmDb_Movie_Into_Local_Shape()
    {
        // Arrange
        var tmDbHandler = new RoutingHandler(request => request.RequestUri?.AbsolutePath switch
        {
            "/3/search/movie" => JsonResponse("""
            {
              "results": [
                {
                  "id": 11,
                  "title": "The Matrix",
                  "original_title": "The Matrix",
                  "release_date": "1999-03-31",
                  "vote_average": 8.2,
                  "vote_count": 25000
                }
              ]
            }
            """),
            "/3/movie/11" => JsonResponse("""
            {
              "id": 11,
              "title": "The Matrix",
              "release_date": "1999-03-31",
              "vote_average": 8.2,
              "vote_count": 25000,
              "genres": [
                { "id": 28, "name": "Action" },
                { "id": 878, "name": "Science Fiction" }
              ],
              "production_companies": [
                { "id": 79, "name": "Warner Bros. Pictures" }
              ],
              "credits": {
                "crew": [
                  { "job": "Director", "name": "Lana Wachowski" },
                  { "job": "Producer", "name": "Joel Silver" }
                ]
              }
            }
            """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var tmDbClient = new HttpClient(tmDbHandler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };

        var openAiHandler = new RoutingHandler(_ => throw new InvalidOperationException("OpenAI fallback should not be called."));
        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiMovieSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiMovieSearchService>.Instance);
        var service = new TmDbMovieSearchService(tmDbClient, Options.Create(new TmDbOptions { BearerToken = "test-token" }), fallback, MockLogger<TmDbMovieSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("The Matrix", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("The Matrix");
        results[0].Studio.Should().Be("Warner Bros. Pictures");
        results[0].Director.Should().Be("Lana Wachowski");
        results[0].ReleasedDate.Should().Be(new DateTime(1999, 3, 31));
        results[0].Rating.Should().Be(8.2m);
        results[0].Genres.Should().Contain("Action");
        results[0].Genres.Should().Contain("Science Fiction");
    }

    [Fact]
    public async Task SearchAsync_Should_Fall_Back_To_OpenAi_When_TmDb_Returns_No_Matches()
    {
        // Arrange
        var tmDbHandler = new RoutingHandler(request => request.RequestUri?.AbsolutePath switch
        {
            "/3/search/movie" => JsonResponse("""
            { "results": [] }
            """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var tmDbClient = new HttpClient(tmDbHandler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };

        var openAiHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri != "https://api.openai.com/v1/responses")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return JsonResponse("""
            {
              "output_text": "{\"results\":[{\"title\":\"The Matrix\",\"studio\":\"Warner Bros. Pictures\",\"releasedDate\":\"1999-03-31\",\"rating\":82,\"genres\":[\"Action\",\"Science Fiction\"],\"director\":\"Lana Wachowski\"}]}"
            }
            """);
        });

        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiMovieSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiMovieSearchService>.Instance);
        var service = new TmDbMovieSearchService(tmDbClient, Options.Create(new TmDbOptions { BearerToken = "test-token" }), fallback, MockLogger<TmDbMovieSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("The Matrix", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("The Matrix");
        results[0].Studio.Should().Be("Warner Bros. Pictures");
        results[0].Director.Should().Be("Lana Wachowski");
    }

    [Fact]
    public async Task SearchAsync_Should_Fall_Back_To_OpenAi_When_TmDb_Is_Not_Configured()
    {
        // Arrange
        var tmDbHandler = new RoutingHandler(_ => throw new InvalidOperationException("TMDb should not be called when it is not configured."));
        var tmDbClient = new HttpClient(tmDbHandler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };

        var openAiHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri != "https://api.openai.com/v1/responses")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return JsonResponse("""
            {
              "output_text": "{\"results\":[{\"title\":\"The Matrix\",\"studio\":\"Warner Bros. Pictures\",\"releasedDate\":\"1999-03-31\",\"rating\":82,\"genres\":[\"Action\",\"Science Fiction\"],\"director\":\"Lana Wachowski\"}]}"
            }
            """);
        });

        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiMovieSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiMovieSearchService>.Instance);
        var service = new TmDbMovieSearchService(tmDbClient, Options.Create(new TmDbOptions { BearerToken = "" }), fallback, MockLogger<TmDbMovieSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("The Matrix", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("The Matrix");
        results[0].Studio.Should().Be("Warner Bros. Pictures");
        results[0].Director.Should().Be("Lana Wachowski");
    }

    [Fact]
    public async Task SearchAsync_Should_Retry_SpiderMan_Query_When_Query_Uses_Spiderman()
    {
        // Arrange
        var tmDbHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/3/search/movie" &&
                request.RequestUri.Query.Contains("query=Spiderman%3A%20Homecoming", StringComparison.Ordinal))
            {
                return JsonResponse("""{ "results": [] }""");
            }

            if (request.RequestUri?.AbsolutePath == "/3/search/movie" &&
                request.RequestUri.Query.Contains("query=Spider-Man%3A%20Homecoming", StringComparison.Ordinal))
            {
                return JsonResponse("""
                {
                  "results": [
                    {
                      "id": 315635,
                      "title": "Spider-Man: Homecoming",
                      "original_title": "Spider-Man: Homecoming",
                      "release_date": "2017-07-07",
                      "vote_average": 7.3,
                      "vote_count": 22000
                    }
                  ]
                }
                """);
            }

            if (request.RequestUri?.AbsolutePath == "/3/movie/315635")
            {
                return JsonResponse("""
                {
                  "id": 315635,
                  "title": "Spider-Man: Homecoming",
                  "release_date": "2017-07-07",
                  "vote_average": 7.3,
                  "vote_count": 22000,
                  "genres": [
                    { "id": 28, "name": "Action" },
                    { "id": 12, "name": "Adventure" }
                  ],
                  "production_companies": [
                    { "id": 5, "name": "Columbia Pictures" }
                  ],
                  "credits": {
                    "crew": [
                      { "job": "Director", "name": "Jon Watts" }
                    ]
                  }
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var tmDbClient = new HttpClient(tmDbHandler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };

        var openAiHandler = new RoutingHandler(_ => throw new InvalidOperationException("OpenAI fallback should not be called."));
        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiMovieSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiMovieSearchService>.Instance);
        var service = new TmDbMovieSearchService(tmDbClient, Options.Create(new TmDbOptions { BearerToken = "test-token" }), fallback, MockLogger<TmDbMovieSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Spiderman: Homecoming", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Spider-Man: Homecoming");
        results[0].Studio.Should().Be("Columbia Pictures");
        results[0].Director.Should().Be("Jon Watts");
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
