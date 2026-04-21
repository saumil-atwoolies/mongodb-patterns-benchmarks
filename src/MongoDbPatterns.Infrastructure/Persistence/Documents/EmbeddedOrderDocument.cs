using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbPatterns.Infrastructure.Persistence.Documents;

public sealed class EmbeddedEventDocument
{
    [BsonElement("eventId")]
    [BsonRepresentation(BsonType.String)]
    public Guid EventId { get; set; }

    [BsonElement("aggregateVersion")]
    public int AggregateVersion { get; set; }

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; }
}

public sealed class EmbeddedOrderDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("domainEvents")]
    public List<EmbeddedEventDocument> DomainEvents { get; set; } = [];
}
