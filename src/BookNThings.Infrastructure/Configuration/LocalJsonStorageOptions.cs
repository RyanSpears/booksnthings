namespace BookNThings.Infrastructure.Configuration;

public sealed class LocalJsonStorageOptions
{
    public const string SectionName = "LocalJsonStorage";

    public string DefaultDataDirectory { get; set; } = "";
}
