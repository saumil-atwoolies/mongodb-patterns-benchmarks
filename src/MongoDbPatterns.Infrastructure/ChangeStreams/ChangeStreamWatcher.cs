using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDbPatterns.Infrastructure.ChangeStreams;

public sealed class ChangeStreamWatcher
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly string _collectionName;
    private long _eventsReceived;
    private DateTime _startTime;
    private CancellationTokenSource? _cts;
    private Task? _watchTask;

    public ChangeStreamWatcher(IMongoCollection<BsonDocument> collection, string collectionName)
    {
        _collection = collection;
        _collectionName = collectionName;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _startTime = DateTime.UtcNow;
        _eventsReceived = 0;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _watchTask = WatchAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task<ChangeStreamResult> StopAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        if (_watchTask != null)
        {
            try
            {
                await _watchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
        }

        var endTime = DateTime.UtcNow;

        return new ChangeStreamResult
        {
            CollectionName = _collectionName,
            StartTime = _startTime,
            EndTime = endTime,
            EventsReceived = Interlocked.Read(ref _eventsReceived)
        };
    }

    private async Task WatchAsync(CancellationToken ct)
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(change =>
                change.OperationType == ChangeStreamOperationType.Insert ||
                change.OperationType == ChangeStreamOperationType.Update ||
                change.OperationType == ChangeStreamOperationType.Replace);

        using var cursor = await _collection.WatchAsync(pipeline, cancellationToken: ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var _ in cursor.Current)
            {
                Interlocked.Increment(ref _eventsReceived);
            }
        }
    }
}
