using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IShowRepository
{
    Task SaveAsync(
        Show item,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Show>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Show?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    Task UpdateWatchedDateAsync(
        string id,
        DateTime dateWatched,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string id,
        CancellationToken cancellationToken);
}
