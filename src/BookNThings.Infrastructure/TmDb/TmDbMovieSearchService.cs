using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.OpenAi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.TmDb;

public sealed class TmDbMovieSearchService(
    HttpClient httpClient,
    IOptions<TmDbOptions> options,
    OpenAiMovieSearchService fallbackSearchService,
    ILogger<TmDbMovieSearchService> logger) : IMovieSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TmDbOptions _options = options.Value;

    public async Task<IReadOnlyList<Movie>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("TMDb movie search rejected an empty query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        if (string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            logger.LogInformation("TMDb bearer token is not configured; falling back to OpenAI for {Query}.", query.Trim());
            return await fallbackSearchService.SearchAsync(query.Trim(), cancellationToken);
        }

        var normalizedQuery = query.Trim();

        try
        {
            var results = await SearchMovieCandidatesAsync(normalizedQuery, cancellationToken);
            if (results.Count > 0)
            {
                return results;
            }

            logger.LogInformation("TMDb returned no grounded movie results for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("TMDb movie lookup timed out for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "TMDb lookup failed for {Query}; falling back to OpenAI.", normalizedQuery);
        }

        return await fallbackSearchService.SearchAsync(normalizedQuery, cancellationToken);
    }

    private async Task<IReadOnlyList<Movie>> SearchMovieCandidatesAsync(string query, CancellationToken cancellationToken)
    {
        foreach (var searchQuery in BuildSearchQueries(query))
        {
            var results = await SearchMovieCandidatesForQueryAsync(query, searchQuery, cancellationToken);
            if (results.Count > 0)
            {
                return results;
            }
        }

        return [];
    }

    private async Task<IReadOnlyList<Movie>> SearchMovieCandidatesForQueryAsync(
        string originalQuery,
        string searchQuery,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildSearchUri(searchQuery);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"TMDb movie search failed with status {response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = Deserialize<TmDbMovieSearchResponse>(content);
        if (searchResponse?.Results is null || searchResponse.Results.Count == 0)
        {
            return [];
        }

        var groundedResults = OpenAiSearchResultGrounding.FilterSpecificMatches(
            originalQuery,
            searchResponse.Results,
            result => result.Title ?? "",
            result => new[] { result.Title, result.OriginalTitle });

        if (groundedResults.Count == 0)
        {
            return [];
        }

        var mappedMovies = new List<Movie>();
        foreach (var candidate in groundedResults.Take(5))
        {
            var movie = await BuildMovieAsync(candidate, cancellationToken);
            if (movie is not null)
            {
                mappedMovies.Add(movie);
            }
        }

        return mappedMovies;
    }

    private static IEnumerable<string> BuildSearchQueries(string query)
    {
        yield return query;

        var canonicalQuery = System.Text.RegularExpressions.Regex.Replace(
            query,
            @"\bspiderman\b",
            "Spider-Man",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!string.Equals(canonicalQuery, query, StringComparison.OrdinalIgnoreCase))
        {
            yield return canonicalQuery;
        }
    }

    private async Task<Movie?> BuildMovieAsync(TmDbMovieSearchResult candidate, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"movie/{candidate.Id}?append_to_response=credits");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"TMDb movie details failed with status {response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var details = Deserialize<TmDbMovieDetailsResponse>(content);
        if (details is null)
        {
            return null;
        }

        var studio = details.ProductionCompanies?
            .Select(company => company.Name?.Trim())
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        if (string.IsNullOrWhiteSpace(studio))
        {
            return null;
        }

        var director = details.Credits?.Crew?
            .FirstOrDefault(member => string.Equals(member.Job, "Director", StringComparison.OrdinalIgnoreCase))
            ?.Name?.Trim();

        var genres = details.Genres?
            .Select(genre => genre.Name?.Trim())
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var movie = new Movie
        {
            Title = (details.Title ?? candidate.Title ?? "").Trim(),
            Studio = studio,
            ReleasedDate = ParseDate(details.ReleaseDate) ?? ParseDate(candidate.ReleaseDate) ?? default,
            DateWatched = null,
            Rating = details.VoteCount > 0 ? details.VoteAverage : null,
            Genres = genres,
            Director = string.IsNullOrWhiteSpace(director) ? null : director,
            CreatedAt = DateTime.UtcNow
        };

        return string.IsNullOrWhiteSpace(movie.Title) || string.IsNullOrWhiteSpace(movie.Studio) || movie.ReleasedDate == default
            ? null
            : movie;
    }

    private static string BuildSearchUri(string query)
    {
        var parts = new List<string>
        {
            $"query={Uri.EscapeDataString(query)}",
            "include_adult=false",
            "page=1"
        };

        var year = ExtractYear(query);
        if (year is not null)
        {
            parts.Add($"primary_release_year={year}");
        }

        return $"search/movie?{string.Join("&", parts)}";
    }

    private static int? ExtractYear(string query)
    {
        foreach (var match in System.Text.RegularExpressions.Regex.Matches(query, @"\b(18|19|20)\d{2}\b").Cast<System.Text.RegularExpressions.Match>())
        {
            if (int.TryParse(match.Value, out var year))
            {
                return year;
            }
        }

        return null;
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, out var parsed) ? parsed.Date : null;

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("TMDb response contained invalid JSON.", ex);
        }
    }
}
