using System.Text.Json;
using BookNThings.Application.Validation;
using BookNThings.Domain.Models;

namespace BookNThings.Infrastructure.OpenAi;

public static class OpenAiMovieResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<Movie> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        OpenAiMovieSearchResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<OpenAiMovieSearchResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OpenAI returned invalid JSON.", ex);
        }

        if (response?.Results is null)
        {
            throw new InvalidOperationException("OpenAI response did not match the expected schema.");
        }

        var movies = response.Results.Select(result => new Movie
        {
            Title = result.Title.Trim(),
            Studio = result.Studio.Trim(),
            ReleasedDate = result.ReleasedDate.Date,
            DateWatched = null,
            Rating = result.Rating,
            Genres = result.Genres
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Select(genre => genre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Director = string.IsNullOrWhiteSpace(result.Director) ? null : result.Director.Trim(),
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var invalid = movies
            .Select((movie, index) => new { Index = index, Errors = MovieValidator.Validate(movie) })
            .FirstOrDefault(item => item.Errors.Count > 0);

        if (invalid is not null)
        {
            throw new InvalidOperationException($"OpenAI response item {invalid.Index + 1} failed validation: {string.Join(" ", invalid.Errors)}");
        }

        return movies;
    }
}
