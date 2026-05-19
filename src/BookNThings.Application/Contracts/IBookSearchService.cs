using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IBookSearchService
{
    Task<IReadOnlyList<Book>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
