using BookNThings.Domain.Models;
using FluentAssertions;

namespace BookNThings.Tests;

public class BookTests
{
    [Fact]
    public void Should_Create_Book()
    {
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412,
            DateRead = new DateTime(2026, 5, 19)
        };

        book.Title.Should().Be("Dune");
        book.Author.Should().Be("Frank Herbert");
        book.Pages.Should().Be(412);
        book.DateRead.Should().Be(new DateTime(2026, 5, 19));
    }

    [Fact]
    public void Should_Create_Currently_Reading_Book()
    {
        var book = new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Pages = 412
        };

        book.DateRead.Should().BeNull();
    }
}
