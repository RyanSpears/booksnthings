using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.OpenAi;

public sealed class OpenAiMovieSearchResponse
{
    [JsonPropertyName("results")]
    public List<OpenAiMovieSearchResult>? Results { get; set; }
}

public sealed class OpenAiMovieSearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("studio")]
    public string Studio { get; set; } = "";

    [JsonPropertyName("releasedDate")]
    public DateTime ReleasedDate { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    [JsonPropertyName("director")]
    public string? Director { get; set; }
}
