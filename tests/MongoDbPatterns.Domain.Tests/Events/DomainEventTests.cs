using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Domain.Events;

namespace MongoDbPatterns.Domain.Tests.Events;

public class DomainEventTests
{
    [Fact]
    public void OrderCreated_HasCorrectAggregateVersion()
    {
        var order = OrderAggregate.Create();
        var evt = order.DomainEvents.OfType<OrderCreated>().Single();

        Assert.Equal(0, evt.AggregateVersion);
    }

    [Fact]
    public void OrderReadyForFulfilment_HasCorrectAggregateVersion()
    {
        var order = OrderAggregate.Create();
        order.MarkReadyForFulfilment();

        var evt = order.DomainEvents.OfType<OrderReadyForFulfilment>().Single();

        Assert.Equal(1, evt.AggregateVersion);
    }
}
