using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookNThings.Tests;

public class SynchronizingBookRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_Should_Fallback_To_Local_Json_When_Mongo_Times_Out()
    {
        // Arrange
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Book
                {
                    Id = "book-1",
                    Title = "Kindred",
                    Author = "Octavia E. Butler",
                    Description = "A time travel novel.",
                    Pages = 264,
                    DatePublished = new DateTime(1979, 6, 1),
                    DateRead = new DateTime(2026, 5, 19),
                    Genres = ["Science Fiction"]
                },
                CancellationToken.None);

            var mongoRepository = new Mock<IMongoBookRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("MongoDB server selection timed out."));

            var repository = new SynchronizingBookRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingBookRepository>.Instance);

            // Act
            var books = await repository.GetAllAsync(CancellationToken.None);

            // Assert
            books.Should().ContainSingle();
            books[0].Id.Should().Be("book-1");
            books[0].Title.Should().Be("Kindred");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task AlignAsync_Should_Reconcile_Missing_Books_Between_Mongo_And_Local_Json()
    {
        // Arrange
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Book
                {
                    Id = "local-book",
                    Title = "Local Book",
                    Author = "Local Author",
                    Description = "Stored locally while MongoDB was unavailable.",
                    Pages = 240,
                    DatePublished = new DateTime(2026, 1, 1),
                    DateRead = new DateTime(2026, 5, 19),
                    Genres = ["Fiction"]
                },
                CancellationToken.None);

            IReadOnlyList<Book>? replacedBooks = null;
            var mongoRepository = new Mock<IMongoBookRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new Book
                    {
                        Id = "mongo-book",
                        Title = "Mongo Book",
                        Author = "Mongo Author",
                        Description = "Stored in MongoDB from another device.",
                        Pages = 320,
                        DatePublished = new DateTime(2026, 1, 2),
                        DateRead = new DateTime(2026, 5, 20),
                        Genres = ["Nonfiction"]
                    }
                ]);
            mongoRepository.Setup(repository => repository.ReplaceAllAsync(It.IsAny<IReadOnlyList<Book>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<Book>, CancellationToken>((books, _) => replacedBooks = books)
                .Returns(Task.CompletedTask);

            var repository = new SynchronizingBookRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingBookRepository>.Instance);

            // Act
            await repository.AlignAsync(CancellationToken.None);

            // Assert
            replacedBooks.Should().NotBeNull();
            replacedBooks!.Select(book => book.Id).Should().BeEquivalentTo("local-book", "mongo-book");

            var localBooks = await jsonStore.GetAllAsync(CancellationToken.None);
            localBooks.Select(book => book.Id).Should().BeEquivalentTo("local-book", "mongo-book");
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
            "SynchronizingBookRepositoryTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalBooksOptions
        {
            DataDirectory = dataDirectory,
            FileName = "books.json"
        });

        return new JsonBookStore(options, NullLogger<JsonBookStore>.Instance);
    }
}
