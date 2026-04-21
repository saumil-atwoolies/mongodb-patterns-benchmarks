using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;

namespace MongoDbPatterns.Benchmarks.Scenarios;

public interface IBenchmarkScenario
{
    string Name { get; }
    Task<ScenarioResult> RunAsync(BenchmarkConfig config, CancellationToken ct);
}
