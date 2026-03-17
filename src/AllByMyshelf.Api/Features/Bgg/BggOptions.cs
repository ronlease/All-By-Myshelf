namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Strongly-typed configuration options for the BoardGameGeek API.
/// Bound from the "Bgg" configuration section.
/// </summary>
public class BggOptions
{
    public const string SectionName = "Bgg";

    /// <summary>
    /// The BGG application Bearer token used for authenticated API access.
    /// Must be set via dotnet user-secrets (key: Bgg:ApiToken) or via the Settings page.
    /// Obtain a token at https://boardgamegeek.com/applications.
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>
    /// The BoardGameGeek username used to fetch the collection.
    /// Must be set via dotnet user-secrets (key: Bgg:Username).
    /// Not exposed in the Settings UI — the API token is sufficient to enable BGG.
    /// </summary>
    public string Username { get; init; } = string.Empty;
}
