using System.Text;
using MongoDbPatterns.Benchmarks.Configuration;

namespace MongoDbPatterns.Benchmarks.Results;

public static class StatisticsFormatter
{
    public static string Format(BenchmarkConfig config, IReadOnlyList<ScenarioResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           MongoDB Patterns & Benchmarks — Results           ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // Configuration header
        sb.AppendLine("┌─ Configuration ─────────────────────────────────────────────┐");
        sb.AppendLine($"│  Load Size:   {config.LoadSize,10}                                │");
        sb.AppendLine($"│  Concurrency: {config.Concurrency,10}                                │");
        sb.AppendLine($"│  Batch Size:  {config.BatchSize,10}                                │");
        sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
        sb.AppendLine();

        foreach (var result in results)
        {
            sb.AppendLine($"┌─ {result.ScenarioName} {"─".PadRight(Math.Max(1, 57 - result.ScenarioName.Length), '─')}┐");
            sb.AppendLine($"│  Start Time:       {result.StartTime:yyyy-MM-dd HH:mm:ss.fff} UTC           │");
            sb.AppendLine($"│  End Time:         {result.EndTime:yyyy-MM-dd HH:mm:ss.fff} UTC           │");
            sb.AppendLine($"│  Duration:         {result.Duration.TotalSeconds,10:F3} seconds                  │");
            sb.AppendLine($"│  Total Operations: {result.TotalOperations,10}                          │");
            sb.AppendLine($"│  Throughput:       {result.ThroughputOpsPerSec,10:F2} ops/sec                  │");

            if (result.ChangeStreamResults.Count > 0)
            {
                sb.AppendLine("│                                                             │");
                sb.AppendLine("│  Change Streams:                                             │");

                foreach (var cs in result.ChangeStreamResults)
                {
                    sb.AppendLine($"│    [{cs.CollectionName}]");
                    sb.AppendLine($"│      Start:  {cs.StartTime:HH:mm:ss.fff} UTC                              │");
                    sb.AppendLine($"│      End:    {cs.EndTime:HH:mm:ss.fff} UTC                              │");
                    sb.AppendLine($"│      Events: {cs.EventsReceived,10}                                │");
                }
            }

            sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
