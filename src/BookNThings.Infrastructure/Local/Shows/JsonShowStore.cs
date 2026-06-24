using System.Text.Json;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonShowStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalShowsOptions _options;
    private readonly ILogger<JsonShowStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonShowStore(IOptions<LocalShowsOptions> options, ILogger<JsonShowStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool FileExists => File.Exists(GetFilePath());

    public async Task<IReadOnlyList<Show>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadShowsUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Show?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var shows = await GetAllAsync(cancellationToken);
        return shows.FirstOrDefault(show => show.Id == id);
    }

    public async Task UpsertAsync(Show show, CancellationToken cancellationToken)
    {
        EnsureShowId(show);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var shows = (await ReadShowsUnsafeAsync(cancellationToken)).ToList();
            var existingIndex = shows.FindIndex(item => item.Id == show.Id);
            if (existingIndex >= 0)
            {
                shows[existingIndex] = Clone(show);
            }
            else
            {
                shows.Add(Clone(show));
            }

            await WriteShowsUnsafeAsync(shows, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateWatchedDateAsync(string id, DateTime dateWatched, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var shows = (await ReadShowsUnsafeAsync(cancellationToken)).ToList();
            var show = shows.FirstOrDefault(item => item.Id == id);
            if (show is null)
            {
                throw new InvalidOperationException("Show record was not found.");
            }

            show.DateWatched = dateWatched.Date;
            await WriteShowsUnsafeAsync(shows, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var shows = (await ReadShowsUnsafeAsync(cancellationToken)).ToList();
            var removed = shows.RemoveAll(show => show.Id == id);
            if (removed == 0)
            {
                throw new InvalidOperationException("Show record was not found.");
            }

            await WriteShowsUnsafeAsync(shows, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<Show> shows, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteShowsUnsafeAsync(shows, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<Show>> ReadShowsUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var shows = await JsonSerializer.DeserializeAsync<List<Show>>(stream, SerializerOptions, cancellationToken);
        return Normalize(shows ?? []);
    }

    private async Task WriteShowsUnsafeAsync(IEnumerable<Show> shows, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalized = Normalize(shows.Select(Clone)).ToList();
        var tempFile = $"{filePath}.tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, SerializerOptions, cancellationToken);
        }

        File.Copy(tempFile, filePath, true);
        File.Delete(tempFile);
        _logger.LogInformation("Updated local show mirror at {ShowsFilePath}.", filePath);
    }

    private string GetFilePath()
    {
        var dataDirectory = string.IsNullOrWhiteSpace(_options.DataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : _options.DataDirectory;

        var fileName = string.IsNullOrWhiteSpace(_options.FileName) ? "show.json" : _options.FileName;
        return Path.Combine(dataDirectory, fileName);
    }

    private static IReadOnlyList<Show> Normalize(IEnumerable<Show> shows) =>
        shows
            .Select(Clone)
            .Where(show => !string.IsNullOrWhiteSpace(show.Id))
            .GroupBy(show => show.Id)
            .Select(group => group.First())
            .OrderByDescending(show => show.DateWatched)
            .ThenBy(show => show.Title)
            .ThenBy(show => show.Season)
            .ToList();

    private static void EnsureShowId(Show show)
    {
        if (string.IsNullOrWhiteSpace(show.Id))
        {
            show.Id = ObjectId.GenerateNewId().ToString();
        }
    }

    private static Show Clone(Show show) => new()
    {
        Id = show.Id,
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
}
