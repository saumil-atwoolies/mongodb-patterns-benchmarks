using System.Text.Json;

namespace MongoDbPatterns.Infrastructure.Configuration;

public sealed class ConnectionSettingsProvider
{
    private const string DefaultFileName = "connection-setting.local";

    private static readonly ConnectionSettings DefaultSettings = new()
    {
        ConnectionString = "mongodb://localhost:27018/?directConnection=true",
        DatabaseName = "MongoDbPatterns"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly string _filePath;

    /// <summary>
    /// The detected solution root directory, or null when running outside a solution tree (e.g. Docker).
    /// </summary>
    public string? SolutionRoot { get; }

    public ConnectionSettingsProvider()
    {
        SolutionRoot = FindSolutionRoot();
        _filePath = Path.Combine(SolutionRoot ?? Directory.GetCurrentDirectory(), DefaultFileName);
    }

    public ConnectionSettingsProvider(string filePath)
    {
        _filePath = filePath;
    }

    private static string? FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.slnx").Any() || dir.EnumerateFiles("*.sln").Any())
                return dir.FullName;
            dir = dir.Parent;
        }

        // No solution file found (e.g., Docker container)
        return null;
    }

    public ConnectionSettings GetSettings()
    {
        var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            return new ConnectionSettings
            {
                ConnectionString = envConnectionString,
                DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? DefaultSettings.DatabaseName
            };
        }

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
