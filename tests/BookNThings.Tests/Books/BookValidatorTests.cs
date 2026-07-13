using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class BookValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Book()
    {
        // Arrange
        var book = new Book { Pages = -1 };

        // Act
        var errors = BookValidator.Validate(book);

        // Assert
        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Author is required.");
        errors.Should().Contain("Pages cannot be negative.");
        errors.Should().Contain("Publication date is required.");
    }

    [Fact]
    public void Validate_Should_Accept_Valid_Book()
    {
        // Arrange
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1)
        };

        // Act
        var errors = BookValidator.Validate(book);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateForRead_Should_Require_Read_Date()
    {
        // Arrange
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1)
        };

        // Act
        var errors = BookValidator.ValidateForRead(book);

        // Assert
        errors.Should().Contain("Read date is required.");
    }

    [Fact]
    public void ValidateForRead_Should_Accept_Book_With_Read_Date()
    {
        // Arrange
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1),
            DateRead = new DateTime(2026, 5, 19)
        };

        // Act
        var errors = BookValidator.ValidateForRead(book);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateForCurrentlyReading_Should_Not_Require_Read_Date()
    {
        // Arrange
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1)
        };

        // Act
        var errors = BookValidator.ValidateForCurrentlyReading(book);

        // Assert
        errors.Should().BeEmpty();
    }
}
