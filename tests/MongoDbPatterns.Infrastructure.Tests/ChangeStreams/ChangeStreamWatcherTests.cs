using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbPatterns.Infrastructure.ChangeStreams;

namespace MongoDbPatterns.Infrastructure.Tests.ChangeStreams;

public class ChangeStreamWatcherTests : MongoDbIntegrationTestBase
{
    [Fact]
    public async Task CountsInsertEvents()
    {
        if (IsMongoUnavailable()) return;

        var collectionName = $"TestChangeStream_{Guid.NewGuid():N}";
        var collection = Context.Database.GetCollection<BsonDocument>(collectionName);
        var watcher = new ChangeStreamWatcher(collection, collectionName);

        await watcher.StartAsync(CancellationToken.None);

        // Give the watcher a moment to establish the cursor
        await Task.Delay(500);

        const int insertCount = 5;
        for (var i = 0; i < insertCount; i++)
        {
            await collection.InsertOneAsync(new BsonDocument("value", i));
        }

        // Allow time for events to propagate
        await Task.Delay(1000);

        var result = await watcher.StopAsync();

        Assert.True(result.EventsReceived >= insertCount,
            $"Expected at least {insertCount} events but got {result.EventsReceived}");
    }

    [Fact]
    public async Task RecordsStartAndEndTimes()
    {
        if (IsMongoUnavailable()) return;

        var collectionName = $"TestChangeStream_{Guid.NewGuid():N}";
        var collection = Context.Database.GetCollection<BsonDocument>(collectionName);
        var watcher = new ChangeStreamWatcher(collection, collectionName);

        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        var result = await watcher.StopAsync();

        Assert.True(result.StartTime < result.EndTime,
            $"StartTime ({result.StartTime}) should be before EndTime ({result.EndTime})");
    }

    [Fact]
    public async Task StopAsync_ReturnsAccumulatedCount()
    {
        if (IsMongoUnavailable()) return;

        var collectionName = $"TestChangeStream_{Guid.NewGuid():N}";
        var collection = Context.Database.GetCollection<BsonDocument>(collectionName);
        var watcher = new ChangeStreamWatcher(collection, collectionName);

        await watcher.StartAsync(CancellationToken.None);
        await Task.Delay(500);

        const int insertCount = 10;
        for (var i = 0; i < insertCount; i++)
        {
            await collection.InsertOneAsync(new BsonDocument("value", i));
        }

        await Task.Delay(1000);
        var result = await watcher.StopAsync();

        Assert.True(result.EventsReceived >= insertCount,
            $"Expected at least {insertCount} events but got {result.EventsReceived}");
        Assert.Equal(collectionName, result.CollectionName);
    }
}
