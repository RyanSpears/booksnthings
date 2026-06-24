using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class ShowValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Show()
    {
        var errors = ShowValidator.Validate(new Show());

        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Network is required.");
        errors.Should().Contain("Studio is required.");
        errors.Should().Contain("Season must be at least 1.");
    }

    [Fact]
    public void ValidateForWatched_Should_Require_Watched_Date()
    {
        var errors = ShowValidator.ValidateForWatched(new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2
        });

        errors.Should().Contain("Watched date is required.");
    }

    [Fact]
    public void ValidateForCurrentlyWatching_Should_Not_Require_Watched_Date()
    {
        var errors = ShowValidator.ValidateForCurrentlyWatching(new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2
        });

        errors.Should().BeEmpty();
    }
}
