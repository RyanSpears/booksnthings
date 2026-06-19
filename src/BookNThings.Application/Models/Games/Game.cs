namespace BookNThings.Domain.Models;

public class Game
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string Studio { get; set; } = "";

    public DateTime ReleasedDate { get; set; }

    public DateTime? DatePlayed { get; set; }

    public decimal? Rating { get; set; }

    public List<string> Genres { get; set; } = [];

    public string? Developer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
