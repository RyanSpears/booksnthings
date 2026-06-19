using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BookNThings.Application.Services;

public sealed class GameSearchOrchestrator(
    IGameSearchService searchService,
    ILogger<GameSearchOrchestrator> logger)
{
    public async Task<IReadOnlyList<Game>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("Rejected empty game search query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        return await searchService.SearchAsync(query.Trim(), cancellationToken);
    }
}
