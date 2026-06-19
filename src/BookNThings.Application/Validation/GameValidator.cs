using BookNThings.Domain.Models;

namespace BookNThings.Application.Validation;

public static class GameValidator
{
    public static IReadOnlyList<string> Validate(Game? game)
    {
        var errors = new List<string>();

        if (game is null)
        {
            errors.Add("Game is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(game.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(game.Publisher))
        {
            errors.Add("Publisher is required.");
        }

        if (string.IsNullOrWhiteSpace(game.Studio))
        {
            errors.Add("Studio is required.");
        }

        if (game.ReleasedDate == default)
        {
            errors.Add("Released date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForPlayed(Game? game)
    {
        var errors = Validate(game).ToList();

        if (game is not null && !game.DatePlayed.HasValue)
        {
            errors.Add("Played date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForCurrentlyPlaying(Game? game) =>
        Validate(game);
}
