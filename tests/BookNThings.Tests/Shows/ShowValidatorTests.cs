using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class ShowValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Show()
    {
        // Arrange
        var show = new Show();

        // Act
        var errors = ShowValidator.Validate(show);

        // Assert
        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Network is required.");
        errors.Should().Contain("Studio is required.");
        errors.Should().Contain("Season must be at least 1.");
    }

    [Fact]
    public void ValidateForWatched_Should_Require_Watched_Date()
    {
        // Arrange
        var show = new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2
        };

        // Act
        var errors = ShowValidator.ValidateForWatched(show);

        // Assert
        errors.Should().Contain("Watched date is required.");
    }

    [Fact]
    public void ValidateForCurrentlyWatching_Should_Not_Require_Watched_Date()
    {
        // Arrange
        var show = new Show
        {
            Title = "Severance",
            Network = "Apple TV+",
            Studio = "Endeavor Content",
            Season = 2
        };

        // Act
        var errors = ShowValidator.ValidateForCurrentlyWatching(show);

        // Assert
        errors.Should().BeEmpty();
    }
}
