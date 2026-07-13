using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class BookTests
{
    [Fact]
    public void Should_Create_Book()
    {
        // Arrange
        const string title = "Dune";
        const string author = "Frank Herbert";
        const int pages = 412;
        var dateRead = new DateTime(2026, 5, 19);

        // Act
        var book = new Book
        {
            Title = title,
            Author = author,
            Pages = pages,
            DateRead = dateRead
        };

        // Assert
        book.Title.Should().Be(title);
        book.Author.Should().Be(author);
        book.Pages.Should().Be(pages);
        book.DateRead.Should().Be(dateRead);
    }

    [Fact]
    public void Should_Create_Currently_Reading_Book()
    {
        // Arrange
        const string title = "Dune";
        const string author = "Frank Herbert";
        const int pages = 412;

        // Act
        var book = new Book
        {
            Title = title,
            Author = author,
            Pages = pages
        };

        // Assert
        book.DateRead.Should().BeNull();
    }
}
