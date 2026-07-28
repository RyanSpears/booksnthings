using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.TmDb;

public sealed class TmDbMovieSearchResponse
{
    [JsonPropertyName("results")]
    public List<TmDbMovieSearchResult> Results { get; set; } = [];
}

public sealed class TmDbMovieSearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public decimal VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }
}

public sealed class TmDbMovieDetailsResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public decimal VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("genres")]
    public List<TmDbGenre>? Genres { get; set; }

    [JsonPropertyName("production_companies")]
    public List<TmDbProductionCompany>? ProductionCompanies { get; set; }

    [JsonPropertyName("credits")]
    public TmDbCredits? Credits { get; set; }
}

public sealed class TmDbGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class TmDbProductionCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class TmDbCredits
{
    [JsonPropertyName("crew")]
    public List<TmDbCrewMember>? Crew { get; set; }
}

public sealed class TmDbCrewMember
{
    [JsonPropertyName("job")]
    public string? Job { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
