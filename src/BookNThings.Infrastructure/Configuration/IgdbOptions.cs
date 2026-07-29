namespace BookNThings.Infrastructure.Configuration;

public sealed class IgdbOptions
{
    public const string SectionName = "IGDB";

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";
}
