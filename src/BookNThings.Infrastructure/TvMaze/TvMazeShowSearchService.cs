using System.Net;
using System.Text.Json;
using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.OpenAi;
using Microsoft.Extensions.Logging;

namespace BookNThings.Infrastructure.TvMaze;

public sealed class TvMazeShowSearchService(
    HttpClient httpClient,
    OpenAiShowSearchService fallbackSearchService,
    ILogger<TvMazeShowSearchService> logger) : IShowSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Show>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("TVmaze show search rejected an empty query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        var normalizedQuery = query.Trim();

        try
        {
            var tvMazeShows = await SearchTvMazeAsync(normalizedQuery, cancellationToken);
            if (tvMazeShows.Count > 0)
            {
                return tvMazeShows;
            }

            logger.LogInformation("TVmaze returned no grounded show results for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("TVmaze lookup timed out for {Query}; falling back to OpenAI.", normalizedQuery);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "TVmaze lookup failed for {Query}; falling back to OpenAI.", normalizedQuery);
        }

        return await fallbackSearchService.SearchAsync(normalizedQuery, cancellationToken);
    }

    private async Task<IReadOnlyList<Show>> SearchTvMazeAsync(string query, CancellationToken cancellationToken)
    {
        using var searchRequest = new HttpRequestMessage(HttpMethod.Get, $"search/shows?q={Uri.EscapeDataString(query)}");
        using var searchResponse = await httpClient.SendAsync(searchRequest, cancellationToken);

        if (searchResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!searchResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"TVmaze search failed with status {searchResponse.StatusCode}.");
        }

        var searchContent = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
        var searchResults = Deserialize<TvMazeShowSearchResult[]>(searchContent) ?? [];

        var groundedResults = OpenAiSearchResultGrounding.FilterSpecificMatches(
            query,
            searchResults,
            result => result.Show.Name ?? "",
            result => new[]
            {
                result.Show.Name,
                result.Show.Network?.Name,
                result.Show.WebChannel?.Name,
                string.Join(" ", result.Show.Genres ?? [])
            });

        if (groundedResults.Count == 0)
        {
            return [];
        }

        foreach (var result in groundedResults)
        {
            var seasons = await GetSeasonsAsync(result.Show.Id, cancellationToken);
            if (seasons.Count == 0)
            {
                continue;
            }

            var mapped = MapToShows(result.Show, seasons);
            if (mapped.Count > 0)
            {
                return mapped;
            }
        }

        return [];
    }

    private async Task<IReadOnlyList<TvMazeSeason>> GetSeasonsAsync(int showId, CancellationToken cancellationToken)
    {
        using var seasonsRequest = new HttpRequestMessage(HttpMethod.Get, $"shows/{showId}/seasons");
        using var seasonsResponse = await httpClient.SendAsync(seasonsRequest, cancellationToken);

        if (seasonsResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!seasonsResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"TVmaze season lookup failed with status {seasonsResponse.StatusCode}.");
        }

        var seasonsContent = await seasonsResponse.Content.ReadAsStringAsync(cancellationToken);
        return Deserialize<TvMazeSeason[]>(seasonsContent) ?? [];
    }

    private static IReadOnlyList<Show> MapToShows(TvMazeShow show, IReadOnlyList<TvMazeSeason> seasons) =>
        seasons
            .Where(season => season.Number > 0)
            .Select(season => new Show
            {
                Title = show.Name?.Trim() ?? "",
                Network = season.Network?.Name?.Trim()
                    ?? season.WebChannel?.Name?.Trim()
                    ?? show.Network?.Name?.Trim()
                    ?? show.WebChannel?.Name?.Trim()
                    ?? "Unknown Network",
                Studio = season.Network?.Name?.Trim()
                    ?? season.WebChannel?.Name?.Trim()
                    ?? show.Network?.Name?.Trim()
                    ?? show.WebChannel?.Name?.Trim()
                    ?? "TVmaze",
                Season = season.Number,
                Rating = show.Rating?.Average,
                Genres = show.Genres?.Where(genre => !string.IsNullOrWhiteSpace(genre)).Select(genre => genre.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
                Creator = null,
                CreatedAt = DateTime.UtcNow
            })
            .Where(show => ShowIsValid(show))
            .ToList();

    private static bool ShowIsValid(Show show) =>
        !string.IsNullOrWhiteSpace(show.Title)
        && !string.IsNullOrWhiteSpace(show.Network)
        && !string.IsNullOrWhiteSpace(show.Studio)
        && show.Season > 0;

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("TVmaze response contained invalid JSON.", ex);
        }
    }
}
