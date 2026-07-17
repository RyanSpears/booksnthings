namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalMoviesOptions
{
    public const string SectionName = "LocalMovies";

    public string DataDirectory { get; set; } = "";

    public string FileName { get; set; } = "movies.json";
}
