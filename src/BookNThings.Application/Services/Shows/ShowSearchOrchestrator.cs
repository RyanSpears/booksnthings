using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BookNThings.Application.Services;

public sealed class ShowSearchOrchestrator(
    IShowSearchService searchService,
    ILogger<ShowSearchOrchestrator> logger)
{
    public async Task<IReadOnlyList<Show>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("Rejected empty show search query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        return await searchService.SearchAsync(query.Trim(), cancellationToken);
    }
}
