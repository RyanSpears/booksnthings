using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;

namespace BookNThings.Tests;

public class OpenAiShowResponseParserTests
{
    [Fact]
    public void Parse_Should_Map_Valid_Structured_Response()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "Severance",
              "network": "Apple TV+",
              "studio": "Endeavor Content",
              "season": 2,
              "rating": 94,
              "genres": [ "Drama", "Mystery" ],
              "creator": "Dan Erickson"
            }
          ]
        }
        """;

        // Act
        var results = OpenAiShowResponseParser.Parse(json);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Severance");
        results[0].Network.Should().Be("Apple TV+");
        results[0].Season.Should().Be(2);
        results[0].Genres.Should().ContainEquivalentOf("Drama");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Json()
    {
        // Arrange
        const string json = "{ nope";

        // Act
        var act = () => OpenAiShowResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Show()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "",
              "network": "",
              "studio": "Unknown",
              "season": 0,
              "rating": null,
              "genres": [],
              "creator": null
            }
          ]
        }
        """;

        // Act
        var act = () => OpenAiShowResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed validation*");
    }
}
