using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Local;

public sealed class SynchronizingGameRepository : IGameRepository, IGameDataSynchronizer
{
    private readonly IMongoGameRepository _mongoRepository;
    private readonly JsonGameStore _jsonStore;
    private readonly ILogger<SynchronizingGameRepository> _logger;

    public SynchronizingGameRepository(
        IMongoGameRepository mongoRepository,
        JsonGameStore jsonStore,
        ILogger<SynchronizingGameRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _jsonStore = jsonStore;
        _logger = logger;
    }

    public async Task SaveAsync(Game item, CancellationToken cancellationToken)
    {
        var errors = item.DatePlayed.HasValue
            ? GameValidator.ValidateForPlayed(item)
            : GameValidator.ValidateForCurrentlyPlaying(item);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(item));
        }

        try
        {
            await _mongoRepository.SaveAsync(item, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game save failed. Saving game to the local JSON mirror.");
            await _jsonStore.UpsertAsync(item, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var games = await _mongoRepository.GetAllAsync(cancellationToken);
            await _jsonStore.ReplaceAllAsync(games, cancellationToken);
            return games;
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game read failed. Loading games from the local JSON mirror.");
            return await _jsonStore.GetAllAsync(cancellationToken);
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
            var game = await _mongoRepository.GetByIdAsync(id, cancellationToken);
            if (game is not null)
            {
                await MirrorMongoAsync(cancellationToken);
                return game;
            }
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game read by id failed. Loading game from the local JSON mirror.");
        }

        return await _jsonStore.GetByIdAsync(id, cancellationToken);
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
            await _mongoRepository.UpdatePlayedDateAsync(id, datePlayed, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game update failed. Updating the local JSON mirror.");
            await _jsonStore.UpdatePlayedDateAsync(id, datePlayed, cancellationToken);
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
            await _mongoRepository.DeleteAsync(id, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game delete failed. Deleting from the local JSON mirror.");
            await _jsonStore.DeleteAsync(id, cancellationToken);
        }
    }

    public async Task AlignAsync(CancellationToken cancellationToken)
    {
        try
        {
            var mongoGames = await _mongoRepository.GetAllAsync(cancellationToken);
            if (_jsonStore.FileExists)
            {
                var localGames = await _jsonStore.GetAllAsync(cancellationToken);
                var reconciledGames = ReconcileById(mongoGames, localGames);
                await _mongoRepository.ReplaceAllAsync(reconciledGames, cancellationToken);
                await _jsonStore.ReplaceAllAsync(reconciledGames, cancellationToken);
                return;
            }

            await _jsonStore.ReplaceAllAsync(mongoGames, cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB game startup alignment failed. The local JSON mirror will be used until MongoDB is available.");
        }
    }

    private async Task MirrorMongoAsync(CancellationToken cancellationToken)
    {
        var games = await _mongoRepository.GetAllAsync(cancellationToken);
        await _jsonStore.ReplaceAllAsync(games, cancellationToken);
    }

    private static IReadOnlyList<Game> ReconcileById(
        IReadOnlyList<Game> mongoGames,
        IReadOnlyList<Game> localGames)
    {
        var mongoIds = mongoGames
            .Where(game => !string.IsNullOrWhiteSpace(game.Id))
            .Select(game => game.Id)
            .ToHashSet(StringComparer.Ordinal);

        return mongoGames
            .Concat(localGames.Where(game => !string.IsNullOrWhiteSpace(game.Id) && !mongoIds.Contains(game.Id)))
            .ToList();
    }

    private static bool ShouldFallbackToLocal(Exception exception) =>
        exception is InvalidOperationException or TimeoutException or MongoException;
}
