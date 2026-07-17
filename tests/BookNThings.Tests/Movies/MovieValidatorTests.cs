using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class MovieValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Movie()
    {
        // Arrange
        var movie = new Movie();

        // Act
        var errors = MovieValidator.Validate(movie);

        // Assert
        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Studio is required.");
        errors.Should().Contain("Released date is required.");
    }

    [Fact]
    public void ValidateForWatched_Should_Require_Watched_Date()
    {
        // Arrange
        var movie = new Movie
        {
            Title = "Dune: Part Two",
            Studio = "Legendary Pictures",
            ReleasedDate = new DateTime(2024, 3, 1)
        };

        // Act
        var errors = MovieValidator.ValidateForWatched(movie);

        // Assert
        errors.Should().Contain("Watched date is required.");
    }

    [Fact]
    public void ValidateForCurrentlyWatching_Should_Not_Require_Watched_Date()
    {
        // Arrange
        var movie = new Movie
        {
            Title = "Dune: Part Two",
            Studio = "Legendary Pictures",
            ReleasedDate = new DateTime(2024, 3, 1)
        };

        // Act
        var errors = MovieValidator.ValidateForCurrentlyWatching(movie);

        // Assert
        errors.Should().BeEmpty();
    }
}
