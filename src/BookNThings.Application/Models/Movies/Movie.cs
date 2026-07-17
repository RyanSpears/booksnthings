namespace BookNThings.Domain.Models;

public class Movie
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Studio { get; set; } = "";

    public DateTime ReleasedDate { get; set; }

    public DateTime? DateWatched { get; set; }

    public decimal? Rating { get; set; }

    public List<string> Genres { get; set; } = [];

    public string? Director { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
