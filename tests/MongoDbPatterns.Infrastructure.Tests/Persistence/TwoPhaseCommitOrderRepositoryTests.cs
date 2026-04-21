using MongoDB.Driver;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Infrastructure.Persistence;
using MongoDbPatterns.Infrastructure.Persistence.Documents;

namespace MongoDbPatterns.Infrastructure.Tests.Persistence;

public class TwoPhaseCommitOrderRepositoryTests : MongoDbIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_InsertsOrderAndEvent_InTransaction()
    {
        if (IsMongoUnavailable()) return;

        var repo = new TwoPhaseCommitOrderRepository(Context);
        var order = OrderAggregate.Create();

        await repo.CreateAsync(order, CancellationToken.None);

        var orderDoc = await Context.GetCollection<OrderDocument>(TwoPhaseCommitOrderRepository.OrdersCollectionName)
            .Find(Builders<OrderDocument>.Filter.Eq(d => d.Id, order.Id))
            .FirstOrDefaultAsync();

        Assert.NotNull(orderDoc);
        Assert.Equal(order.Id, orderDoc.Id);
        Assert.Equal(0, orderDoc.Version);
        Assert.Equal("Created", orderDoc.Status);

        var eventDocs = await Context.GetCollection<OrderEventDocument>(TwoPhaseCommitOrderRepository.OrderEventsCollectionName)
            .Find(Builders<OrderEventDocument>.Filter.Eq(d => d.AggregateId, order.Id))
            .ToListAsync();

        Assert.Single(eventDocs);
        Assert.Equal("OrderCreated", eventDocs[0].EventType);
        Assert.Equal(0, eventDocs[0].AggregateVersion);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOrderAndInsertsEvent_InTransaction()
    {
        if (IsMongoUnavailable()) return;

        var repo = new TwoPhaseCommitOrderRepository(Context);
        var order = OrderAggregate.Create();
        await repo.CreateAsync(order, CancellationToken.None);

        order.MarkReadyForFulfilment();
        await repo.UpdateAsync(order, CancellationToken.None);

        var orderDoc = await Context.GetCollection<OrderDocument>(TwoPhaseCommitOrderRepository.OrdersCollectionName)
            .Find(Builders<OrderDocument>.Filter.Eq(d => d.Id, order.Id))
            .FirstOrDefaultAsync();

        Assert.NotNull(orderDoc);
        Assert.Equal(1, orderDoc.Version);
        Assert.Equal("ReadyForFulfilment", orderDoc.Status);

        var eventDocs = await Context.GetCollection<OrderEventDocument>(TwoPhaseCommitOrderRepository.OrderEventsCollectionName)
            .Find(Builders<OrderEventDocument>.Filter.Eq(d => d.AggregateId, order.Id))
            .ToListAsync();

        Assert.Equal(2, eventDocs.Count);
        Assert.Contains(eventDocs, e => e.EventType == "OrderCreated");
        Assert.Contains(eventDocs, e => e.EventType == "OrderReadyForFulfilment");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedOrder()
    {
        if (IsMongoUnavailable()) return;

        var repo = new TwoPhaseCommitOrderRepository(Context);
        var order = OrderAggregate.Create();
        await repo.CreateAsync(order, CancellationToken.None);

        var retrieved = await repo.GetByIdAsync(order.Id, CancellationToken.None);

        Assert.Equal(order.Id, retrieved.Id);
        Assert.Equal(order.Version, retrieved.Version);
        Assert.Equal(order.Status, retrieved.Status);
    }
}
