using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Mongo;

public sealed class MongoBookRepository(IOptions<MongoDbOptions> options, ILogger<MongoBookRepository> logger) : IMongoBookRepository
{
    private readonly MongoDbOptions _options = options.Value;
    private readonly ILogger<MongoBookRepository> _logger = logger;
    private IMongoCollection<MongoBookDocument>? _collection;
    private bool _indexesCreated;

    public async Task SaveAsync(Book item, CancellationToken cancellationToken)
    {
        var errors = item.DateRead.HasValue
            ? BookValidator.ValidateForRead(item)
            : BookValidator.ValidateForCurrentlyReading(item);
        if (errors.Count > 0)
        {
            _logger.LogWarning("MongoDB save validation failed: {ValidationErrors}", string.Join(" ", errors));
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = ObjectId.GenerateNewId().ToString();
            }

            var collection = await GetCollectionAsync(cancellationToken);
            await collection.ReplaceOneAsync(
                book => book.Id == item.Id,
                ToDocument(item),
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB save failed.");
            throw new InvalidOperationException("Could not save the book. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB save failed.");
            throw new InvalidOperationException("Could not save the book. Please try again.", ex);
        }
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var documents = await collection
                .Find(FilterDefinition<MongoBookDocument>.Empty)
                .SortByDescending(book => book.DateRead)
                .ThenBy(book => book.Title)
                .ToListAsync(cancellationToken);

            return documents.Select(ToModel).ToList();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB read failed.");
            throw new InvalidOperationException("Could not load saved books. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB read failed.");
            throw new InvalidOperationException("Could not load saved books. Please try again.", ex);
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
            var collection = await GetCollectionAsync(cancellationToken);
            var document = await collection
                .Find(book => book.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return document is null ? null : ToModel(document);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB read by id failed.");
            throw new InvalidOperationException("Could not load the book read record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB read by id failed.");
            throw new InvalidOperationException("Could not load the book read record. Please try again.", ex);
        }
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
            var collection = await GetCollectionAsync(cancellationToken);
            var update = Builders<MongoBookDocument>.Update.Set(book => book.DateRead, dateRead.Date);
            var result = await collection.UpdateOneAsync(book => book.Id == id, update, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Book read record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB update read date failed.");
            throw new InvalidOperationException("Could not update the read date. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB update read date failed.");
            throw new InvalidOperationException("Could not update the read date. Please try again.", ex);
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
            var collection = await GetCollectionAsync(cancellationToken);
            var result = await collection.DeleteOneAsync(book => book.Id == id, cancellationToken);

            if (result.DeletedCount == 0)
            {
                throw new InvalidOperationException("Book read record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB delete failed.");
            throw new InvalidOperationException("Could not delete the book read record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB delete failed.");
            throw new InvalidOperationException("Could not delete the book read record. Please try again.", ex);
        }
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Book> books, CancellationToken cancellationToken)
    {
        var errors = books
            .SelectMany(book => book.DateRead.HasValue
                ? BookValidator.ValidateForRead(book)
                : BookValidator.ValidateForCurrentlyReading(book))
            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogWarning("MongoDB replace validation failed: {ValidationErrors}", string.Join(" ", errors));
            throw new ArgumentException(string.Join(" ", errors), nameof(books));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var normalized = books
                .Where(book => !string.IsNullOrWhiteSpace(book.Id))
                .GroupBy(book => book.Id)
                .Select(group => group.First())
                .ToList();

            var ids = normalized.Select(book => book.Id).ToList();
            if (ids.Count == 0)
            {
                await collection.DeleteManyAsync(FilterDefinition<MongoBookDocument>.Empty, cancellationToken);
                return;
            }

            var writes = normalized
                .Select(book => new ReplaceOneModel<MongoBookDocument>(
                    Builders<MongoBookDocument>.Filter.Eq(document => document.Id, book.Id),
                    ToDocument(book))
                {
                    IsUpsert = true
                })
                .Cast<WriteModel<MongoBookDocument>>()
                .ToList();

            await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
            await collection.DeleteManyAsync(
                Builders<MongoBookDocument>.Filter.Nin(document => document.Id, ids),
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB replace failed.");
            throw new InvalidOperationException("Could not align saved books with MongoDB. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB replace failed.");
            throw new InvalidOperationException("Could not align saved books with MongoDB. Please try again.", ex);
        }
    }

    private async Task CreateIndexes(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        var titleAuthor = new CreateIndexModel<MongoBookDocument>(
            Builders<MongoBookDocument>.IndexKeys
                .Ascending(book => book.Title)
                .Ascending(book => book.Author));

        var dateRead = new CreateIndexModel<MongoBookDocument>(
            Builders<MongoBookDocument>.IndexKeys.Descending(book => book.DateRead));

        await collection.Indexes.CreateManyAsync([titleAuthor, dateRead], cancellationToken);
    }

    private async Task<IMongoCollection<MongoBookDocument>> GetCollectionAsync(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        if (!_indexesCreated)
        {
            await CreateIndexes(cancellationToken);
            _indexesCreated = true;
        }

        return collection;
    }

    private IMongoCollection<MongoBookDocument> GetCollection()
    {
        if (_collection is not null)
        {
            return _collection;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning("MongoDB connection string is not configured.");
            throw new InvalidOperationException("MongoDB is not configured. Add MongoDb__ConnectionString and try again.");
        }

        var client = new MongoClient(_options.ConnectionString);
        var database = client.GetDatabase(string.IsNullOrWhiteSpace(_options.DatabaseName) ? "booknthings" : _options.DatabaseName);
        _collection = database.GetCollection<MongoBookDocument>(string.IsNullOrWhiteSpace(_options.BooksCollection) ? "books" : _options.BooksCollection);
        return _collection;
    }

    private static MongoBookDocument ToDocument(Book book) => new()
    {
        Id = string.IsNullOrWhiteSpace(book.Id) ? null : book.Id,
        Title = book.Title,
        Description = book.Description,
        Pages = book.Pages,
        DatePublished = book.DatePublished,
        DateRead = book.DateRead,
        Genres = book.Genres,
        Author = book.Author
    };

    private static Book ToModel(MongoBookDocument document) => new()
    {
        Id = document.Id ?? "",
        Title = document.Title,
        Description = document.Description,
        Pages = document.Pages,
        DatePublished = document.DatePublished,
        DateRead = document.DateRead,
        Genres = document.Genres,
        Author = document.Author
    };
}
