using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class ShowTests
{
    [Fact]
    public void Should_Create_Show()
    {
        // Arrange
        const string title = "Severance";
        const string network = "Apple TV+";
        const string studio = "Endeavor Content";
        const int season = 2;
        var dateWatched = new DateTime(2026, 6, 24);
        const int rating = 94;
        const string creator = "Dan Erickson";

        // Act
        var show = new Show
        {
            Title = title,
            Network = network,
            Studio = studio,
            Season = season,
            DateWatched = dateWatched,
            Rating = rating,
            Creator = creator
        };

        // Assert
        show.Title.Should().Be(title);
        show.Network.Should().Be(network);
        show.Studio.Should().Be(studio);
        show.Season.Should().Be(season);
        show.DateWatched.Should().Be(dateWatched);
        show.Rating.Should().Be(rating);
        show.Creator.Should().Be(creator);
    }

    [Fact]
    public void Should_Create_Currently_Watching_Show()
    {
        // Arrange
        const string title = "Silo";
        const string network = "Apple TV+";
        const string studio = "Mímir Films, Nemo Films, AMC Studios, Apple Studios";
        const int season = 1;

        // Act
        var show = new Show
        {
            Title = title,
            Network = network,
            Studio = studio,
            Season = season
        };

        // Assert
        show.DateWatched.Should().BeNull();
        show.Rating.Should().BeNull();
    }
}
