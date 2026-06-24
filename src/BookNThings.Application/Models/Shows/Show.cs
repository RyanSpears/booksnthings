namespace BookNThings.Domain.Models;

public class Show
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Network { get; set; } = "";

    public string Studio { get; set; } = "";

    public int Season { get; set; }

    public DateTime? DateWatched { get; set; }

    public decimal? Rating { get; set; }

    public List<string> Genres { get; set; } = [];

    public string? Creator { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
