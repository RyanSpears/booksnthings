using System.Text.Json;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.OpenAi;

public static class OpenAiShowResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Show> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("OpenAI show response was empty.");
        }

        OpenAiShowSearchResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<OpenAiShowSearchResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OpenAI show response contained invalid JSON.", ex);
        }

        if (response?.Results is null)
        {
            throw new InvalidOperationException("OpenAI show response did not match the expected schema.");
        }

        var shows = response.Results.Select(result => new Show
        {
            Title = result.Title,
            Network = result.Network,
            Studio = result.Studio,
            Season = result.Season,
            Rating = result.Rating,
            Genres = result.Genres ?? [],
            Creator = result.Creator,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var invalid = shows
            .Select((show, index) => new { Index = index, Errors = ShowValidator.Validate(show) })
            .Where(item => item.Errors.Count > 0)
            .ToList();

        if (invalid.Count > 0)
        {
            var details = string.Join("; ", invalid.Select(item => $"item {item.Index}: {string.Join(" ", item.Errors)}"));
            throw new InvalidOperationException($"OpenAI show response failed validation. {details}");
        }

        return shows;
    }
}
