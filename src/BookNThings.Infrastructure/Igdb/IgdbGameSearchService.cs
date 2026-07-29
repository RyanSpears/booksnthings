using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.OpenAi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.Igdb;

public sealed class IgdbGameSearchService(
    HttpClient httpClient,
    IOptions<IgdbOptions> options,
    OpenAiGameSearchService fallbackSearchService,
    ILogger<IgdbGameSearchService> logger) : IGameSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IgdbOptions _options = options.Value;

    public async Task<IReadOnlyList<Game>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("IGDB game search rejected an empty query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            logger.LogInformation("IGDB credentials are not configured; falling back to OpenAI for {Query}.", query.Trim());
            return await fallbackSearchService.SearchAsync(query.Trim(), cancellationToken);
        }

        var normalizedQuery = query.Trim();

        try
        {
            var results = await SearchIgdbAsync(normalizedQuery, cancellationToken);
            if (results.Count > 0)
            {
                return results;
            }

            logger.LogInformation("IGDB returned no grounded game results for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("IGDB lookup timed out for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "IGDB lookup failed for {Query}; falling back to OpenAI.", normalizedQuery);
        }

        return await fallbackSearchService.SearchAsync(normalizedQuery, cancellationToken);
    }

    private async Task<IReadOnlyList<Game>> SearchIgdbAsync(string query, CancellationToken cancellationToken)
    {
        var accessToken = await RequestAccessTokenAsync(cancellationToken);
        using var request = BuildSearchRequest(query, accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"IGDB game search failed with status {response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var candidates = Deserialize<IgdbGameSearchResponse[]>(content) ?? [];
        if (candidates.Length == 0)
        {
            return [];
        }

        var grounded = OpenAiSearchResultGrounding.FilterSpecificMatches(
            query,
            candidates,
            result => result.Name,
            result => new[]
            {
                result.Name,
                string.Join(" ", result.Genres.Select(genre => genre.Name)),
                string.Join(" ", result.InvolvedCompanies.Select(company => company.Company?.Name))
            });

        if (grounded.Count == 0)
        {
            return [];
        }

        var mappedGames = new List<Game>();
        foreach (var candidate in grounded.Take(5))
        {
            var game = MapToGame(candidate);
            if (game is not null)
            {
                mappedGames.Add(game);
            }
        }

        return mappedGames;
    }

    private async Task<string> RequestAccessTokenAsync(CancellationToken cancellationToken)
    {
        var requestUri = BuildTokenRequestUri(_options.ClientId, _options.ClientSecret);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"IGDB token request failed with status {response.StatusCode}.");
        }

        var tokenResponse = Deserialize<IgdbTokenResponse>(content)
            ?? throw new InvalidOperationException("IGDB token response was empty.");

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("IGDB token response did not include an access token.");
        }

        return tokenResponse.AccessToken.Trim();
    }

    private HttpRequestMessage BuildSearchRequest(string query, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "games");
        request.Headers.Add("Client-ID", _options.ClientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(BuildSearchQuery(query), Encoding.UTF8, "text/plain");
        return request;
    }

    private static Game? MapToGame(IgdbGameSearchResponse candidate)
    {
        var title = candidate.Name.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var releaseDate = candidate.FirstReleaseDate is not null
            ? DateTimeOffset.FromUnixTimeSeconds(candidate.FirstReleaseDate.Value).UtcDateTime.Date
            : default;

        if (releaseDate == default)
        {
            return null;
        }

        var involvedCompanies = candidate.InvolvedCompanies
            .Where(company => !string.IsNullOrWhiteSpace(company.Company?.Name))
            .Select(company => new
            {
                Name = company.Company!.Name.Trim(),
                IsDeveloper = company.Developer == true,
                IsPublisher = company.Publisher == true
            })
            .ToList();

        if (involvedCompanies.Count == 0)
        {
            return null;
        }

        var publisher = involvedCompanies.FirstOrDefault(company => company.IsPublisher)?.Name
            ?? involvedCompanies.FirstOrDefault(company => company.IsDeveloper)?.Name
            ?? involvedCompanies.First().Name;

        var studio = involvedCompanies.FirstOrDefault(company => company.IsDeveloper)?.Name
            ?? involvedCompanies.FirstOrDefault(company => company.IsPublisher)?.Name
            ?? involvedCompanies.First().Name;

        var developer = involvedCompanies.FirstOrDefault(company => company.IsDeveloper)?.Name;
        var genres = candidate.Genres
            .Select(genre => genre.Name?.Trim())
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var game = new Game
        {
            Title = title,
            Publisher = publisher,
            Studio = studio,
            ReleasedDate = releaseDate,
            DatePlayed = null,
            Rating = candidate.Rating,
            Genres = genres,
            Developer = string.IsNullOrWhiteSpace(developer) ? null : developer.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return GameValidator.Validate(game).Count == 0 ? game : null;
    }

    private static string BuildSearchQuery(string query)
    {
        var searchTerm = EscapeApicalypseString(query);
        var statements = new List<string>
        {
            $"search \"{searchTerm}\";",
            "fields name, first_release_date, rating, genres.name, involved_companies.company.name, involved_companies.publisher, involved_companies.developer;"
        };

        var whereParts = new List<string> { "version_parent = null" };
        var year = ExtractYear(query);
        if (year is not null)
        {
            var yearNumber = checked((int)year.Value);
            var start = new DateTimeOffset(yearNumber, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var end = new DateTimeOffset(yearNumber + 1, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            whereParts.Add($"first_release_date >= {start} & first_release_date < {end}");
        }

        statements.Add($"where {string.Join(" & ", whereParts)};");
        statements.Add("limit 10;");
        return string.Join(" ", statements);
    }

    private static long? ExtractYear(string query)
    {
        foreach (var match in System.Text.RegularExpressions.Regex.Matches(query, @"\b(18|19|20)\d{2}\b").Cast<System.Text.RegularExpressions.Match>())
        {
            if (long.TryParse(match.Value, out var year))
            {
                return year;
            }
        }

        return null;
    }

    private static string EscapeApicalypseString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static Uri BuildTokenRequestUri(string clientId, string clientSecret)
    {
        var query = string.Join("&", new[]
        {
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"client_secret={Uri.EscapeDataString(clientSecret)}",
            "grant_type=client_credentials"
        });

        return new Uri($"https://id.twitch.tv/oauth2/token?{query}");
    }

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("IGDB response contained invalid JSON.", ex);
        }
    }
}
