namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a configuration setting stored in the database.
/// Allows runtime configuration changes without requiring app restarts.
/// </summary>
public class AppSetting
{
    /// <summary>
    /// The configuration key (e.g., "Discogs:PersonalAccessToken").
    /// Primary key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when this setting was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The configuration value.
    /// Stored as plaintext (encryption deferred to PUB-001).
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
