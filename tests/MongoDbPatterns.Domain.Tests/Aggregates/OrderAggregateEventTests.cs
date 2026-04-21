using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Domain.Events;

namespace MongoDbPatterns.Domain.Tests.Aggregates;

public class OrderAggregateEventTests
{
    [Fact]
    public void Create_ProducesOrderCreatedEvent()
    {
        var order = OrderAggregate.Create();

        Assert.Single(order.DomainEvents);
        var evt = Assert.IsType<OrderCreated>(order.DomainEvents[0]);
        Assert.Equal(0, evt.AggregateVersion);
        Assert.Equal(order.Id, evt.AggregateId);
    }

    [Fact]
    public void MarkReadyForFulfilment_ProducesOrderReadyForFulfilmentEvent()
    {
        var order = OrderAggregate.Create();
        order.MarkReadyForFulfilment();

        Assert.Equal(2, order.DomainEvents.Count);
        var evt = Assert.IsType<OrderReadyForFulfilment>(order.DomainEvents[1]);
        Assert.Equal(1, evt.AggregateVersion);
        Assert.Equal(order.Id, evt.AggregateId);
    }

    [Fact]
    public void ClearEvents_EmptiesEventList()
    {
        var order = OrderAggregate.Create();
        Assert.NotEmpty(order.DomainEvents);

        order.ClearEvents();

        Assert.Empty(order.DomainEvents);
    }
}
