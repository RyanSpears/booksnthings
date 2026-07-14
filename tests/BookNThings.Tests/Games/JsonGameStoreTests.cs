using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class JsonGameStoreTests
{
    [Fact]
    public async Task JsonGameStore_Should_Upsert_Update_And_Delete_Games()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            var game = new Game
            {
                Title = "Baldur's Gate 3",
                Publisher = "Larian Studios",
                Studio = "Larian Studios",
                ReleasedDate = new DateTime(2023, 8, 3),
                DatePlayed = new DateTime(2026, 6, 18),
                Rating = 96,
                Genres = ["RPG", "Fantasy"],
                Developer = "Larian Studios"
            };

            await store.UpsertAsync(game, CancellationToken.None);
            await store.UpdatePlayedDateAsync(game.Id, new DateTime(2026, 6, 19), CancellationToken.None);

            var updated = await store.GetByIdAsync(game.Id, CancellationToken.None);
            updated.Should().NotBeNull();
            updated!.DatePlayed.Should().Be(new DateTime(2026, 6, 19));

            await store.DeleteAsync(game.Id, CancellationToken.None);

            var games = await store.GetAllAsync(CancellationToken.None);
            games.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonGameStore_Should_Save_Currently_Playing_Game_Without_Played_Date()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            var game = new Game
            {
                Title = "Hades",
                Publisher = "Supergiant Games",
                Studio = "Supergiant Games",
                ReleasedDate = new DateTime(2020, 9, 17),
                DatePlayed = null,
                Rating = 93,
                Genres = ["Action", "Roguelike"],
                Developer = "Supergiant Games"
            };

            await store.UpsertAsync(game, CancellationToken.None);

            var saved = await store.GetByIdAsync(game.Id, CancellationToken.None);
            saved.Should().NotBeNull();
            saved!.DatePlayed.Should().BeNull();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonGameStore_Should_Replace_All_Games_With_The_Current_State()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "game-1",
                        Title = "Hades",
                        Publisher = "Supergiant Games",
                        Studio = "Supergiant Games",
                        ReleasedDate = new DateTime(2020, 9, 17),
                        DatePlayed = new DateTime(2026, 6, 16),
                        Genres = ["Action", "Roguelike"],
                        Developer = "Supergiant Games"
                    }
                ],
                CancellationToken.None);

            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "game-2",
                        Title = "Cyberpunk 2077",
                        Publisher = "CD Projekt",
                        Studio = "CD Projekt Red",
                        ReleasedDate = new DateTime(2020, 12, 10),
                        DatePlayed = new DateTime(2026, 6, 17),
                        Genres = ["RPG", "Open World"],
                        Developer = "CD Projekt Red"
                    }
                ],
                CancellationToken.None);

            var games = await store.GetAllAsync(CancellationToken.None);

            games.Should().ContainSingle();
            games[0].Id.Should().Be("game-2");
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
            "JsonGameStoreTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalGamesOptions
        {
            FileName = "games.json"
        });

        return new JsonGameStore(options, new TestLocalJsonStorageSettings(dataDirectory), NullLogger<JsonGameStore>.Instance);
    }
}
