namespace MongoDbPatterns.Benchmarks.Configuration;

public sealed record BenchmarkConfig
{
    public int LoadSize { get; init; } = 1000;
    public int Concurrency { get; init; } = 5;
    public int BatchSize { get; init; } = 1;
    public bool ReportServerStats { get; init; } = true;

    public static BenchmarkConfig FromEnvironment()
    {
        return new BenchmarkConfig
        {
            LoadSize = GetEnvInt("LOAD_SIZE", 1000),
            Concurrency = GetEnvInt("CONCURRENCY", 5),
            BatchSize = GetEnvInt("BATCH_SIZE", 1),
            ReportServerStats = GetEnvBool("REPORT_SERVER_STATS", true)
        };
    }

    private static int GetEnvInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetEnvBool(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
