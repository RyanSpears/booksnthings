using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using BookNThings.Infrastructure.Mongo;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookNThings.Tests;

public class SynchronizingGameRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_Should_Fallback_To_Local_Json_When_Mongo_Times_Out()
    {
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Game
                {
                    Id = "game-1",
                    Title = "Baldur's Gate 3",
                    Publisher = "Larian Studios",
                    Studio = "Larian Studios",
                    ReleasedDate = new DateTime(2023, 8, 3),
                    DatePlayed = new DateTime(2026, 6, 18),
                    Rating = 96,
                    Genres = ["RPG", "Fantasy"],
                    Developer = "Larian Studios"
                },
                CancellationToken.None);

            var mongoRepository = new Mock<IMongoGameRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("MongoDB server selection timed out."));

            var repository = new SynchronizingGameRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingGameRepository>.Instance);

            var games = await repository.GetAllAsync(CancellationToken.None);

            games.Should().ContainSingle();
            games[0].Id.Should().Be("game-1");
            games[0].Title.Should().Be("Baldur's Gate 3");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    private static JsonGameStore CreateStore(out string dataDirectory)
    {
        dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "SynchronizingGameRepositoryTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalGamesOptions
        {
            DataDirectory = dataDirectory,
            FileName = "games.json"
        });

        return new JsonGameStore(options, NullLogger<JsonGameStore>.Instance);
    }
}
