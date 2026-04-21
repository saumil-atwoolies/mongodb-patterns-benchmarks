namespace MongoDbPatterns.Infrastructure.Configuration;

public sealed record ConnectionSettings
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
}
