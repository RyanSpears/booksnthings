using BookNThings.Domain.Models;

namespace BookNThings.Application.Validation;

public static class ShowValidator
{
    public static IReadOnlyList<string> Validate(Show? show)
    {
        var errors = new List<string>();

        if (show is null)
        {
            errors.Add("Show is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(show.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(show.Network))
        {
            errors.Add("Network is required.");
        }

        if (string.IsNullOrWhiteSpace(show.Studio))
        {
            errors.Add("Studio is required.");
        }

        if (show.Season < 1)
        {
            errors.Add("Season must be at least 1.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForWatched(Show? show)
    {
        var errors = Validate(show).ToList();

        if (show is not null && !show.DateWatched.HasValue)
        {
            errors.Add("Watched date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForCurrentlyWatching(Show? show) =>
        Validate(show);
}
