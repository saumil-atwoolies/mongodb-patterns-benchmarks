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

// Persist report to file
var resultsDir = Environment.GetEnvironmentVariable("RESULTS_PATH");
if (string.IsNullOrEmpty(resultsDir))
{
    // Look for results/ at the solution root (Visual Studio / dotnet run)
    var solutionRoot = settingsProvider.SolutionRoot;
    if (solutionRoot != null)
        resultsDir = Path.Combine(solutionRoot, "results");
    else
        resultsDir = Path.Combine(AppContext.BaseDirectory, "results");
}

Directory.CreateDirectory(resultsDir);
var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
var reportPath = Path.Combine(resultsDir, $"benchmark-{timestamp}.txt");
var report = StatisticsFormatter.Format(config, results);
await File.WriteAllTextAsync(reportPath, report);
Console.WriteLine($"Report saved to: {reportPath}");

Console.WriteLine("\n=== Benchmark Complete ===");
Console.WriteLine($"Scenarios executed: {results.Count}");
