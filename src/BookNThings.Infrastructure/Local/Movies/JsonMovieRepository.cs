using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonMovieRepository(JsonMovieStore jsonStore) : IMovieRepository
{
    private readonly JsonMovieStore _jsonStore = jsonStore;

    public async Task SaveAsync(Movie item, CancellationToken cancellationToken)
    {
        var errors = item.DateWatched.HasValue
            ? MovieValidator.ValidateForWatched(item)
            : MovieValidator.ValidateForCurrentlyWatching(item);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        await _jsonStore.UpsertAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken) =>
        _jsonStore.GetAllAsync(cancellationToken);

    public Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Movie id is required.", nameof(id));
        }

        return _jsonStore.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdateWatchedDateAsync(string id, DateTime dateWatched, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Movie id is required.", nameof(id));
        }

        if (dateWatched == default)
        {
            throw new ArgumentException("Watched date is required.", nameof(dateWatched));
        }

        return _jsonStore.UpdateWatchedDateAsync(id, dateWatched, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Movie id is required.", nameof(id));
        }

        return _jsonStore.DeleteAsync(id, cancellationToken);
    }
}
