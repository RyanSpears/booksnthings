using BookNThings.Application.Contracts;
using BookNThings.Application.Services;
using BookNThings.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookNThings.Tests;

public class BookSearchOrchestratorTests
{
    [Fact]
    public async Task SearchAsync_Should_Reject_Empty_Query()
    {
        var service = new Mock<IBookSearchService>();
        var logger = Mock.Of<ILogger<BookSearchOrchestrator>>();
        var orchestrator = new BookSearchOrchestrator(service.Object, logger);

        var act = () => orchestrator.SearchAsync(" ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchAsync_Should_Trim_Query_And_Return_Results()
    {
        var expected = new[]
        {
            new Book
            {
                Title = "Dune",
                Author = "Frank Herbert",
                DatePublished = new DateTime(1965, 8, 1)
            }
        };
        var service = new Mock<IBookSearchService>();
        service.Setup(s => s.SearchAsync("Dune", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var orchestrator = new BookSearchOrchestrator(service.Object, Mock.Of<ILogger<BookSearchOrchestrator>>());

        var results = await orchestrator.SearchAsync(" Dune ", CancellationToken.None);

        results.Should().ContainSingle().Which.Title.Should().Be("Dune");
    }
}
