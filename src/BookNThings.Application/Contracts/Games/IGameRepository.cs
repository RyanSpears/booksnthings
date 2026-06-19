using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IGameRepository
{
    Task SaveAsync(
        Game item,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Game>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Game?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    Task UpdatePlayedDateAsync(
        string id,
        DateTime datePlayed,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string id,
        CancellationToken cancellationToken);
}
