using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;

namespace BookNThings.Infrastructure.Local;

public sealed class SynchronizingBookRepository : IBookRepository, IBookDataSynchronizer
{
    private readonly MongoBookRepository _mongoRepository;
    private readonly JsonBookStore _jsonStore;
    private readonly ILogger<SynchronizingBookRepository> _logger;

    public SynchronizingBookRepository(
        MongoBookRepository mongoRepository,
        JsonBookStore jsonStore,
        ILogger<SynchronizingBookRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _jsonStore = jsonStore;
        _logger = logger;
    }

    public async Task SaveAsync(Book item, CancellationToken cancellationToken)
    {
        if (item.DateRead == default)
        {
            item.DateRead = DateTime.UtcNow;
        }

        var errors = BookValidator.ValidateForSave(item);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        try
        {
            await _mongoRepository.SaveAsync(item, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB save failed. Saving book to the local JSON mirror.");
            await _jsonStore.UpsertAsync(item, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var books = await _mongoRepository.GetAllAsync(cancellationToken);
            await _jsonStore.ReplaceAllAsync(books, cancellationToken);
            return books;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB read failed. Loading books from the local JSON mirror.");
            return await _jsonStore.GetAllAsync(cancellationToken);
        }
    }

    public async Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        try
        {
            var book = await _mongoRepository.GetByIdAsync(id, cancellationToken);
            if (book is not null)
            {
                await MirrorMongoAsync(cancellationToken);
                return book;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB read by id failed. Loading book from the local JSON mirror.");
        }

        return await _jsonStore.GetByIdAsync(id, cancellationToken);
    }

    public async Task UpdateReadDateAsync(string id, DateTime dateRead, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        if (dateRead == default)
        {
            throw new ArgumentException("Read date is required.", nameof(dateRead));
        }

        try
        {
            await _mongoRepository.UpdateReadDateAsync(id, dateRead, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB update failed. Updating the local JSON mirror.");
            await _jsonStore.UpdateReadDateAsync(id, dateRead, cancellationToken);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Book id is required.", nameof(id));
        }

        try
        {
            await _mongoRepository.DeleteAsync(id, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB delete failed. Deleting from the local JSON mirror.");
            await _jsonStore.DeleteAsync(id, cancellationToken);
        }
    }

    public async Task AlignAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_jsonStore.FileExists)
            {
                var localBooks = await _jsonStore.GetAllAsync(cancellationToken);
                await _mongoRepository.ReplaceAllAsync(localBooks, cancellationToken);
                await MirrorMongoAsync(cancellationToken);
                return;
            }

            await MirrorMongoAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "MongoDB startup alignment failed. The local JSON mirror will be used until MongoDB is available.");
        }
    }

    private async Task MirrorMongoAsync(CancellationToken cancellationToken)
    {
        var books = await _mongoRepository.GetAllAsync(cancellationToken);
        await _jsonStore.ReplaceAllAsync(books, cancellationToken);
    }
}
