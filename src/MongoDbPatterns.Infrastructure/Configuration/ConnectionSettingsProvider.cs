using System.Text.Json;

namespace MongoDbPatterns.Infrastructure.Configuration;

public sealed class ConnectionSettingsProvider
{
    private const string DefaultFileName = "settings.local.json";

    private static readonly LocalSettings DefaultLocalSettings = new();

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

    /// <summary>
    /// Returns all local settings from the JSON file (or defaults).
    /// Environment variables for CONNECTION_STRING / DATABASE_NAME override the file values.
    /// </summary>
    public LocalSettings GetLocalSettings()
    {
        var local = ReadOrCreateFile();

        var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            local = local with
            {
                ConnectionString = envConnectionString,
                DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? local.DatabaseName
            };
        }

        return local;
    }

    /// <summary>
    /// Returns connection settings only. Kept for backward compatibility.
    /// </summary>
    public ConnectionSettings GetSettings()
    {
        var local = GetLocalSettings();
        return new ConnectionSettings
        {
            ConnectionString = local.ConnectionString,
            DatabaseName = local.DatabaseName
        };
    }

    private LocalSettings ReadOrCreateFile()
    {
        if (!File.Exists(_filePath))
        {
            var json = JsonSerializer.Serialize(DefaultLocalSettings, JsonOptions);
            File.WriteAllText(_filePath, json);
            return DefaultLocalSettings;
        }

        var content = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<LocalSettings>(content, JsonOptions)
               ?? throw new InvalidOperationException($"Failed to deserialize settings from '{_filePath}'.");
    }
}
