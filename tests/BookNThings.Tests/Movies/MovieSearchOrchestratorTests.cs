using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNThings.Tests;

public class MovieSearchOrchestratorTests
{
    [Fact]
    public async Task SearchAsync_Should_Reject_Empty_Query()
    {
        // Arrange
        var service = new Mock<IMovieSearchService>();
        var logger = Mock.Of<ILogger<MovieSearchOrchestrator>>();
        var orchestrator = new MovieSearchOrchestrator(service.Object, logger);

        // Act
        var act = () => orchestrator.SearchAsync(" ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchAsync_Should_Trim_Query_And_Return_Results()
    {
        // Arrange
        var expected = new[]
        {
            new Movie
            {
                Title = "Dune: Part Two",
                Studio = "Legendary Pictures",
                ReleasedDate = new DateTime(2024, 3, 1),
                Rating = 91
            }
        };
        var service = new Mock<IMovieSearchService>();
        service.Setup(s => s.SearchAsync("Dune: Part Two", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var orchestrator = new MovieSearchOrchestrator(service.Object, Mock.Of<ILogger<MovieSearchOrchestrator>>());

        // Act
        var results = await orchestrator.SearchAsync(" Dune: Part Two ", CancellationToken.None);

        // Assert
        results.Should().ContainSingle().Which.Title.Should().Be("Dune: Part Two");
    }
}
