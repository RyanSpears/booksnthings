namespace BookNThings.Infrastructure.Configuration;

public sealed class TmDbOptions
{
    public const string SectionName = "TMDb";

    public string BearerToken { get; set; } = "";
}
