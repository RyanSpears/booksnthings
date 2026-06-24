namespace BookNThings.Infrastructure.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "";

    public string DatabaseName { get; set; } = "booknthings";

    public string BooksCollection { get; set; } = "books";

    public string GamesCollection { get; set; } = "games";

    public string ShowsCollection { get; set; } = "shows";
}
