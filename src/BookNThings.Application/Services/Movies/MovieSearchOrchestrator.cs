using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BookNThings.Application.Services;

public sealed class MovieSearchOrchestrator(
    IMovieSearchService searchService,
    ILogger<MovieSearchOrchestrator> logger)
{
    public async Task<IReadOnlyList<Movie>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("Rejected empty movie search query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        return await searchService.SearchAsync(query.Trim(), cancellationToken);
    }
}
