using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AllByMyshelf.Api.Infrastructure.Configuration;

/// <summary>
/// Configuration provider that loads settings from the app_settings database table.
/// Uses raw Npgsql connection to avoid circular DI with EF Core.
/// </summary>
[ExcludeFromCodeCoverage] // Requires a live PostgreSQL database — tested via integration/manual testing.
public class DbConfigurationProvider(string connectionString) : ConfigurationProvider
{
    private readonly string _connectionString = connectionString;

    public override void Load()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT key, value FROM app_settings";

            using var reader = command.ExecuteReader();
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);
                data[key] = value;
            }

            Data = data;
        }
        catch
        {
            // Table might not exist yet (first run before migration)
            // Return empty data and let user-secrets/appsettings take precedence
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Reloads the configuration from the database.
    /// Call this after updating settings to propagate changes.
    /// </summary>
    public void Reload()
    {
        Load();
        OnReload();
    }
}
