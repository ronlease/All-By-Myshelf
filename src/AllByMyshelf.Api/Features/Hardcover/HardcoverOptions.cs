namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Strongly-typed configuration options for the Hardcover API.
/// Bound from the "Hardcover" configuration section.
/// </summary>
public class HardcoverOptions
{
    public const string SectionName = "Hardcover";

    /// <summary>
    /// The Hardcover API token used to authenticate GraphQL requests.
    /// Must be set via dotnet user-secrets (key: Hardcover:ApiToken).
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;
}
