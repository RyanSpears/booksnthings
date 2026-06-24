using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookNThings.Tests;

public class SynchronizingShowRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_Should_Fallback_To_Local_Json_When_Mongo_Times_Out()
    {
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Show
                {
                    Id = "show-1",
                    Title = "Severance",
                    Network = "Apple TV+",
                    Studio = "Endeavor Content",
                    Season = 2,
                    DateWatched = new DateTime(2026, 6, 18),
                    Rating = 94,
                    Genres = ["Drama", "Mystery"],
                    Creator = "Dan Erickson"
                },
                CancellationToken.None);

            var mongoRepository = new Mock<IMongoShowRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("MongoDB server selection timed out."));

            var repository = new SynchronizingShowRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingShowRepository>.Instance);

            var shows = await repository.GetAllAsync(CancellationToken.None);

            shows.Should().ContainSingle();
            shows[0].Id.Should().Be("show-1");
            shows[0].Title.Should().Be("Severance");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    private static JsonShowStore CreateStore(out string dataDirectory)
    {
        dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "SynchronizingShowRepositoryTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalShowsOptions
        {
            DataDirectory = dataDirectory,
            FileName = "show.json"
        });

        return new JsonShowStore(options, NullLogger<JsonShowStore>.Instance);
    }
}
