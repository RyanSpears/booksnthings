using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Mongo;

public sealed class MongoShowRepository(IOptions<MongoDbOptions> options, ILogger<MongoShowRepository> logger) : IMongoShowRepository
{
    private readonly MongoDbOptions _options = options.Value;
    private readonly ILogger<MongoShowRepository> _logger = logger;
    private IMongoCollection<MongoShowDocument>? _collection;
    private bool _indexesCreated;

    public async Task SaveAsync(Show item, CancellationToken cancellationToken)
    {
        var errors = item.DateWatched.HasValue
            ? ShowValidator.ValidateForWatched(item)
            : ShowValidator.ValidateForCurrentlyWatching(item);
        if (errors.Count > 0)
        {
            _logger.LogWarning("MongoDB show save validation failed: {ValidationErrors}", string.Join(" ", errors));
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
                show => show.Id == item.Id,
                ToDocument(item),
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show save failed.");
            throw new InvalidOperationException("Could not save the show. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show save failed.");
            throw new InvalidOperationException("Could not save the show. Please try again.", ex);
        }
    }

    public async Task<IReadOnlyList<Show>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var documents = await collection
                .Find(FilterDefinition<MongoShowDocument>.Empty)
                .SortByDescending(show => show.DateWatched)
                .ThenBy(show => show.Title)
                .ThenBy(show => show.Season)
                .ToListAsync(cancellationToken);

            return documents.Select(ToModel).ToList();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show read failed.");
            throw new InvalidOperationException("Could not load saved shows. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show read failed.");
            throw new InvalidOperationException("Could not load saved shows. Please try again.", ex);
        }
    }

    public async Task<Show?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Show id is required.", nameof(id));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var document = await collection
                .Find(show => show.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return document is null ? null : ToModel(document);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show read by id failed.");
            throw new InvalidOperationException("Could not load the show record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show read by id failed.");
            throw new InvalidOperationException("Could not load the show record. Please try again.", ex);
        }
    }

    public async Task UpdateWatchedDateAsync(string id, DateTime dateWatched, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Show id is required.", nameof(id));
        }

        if (dateWatched == default)
        {
            throw new ArgumentException("Watched date is required.", nameof(dateWatched));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var update = Builders<MongoShowDocument>.Update.Set(show => show.DateWatched, dateWatched.Date);
            var result = await collection.UpdateOneAsync(show => show.Id == id, update, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Show record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show update watched date failed.");
            throw new InvalidOperationException("Could not update the watched date. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show update watched date failed.");
            throw new InvalidOperationException("Could not update the watched date. Please try again.", ex);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Show id is required.", nameof(id));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var result = await collection.DeleteOneAsync(show => show.Id == id, cancellationToken);

            if (result.DeletedCount == 0)
            {
                throw new InvalidOperationException("Show record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show delete failed.");
            throw new InvalidOperationException("Could not delete the show record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show delete failed.");
            throw new InvalidOperationException("Could not delete the show record. Please try again.", ex);
        }
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Show> shows, CancellationToken cancellationToken)
    {
        var errors = shows
            .SelectMany(show => show.DateWatched.HasValue
                ? ShowValidator.ValidateForWatched(show)
                : ShowValidator.ValidateForCurrentlyWatching(show))
            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogWarning("MongoDB show replace validation failed: {ValidationErrors}", string.Join(" ", errors));
            throw new ArgumentException(string.Join(" ", errors), nameof(shows));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var normalized = shows
                .Where(show => !string.IsNullOrWhiteSpace(show.Id))
                .GroupBy(show => show.Id)
                .Select(group => group.First())
                .ToList();

            var ids = normalized.Select(show => show.Id).ToList();
            if (ids.Count == 0)
            {
                await collection.DeleteManyAsync(FilterDefinition<MongoShowDocument>.Empty, cancellationToken);
                return;
            }

            var writes = normalized
                .Select(show => new ReplaceOneModel<MongoShowDocument>(
                    Builders<MongoShowDocument>.Filter.Eq(document => document.Id, show.Id),
                    ToDocument(show))
                {
                    IsUpsert = true
                })
                .Cast<WriteModel<MongoShowDocument>>()
                .ToList();

            await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
            await collection.DeleteManyAsync(
                Builders<MongoShowDocument>.Filter.Nin(document => document.Id, ids),
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB show replace failed.");
            throw new InvalidOperationException("Could not align saved shows with MongoDB. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB show replace failed.");
            throw new InvalidOperationException("Could not align saved shows with MongoDB. Please try again.", ex);
        }
    }

    private async Task CreateIndexes(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        var titleNetworkSeason = new CreateIndexModel<MongoShowDocument>(
            Builders<MongoShowDocument>.IndexKeys
                .Ascending(show => show.Title)
                .Ascending(show => show.Network)
                .Ascending(show => show.Season));

        var dateWatched = new CreateIndexModel<MongoShowDocument>(
            Builders<MongoShowDocument>.IndexKeys.Descending(show => show.DateWatched));

        await collection.Indexes.CreateManyAsync([titleNetworkSeason, dateWatched], cancellationToken);
    }

    private async Task<IMongoCollection<MongoShowDocument>> GetCollectionAsync(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        if (!_indexesCreated)
        {
            await CreateIndexes(cancellationToken);
            _indexesCreated = true;
        }

        return collection;
    }

    private IMongoCollection<MongoShowDocument> GetCollection()
    {
        if (_collection is not null)
        {
            return _collection;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("MongoDB is not configured. Add MongoDb__ConnectionString and try again.");
        }

        if (string.IsNullOrWhiteSpace(_options.DatabaseName))
        {
            throw new InvalidOperationException("MongoDB database name is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ShowsCollection))
        {
            throw new InvalidOperationException("MongoDB shows collection is not configured.");
        }

        var client = new MongoClient(_options.ConnectionString);
        _collection = client
            .GetDatabase(_options.DatabaseName)
            .GetCollection<MongoShowDocument>(_options.ShowsCollection);

        return _collection;
    }

    private static MongoShowDocument ToDocument(Show show) => new()
    {
        Id = string.IsNullOrWhiteSpace(show.Id) ? null : show.Id,
        Title = show.Title,
        Network = show.Network,
        Studio = show.Studio,
        Season = show.Season,
        DateWatched = show.DateWatched,
        Rating = show.Rating,
        Genres = show.Genres.ToList(),
        Creator = show.Creator,
        CreatedAt = show.CreatedAt
    };

    private static Show ToModel(MongoShowDocument document) => new()
    {
        Id = document.Id ?? "",
        Title = document.Title,
        Network = document.Network,
        Studio = document.Studio,
        Season = document.Season,
        DateWatched = document.DateWatched,
        Rating = document.Rating,
        Genres = document.Genres.ToList(),
        Creator = document.Creator,
        CreatedAt = document.CreatedAt
    };
}
