namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Strongly-typed configuration options for the BoardGameGeek API.
/// Bound from the "BoardGameGeek" configuration section.
/// </summary>
public class BoardGameGeekOptions
{
    public const string SectionName = "BoardGameGeek";

    /// <summary>
    /// The BoardGameGeek application Bearer token used for authenticated API access.
    /// Must be set via dotnet user-secrets (key: BoardGameGeek:ApiToken) or via the Settings page.
    /// Obtain a token at https://boardgamegeek.com/applications.
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>
    /// The BoardGameGeek username used to fetch the collection.
    /// Must be set via dotnet user-secrets (key: BoardGameGeek:Username).
    /// Not exposed in the Settings UI — both the API token and username are required to enable BoardGameGeek.
    /// </summary>
    public string Username { get; init; } = string.Empty;
}
