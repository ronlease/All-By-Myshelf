namespace AllByMyshelf.Api.Features.Settings;

/// <summary>
/// Response DTO with masked token values.
/// Tokens show first 4 + "••••" + last 2 characters (or "••••••" if too short).
/// </summary>
public record SettingsDto(
    string BoardGameGeekApiToken,
    string BoardGameGeekUsername,
    string DiscogsPersonalAccessToken,
    string DiscogsUsername,
    string HardcoverApiToken,
    string Theme);

/// <summary>
/// Request DTO for updating settings.
/// Null fields are left unchanged.
/// </summary>
public record UpdateSettingsDto(
    string? BoardGameGeekApiToken,
    string? BoardGameGeekUsername,
    string? DiscogsPersonalAccessToken,
    string? DiscogsUsername,
    string? HardcoverApiToken,
    string? Theme);
