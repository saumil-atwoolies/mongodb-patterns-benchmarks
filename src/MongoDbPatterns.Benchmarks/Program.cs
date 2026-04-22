using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Benchmarks.Runner;
using MongoDbPatterns.Benchmarks.Scenarios;
using MongoDbPatterns.Infrastructure.Configuration;
using MongoDbPatterns.Infrastructure.Persistence;

var settingsProvider = new ConnectionSettingsProvider();
var localSettings = settingsProvider.GetLocalSettings();
var config = BenchmarkConfig.FromEnvironment(localSettings);
Console.WriteLine("=== MongoDB Patterns & Benchmarks ===");
Console.WriteLine($"Load Size: {config.LoadSize} | Concurrency: {config.Concurrency} | Batch Size: {config.BatchSize}");
Console.WriteLine();

var connectionSettings = new ConnectionSettings
{
    ConnectionString = localSettings.ConnectionString,
    DatabaseName = localSettings.DatabaseName
};
var context = new MongoDbContext(connectionSettings);

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
