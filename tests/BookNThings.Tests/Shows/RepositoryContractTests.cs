using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using FluentAssertions;
using Moq;

namespace BookNThings.Tests;

public class ShowRepositoryContractTests
{
    [Fact]
    public async Task Repository_Should_Save_And_Return_Shows_Through_Contract()
    {
        var saved = new List<Show>();
        var repository = new Mock<IShowRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<Show>(), It.IsAny<CancellationToken>()))
            .Callback<Show, CancellationToken>((show, _) => saved.Add(show))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var show = new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2,
            DateWatched = new DateTime(2026, 6, 24)
        };

        await repository.Object.SaveAsync(show, CancellationToken.None);
        var results = await repository.Object.GetAllAsync(CancellationToken.None);

        var result = results.Should().ContainSingle().Which;
        result.Network.Should().Be("Apple TV+");
        result.Season.Should().Be(2);
        result.DateWatched.Should().Be(new DateTime(2026, 6, 24));
    }

    [Fact]
    public async Task Repository_Should_Save_Currently_Watching_Show_Without_Watched_Date()
    {
        var saved = new List<Show>();
        var repository = new Mock<IShowRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<Show>(), It.IsAny<CancellationToken>()))
            .Callback<Show, CancellationToken>((show, _) => saved.Add(show))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var show = new Show
        {
            Title = "Silo",
            Network = "Apple TV+",
            Studio = "Mímir Films, Nemo Films, AMC Studios, Apple Studios",
            Season = 1
        };

        await repository.Object.SaveAsync(show, CancellationToken.None);
        var results = await repository.Object.GetAllAsync(CancellationToken.None);

        results.Should().ContainSingle().Which.DateWatched.Should().BeNull();
    }

    [Fact]
    public async Task Repository_Should_Update_And_Delete_Show_Through_Contract()
    {
        var saved = new List<Show>
        {
            new()
            {
                Id = "show-1",
                Title = "Severance",
                Network = "Apple TV+",
                Studio = "Endeavor Content",
                Season = 2,
                DateWatched = new DateTime(2026, 6, 24)
            }
        };

        var repository = new Mock<IShowRepository>();
        repository.Setup(r => r.GetByIdAsync("show-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved.SingleOrDefault(show => show.Id == "show-1"));
        repository.Setup(r => r.UpdateWatchedDateAsync("show-1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, CancellationToken>((id, dateWatched, _) => saved.Single(show => show.Id == id).DateWatched = dateWatched)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeleteAsync("show-1", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => saved.RemoveAll(show => show.Id == id))
            .Returns(Task.CompletedTask);

        await repository.Object.UpdateWatchedDateAsync("show-1", new DateTime(2026, 6, 25), CancellationToken.None);
        var updated = await repository.Object.GetByIdAsync("show-1", CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.DateWatched.Should().Be(new DateTime(2026, 6, 25));

        await repository.Object.DeleteAsync("show-1", CancellationToken.None);

        saved.Should().BeEmpty();
    }
}
