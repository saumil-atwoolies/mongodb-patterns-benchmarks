using System.Text.Json;

namespace MongoDbPatterns.Infrastructure.Configuration;

public sealed class ConnectionSettingsProvider
{
    private const string DefaultFileName = "connection-setting.local";

    private static readonly ConnectionSettings DefaultSettings = new()
    {
        ConnectionString = "mongodb://admin:N05%40ssword@localhost:27018/?replicaSet=rs0&authSource=admin",
        DatabaseName = "MongoDbPatterns"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public ConnectionSettingsProvider()
        : this(Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName))
    {
    }

    public ConnectionSettingsProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ConnectionSettings GetSettings()
    {
        if (!File.Exists(_filePath))
        {
            var json = JsonSerializer.Serialize(DefaultSettings, JsonOptions);
            File.WriteAllText(_filePath, json);
            return DefaultSettings;
        }

        var content = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<ConnectionSettings>(content, JsonOptions)
               ?? throw new InvalidOperationException($"Failed to deserialize connection settings from '{_filePath}'.");
    }
}
