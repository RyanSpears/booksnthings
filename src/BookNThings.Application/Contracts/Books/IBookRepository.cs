using BookNThings.Domain.Models;

namespace BookNThings.Application.Contracts;

public interface IBookRepository
{
    Task SaveAsync(
        Book item,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Book>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Book?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    Task UpdateReadDateAsync(
        string id,
        DateTime dateRead,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string id,
        CancellationToken cancellationToken);
}
