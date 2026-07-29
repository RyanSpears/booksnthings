using System.Net;
using System.Text;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Igdb;
using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class IgdbGameSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Map_Igdb_Game_Into_Local_Shape()
    {
        // Arrange
        var releaseDate = new DateTimeOffset(2023, 8, 3, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var igdbHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.StartsWith("https://id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase) == true)
            {
                request.RequestUri!.Query.Should().Contain("client_id=test-client-id");
                request.RequestUri!.Query.Should().Contain("client_secret=test-client-secret");
                request.RequestUri!.Query.Should().Contain("grant_type=client_credentials");
                return JsonResponse("""
                {
                  "access_token": "access-token",
                  "expires_in": 3600,
                  "token_type": "bearer"
                }
                """);
            }

            if (request.RequestUri?.AbsolutePath == "/v4/games")
            {
                request.Headers.TryGetValues("Client-ID", out var clientIdValues).Should().BeTrue();
                clientIdValues.Should().ContainSingle().Which.Should().Be("test-client-id");
                request.Headers.Authorization.Should().NotBeNull();
                request.Headers.Authorization!.Scheme.Should().Be("Bearer");
                request.Headers.Authorization!.Parameter.Should().Be("access-token");

                return JsonResponse($$"""
                [
                  {
                    "id": 1,
                    "name": "Baldur's Gate 3",
                    "first_release_date": {{releaseDate}},
                    "rating": 96,
                    "genres": [
                      { "name": "RPG" },
                      { "name": "Fantasy" }
                    ],
                    "involved_companies": [
                      {
                        "company": { "name": "Larian Studios" },
                        "developer": true,
                        "publisher": true
                      }
                    ]
                  }
                ]
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var igdbClient = new HttpClient(igdbHandler)
        {
            BaseAddress = new Uri("https://api.igdb.com/v4/")
        };

        var openAiHandler = new RoutingHandler(_ => throw new InvalidOperationException("OpenAI fallback should not be called."));
        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiGameSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiGameSearchService>.Instance);
        var service = new IgdbGameSearchService(
            igdbClient,
            Options.Create(new IgdbOptions { ClientId = "test-client-id", ClientSecret = "test-client-secret" }),
            fallback,
            MockLogger<IgdbGameSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Baldur's Gate 3", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        var game = results[0];
        game.Title.Should().Be("Baldur's Gate 3");
        game.Publisher.Should().Be("Larian Studios");
        game.Studio.Should().Be("Larian Studios");
        game.Developer.Should().Be("Larian Studios");
        game.ReleasedDate.Should().Be(new DateTime(2023, 8, 3));
        game.Rating.Should().Be(96);
        game.Genres.Should().Contain("RPG");
        game.Genres.Should().Contain("Fantasy");
    }

    [Fact]
    public async Task SearchAsync_Should_Fall_Back_To_OpenAi_When_Igdb_Is_Not_Configured()
    {
        // Arrange
        var igdbHandler = new RoutingHandler(_ => throw new InvalidOperationException("IGDB should not be called when it is not configured."));
        var igdbClient = new HttpClient(igdbHandler)
        {
            BaseAddress = new Uri("https://api.igdb.com/v4/")
        };

        var openAiHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri != "https://api.openai.com/v1/responses")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return JsonResponse("""
            {
              "output_text": "{\"results\":[{\"title\":\"Baldur's Gate 3\",\"publisher\":\"Larian Studios\",\"studio\":\"Larian Studios\",\"releasedDate\":\"2023-08-03\",\"rating\":96,\"genres\":[\"RPG\",\"Fantasy\"],\"developer\":\"Larian Studios\"}]}"
            }
            """);
        });

        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiGameSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiGameSearchService>.Instance);
        var service = new IgdbGameSearchService(
            igdbClient,
            Options.Create(new IgdbOptions { ClientId = "", ClientSecret = "" }),
            fallback,
            MockLogger<IgdbGameSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Baldur's Gate 3", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Baldur's Gate 3");
        results[0].Studio.Should().Be("Larian Studios");
    }

    [Fact]
    public async Task SearchAsync_Should_Fall_Back_To_OpenAi_When_Igdb_Returns_No_Grounded_Matches()
    {
        // Arrange
        var igdbHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.StartsWith("https://id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase) == true)
            {
                return JsonResponse("""
                {
                  "access_token": "access-token",
                  "expires_in": 3600,
                  "token_type": "bearer"
                }
                """);
            }

            return JsonResponse("""
            [
              {
                "id": 1,
                "name": "Unrelated Game",
                "first_release_date": 1691017200,
                "rating": 72,
                "genres": [
                  { "name": "Adventure" }
                ],
                "involved_companies": [
                  {
                    "company": { "name": "Another Studio" },
                    "developer": true,
                    "publisher": true
                  }
                ]
              }
            ]
            """);
        });

        var igdbClient = new HttpClient(igdbHandler)
        {
            BaseAddress = new Uri("https://api.igdb.com/v4/")
        };

        var openAiHandler = new RoutingHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri != "https://api.openai.com/v1/responses")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return JsonResponse("""
            {
              "output_text": "{\"results\":[{\"title\":\"Baldur's Gate 3\",\"publisher\":\"Larian Studios\",\"studio\":\"Larian Studios\",\"releasedDate\":\"2023-08-03\",\"rating\":96,\"genres\":[\"RPG\",\"Fantasy\"],\"developer\":\"Larian Studios\"}]}"
            }
            """);
        });

        var openAiClient = new HttpClient(openAiHandler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var fallback = new OpenAiGameSearchService(openAiClient, Options.Create(new OpenAiOptions { ApiKey = "test-key" }), MockLogger<OpenAiGameSearchService>.Instance);
        var service = new IgdbGameSearchService(
            igdbClient,
            Options.Create(new IgdbOptions { ClientId = "test-client-id", ClientSecret = "test-client-secret" }),
            fallback,
            MockLogger<IgdbGameSearchService>.Instance);

        // Act
        var results = await service.SearchAsync("Baldur's Gate 3", CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Baldur's Gate 3");
        results[0].Publisher.Should().Be("Larian Studios");
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
