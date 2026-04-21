using MongoDbPatterns.Infrastructure.ChangeStreams;

namespace MongoDbPatterns.Benchmarks.Results;

public sealed record ScenarioResult
{
    public required string ScenarioName { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public required int TotalOperations { get; init; }
    public double ThroughputOpsPerSec => TotalOperations / Duration.TotalSeconds;
    public required List<ChangeStreamResult> ChangeStreamResults { get; init; }
}
