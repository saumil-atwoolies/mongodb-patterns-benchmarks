using MongoDbPatterns.Domain.Aggregates;

namespace MongoDbPatterns.Domain.Tests.Aggregates;

public class OrderAggregateTests
{
    [Fact]
    public void Create_ReturnsNewOrder_WithVersionZero()
    {
        var order = OrderAggregate.Create();

        Assert.Equal(0, order.Version);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        var order = OrderAggregate.Create();

        Assert.NotEqual(Guid.Empty, order.Id);
    }

    [Fact]
    public void MarkReadyForFulfilment_IncrementsVersion()
    {
        var order = OrderAggregate.Create();

        order.MarkReadyForFulfilment();

        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void MarkReadyForFulfilment_SetsStatus()
    {
        var order = OrderAggregate.Create();

        order.MarkReadyForFulfilment();

        Assert.Equal(OrderStatus.ReadyForFulfilment, order.Status);
    }
}
