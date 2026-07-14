using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class JsonBookStoreTests
{
    [Fact]
    public async Task JsonBookStore_Should_Upsert_Update_And_Delete_Books()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            var book = new Book
            {
                Title = "Kindred",
                Author = "Octavia E. Butler",
                Description = "A time travel novel.",
                Pages = 264,
                DatePublished = new DateTime(1979, 6, 1),
                DateRead = new DateTime(2026, 5, 19),
                Genres = ["Science Fiction"]
            };

            await store.UpsertAsync(book, CancellationToken.None);
            await store.UpdateReadDateAsync(book.Id, new DateTime(2026, 5, 20), CancellationToken.None);

            var updated = await store.GetByIdAsync(book.Id, CancellationToken.None);
            updated.Should().NotBeNull();
            updated!.DateRead.Should().Be(new DateTime(2026, 5, 20));

            await store.DeleteAsync(book.Id, CancellationToken.None);

            var books = await store.GetAllAsync(CancellationToken.None);
            books.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonBookStore_Should_Replace_All_Books_With_The_Current_State()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "book-1",
                        Title = "Upgrade",
                        Author = "Blake Crouch",
                        Description = "A thriller.",
                        Pages = 320,
                        DatePublished = new DateTime(2022, 9, 19),
                        DateRead = new DateTime(2026, 5, 14),
                        Genres = ["Science Fiction", "Thriller"]
                    }
                ],
                CancellationToken.None);

            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "book-2",
                        Title = "All the Pretty Horses",
                        Author = "Cormac McCarthy",
                        Description = "A border trilogy novel.",
                        Pages = 309,
                        DatePublished = new DateTime(1992, 5, 25),
                        DateRead = new DateTime(2026, 5, 8),
                        Genres = ["Fiction", "Western"]
                    }
                ],
                CancellationToken.None);

            var books = await store.GetAllAsync(CancellationToken.None);

            books.Should().ContainSingle();
            books[0].Id.Should().Be("book-2");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    private static JsonBookStore CreateStore(out string dataDirectory)
    {
        dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "JsonBookStoreTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalBooksOptions
        {
            FileName = "books.json"
        });

        return new JsonBookStore(options, new TestLocalJsonStorageSettings(dataDirectory), NullLogger<JsonBookStore>.Instance);
    }
}
