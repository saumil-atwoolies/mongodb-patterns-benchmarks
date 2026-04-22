namespace MongoDbPatterns.Infrastructure.Configuration;

public sealed record LocalSettings
{
    public string ConnectionString { get; init; } = "mongodb://localhost:27018/?directConnection=true";
    public string DatabaseName { get; init; } = "MongoDbPatterns";
    public int LoadSize { get; init; } = 1000;
    public int Concurrency { get; init; } = 5;
    public int BatchSize { get; init; } = 1;
    public bool ReportServerStats { get; init; } = true;
}
