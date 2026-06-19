using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.Mongo;

public interface IMongoBookRepository : IBookRepository
{
    Task ReplaceAllAsync(
        IReadOnlyList<Book> books,
        CancellationToken cancellationToken);
}
