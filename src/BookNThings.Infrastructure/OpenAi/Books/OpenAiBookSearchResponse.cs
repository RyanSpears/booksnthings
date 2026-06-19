using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiBookSearchResponse
{
    [JsonPropertyName("results")]
    public List<OpenAiBookSearchResult> Results { get; set; } = [];
}

public sealed class OpenAiBookSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("datePublished")]
    public DateTime DatePublished { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
}
