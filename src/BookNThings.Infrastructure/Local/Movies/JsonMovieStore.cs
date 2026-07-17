using System.Text.Json;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonMovieStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalMoviesOptions _options;
    private readonly ILocalJsonStorageSettings _storageSettings;
    private readonly ILogger<JsonMovieStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonMovieStore(
        IOptions<LocalMoviesOptions> options,
        ILocalJsonStorageSettings storageSettings,
        ILogger<JsonMovieStore> logger)
    {
        _options = options.Value;
        _storageSettings = storageSettings;
        _logger = logger;
    }

    public bool FileExists => File.Exists(GetFilePath());

    public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadMoviesUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var movies = await GetAllAsync(cancellationToken);
        return movies.FirstOrDefault(movie => movie.Id == id);
    }

    public async Task UpsertAsync(Movie movie, CancellationToken cancellationToken)
    {
        EnsureMovieId(movie);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var movies = (await ReadMoviesUnsafeAsync(cancellationToken)).ToList();
            var existingIndex = movies.FindIndex(item => item.Id == movie.Id);
            if (existingIndex >= 0)
            {
                movies[existingIndex] = Clone(movie);
            }
            else
            {
                movies.Add(Clone(movie));
            }

            await WriteMoviesUnsafeAsync(movies, cancellationToken);
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
            var movies = (await ReadMoviesUnsafeAsync(cancellationToken)).ToList();
            var movie = movies.FirstOrDefault(item => item.Id == id);
            if (movie is null)
            {
                throw new InvalidOperationException("Movie record was not found.");
            }

            movie.DateWatched = dateWatched.Date;
            await WriteMoviesUnsafeAsync(movies, cancellationToken);
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
            var movies = (await ReadMoviesUnsafeAsync(cancellationToken)).ToList();
            var removed = movies.RemoveAll(movie => movie.Id == id);
            if (removed == 0)
            {
                throw new InvalidOperationException("Movie record was not found.");
            }

            await WriteMoviesUnsafeAsync(movies, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteMoviesUnsafeAsync(movies, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<Movie>> ReadMoviesUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var movies = await JsonSerializer.DeserializeAsync<List<Movie>>(stream, SerializerOptions, cancellationToken);
        return Normalize(movies ?? []);
    }

    private async Task WriteMoviesUnsafeAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalized = Normalize(movies.Select(Clone)).ToList();
        var tempFile = $"{filePath}.tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, SerializerOptions, cancellationToken);
        }

        File.Copy(tempFile, filePath, true);
        File.Delete(tempFile);
        _logger.LogInformation("Updated local movies mirror at {MoviesFilePath}.", filePath);
    }

    private string GetFilePath()
    {
        var dataDirectory = string.IsNullOrWhiteSpace(_storageSettings.DataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : _storageSettings.DataDirectory;

        var fileName = string.IsNullOrWhiteSpace(_options.FileName) ? "movies.json" : _options.FileName;
        return Path.Combine(dataDirectory, fileName);
    }

    private static IReadOnlyList<Movie> Normalize(IEnumerable<Movie> movies) =>
        movies
            .Select(Clone)
            .Where(movie => !string.IsNullOrWhiteSpace(movie.Id))
            .GroupBy(movie => movie.Id)
            .Select(group => group.First())
            .OrderByDescending(movie => movie.DateWatched)
            .ThenBy(movie => movie.Title)
            .ToList();

    private static void EnsureMovieId(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id))
        {
            movie.Id = Guid.NewGuid().ToString("N");
        }
    }

    private static Movie Clone(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Studio = movie.Studio,
        ReleasedDate = movie.ReleasedDate,
        DateWatched = movie.DateWatched,
        Rating = movie.Rating,
        Genres = movie.Genres.ToList(),
        Director = movie.Director,
        CreatedAt = movie.CreatedAt
    };
}
