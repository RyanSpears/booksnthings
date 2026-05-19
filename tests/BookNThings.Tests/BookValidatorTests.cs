using BookNThings.Application.Validation;
using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class BookValidatorTests
{
    [Fact]
    public void Validate_Should_Return_Errors_For_Invalid_Book()
    {
        var errors = BookValidator.Validate(new Book { Pages = -1 });

        errors.Should().Contain("Title is required.");
        errors.Should().Contain("Author is required.");
        errors.Should().Contain("Pages cannot be negative.");
        errors.Should().Contain("Publication date is required.");
    }

    [Fact]
    public void Validate_Should_Accept_Valid_Book()
    {
        var errors = BookValidator.Validate(new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1)
        });

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateForSave_Should_Require_Read_Date()
    {
        var errors = BookValidator.ValidateForSave(new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1)
        });

        errors.Should().Contain("Read date is required.");
    }

    [Fact]
    public void ValidateForSave_Should_Accept_Book_With_Read_Date()
    {
        var errors = BookValidator.ValidateForSave(new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DatePublished = new DateTime(1965, 8, 1),
            DateRead = new DateTime(2026, 5, 19)
        });

        errors.Should().BeEmpty();
    }
}
