namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalBooksOptions
{
    public const string SectionName = "LocalBooks";

    public string DataDirectory { get; set; } = "";

    public string FileName { get; set; } = "books.json";
}
