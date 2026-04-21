using MongoDB.Driver;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Domain.Exceptions;
using MongoDbPatterns.Infrastructure.Persistence;
using MongoDbPatterns.Infrastructure.Persistence.Documents;

namespace MongoDbPatterns.Infrastructure.Tests.Persistence;

public class EmbeddedEventsOrderRepositoryTests : MongoDbIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_InsertsOrderWithEmbeddedEvent()
    {
        if (IsMongoUnavailable()) return;

        var repo = new EmbeddedEventsOrderRepository(Context);
        var order = OrderAggregate.Create();

        await repo.CreateAsync(order, CancellationToken.None);

        var doc = await Context.GetCollection<EmbeddedOrderDocument>(EmbeddedEventsOrderRepository.CollectionName)
            .Find(Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Id, order.Id))
            .FirstOrDefaultAsync();

        Assert.NotNull(doc);
        Assert.Equal(0, doc.Version);
        Assert.Single(doc.DomainEvents);
        Assert.Equal("OrderCreated", doc.DomainEvents[0].EventType);
    }

    [Fact]
    public async Task UpdateAsync_AppendsEventAndIncrementsVersion()
    {
        if (IsMongoUnavailable()) return;

        var repo = new EmbeddedEventsOrderRepository(Context);
        var order = OrderAggregate.Create();
        await repo.CreateAsync(order, CancellationToken.None);

        order.MarkReadyForFulfilment();
        await repo.UpdateAsync(order, CancellationToken.None);

        var doc = await Context.GetCollection<EmbeddedOrderDocument>(EmbeddedEventsOrderRepository.CollectionName)
            .Find(Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Id, order.Id))
            .FirstOrDefaultAsync();

        Assert.NotNull(doc);
        Assert.Equal(1, doc.Version);
        Assert.Equal(2, doc.DomainEvents.Count);
        Assert.Equal("OrderCreated", doc.DomainEvents[0].EventType);
        Assert.Equal("OrderReadyForFulfilment", doc.DomainEvents[1].EventType);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsConcurrencyException_OnVersionMismatch()
    {
        if (IsMongoUnavailable()) return;

        var repo = new EmbeddedEventsOrderRepository(Context);
        var order = OrderAggregate.Create();
        await repo.CreateAsync(order, CancellationToken.None);

        // Simulate a concurrent modification by directly updating the version in the database
        var filter = Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Id, order.Id);
        var tamper = Builders<EmbeddedOrderDocument>.Update.Set(d => d.Version, 99);
        await Context.GetCollection<EmbeddedOrderDocument>(EmbeddedEventsOrderRepository.CollectionName)
            .UpdateOneAsync(filter, tamper);

        order.MarkReadyForFulfilment();

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => repo.UpdateAsync(order, CancellationToken.None));
    }
}
