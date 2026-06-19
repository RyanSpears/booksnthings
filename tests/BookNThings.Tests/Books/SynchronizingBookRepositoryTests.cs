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

            var books = await repository.GetAllAsync(CancellationToken.None);

            books.Should().ContainSingle();
            books[0].Id.Should().Be("book-1");
            books[0].Title.Should().Be("Kindred");
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
