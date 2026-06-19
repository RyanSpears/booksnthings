using System.Text.Json;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.OpenAi;

public static class OpenAiGameResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Game> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        OpenAiGameSearchResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<OpenAiGameSearchResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OpenAI returned invalid JSON.", ex);
        }

        if (response?.Results is null)
        {
            throw new InvalidOperationException("OpenAI response did not match the expected schema.");
        }

        var games = response.Results.Select(result => new Game
        {
            Title = result.Title.Trim(),
            Publisher = result.Publisher.Trim(),
            Studio = result.Studio.Trim(),
            ReleasedDate = result.ReleasedDate.Date,
            DatePlayed = null,
            Rating = result.Rating,
            Genres = result.Genres
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Select(genre => genre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Developer = string.IsNullOrWhiteSpace(result.Developer) ? null : result.Developer.Trim(),
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var invalid = games
            .Select((game, index) => new { Index = index, Errors = GameValidator.Validate(game) })
            .FirstOrDefault(item => item.Errors.Count > 0);

        if (invalid is not null)
        {
            throw new InvalidOperationException($"OpenAI response item {invalid.Index + 1} failed validation: {string.Join(" ", invalid.Errors)}");
        }

        return games;
    }
}
