using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNThings.Tests;

public class GameSearchOrchestratorTests
{
    [Fact]
    public async Task SearchAsync_Should_Reject_Empty_Query()
    {
        var service = new Mock<IGameSearchService>();
        var logger = Mock.Of<ILogger<GameSearchOrchestrator>>();
        var orchestrator = new GameSearchOrchestrator(service.Object, logger);

        var act = () => orchestrator.SearchAsync(" ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchAsync_Should_Trim_Query_And_Return_Results()
    {
        var expected = new[]
        {
            new Game
            {
                Title = "Baldur's Gate 3",
                Publisher = "Larian Studios",
                Studio = "Larian Studios",
                ReleasedDate = new DateTime(2023, 8, 3),
                Rating = 96
            }
        };
        var service = new Mock<IGameSearchService>();
        service.Setup(s => s.SearchAsync("Baldur's Gate 3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var orchestrator = new GameSearchOrchestrator(service.Object, Mock.Of<ILogger<GameSearchOrchestrator>>());

        var results = await orchestrator.SearchAsync(" Baldur's Gate 3 ", CancellationToken.None);

        results.Should().ContainSingle().Which.Title.Should().Be("Baldur's Gate 3");
    }
}
