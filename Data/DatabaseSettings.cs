using System.Text.Json;
using System.Text.RegularExpressions;

namespace ElevatorSystem.Data;

/// <summary>
/// Which SQL Server and database the log uses. Read from appsettings.json in the application folder
/// (Application.StartupPath). Falls back to LocalDB defaults when the file is missing or unreadable.
/// </summary>
public sealed class DatabaseSettings
{
    public const string FileName = "appsettings.json";
    public const string DefaultServer = @"(localdb)\MSSQLLocalDB";
    public const string DefaultName = "ElevatorSystem";

    private DatabaseSettings(string server, string name, string? warning)
    {
        Server = server;
        Name = name;
        Warning = warning;
    }

    public string Server { get; }

    /// <summary>Database name; restricted to letters, digits and underscores because it is used in CREATE DATABASE.</summary>
    public string Name { get; }

    /// <summary>Set when the settings file could not be used and the defaults were taken instead.</summary>
    public string? Warning { get; }

    public static DatabaseSettings Load()
    {
        string path = Path.Combine(Application.StartupPath, FileName);
        try
        {
            if (!File.Exists(path))
            {
                return new DatabaseSettings(DefaultServer, DefaultName, null);
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement database = document.RootElement.GetProperty("Database");
            string server = database.TryGetProperty("Server", out JsonElement s) ? s.GetString() ?? DefaultServer : DefaultServer;
            string name = database.TryGetProperty("Name", out JsonElement n) ? n.GetString() ?? DefaultName : DefaultName;

            if (!Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]{0,99}$"))
            {
                return new DatabaseSettings(server, DefaultName,
                    $"The database name '{name}' in {FileName} is not valid (letters, digits and _ only). Using '{DefaultName}'.");
            }

            return new DatabaseSettings(server, name, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                       or KeyNotFoundException or InvalidOperationException)
        {
            return new DatabaseSettings(DefaultServer, DefaultName,
                $"{FileName} could not be read ({ex.Message}). Using {DefaultServer}, database '{DefaultName}'.");
        }
    }
}
