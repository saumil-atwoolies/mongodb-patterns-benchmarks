using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Benchmarks.Runner;
using MongoDbPatterns.Benchmarks.Scenarios;
using MongoDbPatterns.Infrastructure.Configuration;
using MongoDbPatterns.Infrastructure.Persistence;

var config = BenchmarkConfig.FromEnvironment();
Console.WriteLine("=== MongoDB Patterns & Benchmarks ===");
Console.WriteLine($"Load Size: {config.LoadSize} | Concurrency: {config.Concurrency} | Batch Size: {config.BatchSize}");
Console.WriteLine();

var settingsProvider = new ConnectionSettingsProvider();
var settings = settingsProvider.GetSettings();
var context = new MongoDbContext(settings);

IBenchmarkScenario[] scenarios =
[
    new TwoPhaseCommitScenario(context),
    new EmbeddedEventsScenario(context)
];

var runner = new BenchmarkRunner(scenarios);
var results = await runner.RunAllAsync(config, CancellationToken.None);

// Persist report to file when RESULTS_PATH is set or /app/results exists (Docker volume mount)
var resultsDir = Environment.GetEnvironmentVariable("RESULTS_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "results");
if (Directory.Exists(resultsDir))
{
    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
    var reportPath = Path.Combine(resultsDir, $"benchmark-{timestamp}.txt");
    var report = StatisticsFormatter.Format(config, results);
    await File.WriteAllTextAsync(reportPath, report);
    Console.WriteLine($"Report saved to: {reportPath}");
}

Console.WriteLine("\n=== Benchmark Complete ===");
Console.WriteLine($"Scenarios executed: {results.Count}");
