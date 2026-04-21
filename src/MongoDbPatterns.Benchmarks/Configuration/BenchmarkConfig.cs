namespace MongoDbPatterns.Benchmarks.Configuration;

public sealed record BenchmarkConfig
{
    public int LoadSize { get; init; } = 1000;
    public int Concurrency { get; init; } = 5;
    public int BatchSize { get; init; } = 1;

    public static BenchmarkConfig FromEnvironment()
    {
        return new BenchmarkConfig
        {
            LoadSize = GetEnvInt("LOAD_SIZE", 1000),
            Concurrency = GetEnvInt("CONCURRENCY", 5),
            BatchSize = GetEnvInt("BATCH_SIZE", 1)
        };
    }

    private static int GetEnvInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
