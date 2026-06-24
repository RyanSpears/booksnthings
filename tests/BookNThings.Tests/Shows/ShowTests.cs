using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class ShowTests
{
    [Fact]
    public void Should_Create_Show()
    {
        var show = new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2,
            DateWatched = new DateTime(2026, 6, 24),
            Rating = 94,
            Creator = "Dan Erickson"
        };

        show.Title.Should().Be("Severance");
        show.Network.Should().Be("Apple TV+");
        show.Studio.Should().Be("Endeavor Content");
        show.Season.Should().Be(2);
        show.DateWatched.Should().Be(new DateTime(2026, 6, 24));
        show.Rating.Should().Be(94);
        show.Creator.Should().Be("Dan Erickson");
    }

    [Fact]
    public void Should_Create_Currently_Watching_Show()
    {
        var show = new Show
        {
            Title = "Silo",
            Network = "Apple TV+",
            Studio = "Mímir Films, Nemo Films, AMC Studios, Apple Studios",
            Season = 1
        };

        show.DateWatched.Should().BeNull();
        show.Rating.Should().BeNull();
    }
}
