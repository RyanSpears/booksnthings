using BookNThings.Domain.Models;

namespace BookNThings.Application.Validation;

public static class MovieValidator
{
    public static IReadOnlyList<string> Validate(Movie? movie)
    {
        var errors = new List<string>();

        if (movie is null)
        {
            errors.Add("Movie is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(movie.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(movie.Studio))
        {
            errors.Add("Studio is required.");
        }

        if (movie.ReleasedDate == default)
        {
            errors.Add("Released date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForWatched(Movie? movie)
    {
        var errors = Validate(movie).ToList();

        if (movie is not null && !movie.DateWatched.HasValue)
        {
            errors.Add("Watched date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForCurrentlyWatching(Movie? movie) =>
        Validate(movie);
}
