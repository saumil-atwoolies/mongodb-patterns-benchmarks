using MongoDbPatterns.Domain.Aggregates;

namespace MongoDbPatterns.Domain.Repositories;

public interface IOrderRepository
{
    Task CreateAsync(OrderAggregate order, CancellationToken ct);
    Task<OrderAggregate> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(OrderAggregate order, CancellationToken ct);
}
