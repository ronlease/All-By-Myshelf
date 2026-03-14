namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Strongly-typed configuration options for the BoardGameGeek API.
/// Bound from the "Bgg" configuration section.
/// </summary>
public class BggOptions
{
    public const string SectionName = "Bgg";

    /// <summary>
    /// The BoardGameGeek username used to fetch the collection.
    /// Must be set via dotnet user-secrets (key: Bgg:Username).
    /// </summary>
    public string Username { get; init; } = string.Empty;
}
