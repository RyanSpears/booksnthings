using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiGameSearchResponse
{
    [JsonPropertyName("results")]
    public List<OpenAiGameSearchResult> Results { get; set; } = [];
}

public sealed class OpenAiGameSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = "";

    [JsonPropertyName("studio")]
    public string Studio { get; set; } = "";

    [JsonPropertyName("releasedDate")]
    public DateTime ReleasedDate { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    [JsonPropertyName("developer")]
    public string? Developer { get; set; }
}
