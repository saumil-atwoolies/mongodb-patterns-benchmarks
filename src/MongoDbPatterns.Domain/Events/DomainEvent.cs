namespace MongoDbPatterns.Domain.Events;

public abstract class DomainEvent
{
    public Guid EventId { get; }
    public Guid AggregateId { get; }
    public int AggregateVersion { get; }
    public DateTime OccurredAt { get; }

    protected DomainEvent(Guid aggregateId, int aggregateVersion)
    {
        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        AggregateVersion = aggregateVersion;
        OccurredAt = DateTime.UtcNow;
    }
}
