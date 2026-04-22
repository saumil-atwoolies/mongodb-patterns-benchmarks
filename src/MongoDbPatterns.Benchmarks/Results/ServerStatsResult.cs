namespace MongoDbPatterns.Benchmarks.Results;

public sealed record ServerStatsResult
{
    public required double AverageResidentMemoryMb { get; init; }
    public required double PeakResidentMemoryMb { get; init; }
    public required double AverageCpuPercent { get; init; }
    public required int SampleCount { get; init; }
}
