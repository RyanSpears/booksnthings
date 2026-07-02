using BookNThings.Application.Contracts;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BookNThings.Infrastructure.Local;

public sealed class SynchronizingShowRepository : IShowRepository, IShowDataSynchronizer
{
    private readonly IMongoShowRepository _mongoRepository;
    private readonly JsonShowStore _jsonStore;
    private readonly ILogger<SynchronizingShowRepository> _logger;

    public SynchronizingShowRepository(
        IMongoShowRepository mongoRepository,
        JsonShowStore jsonStore,
        ILogger<SynchronizingShowRepository> logger)
    {
        _mongoRepository = mongoRepository;
        _jsonStore = jsonStore;
        _logger = logger;
    }

    public async Task SaveAsync(Show item, CancellationToken cancellationToken)
    {
        var errors = item.DateWatched.HasValue
            ? ShowValidator.ValidateForWatched(item)
            : ShowValidator.ValidateForCurrentlyWatching(item);
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
            _logger.LogWarning(ex, "MongoDB show save failed. Saving show to the local JSON mirror.");
            await _jsonStore.UpsertAsync(item, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Show>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var shows = await _mongoRepository.GetAllAsync(cancellationToken);
            await _jsonStore.ReplaceAllAsync(shows, cancellationToken);
            return shows;
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB show read failed. Loading shows from the local JSON mirror.");
            return await _jsonStore.GetAllAsync(cancellationToken);
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
            var show = await _mongoRepository.GetByIdAsync(id, cancellationToken);
            if (show is not null)
            {
                await MirrorMongoAsync(cancellationToken);
                return show;
            }
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB show read by id failed. Loading show from the local JSON mirror.");
        }

        return await _jsonStore.GetByIdAsync(id, cancellationToken);
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
            await _mongoRepository.UpdateWatchedDateAsync(id, dateWatched, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB show update failed. Updating the local JSON mirror.");
            await _jsonStore.UpdateWatchedDateAsync(id, dateWatched, cancellationToken);
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
            await _mongoRepository.DeleteAsync(id, cancellationToken);
            await MirrorMongoAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB show delete failed. Deleting from the local JSON mirror.");
            await _jsonStore.DeleteAsync(id, cancellationToken);
        }
    }

    public async Task AlignAsync(CancellationToken cancellationToken)
    {
        try
        {
            var mongoShows = await _mongoRepository.GetAllAsync(cancellationToken);
            if (_jsonStore.FileExists)
            {
                var localShows = await _jsonStore.GetAllAsync(cancellationToken);
                var reconciledShows = ReconcileById(mongoShows, localShows);
                await _mongoRepository.ReplaceAllAsync(reconciledShows, cancellationToken);
                await _jsonStore.ReplaceAllAsync(reconciledShows, cancellationToken);
                return;
            }

            await _jsonStore.ReplaceAllAsync(mongoShows, cancellationToken);
        }
        catch (Exception ex) when (ShouldFallbackToLocal(ex))
        {
            _logger.LogWarning(ex, "MongoDB show startup alignment failed. The local JSON mirror will be used until MongoDB is available.");
        }
    }

    private async Task MirrorMongoAsync(CancellationToken cancellationToken)
    {
        var shows = await _mongoRepository.GetAllAsync(cancellationToken);
        await _jsonStore.ReplaceAllAsync(shows, cancellationToken);
    }

    private static IReadOnlyList<Show> ReconcileById(
        IReadOnlyList<Show> mongoShows,
        IReadOnlyList<Show> localShows)
    {
        var mongoIds = mongoShows
            .Where(show => !string.IsNullOrWhiteSpace(show.Id))
            .Select(show => show.Id)
            .ToHashSet(StringComparer.Ordinal);

        return mongoShows
            .Concat(localShows.Where(show => !string.IsNullOrWhiteSpace(show.Id) && !mongoIds.Contains(show.Id)))
            .ToList();
    }

    private static bool ShouldFallbackToLocal(Exception exception) =>
        exception is InvalidOperationException or TimeoutException or MongoException;
}
