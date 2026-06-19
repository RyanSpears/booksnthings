using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class GameValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Game()
    {
        var errors = GameValidator.Validate(new Game());

        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Publisher is required.");
        errors.Should().Contain("Studio is required.");
        errors.Should().Contain("Released date is required.");
    }

    [Fact]
    public void ValidateForPlayed_Should_Require_Played_Date()
    {
        var errors = GameValidator.ValidateForPlayed(new Game
        {
            Title = "Hades",
            Publisher = "Supergiant Games",
            Studio = "Supergiant Games",
            ReleasedDate = new DateTime(2020, 9, 17)
        });

        errors.Should().Contain("Played date is required.");
    }

    [Fact]
    public void ValidateForCurrentlyPlaying_Should_Not_Require_Played_Date()
    {
        var errors = GameValidator.ValidateForCurrentlyPlaying(new Game
        {
            Title = "Hades",
            Publisher = "Supergiant Games",
            Studio = "Supergiant Games",
            ReleasedDate = new DateTime(2020, 9, 17)
        });

        errors.Should().BeEmpty();
    }
}
