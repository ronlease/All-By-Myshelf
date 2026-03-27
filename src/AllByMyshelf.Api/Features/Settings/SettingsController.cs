using AllByMyshelf.Api.Infrastructure;
using AllByMyshelf.Api.Infrastructure.Configuration;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Settings;

/// <summary>
/// API controller for managing application settings.
/// Settings can be stored in the database (runtime-editable) or in configuration (user-secrets fallback).
/// </summary>
[ApiController]
[Route("api/v1/settings")]
public class SettingsController(
    AllByMyshelfDbContext dbContext,
    IConfiguration configuration,
    IConfigurationRoot configurationRoot) : ControllerBase
{
    // Well-known setting keys
    private const string BoardGameGeekApiTokenKey = "BoardGameGeek:ApiToken";
    private const string BoardGameGeekUsernameKey = "BoardGameGeek:Username";
    private const string DiscogsPersonalAccessTokenKey = "Discogs:PersonalAccessToken";
    private const string DiscogsUsernameKey = "Discogs:Username";
    private const string HardcoverApiTokenKey = "Hardcover:ApiToken";
    private const string ThemeKey = "App:Theme";

    private static readonly HashSet<string> ValidThemes = ["light", "dark", "os-default"];

    /// <summary>
    /// Retrieves all application settings with sensitive values masked.
    /// </summary>
    /// <returns>A <see cref="SettingsDto"/> with masked token values.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsDto>> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var boardGameGeekApiToken = await GetSettingValueAsync(BoardGameGeekApiTokenKey, cancellationToken);
        var boardGameGeekUsername = await GetSettingValueAsync(BoardGameGeekUsernameKey, cancellationToken);
        var discogsToken = await GetSettingValueAsync(DiscogsPersonalAccessTokenKey, cancellationToken);
        var discogsUsername = await GetSettingValueAsync(DiscogsUsernameKey, cancellationToken);
        var hardcoverToken = await GetSettingValueAsync(HardcoverApiTokenKey, cancellationToken);
        var theme = await GetSettingValueAsync(ThemeKey, cancellationToken) ?? "os-default";

        return Ok(new SettingsDto(
            BoardGameGeekApiToken: MaskToken(boardGameGeekApiToken ?? string.Empty),
            BoardGameGeekUsername: boardGameGeekUsername ?? string.Empty,
            DiscogsPersonalAccessToken: MaskToken(discogsToken ?? string.Empty),
            DiscogsUsername: discogsUsername ?? string.Empty,
            HardcoverApiToken: MaskToken(hardcoverToken ?? string.Empty),
            Theme: theme));
    }

    /// <summary>
    /// Updates application settings.
    /// Only non-null fields are updated. After save, configuration is reloaded to propagate changes.
    /// </summary>
    /// <param name="dto">The settings to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success, 400 Bad Request if theme is invalid.</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] UpdateSettingsDto dto,
        CancellationToken cancellationToken)
    {
        // Validate and sanitize theme
        if (dto.Theme is not null)
        {
            var sanitizedTheme = InputSanitizer.Sanitize(dto.Theme, maxLength: 20);
            if (!ValidThemes.Contains(sanitizedTheme))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Theme",
                    Detail = "Theme must be one of: light, dark, os-default."
                });
            }
            await UpsertSettingAsync(ThemeKey, sanitizedTheme, cancellationToken);
        }

        // Sanitize and save token fields (maxLength 2000)
        if (dto.BoardGameGeekApiToken is not null)
        {
            var sanitized = InputSanitizer.Sanitize(dto.BoardGameGeekApiToken, maxLength: 2000);
            await UpsertSettingAsync(BoardGameGeekApiTokenKey, sanitized, cancellationToken);
        }

        if (dto.DiscogsPersonalAccessToken is not null)
        {
            var sanitized = InputSanitizer.Sanitize(dto.DiscogsPersonalAccessToken, maxLength: 2000);
            await UpsertSettingAsync(DiscogsPersonalAccessTokenKey, sanitized, cancellationToken);
        }

        if (dto.HardcoverApiToken is not null)
        {
            var sanitized = InputSanitizer.Sanitize(dto.HardcoverApiToken, maxLength: 2000);
            await UpsertSettingAsync(HardcoverApiTokenKey, sanitized, cancellationToken);
        }

        // Sanitize and save username fields (maxLength 100)
        if (dto.BoardGameGeekUsername is not null)
        {
            var sanitized = InputSanitizer.Sanitize(dto.BoardGameGeekUsername, maxLength: 100);
            await UpsertSettingAsync(BoardGameGeekUsernameKey, sanitized, cancellationToken);
        }

        if (dto.DiscogsUsername is not null)
        {
            var sanitized = InputSanitizer.Sanitize(dto.DiscogsUsername, maxLength: 100);
            await UpsertSettingAsync(DiscogsUsernameKey, sanitized, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload configuration to propagate DB changes
        configurationRoot.Reload();

        return NoContent();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets a setting value from the database first, then falls back to IConfiguration.
    /// </summary>
    private async Task<string?> GetSettingValueAsync(string key, CancellationToken cancellationToken)
    {
        var dbValue = await dbContext.AppSettings
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return dbValue ?? configuration[key];
    }

    /// <summary>
    /// Masks a token for display: first 4 + "••••" + last 2 chars.
    /// If the token is shorter than 8 characters, just show "••••••".
    /// </summary>
    private static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        if (token.Length < 8)
            return "••••••";

        return $"{token[..4]}••••{token[^2..]}";
    }

    /// <summary>
    /// Inserts or updates a setting in the database.
    /// </summary>
    private async Task UpsertSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await dbContext.AppSettings.FindAsync([key], cancellationToken);

        if (existing is not null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            dbContext.AppSettings.Add(new AppSetting
            {
                Key = key,
                UpdatedAt = DateTimeOffset.UtcNow,
                Value = value
            });
        }
    }
}
