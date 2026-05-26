using System.Text.Json;
using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace BookNThings.Infrastructure.Local;

public sealed class JsonBookStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalBooksOptions _options;
    private readonly ILogger<JsonBookStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonBookStore(IOptions<LocalBooksOptions> options, ILogger<JsonBookStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool FileExists => File.Exists(GetFilePath());

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadBooksUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var books = await GetAllAsync(cancellationToken);
        return books.FirstOrDefault(book => book.Id == id);
    }

    public async Task UpsertAsync(Book book, CancellationToken cancellationToken)
    {
        EnsureBookId(book);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var books = (await ReadBooksUnsafeAsync(cancellationToken)).ToList();
            var existingIndex = books.FindIndex(item => item.Id == book.Id);
            if (existingIndex >= 0)
            {
                books[existingIndex] = Clone(book);
            }
            else
            {
                books.Add(Clone(book));
            }

            await WriteBooksUnsafeAsync(books, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateReadDateAsync(string id, DateTime dateRead, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var books = (await ReadBooksUnsafeAsync(cancellationToken)).ToList();
            var book = books.FirstOrDefault(item => item.Id == id);
            if (book is null)
            {
                throw new InvalidOperationException("Book read record was not found.");
            }

            book.DateRead = dateRead.Date;
            await WriteBooksUnsafeAsync(books, cancellationToken);
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
            var books = (await ReadBooksUnsafeAsync(cancellationToken)).ToList();
            var removed = books.RemoveAll(book => book.Id == id);
            if (removed == 0)
            {
                throw new InvalidOperationException("Book read record was not found.");
            }

            await WriteBooksUnsafeAsync(books, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<Book> books, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteBooksUnsafeAsync(books, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<Book>> ReadBooksUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var books = await JsonSerializer.DeserializeAsync<List<Book>>(stream, SerializerOptions, cancellationToken);
        return Normalize(books ?? []);
    }

    private async Task WriteBooksUnsafeAsync(IEnumerable<Book> books, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalized = Normalize(books.Select(Clone)).ToList();
        var tempFile = $"{filePath}.tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, SerializerOptions, cancellationToken);
        }

        File.Move(tempFile, filePath, true);
        _logger.LogInformation("Updated local books mirror at {BooksFilePath}.", filePath);
    }

    private string GetFilePath()
    {
        var dataDirectory = string.IsNullOrWhiteSpace(_options.DataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : _options.DataDirectory;

        var fileName = string.IsNullOrWhiteSpace(_options.FileName) ? "books.json" : _options.FileName;
        return Path.Combine(dataDirectory, fileName);
    }

    private static IReadOnlyList<Book> Normalize(IEnumerable<Book> books) =>
        books
            .Select(Clone)
            .Where(book => !string.IsNullOrWhiteSpace(book.Id))
            .GroupBy(book => book.Id)
            .Select(group => group.First())
            .OrderByDescending(book => book.DateRead)
            .ThenBy(book => book.Title)
            .ToList();

    private static void EnsureBookId(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Id))
        {
            book.Id = ObjectId.GenerateNewId().ToString();
        }
    }

    private static Book Clone(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Description = book.Description,
        Pages = book.Pages,
        DatePublished = book.DatePublished,
        DateRead = book.DateRead,
        Genres = book.Genres.ToList(),
        Author = book.Author
    };
}
