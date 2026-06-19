using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookNThings.Infrastructure.Mongo;

public sealed class MongoGameDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Title { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string Studio { get; set; } = "";

    public DateTime ReleasedDate { get; set; }

    public DateTime? DatePlayed { get; set; }

    public decimal? Rating { get; set; }

    public List<string> Genres { get; set; } = [];

    public string? Developer { get; set; }

    public DateTime CreatedAt { get; set; }
}
