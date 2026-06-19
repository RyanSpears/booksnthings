namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalGamesOptions
{
    public const string SectionName = "LocalGames";

    public string DataDirectory { get; set; } = "";

    public string FileName { get; set; } = "games.json";
}
