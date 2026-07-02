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

    [Fact]
    public async Task AlignAsync_Should_Reconcile_Missing_Shows_Between_Mongo_And_Local_Json()
    {
        var jsonStore = CreateStore(out var dataDirectory);
        try
        {
            await jsonStore.UpsertAsync(
                new Show
                {
                    Id = "local-show",
                    Title = "Local Show",
                    Network = "Local Network",
                    Studio = "Local Studio",
                    Season = 1,
                    DateWatched = new DateTime(2026, 6, 18),
                    Rating = 80,
                    Genres = ["Drama"],
                    Creator = "Local Creator"
                },
                CancellationToken.None);

            IReadOnlyList<Show>? replacedShows = null;
            var mongoRepository = new Mock<IMongoShowRepository>();
            mongoRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new Show
                    {
                        Id = "mongo-show",
                        Title = "Mongo Show",
                        Network = "Mongo Network",
                        Studio = "Mongo Studio",
                        Season = 2,
                        DateWatched = new DateTime(2026, 6, 19),
                        Rating = 90,
                        Genres = ["Mystery"],
                        Creator = "Mongo Creator"
                    }
                ]);
            mongoRepository.Setup(repository => repository.ReplaceAllAsync(It.IsAny<IReadOnlyList<Show>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<Show>, CancellationToken>((shows, _) => replacedShows = shows)
                .Returns(Task.CompletedTask);

            var repository = new SynchronizingShowRepository(
                mongoRepository.Object,
                jsonStore,
                NullLogger<SynchronizingShowRepository>.Instance);

            await repository.AlignAsync(CancellationToken.None);

            replacedShows.Should().NotBeNull();
            replacedShows!.Select(show => show.Id).Should().BeEquivalentTo("local-show", "mongo-show");

            var localShows = await jsonStore.GetAllAsync(CancellationToken.None);
            localShows.Select(show => show.Id).Should().BeEquivalentTo("local-show", "mongo-show");
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
