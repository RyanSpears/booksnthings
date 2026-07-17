using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;

namespace BookNThings.Tests;

public class OpenAiMovieResponseParserTests
{
    [Fact]
    public void Parse_Should_Map_Valid_Structured_Response()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "Dune: Part Two",
              "studio": "Legendary Pictures",
              "releasedDate": "2024-03-01",
              "rating": 91,
              "genres": [ "Science Fiction", "Adventure" ],
              "director": "Denis Villeneuve"
            }
          ]
        }
        """;

        // Act
        var results = OpenAiMovieResponseParser.Parse(json);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Dune: Part Two");
        results[0].ReleasedDate.Should().Be(new DateTime(2024, 3, 1));
        results[0].Rating.Should().Be(91);
        results[0].Genres.Should().ContainEquivalentOf("Science Fiction");
        results[0].Director.Should().Be("Denis Villeneuve");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Json()
    {
        // Arrange
        const string json = "{ nope";

        // Act
        var act = () => OpenAiMovieResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Movie()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "",
              "studio": "",
              "releasedDate": "2024-03-01",
              "rating": null,
              "genres": [],
              "director": null
            }
          ]
        }
        """;

        // Act
        var act = () => OpenAiMovieResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed validation*");
    }
}
