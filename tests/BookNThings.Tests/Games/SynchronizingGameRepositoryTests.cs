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
        // Arrange
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

            // Act
            var games = await repository.GetAllAsync(CancellationToken.None);

            // Assert
            games.Should().ContainSingle();
            games[0].Id.Should().Be("game-1");
            games[0].Title.Should().Be("Baldur's Gate 3");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task AlignAsync_Should_Reconcile_Missing_Games_Between_Mongo_And_Local_Json()
    {
        // Arrange
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Game
                {
                    Id = "local-game",
                    Title = "Local Game",
                    Publisher = "Local Publisher",
                    Studio = "Local Studio",
                    ReleasedDate = new DateTime(2026, 1, 1),
                    DatePlayed = new DateTime(2026, 6, 18),
                    Rating = 80,
                    Genres = ["Puzzle"],
                    Developer = "Local Developer"
                },
                CancellationToken.None);

            IReadOnlyList<Game>? replacedGames = null;
            var mongoRepository = new Mock<IMongoGameRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new Game
                    {
                        Id = "mongo-game",
                        Title = "Mongo Game",
                        Publisher = "Mongo Publisher",
                        Studio = "Mongo Studio",
                        ReleasedDate = new DateTime(2026, 1, 2),
                        DatePlayed = new DateTime(2026, 6, 19),
                        Rating = 90,
                        Genres = ["RPG"],
                        Developer = "Mongo Developer"
                    }
                ]);
            mongoRepository.Setup(repository => repository.ReplaceAllAsync(It.IsAny<IReadOnlyList<Game>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<Game>, CancellationToken>((games, _) => replacedGames = games)
                .Returns(Task.CompletedTask);

            var repository = new SynchronizingGameRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingGameRepository>.Instance);

            // Act
            await repository.AlignAsync(CancellationToken.None);

            // Assert
            replacedGames.Should().NotBeNull();
            replacedGames!.Select(game => game.Id).Should().BeEquivalentTo("local-game", "mongo-game");

            var localGames = await jsonStore.GetAllAsync(CancellationToken.None);
            localGames.Select(game => game.Id).Should().BeEquivalentTo("local-game", "mongo-game");
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
            FileName = "games.json"
        });

        return new JsonGameStore(options, new TestLocalJsonStorageSettings(dataDirectory), NullLogger<JsonGameStore>.Instance);
    }
}
