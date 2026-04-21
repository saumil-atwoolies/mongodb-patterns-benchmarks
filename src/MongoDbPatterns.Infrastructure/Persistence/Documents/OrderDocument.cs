using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbPatterns.Infrastructure.Persistence.Documents;

public sealed class OrderDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }
}
