using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonBookRepository(JsonBookStore jsonStore) : IBookRepository
{
    private readonly JsonBookStore _jsonStore = jsonStore;

    public async Task SaveAsync(Book item, CancellationToken cancellationToken)
    {
        var errors = item.DateRead.HasValue
            ? BookValidator.ValidateForRead(item)
            : BookValidator.ValidateForCurrentlyReading(item);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        await _jsonStore.UpsertAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken) =>
        _jsonStore.GetAllAsync(cancellationToken);

    public Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        return _jsonStore.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdateReadDateAsync(string id, DateTime dateRead, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        if (dateRead == default)
        {
            throw new ArgumentException("Read date is required.", nameof(dateRead));
        }

        return _jsonStore.UpdateReadDateAsync(id, dateRead, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        return _jsonStore.DeleteAsync(id, cancellationToken);
    }
}
