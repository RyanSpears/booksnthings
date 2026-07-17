using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IMovieRepository
{
    Task SaveAsync(Movie item, CancellationToken cancellationToken);

    Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken);

    Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task UpdateWatchedDateAsync(string id, DateTime dateWatched, CancellationToken cancellationToken);

    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
