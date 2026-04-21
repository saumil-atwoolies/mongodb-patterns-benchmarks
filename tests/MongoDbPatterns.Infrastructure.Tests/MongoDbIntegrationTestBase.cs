using MongoDB.Driver;
using MongoDbPatterns.Infrastructure.Configuration;
using MongoDbPatterns.Infrastructure.Persistence;

namespace MongoDbPatterns.Infrastructure.Tests;

public abstract class MongoDbIntegrationTestBase : IAsyncLifetime
{
    private const string TestConnectionString = "mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin";
    private const string TestDatabaseName = "MongoDbPatternsTests";

    private static bool? _mongoAvailable;

    protected MongoDbContext Context { get; private set; } = null!;

    protected bool IsMongoUnavailable() => _mongoAvailable == false;

    public async Task InitializeAsync()
    {
        var settings = new ConnectionSettings
        {
            ConnectionString = TestConnectionString,
            DatabaseName = $"{TestDatabaseName}_{Guid.NewGuid():N}"
        };

        Context = new MongoDbContext(settings);

        if (_mongoAvailable == false)
            return;

        try
        {
            // Ping to verify connectivity
            var pingCommand = new MongoDB.Bson.BsonDocument("ping", 1);
            await Context.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(pingCommand);
            _mongoAvailable = true;
        }
        catch
        {
            _mongoAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (Context?.Database != null && _mongoAvailable == true)
        {
            try
            {
                await Context.Client.DropDatabaseAsync(Context.Database.DatabaseNamespace.DatabaseName);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
