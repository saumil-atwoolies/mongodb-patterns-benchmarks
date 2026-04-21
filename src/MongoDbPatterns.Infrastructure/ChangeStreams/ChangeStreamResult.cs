namespace MongoDbPatterns.Infrastructure.ChangeStreams;

public sealed record ChangeStreamResult
{
    public required string CollectionName { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required long EventsReceived { get; init; }
}
