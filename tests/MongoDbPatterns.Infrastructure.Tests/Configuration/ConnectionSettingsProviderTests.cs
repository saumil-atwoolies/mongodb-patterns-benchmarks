using System.Text.Json;
using MongoDbPatterns.Infrastructure.Configuration;

namespace MongoDbPatterns.Infrastructure.Tests.Configuration;

public class ConnectionSettingsProviderTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public ConnectionSettingsProviderTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"MongoDbPatternsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "connection-setting.local");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void CreatesDefaultFile_WhenFileDoesNotExist()
    {
        var provider = new ConnectionSettingsProvider(_filePath);

        var settings = provider.GetSettings();

        Assert.True(File.Exists(_filePath));
        Assert.Equal("mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin", settings.ConnectionString);
        Assert.Equal("MongoDbPatterns", settings.DatabaseName);
    }

    [Fact]
    public void ReadsExistingFile_WhenFileExists()
    {
        var custom = new ConnectionSettings
        {
            ConnectionString = "mongodb://custom:password@otherhost:27017",
            DatabaseName = "CustomDb"
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(custom, new JsonSerializerOptions { WriteIndented = true }));

        var provider = new ConnectionSettingsProvider(_filePath);

        var settings = provider.GetSettings();

        Assert.Equal("mongodb://custom:password@otherhost:27017", settings.ConnectionString);
        Assert.Equal("CustomDb", settings.DatabaseName);
    }

    [Fact]
    public void DoesNotOverwriteExistingFile()
    {
        var custom = new ConnectionSettings
        {
            ConnectionString = "mongodb://existing:pass@host:12345",
            DatabaseName = "ExistingDb"
        };
        var originalContent = JsonSerializer.Serialize(custom, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, originalContent);

        var provider = new ConnectionSettingsProvider(_filePath);
        provider.GetSettings();

        var afterContent = File.ReadAllText(_filePath);
        Assert.Equal(originalContent, afterContent);
    }
}
