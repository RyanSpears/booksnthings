using BookNThings.Application.Contracts;
using BookNThings.Domain.Models;
using FluentAssertions;
using Moq;

namespace BookNThings.Tests;

public class RepositoryContractTests
{
    [Fact]
    public async Task Repository_Should_Save_And_Return_Books_Through_Contract()
    {
        var saved = new List<Book>();
        var repository = new Mock<IBookRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => saved.Add(book))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var book = new Book
        {
            Title = "Kindred",
            Author = "Octavia E. Butler",
            DatePublished = new DateTime(1979, 6, 1),
            DateRead = new DateTime(2026, 5, 19)
        };

        await repository.Object.SaveAsync(book, CancellationToken.None);
        var results = await repository.Object.GetAllAsync(CancellationToken.None);

        var result = results.Should().ContainSingle().Which;
        result.Author.Should().Be("Octavia E. Butler");
        result.DateRead.Should().Be(new DateTime(2026, 5, 19));
    }

    [Fact]
    public async Task Repository_Should_Update_And_Delete_Book_Read_Through_Contract()
    {
        var saved = new List<Book>
        {
            new()
            {
                Id = "book-read-1",
                Title = "Kindred",
                Author = "Octavia E. Butler",
                DatePublished = new DateTime(1979, 6, 1),
                DateRead = new DateTime(2026, 5, 19)
            }
        };

        var repository = new Mock<IBookRepository>();
        repository.Setup(r => r.GetByIdAsync("book-read-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved.SingleOrDefault(book => book.Id == "book-read-1"));
        repository.Setup(r => r.UpdateReadDateAsync("book-read-1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, CancellationToken>((id, dateRead, _) => saved.Single(book => book.Id == id).DateRead = dateRead)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeleteAsync("book-read-1", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => saved.RemoveAll(book => book.Id == id))
            .Returns(Task.CompletedTask);

        await repository.Object.UpdateReadDateAsync("book-read-1", new DateTime(2026, 5, 20), CancellationToken.None);
        var updated = await repository.Object.GetByIdAsync("book-read-1", CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.DateRead.Should().Be(new DateTime(2026, 5, 20));

        await repository.Object.DeleteAsync("book-read-1", CancellationToken.None);

        saved.Should().BeEmpty();
    }
}
