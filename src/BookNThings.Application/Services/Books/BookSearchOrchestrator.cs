using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BookNThings.Application.Services;

public sealed class BookSearchOrchestrator(
    IBookSearchService searchService,
    ILogger<BookSearchOrchestrator> logger)
{
    public async Task<IReadOnlyList<Book>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogWarning("Rejected empty book search query.");
            throw new ArgumentException("Enter a search query before searching.", nameof(query));
        }

        return await searchService.SearchAsync(query.Trim(), cancellationToken);
    }
}
