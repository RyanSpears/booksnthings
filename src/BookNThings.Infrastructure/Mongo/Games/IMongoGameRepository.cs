using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.Mongo;

public interface IMongoGameRepository : IGameRepository
{
    Task ReplaceAllAsync(
        IReadOnlyList<Game> games,
        CancellationToken cancellationToken);
}
