namespace BookNThings.Domain.Models;

public class Book
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public int Pages { get; set; }

    public DateTime DatePublished { get; set; }

    public DateTime? DateRead { get; set; }

    public List<string> Genres { get; set; } = [];

    public string Author { get; set; } = "";
}
