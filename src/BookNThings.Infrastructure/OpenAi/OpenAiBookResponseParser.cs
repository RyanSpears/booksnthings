using System.Text.Json;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.OpenAi;

public static class OpenAiBookResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Book> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        OpenAiBookSearchResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<OpenAiBookSearchResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OpenAI returned invalid JSON.", ex);
        }

        if (response?.Results is null)
        {
            throw new InvalidOperationException("OpenAI response did not match the expected schema.");
        }

        var books = response.Results.Select(result => new Book
        {
            Title = result.Title.Trim(),
            Description = result.Description.Trim(),
            Pages = result.Pages,
            DatePublished = result.DatePublished.Date,
            Genres = result.Genres.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct().ToList(),
            Author = result.Author.Trim()
        }).ToList();

        var invalid = books
            .Select((book, index) => new { Index = index, Errors = BookValidator.Validate(book) })
            .FirstOrDefault(item => item.Errors.Count > 0);

        if (invalid is not null)
        {
            throw new InvalidOperationException($"OpenAI response item {invalid.Index + 1} failed validation: {string.Join(" ", invalid.Errors)}");
        }

        return books;
    }
}
