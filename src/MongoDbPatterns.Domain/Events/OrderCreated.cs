namespace MongoDbPatterns.Domain.Events;

public sealed class OrderCreated : DomainEvent
{
    public OrderCreated(Guid aggregateId)
        : base(aggregateId, aggregateVersion: 0)
    {
    }
}
