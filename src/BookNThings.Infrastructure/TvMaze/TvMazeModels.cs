using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.TvMaze;

public sealed class TvMazeShowSearchResult
{
    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("show")]
    public TvMazeShow Show { get; set; } = new();
}

public sealed class TvMazeShow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("genres")]
    public List<string>? Genres { get; set; }

    [JsonPropertyName("rating")]
    public TvMazeRating? Rating { get; set; }

    [JsonPropertyName("network")]
    public TvMazeChannel? Network { get; set; }

    [JsonPropertyName("webChannel")]
    public TvMazeChannel? WebChannel { get; set; }
}

public sealed class TvMazeSeason
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("episodeOrder")]
    public int? EpisodeOrder { get; set; }

    [JsonPropertyName("premiereDate")]
    public DateTime? PremiereDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("network")]
    public TvMazeChannel? Network { get; set; }

    [JsonPropertyName("webChannel")]
    public TvMazeChannel? WebChannel { get; set; }
}

public sealed class TvMazeChannel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class TvMazeRating
{
    [JsonPropertyName("average")]
    public decimal? Average { get; set; }
}
