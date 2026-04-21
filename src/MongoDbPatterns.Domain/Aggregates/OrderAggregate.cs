using MongoDbPatterns.Domain.Events;

namespace MongoDbPatterns.Domain.Aggregates;

public class OrderAggregate
{
    private readonly List<DomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private OrderAggregate()
    {
    }

    public static OrderAggregate Create()
    {
        var order = new OrderAggregate
        {
            Id = Guid.NewGuid(),
            Version = 0,
            Status = OrderStatus.Created
        };
        order._domainEvents.Add(new OrderCreated(order.Id));
        return order;
    }

    public void MarkReadyForFulfilment()
    {
        Status = OrderStatus.ReadyForFulfilment;
        Version++;
        _domainEvents.Add(new OrderReadyForFulfilment(Id));
    }

    public void ClearEvents()
    {
        _domainEvents.Clear();
    }
}
