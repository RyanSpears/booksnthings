using BookNThings.Domain.Models;
using BookNThings.Infrastructure.Configuration;
using BookNThings.Infrastructure.Local;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookNThings.Tests;

public class JsonMovieStoreTests
{
    [Fact]
    public async Task JsonMovieStore_Should_Upsert_Update_And_Delete_Movies()
    {
        // Arrange
        var store = CreateStore(out var dataDirectory);
        try
        {
            var movie = new Movie
            {
                Title = "Dune: Part Two",
                Studio = "Legendary Pictures",
                ReleasedDate = new DateTime(2024, 3, 1),
                DateWatched = new DateTime(2026, 7, 17),
                Rating = 91,
                Genres = ["Science Fiction", "Adventure"],
                Director = "Denis Villeneuve"
            };

            // Act
            await store.UpsertAsync(movie, CancellationToken.None);
            await store.UpdateWatchedDateAsync(movie.Id, new DateTime(2026, 7, 18), CancellationToken.None);

            var updated = await store.GetByIdAsync(movie.Id, CancellationToken.None);

            // Assert
            updated.Should().NotBeNull();
            updated!.DateWatched.Should().Be(new DateTime(2026, 7, 18));

            // Act
            await store.DeleteAsync(movie.Id, CancellationToken.None);

            var movies = await store.GetAllAsync(CancellationToken.None);

            // Assert
            movies.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonMovieStore_Should_Save_Currently_Watching_Movie_Without_Watched_Date()
    {
        // Arrange
        var store = CreateStore(out var dataDirectory);
        try
        {
            var movie = new Movie
            {
                Title = "Oppenheimer",
                Studio = "Universal Pictures",
                ReleasedDate = new DateTime(2023, 7, 21),
                DateWatched = null,
                Rating = 93,
                Genres = ["Drama", "History"],
                Director = "Christopher Nolan"
            };

            // Act
            await store.UpsertAsync(movie, CancellationToken.None);

            var saved = await store.GetByIdAsync(movie.Id, CancellationToken.None);

            // Assert
            saved.Should().NotBeNull();
            saved!.DateWatched.Should().BeNull();
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    [Fact]
    public async Task JsonMovieStore_Should_Replace_All_Movies_With_The_Current_State()
    {
        // Arrange
        var store = CreateStore(out var dataDirectory);
        try
        {
            // Act
            await store.ReplaceAllAsync(
                [
                    new()
                    {
                        Id = "movie-1",
                        Title = "Dune: Part Two",
                        Studio = "Legendary Pictures",
                        ReleasedDate = new DateTime(2024, 3, 1),
                        DateWatched = new DateTime(2026, 7, 17),
                        Genres = ["Science Fiction", "Adventure"],
                        Director = "Denis Villeneuve"
                    },
                    new()
                    {
                        Id = "movie-2",
                        Title = "Oppenheimer",
                        Studio = "Universal Pictures",
                        ReleasedDate = new DateTime(2023, 7, 21),
                        DateWatched = new DateTime(2026, 7, 18),
                        Genres = ["Drama", "History"],
                        Director = "Christopher Nolan"
                    }
                ],
                CancellationToken.None);

            var movies = await store.GetAllAsync(CancellationToken.None);

            // Assert
            movies.Should().ContainSingle(m => m.Id == "movie-2");
        }
        finally
        {
            Directory.Delete(dataDirectory, true);
        }
    }

    private static JsonMovieStore CreateStore(out string dataDirectory)
    {
        dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "JsonMovieStoreTests",
            Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LocalMoviesOptions
        {
            FileName = "movies.json"
        });

        return new JsonMovieStore(options, new TestLocalJsonStorageSettings(dataDirectory), NullLogger<JsonMovieStore>.Instance);
    }
}
