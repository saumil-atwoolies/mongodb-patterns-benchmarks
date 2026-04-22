using MongoDbPatterns.Infrastructure.Configuration;

namespace MongoDbPatterns.Benchmarks.Configuration;

public sealed record BenchmarkConfig
{
    public int LoadSize { get; init; } = 1000;
    public int Concurrency { get; init; } = 5;
    public int BatchSize { get; init; } = 1;
    public bool ReportServerStats { get; init; } = true;

    /// <summary>
    /// Creates config from environment variables, using local settings as defaults
    /// when env vars are not set (Visual Studio / dotnet run).
    /// </summary>
    public static BenchmarkConfig FromEnvironment(LocalSettings? localSettings = null)
    {
        var defaults = localSettings ?? new LocalSettings();
        return new BenchmarkConfig
        {
            LoadSize = GetEnvInt("LOAD_SIZE", defaults.LoadSize),
            Concurrency = GetEnvInt("CONCURRENCY", defaults.Concurrency),
            BatchSize = GetEnvInt("BATCH_SIZE", defaults.BatchSize),
            ReportServerStats = GetEnvBool("REPORT_SERVER_STATS", defaults.ReportServerStats)
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
