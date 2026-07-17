using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IMovieSearchService
{
    Task<IReadOnlyList<Movie>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
