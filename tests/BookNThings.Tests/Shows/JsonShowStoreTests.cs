using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class JsonShowStoreTests
{
    [Fact]
    public async Task JsonShowStore_Should_Upsert_Update_And_Delete_Shows()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            var show = new Show
            {
                Title = "Severance",
                Network = "Apple TV+",
                Studio = "Endeavor Content",
                Season = 2,
                DateWatched = new DateTime(2026, 6, 18),
                Rating = 94,
                Genres = ["Drama", "Mystery"],
                Creator = "Dan Erickson"
            };

            await store.UpsertAsync(show, CancellationToken.None);
            await store.UpdateWatchedDateAsync(show.Id, new DateTime(2026, 6, 19), CancellationToken.None);

            var updated = await store.GetByIdAsync(show.Id, CancellationToken.None);
            updated.Should().NotBeNull();
            updated!.DateWatched.Should().Be(new DateTime(2026, 6, 19));

            await store.DeleteAsync(show.Id, CancellationToken.None);

            var shows = await store.GetAllAsync(CancellationToken.None);
            shows.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonShowStore_Should_Save_Currently_Watching_Show_Without_Watched_Date()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            var show = new Show
            {
                Title = "Andor",
                Network = "Disney+",
                Studio = "Lucasfilm",
                Season = 1,
                DateWatched = null,
                Rating = 92,
                Genres = ["Drama", "Sci-Fi"],
                Creator = "Tony Gilroy"
            };

            await store.UpsertAsync(show, CancellationToken.None);

            var saved = await store.GetByIdAsync(show.Id, CancellationToken.None);
            saved.Should().NotBeNull();
            saved!.DateWatched.Should().BeNull();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonShowStore_Should_Replace_All_Shows_With_The_Current_State()
    {
        var store = CreateStore(out var dataDirectory);
        try
        {
            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "show-1",
                        Title = "Severance",
                        Network = "Apple TV+",
                        Studio = "Endeavor Content",
                        Season = 1,
                        DateWatched = new DateTime(2026, 6, 16),
                        Genres = ["Drama", "Mystery"],
                        Creator = "Dan Erickson"
                    },
                    new()
                    {
                        Id = "show-1",
                        Title = "Severance",
                        Network = "Apple TV+",
                        Studio = "Endeavor Content",
                        Season = 2,
                        DateWatched = new DateTime(2026, 6, 17),
                        Genres = ["Drama", "Mystery"],
                        Creator = "Dan Erickson"
                    }
                ],
                CancellationToken.None);

            var shows = await store.GetAllAsync(CancellationToken.None);

            shows.Should().ContainSingle();
            shows[0].Id.Should().Be("show-1");
            shows[0].Season.Should().Be(1);
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
            "JsonShowStoreTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalShowsOptions
        {
            DataDirectory = dataDirectory,
            FileName = "show.json"
        });

        return new JsonShowStore(options, NullLogger<JsonShowStore>.Instance);
    }
}
