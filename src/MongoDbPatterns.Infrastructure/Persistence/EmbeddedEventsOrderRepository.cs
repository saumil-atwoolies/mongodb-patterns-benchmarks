using MongoDB.Driver;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Domain.Events;
using MongoDbPatterns.Domain.Exceptions;
using MongoDbPatterns.Domain.Repositories;
using MongoDbPatterns.Infrastructure.Persistence.Documents;

namespace MongoDbPatterns.Infrastructure.Persistence;

public sealed class EmbeddedEventsOrderRepository : IOrderRepository
{
    public const string CollectionName = "OrdersEmbedded";

    private readonly IMongoCollection<EmbeddedOrderDocument> _orders;

    public EmbeddedEventsOrderRepository(MongoDbContext context)
    {
        _orders = context.GetCollection<EmbeddedOrderDocument>(CollectionName);
    }

    public async Task CreateAsync(OrderAggregate order, CancellationToken ct)
    {
        var doc = new EmbeddedOrderDocument
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            Version = order.Version,
            DomainEvents = order.DomainEvents.Select(ToEmbeddedEvent).ToList()
        };

        await _orders.InsertOneAsync(doc, cancellationToken: ct);
        order.ClearEvents();
    }

    public async Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var filter = Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Id, id);
        var doc = await _orders.Find(filter).FirstOrDefaultAsync(ct)
                  ?? throw new InvalidOperationException($"Order with id '{id}' not found.");
        return ToAggregate(doc);
    }

    public async Task UpdateAsync(OrderAggregate order, CancellationToken ct)
    {
        var expectedVersion = order.Version - 1;

        var filter = Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Id, order.Id)
                     & Builders<EmbeddedOrderDocument>.Filter.Eq(d => d.Version, expectedVersion);

        var eventDocs = order.DomainEvents.Select(ToEmbeddedEvent).ToList();

        var update = Builders<EmbeddedOrderDocument>.Update
            .Set(d => d.Status, order.Status.ToString())
            .Set(d => d.Version, order.Version)
            .PushEach(d => d.DomainEvents, eventDocs);

        var result = await _orders.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConcurrencyException(order.Id, expectedVersion);

        order.ClearEvents();
    }

    private static EmbeddedEventDocument ToEmbeddedEvent(DomainEvent evt) => new()
    {
        EventId = evt.EventId,
        AggregateVersion = evt.AggregateVersion,
        EventType = evt.GetType().Name,
        OccurredAt = evt.OccurredAt
    };

    private static OrderAggregate ToAggregate(EmbeddedOrderDocument doc)
    {
        var status = Enum.Parse<OrderStatus>(doc.Status);
        return OrderAggregate.Reconstitute(doc.Id, doc.Version, status);
    }
}
