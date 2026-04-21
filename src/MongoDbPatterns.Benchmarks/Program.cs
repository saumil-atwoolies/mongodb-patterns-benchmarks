using MongoDbPatterns.Benchmarks.Configuration;
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

Console.WriteLine("\n=== Benchmark Complete ===");
Console.WriteLine($"Scenarios executed: {results.Count}");
