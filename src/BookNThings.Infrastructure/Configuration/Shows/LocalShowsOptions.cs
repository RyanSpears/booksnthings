namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalShowsOptions
{
    public const string SectionName = "LocalShows";

    public string DataDirectory { get; set; } = "";

    public string FileName { get; set; } = "show.json";
}
