using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IShowSearchService
{
    Task<IReadOnlyList<Show>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
