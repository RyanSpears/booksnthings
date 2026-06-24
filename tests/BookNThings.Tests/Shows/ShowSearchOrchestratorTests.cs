using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNThings.Tests;

public class ShowSearchOrchestratorTests
{
    [Fact]
    public async Task SearchAsync_Should_Reject_Empty_Query()
    {
        var service = new Mock<IShowSearchService>();
        var logger = Mock.Of<ILogger<ShowSearchOrchestrator>>();
        var orchestrator = new ShowSearchOrchestrator(service.Object, logger);

        var act = () => orchestrator.SearchAsync(" ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchAsync_Should_Trim_Query_And_Return_Results()
    {
        var expected = new[]
        {
            new Show
            {
                Title = "Severance",
                Network = "Apple TV+",
                Studio = "Endeavor Content",
                Season = 2,
                Rating = 94
            }
        };
        var service = new Mock<IShowSearchService>();
        service.Setup(s => s.SearchAsync("Severance", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var orchestrator = new ShowSearchOrchestrator(service.Object, Mock.Of<ILogger<ShowSearchOrchestrator>>());

        var results = await orchestrator.SearchAsync(" Severance ", CancellationToken.None);

        results.Should().ContainSingle().Which.Title.Should().Be("Severance");
    }
}
