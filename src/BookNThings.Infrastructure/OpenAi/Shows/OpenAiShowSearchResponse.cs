using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiShowSearchResponse
{
    [JsonPropertyName("results")]
    public List<OpenAiShowSearchResult> Results { get; set; } = [];
}

public sealed class OpenAiShowSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("network")]
    public string Network { get; set; } = "";

    [JsonPropertyName("studio")]
    public string Studio { get; set; } = "";

    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }
}
