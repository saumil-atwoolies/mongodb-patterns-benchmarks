using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbPatterns.Infrastructure.Persistence.Documents;

public sealed class OrderEventDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid EventId { get; set; }

    [BsonElement("aggregateId")]
    [BsonRepresentation(BsonType.String)]
    public Guid AggregateId { get; set; }

    [BsonElement("aggregateVersion")]
    public int AggregateVersion { get; set; }

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; }
}
