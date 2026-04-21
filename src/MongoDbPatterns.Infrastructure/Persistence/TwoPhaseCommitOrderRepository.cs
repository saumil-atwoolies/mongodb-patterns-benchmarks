using MongoDB.Driver;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Domain.Events;
using MongoDbPatterns.Domain.Repositories;
using MongoDbPatterns.Infrastructure.Persistence.Documents;

namespace MongoDbPatterns.Infrastructure.Persistence;

public sealed class TwoPhaseCommitOrderRepository : IOrderRepository
{
    public const string OrdersCollectionName = "Orders";
    public const string OrderEventsCollectionName = "OrderEvents";

    private readonly MongoDbContext _context;
    private readonly IMongoCollection<OrderDocument> _orders;
    private readonly IMongoCollection<OrderEventDocument> _orderEvents;

    public TwoPhaseCommitOrderRepository(MongoDbContext context)
    {
        _context = context;
        _orders = context.GetCollection<OrderDocument>(OrdersCollectionName);
        _orderEvents = context.GetCollection<OrderEventDocument>(OrderEventsCollectionName);
    }

    public async Task CreateAsync(OrderAggregate order, CancellationToken ct)
    {
        using var session = await _context.Client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();

        try
        {
            var orderDoc = ToOrderDocument(order);
            await _orders.InsertOneAsync(session, orderDoc, cancellationToken: ct);

            var eventDocs = order.DomainEvents.Select(ToEventDocument).ToList();
            if (eventDocs.Count > 0)
                await _orderEvents.InsertManyAsync(session, eventDocs, cancellationToken: ct);

            await session.CommitTransactionAsync(ct);
            order.ClearEvents();
        }
        catch
        {
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(ct);
            throw;
        }
    }

    public async Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var filter = Builders<OrderDocument>.Filter.Eq(d => d.Id, id);
        var doc = await _orders.Find(filter).FirstOrDefaultAsync(ct)
                  ?? throw new InvalidOperationException($"Order with id '{id}' not found.");
        return ToAggregate(doc);
    }

    public async Task UpdateAsync(OrderAggregate order, CancellationToken ct)
    {
        using var session = await _context.Client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();

        try
        {
            var filter = Builders<OrderDocument>.Filter.Eq(d => d.Id, order.Id);
            var update = Builders<OrderDocument>.Update
                .Set(d => d.Status, order.Status.ToString())
                .Set(d => d.Version, order.Version);

            await _orders.UpdateOneAsync(session, filter, update, cancellationToken: ct);

            var eventDocs = order.DomainEvents.Select(ToEventDocument).ToList();
            if (eventDocs.Count > 0)
                await _orderEvents.InsertManyAsync(session, eventDocs, cancellationToken: ct);

            await session.CommitTransactionAsync(ct);
            order.ClearEvents();
        }
        catch
        {
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(ct);
            throw;
        }
    }

    private static OrderDocument ToOrderDocument(OrderAggregate order) => new()
    {
        Id = order.Id,
        Status = order.Status.ToString(),
        Version = order.Version
    };

    private static OrderEventDocument ToEventDocument(DomainEvent evt) => new()
    {
        EventId = evt.EventId,
        AggregateId = evt.AggregateId,
        AggregateVersion = evt.AggregateVersion,
        EventType = evt.GetType().Name,
        OccurredAt = evt.OccurredAt
    };

    private static OrderAggregate ToAggregate(OrderDocument doc)
    {
        var status = Enum.Parse<OrderStatus>(doc.Status);
        return OrderAggregate.Reconstitute(doc.Id, doc.Version, status);
    }
}
