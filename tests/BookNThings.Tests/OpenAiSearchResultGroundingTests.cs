using BookNThings.Domain.Models;
using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;

namespace BookNThings.Tests;

public class OpenAiSearchResultGroundingTests
{
    [Fact]
    public void FilterSpecificMatches_Should_Remove_Invented_Game_For_TitleLike_Query()
    {
        // Arrange
        var query = "007 First Light by IO Interactive";
        var results = new[]
        {
            new Game
            {
                Title = "007 Legends",
                Publisher = "Activision",
                Studio = "Eurocom",
                ReleasedDate = new DateTime(2012, 11, 1),
                Developer = "Eurocom"
            },
            new Game
            {
                Title = "007 First Light",
                Publisher = "IO Interactive",
                Studio = "IO Interactive",
                ReleasedDate = new DateTime(2026, 1, 1),
                Developer = "IO Interactive"
            }
        };

        // Act
        var grounded = OpenAiSearchResultGrounding.FilterSpecificMatches(
            query,
            results,
            game => game.Title,
            game => new[] { game.Title, game.Publisher, game.Studio, game.Developer, string.Join(" ", game.Genres) });

        // Assert
        grounded.Should().ContainSingle();
        grounded[0].Title.Should().Be("007 First Light");
    }

    [Fact]
    public void FilterSpecificMatches_Should_Not_Filter_Broad_Searches()
    {
        // Arrange
        var query = "stylish sci-fi films";
        var results = new[]
        {
            new Movie
            {
                Title = "Dune: Part Two",
                Studio = "Legendary Pictures",
                ReleasedDate = new DateTime(2024, 3, 1),
                Director = "Denis Villeneuve"
            },
            new Movie
            {
                Title = "Blade Runner 2049",
                Studio = "Warner Bros. Pictures",
                ReleasedDate = new DateTime(2017, 10, 6),
                Director = "Denis Villeneuve"
            }
        };

        // Act
        var grounded = OpenAiSearchResultGrounding.FilterSpecificMatches(
            query,
            results,
            movie => movie.Title,
            movie => new[] { movie.Title, movie.Studio, movie.Director, string.Join(" ", movie.Genres) });

        // Assert
        grounded.Should().HaveCount(2);
    }
}
