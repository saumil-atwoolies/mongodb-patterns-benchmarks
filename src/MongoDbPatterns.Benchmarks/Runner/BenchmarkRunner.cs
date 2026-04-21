using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Benchmarks.Scenarios;

namespace MongoDbPatterns.Benchmarks.Runner;

public sealed class BenchmarkRunner
{
    private readonly IReadOnlyList<IBenchmarkScenario> _scenarios;

    public BenchmarkRunner(IReadOnlyList<IBenchmarkScenario> scenarios)
    {
        _scenarios = scenarios;
    }

    public async Task<List<ScenarioResult>> RunAllAsync(BenchmarkConfig config, CancellationToken ct)
    {
        var results = new List<ScenarioResult>();

        foreach (var scenario in _scenarios)
        {
            Console.WriteLine($"\n--- Running: {scenario.Name} ---\n");
            var result = await scenario.RunAsync(config, ct);
            results.Add(result);
        }

        return results;
    }
}
