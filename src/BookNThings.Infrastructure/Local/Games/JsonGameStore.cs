using System.Text.Json;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonGameStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalGamesOptions _options;
    private readonly ILocalJsonStorageSettings _storageSettings;
    private readonly ILogger<JsonGameStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonGameStore(
        IOptions<LocalGamesOptions> options,
        ILocalJsonStorageSettings storageSettings,
        ILogger<JsonGameStore> logger)
    {
        _options = options.Value;
        _storageSettings = storageSettings;
        _logger = logger;
    }

    public bool FileExists => File.Exists(GetFilePath());

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadGamesUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Game?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var games = await GetAllAsync(cancellationToken);
        return games.FirstOrDefault(game => game.Id == id);
    }

    public async Task UpsertAsync(Game game, CancellationToken cancellationToken)
    {
        EnsureGameId(game);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var games = (await ReadGamesUnsafeAsync(cancellationToken)).ToList();
            var existingIndex = games.FindIndex(item => item.Id == game.Id);
            if (existingIndex >= 0)
            {
                games[existingIndex] = Clone(game);
            }
            else
            {
                games.Add(Clone(game));
            }

            await WriteGamesUnsafeAsync(games, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdatePlayedDateAsync(string id, DateTime datePlayed, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var games = (await ReadGamesUnsafeAsync(cancellationToken)).ToList();
            var game = games.FirstOrDefault(item => item.Id == id);
            if (game is null)
            {
                throw new InvalidOperationException("Game record was not found.");
            }

            game.DatePlayed = datePlayed.Date;
            await WriteGamesUnsafeAsync(games, cancellationToken);
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
            var games = (await ReadGamesUnsafeAsync(cancellationToken)).ToList();
            var removed = games.RemoveAll(game => game.Id == id);
            if (removed == 0)
            {
                throw new InvalidOperationException("Game record was not found.");
            }

            await WriteGamesUnsafeAsync(games, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<Game> games, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteGamesUnsafeAsync(games, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<Game>> ReadGamesUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var games = await JsonSerializer.DeserializeAsync<List<Game>>(stream, SerializerOptions, cancellationToken);
        return Normalize(games ?? []);
    }

    private async Task WriteGamesUnsafeAsync(IEnumerable<Game> games, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalized = Normalize(games.Select(Clone)).ToList();
        var tempFile = $"{filePath}.tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, SerializerOptions, cancellationToken);
        }

        File.Copy(tempFile, filePath, true);
        File.Delete(tempFile);
        _logger.LogInformation("Updated local games mirror at {GamesFilePath}.", filePath);
    }

    private string GetFilePath()
    {
        var dataDirectory = string.IsNullOrWhiteSpace(_storageSettings.DataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : _storageSettings.DataDirectory;

        var fileName = string.IsNullOrWhiteSpace(_options.FileName) ? "games.json" : _options.FileName;
        return Path.Combine(dataDirectory, fileName);
    }

    private static IReadOnlyList<Game> Normalize(IEnumerable<Game> games) =>
        games
            .Select(Clone)
            .Where(game => !string.IsNullOrWhiteSpace(game.Id))
            .GroupBy(game => game.Id)
            .Select(group => group.First())
            .OrderByDescending(game => game.DatePlayed)
            .ThenBy(game => game.Title)
            .ToList();

    private static void EnsureGameId(Game game)
    {
        if (string.IsNullOrWhiteSpace(game.Id))
        {
            game.Id = Guid.NewGuid().ToString("N");
        }
    }

    private static Game Clone(Game game) => new()
    {
        Id = game.Id,
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
}
