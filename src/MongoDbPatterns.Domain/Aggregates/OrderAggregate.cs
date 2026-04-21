namespace MongoDbPatterns.Domain.Aggregates;

public class OrderAggregate
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public OrderStatus Status { get; private set; }

    private OrderAggregate()
    {
    }

    public static OrderAggregate Create()
    {
        return new OrderAggregate
        {
            Id = Guid.NewGuid(),
            Version = 0,
            Status = OrderStatus.Created
        };
    }

    public void MarkReadyForFulfilment()
    {
        Status = OrderStatus.ReadyForFulfilment;
        Version++;
    }
}
