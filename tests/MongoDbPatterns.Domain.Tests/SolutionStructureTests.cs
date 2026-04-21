using MongoDbPatterns.Domain.Aggregates;

namespace MongoDbPatterns.Domain.Tests;

public class SolutionStructureTests
{
    [Fact]
    public void SolutionBuilds_Successfully()
    {
        // Verify solution structure is intact by instantiating a domain type
        var order = OrderAggregate.Create();
        Assert.NotNull(order);
    }
}
