using BookNThings.Infrastructure.OpenAi;
using FluentAssertions;

namespace BookNThings.Tests;

public class OpenAiBookResponseParserTests
{
    [Fact]
    public void Parse_Should_Map_Valid_Structured_Response()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "Dune",
              "description": "Epic science fiction novel.",
              "pages": 412,
              "datePublished": "1965-08-01",
              "genres": [ "Science Fiction", "Adventure" ],
              "author": "Frank Herbert"
            }
          ]
        }
        """;

        // Act
        var results = OpenAiBookResponseParser.Parse(json);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Dune");
        results[0].DatePublished.Should().Be(new DateTime(1965, 8, 1));
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Json()
    {
        // Arrange
        const string json = "{ nope";

        // Act
        var act = () => OpenAiBookResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Book()
    {
        // Arrange
        const string json = """
        {
          "results": [
            {
              "title": "",
              "description": "Missing title.",
              "pages": 10,
              "datePublished": "2020-01-01",
              "genres": [],
              "author": "Someone"
            }
          ]
        }
        """;

        // Act
        var act = () => OpenAiBookResponseParser.Parse(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed validation*");
    }
}
