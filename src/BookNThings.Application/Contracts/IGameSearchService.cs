using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IGameSearchService
{
    Task<IReadOnlyList<Game>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
