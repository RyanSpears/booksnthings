using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using FluentAssertions;
using Moq;

namespace BookNThings.Tests;

public class MovieRepositoryContractTests
{
    [Fact]
    public async Task Repository_Should_Save_And_Return_Movies_Through_Contract()
    {
        // Arrange
        var saved = new List<Movie>();
        var repository = new Mock<IMovieRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => saved.Add(movie))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var movie = new Movie
        {
            Title = "Dune: Part Two",
            Studio = "Legendary Pictures",
            ReleasedDate = new DateTime(2024, 3, 1),
            DateWatched = new DateTime(2026, 7, 17)
        };

        // Act
        await repository.Object.SaveAsync(movie, CancellationToken.None);
        var results = await repository.Object.GetAllAsync(CancellationToken.None);

        // Assert
        var result = results.Should().ContainSingle().Which;
        result.Studio.Should().Be("Legendary Pictures");
        result.DateWatched.Should().Be(new DateTime(2026, 7, 17));
    }

    [Fact]
    public async Task Repository_Should_Save_Currently_Watching_Movie_Without_Watched_Date()
    {
        // Arrange
        var saved = new List<Movie>();
        var repository = new Mock<IMovieRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => saved.Add(movie))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var movie = new Movie
        {
            Title = "Dune: Part Two",
            Studio = "Legendary Pictures",
            ReleasedDate = new DateTime(2024, 3, 1)
        };

        // Act
        await repository.Object.SaveAsync(movie, CancellationToken.None);
        var results = await repository.Object.GetAllAsync(CancellationToken.None);

        // Assert
        results.Should().ContainSingle().Which.DateWatched.Should().BeNull();
    }

    [Fact]
    public async Task Repository_Should_Update_And_Delete_Movie_Watched_Through_Contract()
    {
        // Arrange
        var saved = new List<Movie>
        {
            new()
            {
                Id = "movie-1",
                Title = "Dune: Part Two",
                Studio = "Legendary Pictures",
                ReleasedDate = new DateTime(2024, 3, 1),
                DateWatched = new DateTime(2026, 7, 17)
            }
        };

        var repository = new Mock<IMovieRepository>();
        repository.Setup(r => r.GetByIdAsync("movie-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved.SingleOrDefault(movie => movie.Id == "movie-1"));
        repository.Setup(r => r.UpdateWatchedDateAsync("movie-1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, CancellationToken>((id, dateWatched, _) => saved.Single(movie => movie.Id == id).DateWatched = dateWatched)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeleteAsync("movie-1", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => saved.RemoveAll(movie => movie.Id == id))
            .Returns(Task.CompletedTask);

        // Act
        await repository.Object.UpdateWatchedDateAsync("movie-1", new DateTime(2026, 7, 18), CancellationToken.None);
        var updated = await repository.Object.GetByIdAsync("movie-1", CancellationToken.None);

        // Assert
        updated.Should().NotBeNull();
        updated!.DateWatched.Should().Be(new DateTime(2026, 7, 18));

        // Act
        await repository.Object.DeleteAsync("movie-1", CancellationToken.None);

        // Assert
        saved.Should().BeEmpty();
    }
}
