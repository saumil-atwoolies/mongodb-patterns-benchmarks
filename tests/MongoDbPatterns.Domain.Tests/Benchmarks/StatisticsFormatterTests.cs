using MongoDbPatterns.Benchmarks.Configuration;
using MongoDbPatterns.Benchmarks.Results;
using MongoDbPatterns.Infrastructure.ChangeStreams;

namespace MongoDbPatterns.Domain.Tests.Benchmarks;

public class StatisticsFormatterTests
{
    private static BenchmarkConfig CreateConfig() => new()
    {
        LoadSize = 500,
        Concurrency = 10,
        BatchSize = 2
    };

    private static ScenarioResult CreateResult() => new()
    {
        ScenarioName = "Test Scenario",
        StartTime = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc),
        EndTime = new DateTime(2026, 4, 21, 10, 0, 5, DateTimeKind.Utc),
        TotalOperations = 1000,
        ChangeStreamResults =
        [
            new ChangeStreamResult
            {
                CollectionName = "TestCollection",
                StartTime = new DateTime(2026, 4, 21, 9, 59, 59, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 21, 10, 0, 7, DateTimeKind.Utc),
                EventsReceived = 950
            }
        ]
    };

    [Fact]
    public void FormatsConfigurationHeader()
    {
        var config = CreateConfig();
        var results = new List<ScenarioResult> { CreateResult() };

        var output = StatisticsFormatter.Format(config, results);

        Assert.Contains("500", output);
        Assert.Contains("10", output);
        Assert.Contains("2", output);
        Assert.Contains("Configuration", output);
    }

    [Fact]
    public void FormatsScenarioResult()
    {
        var config = CreateConfig();
        var results = new List<ScenarioResult> { CreateResult() };

        var output = StatisticsFormatter.Format(config, results);

        Assert.Contains("Test Scenario", output);
        Assert.Contains("1000", output);
        Assert.Contains("ops/sec", output);
        Assert.Contains("Duration", output);
    }

    [Fact]
    public void FormatsChangeStreamStats()
    {
        var config = CreateConfig();
        var results = new List<ScenarioResult> { CreateResult() };

        var output = StatisticsFormatter.Format(config, results);

        Assert.Contains("TestCollection", output);
        Assert.Contains("950", output);
        Assert.Contains("Change Streams", output);
    }
}
