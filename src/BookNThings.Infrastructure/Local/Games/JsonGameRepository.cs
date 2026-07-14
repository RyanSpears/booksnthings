using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonGameRepository(JsonGameStore jsonStore) : IGameRepository
{
    private readonly JsonGameStore _jsonStore = jsonStore;

    public async Task SaveAsync(Game item, CancellationToken cancellationToken)
    {
        var errors = item.DatePlayed.HasValue
            ? GameValidator.ValidateForPlayed(item)
            : GameValidator.ValidateForCurrentlyPlaying(item);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        await _jsonStore.UpsertAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken) =>
        _jsonStore.GetAllAsync(cancellationToken);

    public Task<Game?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        return _jsonStore.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdatePlayedDateAsync(string id, DateTime datePlayed, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        if (datePlayed == default)
        {
            throw new ArgumentException("Played date is required.", nameof(datePlayed));
        }

        return _jsonStore.UpdatePlayedDateAsync(id, datePlayed, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        return _jsonStore.DeleteAsync(id, cancellationToken);
    }
}
