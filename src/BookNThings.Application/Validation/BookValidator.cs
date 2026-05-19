using BookNThings.Domain.Models;

namespace BookNThings.Application.Validation;

public static class BookValidator
{
    public static IReadOnlyList<string> Validate(Book? book)
    {
        var errors = new List<string>();

        if (book is null)
        {
            errors.Add("Book is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(book.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(book.Author))
        {
            errors.Add("Author is required.");
        }

        if (book.Pages < 0)
        {
            errors.Add("Pages cannot be negative.");
        }

        if (book.DatePublished == default)
        {
            errors.Add("Publication date is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateForSave(Book? book)
    {
        var errors = Validate(book).ToList();

        if (book is not null && book.DateRead == default)
        {
            errors.Add("Read date is required.");
        }

        return errors;
    }
}
