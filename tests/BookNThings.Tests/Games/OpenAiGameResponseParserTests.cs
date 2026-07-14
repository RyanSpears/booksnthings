using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;

namespace BookNThings.Tests;

public class OpenAiGameResponseParserTests
{
    [Fact]
    public void Parse_Should_Map_Valid_Structured_Response()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "Baldur's Gate 3",
              "publisher": "Larian Studios",
              "studio": "Larian Studios",
              "releasedDate": "2023-08-03",
              "rating": 96,
              "genres": [ "RPG", "Fantasy" ],
              "developer": "Larian Studios"
            }
          ]
        }
        """;

        // Act
        var results = OpenAiGameResponseParser.Parse(json);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Baldur's Gate 3");
        results[0].ReleasedDate.Should().Be(new DateTime(2023, 8, 3));
        results[0].Rating.Should().Be(96);
        results[0].Genres.Should().ContainEquivalentOf("RPG");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Json()
    {
        // Arrange
        const string json = "{ nope";

        // Act
        var act = () => OpenAiGameResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Game()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "",
              "publisher": "",
              "studio": "Unknown",
              "releasedDate": "2023-08-03",
              "rating": null,
              "genres": [],
              "developer": null
            }
          ]
        }
        """;

        // Act
        var act = () => OpenAiGameResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed validation*");
    }
}
