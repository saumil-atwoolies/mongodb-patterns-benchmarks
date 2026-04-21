namespace MongoDbPatterns.Domain.Events;

public sealed class OrderReadyForFulfilment : DomainEvent
{
    public OrderReadyForFulfilment(Guid aggregateId)
        : base(aggregateId, aggregateVersion: 1)
    {
    }
}
