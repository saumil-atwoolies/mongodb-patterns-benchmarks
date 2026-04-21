using MongoDbPatterns.Benchmarks.Configuration;

namespace MongoDbPatterns.Domain.Tests.Benchmarks;

public class BenchmarkConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new BenchmarkConfig();

        Assert.Equal(1000, config.LoadSize);
        Assert.Equal(5, config.Concurrency);
        Assert.Equal(1, config.BatchSize);
    }

    [Fact]
    public void ParsesEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("LOAD_SIZE", "500");
        Environment.SetEnvironmentVariable("CONCURRENCY", "10");
        Environment.SetEnvironmentVariable("BATCH_SIZE", "5");

        try
        {
            var config = BenchmarkConfig.FromEnvironment();

            Assert.Equal(500, config.LoadSize);
            Assert.Equal(10, config.Concurrency);
            Assert.Equal(5, config.BatchSize);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOAD_SIZE", null);
            Environment.SetEnvironmentVariable("CONCURRENCY", null);
            Environment.SetEnvironmentVariable("BATCH_SIZE", null);
        }
    }
}
