using System.Text.Json.Serialization;

namespace BookNThings.Infrastructure.Igdb;

public sealed class IgdbGameSearchResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("first_release_date")]
    public long? FirstReleaseDate { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }

    [JsonPropertyName("genres")]
    public List<IgdbGameGenre> Genres { get; set; } = [];

    [JsonPropertyName("involved_companies")]
    public List<IgdbInvolvedCompany> InvolvedCompanies { get; set; } = [];
}

public sealed class IgdbGameGenre
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public sealed class IgdbInvolvedCompany
{
    [JsonPropertyName("company")]
    public IgdbCompanyRef? Company { get; set; }

    [JsonPropertyName("developer")]
    public bool? Developer { get; set; }

    [JsonPropertyName("publisher")]
    public bool? Publisher { get; set; }
}

public sealed class IgdbCompanyRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public sealed class IgdbTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";
}
