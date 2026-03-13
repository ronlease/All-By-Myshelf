namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Strongly-typed configuration options for the Discogs API.
/// Bound from the "Discogs" configuration section.
/// </summary>
public class DiscogsOptions
{
    public const string SectionName = "Discogs";

    /// <summary>
    /// The Discogs personal access token used to authenticate API requests.
    /// Must be set via dotnet user-secrets (key: Discogs:PersonalAccessToken).
    /// </summary>
    public string PersonalAccessToken { get; init; } = string.Empty;

    /// <summary>
    /// The Discogs username whose collection will be synced.
    /// Must be set via dotnet user-secrets or appsettings (key: Discogs:Username).
    /// </summary>
    public string Username { get; init; } = string.Empty;
}
