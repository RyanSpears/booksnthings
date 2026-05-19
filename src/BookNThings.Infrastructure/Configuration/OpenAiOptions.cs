namespace BookNThings.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "gpt-4.1-mini";
}
