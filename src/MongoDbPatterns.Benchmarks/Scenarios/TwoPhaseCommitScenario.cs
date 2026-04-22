using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Domain.Aggregates;
using MongoDbPatterns.Infrastructure.ChangeStreams;
using MongoDbPatterns.Infrastructure.Persistence;

namespace MongoDbPatterns.Benchmarks.Scenarios;

public sealed class TwoPhaseCommitScenario : IBenchmarkScenario
{
    private readonly MongoDbContext _context;

    public TwoPhaseCommitScenario(MongoDbContext context)
    {
        _context = context;
    }

    public string Name => "Case 1: Two-Phase Commit Outbox";

    public async Task<ScenarioResult> RunAsync(BenchmarkConfig config, CancellationToken ct)
    {
        // Pre-create collections to avoid concurrent implicit creation conflicts in transactions
        var db = _context.Database;
        var existingNames = await (await db.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(cancellationToken: ct);
        if (!existingNames.Contains(TwoPhaseCommitOrderRepository.OrdersCollectionName))
            await db.CreateCollectionAsync(TwoPhaseCommitOrderRepository.OrdersCollectionName, cancellationToken: ct);
        if (!existingNames.Contains(TwoPhaseCommitOrderRepository.OrderEventsCollectionName))
            await db.CreateCollectionAsync(TwoPhaseCommitOrderRepository.OrderEventsCollectionName, cancellationToken: ct);

        var eventsWatcher = new ChangeStreamWatcher(
            _context.Database.GetCollection<BsonDocument>(TwoPhaseCommitOrderRepository.OrderEventsCollectionName),
            TwoPhaseCommitOrderRepository.OrderEventsCollectionName);

        var statsCollector = new ServerStatsCollector(_context.Database);

        await eventsWatcher.StartAsync(ct);

        // Allow watchers to establish cursors
        await Task.Delay(500, ct);

        await statsCollector.StartAsync(ct);

        var startTime = DateTime.UtcNow;
        var repo = new TwoPhaseCommitOrderRepository(_context);

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

        var serverStats = await statsCollector.StopAsync();

        // Allow change stream to drain
        await Task.Delay(2000, ct);

        var eventsResult = await eventsWatcher.StopAsync();

        return new ScenarioResult
        {
            ScenarioName = Name,
            StartTime = startTime,
            EndTime = endTime,
            TotalOperations = config.LoadSize * 2, // create + update
            ChangeStreamResults = [eventsResult],
            ServerStats = serverStats
        };
    }
}
