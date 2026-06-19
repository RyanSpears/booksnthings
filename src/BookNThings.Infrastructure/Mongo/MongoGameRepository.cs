using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Mongo;

public sealed class MongoGameRepository(IOptions<MongoDbOptions> options, ILogger<MongoGameRepository> logger) : IMongoGameRepository
{
    private readonly MongoDbOptions _options = options.Value;
    private readonly ILogger<MongoGameRepository> _logger = logger;
    private IMongoCollection<MongoGameDocument>? _collection;
    private bool _indexesCreated;

    public async Task SaveAsync(Game item, CancellationToken cancellationToken)
    {
        var errors = item.DatePlayed.HasValue
            ? GameValidator.ValidateForPlayed(item)
            : GameValidator.ValidateForCurrentlyPlaying(item);
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
                game => game.Id == item.Id,
                ToDocument(item),
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game save failed.");
            throw new InvalidOperationException("Could not save the game. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game save failed.");
            throw new InvalidOperationException("Could not save the game. Please try again.", ex);
        }
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var documents = await collection
                .Find(FilterDefinition<MongoGameDocument>.Empty)
                .SortByDescending(game => game.DatePlayed)
                .ThenBy(game => game.Title)
                .ToListAsync(cancellationToken);

            return documents.Select(ToModel).ToList();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game read failed.");
            throw new InvalidOperationException("Could not load saved games. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game read failed.");
            throw new InvalidOperationException("Could not load saved games. Please try again.", ex);
        }
    }

    public async Task<Game?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var document = await collection
                .Find(game => game.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return document is null ? null : ToModel(document);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game read by id failed.");
            throw new InvalidOperationException("Could not load the game record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game read by id failed.");
            throw new InvalidOperationException("Could not load the game record. Please try again.", ex);
        }
    }

    public async Task UpdatePlayedDateAsync(string id, DateTime datePlayed, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        if (datePlayed == default)
        {
            throw new ArgumentException("Played date is required.", nameof(datePlayed));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var update = Builders<MongoGameDocument>.Update.Set(game => game.DatePlayed, datePlayed.Date);
            var result = await collection.UpdateOneAsync(game => game.Id == id, update, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Game record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game update date failed.");
            throw new InvalidOperationException("Could not update the played date. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game update date failed.");
            throw new InvalidOperationException("Could not update the played date. Please try again.", ex);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var result = await collection.DeleteOneAsync(game => game.Id == id, cancellationToken);

            if (result.DeletedCount == 0)
            {
                throw new InvalidOperationException("Game record was not found.");
            }
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game delete failed.");
            throw new InvalidOperationException("Could not delete the game record. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game delete failed.");
            throw new InvalidOperationException("Could not delete the game record. Please try again.", ex);
        }
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Game> games, CancellationToken cancellationToken)
    {
        var errors = games
            .SelectMany(GameValidator.Validate)
            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogWarning("MongoDB game replace validation failed: {ValidationErrors}", string.Join(" ", errors));
            throw new ArgumentException(string.Join(" ", errors), nameof(games));
        }

        try
        {
            var collection = await GetCollectionAsync(cancellationToken);
            var normalized = games
                .Where(game => !string.IsNullOrWhiteSpace(game.Id))
                .GroupBy(game => game.Id)
                .Select(group => group.First())
                .ToList();

            var ids = normalized.Select(game => game.Id).ToList();
            if (ids.Count == 0)
            {
                await collection.DeleteManyAsync(FilterDefinition<MongoGameDocument>.Empty, cancellationToken);
                return;
            }

            var writes = normalized
                .Select(game => new ReplaceOneModel<MongoGameDocument>(
                    Builders<MongoGameDocument>.Filter.Eq(document => document.Id, game.Id),
                    ToDocument(game))
                {
                    IsUpsert = true
                })
                .Cast<WriteModel<MongoGameDocument>>()
                .ToList();

            await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
            await collection.DeleteManyAsync(
                Builders<MongoGameDocument>.Filter.Nin(document => document.Id, ids),
                cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB game replace failed.");
            throw new InvalidOperationException("Could not align saved games with MongoDB. Please try again.", ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "MongoDB game replace failed.");
            throw new InvalidOperationException("Could not align saved games with MongoDB. Please try again.", ex);
        }
    }

    private async Task CreateIndexes(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        var titlePublisher = new CreateIndexModel<MongoGameDocument>(
            Builders<MongoGameDocument>.IndexKeys
                .Ascending(game => game.Title)
                .Ascending(game => game.Publisher));

        var datePlayed = new CreateIndexModel<MongoGameDocument>(
            Builders<MongoGameDocument>.IndexKeys.Descending(game => game.DatePlayed));

        await collection.Indexes.CreateManyAsync([titlePublisher, datePlayed], cancellationToken);
    }

    private async Task<IMongoCollection<MongoGameDocument>> GetCollectionAsync(CancellationToken cancellationToken)
    {
        var collection = GetCollection();
        if (!_indexesCreated)
        {
            await CreateIndexes(cancellationToken);
            _indexesCreated = true;
        }

        return collection;
    }

    private IMongoCollection<MongoGameDocument> GetCollection()
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

        if (string.IsNullOrWhiteSpace(_options.GamesCollection))
        {
            throw new InvalidOperationException("MongoDB games collection is not configured.");
        }

        var client = new MongoClient(_options.ConnectionString);
        _collection = client
            .GetDatabase(_options.DatabaseName)
            .GetCollection<MongoGameDocument>(_options.GamesCollection);

        return _collection;
    }

    private static MongoGameDocument ToDocument(Game game) => new()
    {
        Id = string.IsNullOrWhiteSpace(game.Id) ? null : game.Id,
        Title = game.Title,
        Publisher = game.Publisher,
        Studio = game.Studio,
        ReleasedDate = game.ReleasedDate,
        DatePlayed = game.DatePlayed,
        Rating = game.Rating,
        Genres = game.Genres.ToList(),
        Developer = game.Developer,
        CreatedAt = game.CreatedAt
    };

    private static Game ToModel(MongoGameDocument document) => new()
    {
        Id = document.Id ?? "",
        Title = document.Title,
        Publisher = document.Publisher,
        Studio = document.Studio,
        ReleasedDate = document.ReleasedDate,
        DatePlayed = document.DatePlayed,
        Rating = document.Rating,
        Genres = document.Genres.ToList(),
        Developer = document.Developer,
        CreatedAt = document.CreatedAt
    };
}
