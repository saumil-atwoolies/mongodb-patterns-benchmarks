using MongoDB.Bson;
using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Infrastructure.ChangeStreams;
using MongoDbPatterns.Infrastructure.Persistence;

namespace MongoDbPatterns.Benchmarks.Scenarios;

public sealed class EmbeddedEventsScenario : IBenchmarkScenario
{
    private readonly MongoDbContext _context;

    public EmbeddedEventsScenario(MongoDbContext context)
    {
        _context = context;
    }

    public string Name => "Case 2: Embedded Events Outbox";

    public async Task<ScenarioResult> RunAsync(BenchmarkConfig config, CancellationToken ct)
    {
        var watcher = new ChangeStreamWatcher(
            _context.Database.GetCollection<BsonDocument>(EmbeddedEventsOrderRepository.CollectionName),
            EmbeddedEventsOrderRepository.CollectionName);

        await watcher.StartAsync(ct);

        // Allow watcher to establish cursor
        await Task.Delay(500, ct);

        var startTime = DateTime.UtcNow;
        var repo = new EmbeddedEventsOrderRepository(_context);

        // Phase 1: Create orders concurrently
        var orders = new OrderAggregate[config.LoadSize];
        await Parallel.ForAsync(0, config.LoadSize, new ParallelOptions
        {
            MaxDegreeOfParallelism = config.Concurrency,
            CancellationToken = ct
        }, async (i, token) =>
        {
            var order = OrderAggregate.Create();
            orders[i] = order;
            await repo.CreateAsync(order, token);
        });

        // Phase 2: Update orders concurrently
        await Parallel.ForAsync(0, config.LoadSize, new ParallelOptions
        {
            MaxDegreeOfParallelism = config.Concurrency,
            CancellationToken = ct
        }, async (i, token) =>
        {
            orders[i].MarkReadyForFulfilment();
            await repo.UpdateAsync(orders[i], token);
        });

        var endTime = DateTime.UtcNow;

        // Allow change stream to drain
        await Task.Delay(2000, ct);

        var watcherResult = await watcher.StopAsync();

        return new ScenarioResult
        {
            ScenarioName = Name,
            StartTime = startTime,
            EndTime = endTime,
            TotalOperations = config.LoadSize * 2, // create + update
            ChangeStreamResults = [watcherResult]
        };
    }
}
