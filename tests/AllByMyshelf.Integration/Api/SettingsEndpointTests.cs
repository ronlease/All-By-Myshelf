// Feature: Configuration & settings page (ABM-039)
// Feature: BGG API Token Authentication (ABM-063)
//
// Scenario: User retrieves settings with no values configured
//   Given no settings are saved in the database
//   And no tokens are in configuration
//   When I call GET /api/v1/settings with a valid auth token
//   Then the response status is 200
//   And bggApiToken is ""
//   And bggUsername is ""
//   And discogsPersonalAccessToken is ""
//   And discogsUsername is ""
//   And hardcoverApiToken is ""
//   And theme is "os-default"
//
// Scenario: User retrieves settings with tokens from user-secrets fallback
//   Given no settings are saved in the database
//   And "Discogs:PersonalAccessToken" is "my-secret-token-value" in configuration
//   And "Discogs:Username" is "testuser" in configuration
//   When I call GET /api/v1/settings with a valid auth token
//   Then discogsPersonalAccessToken is masked (e.g., "my-s••••ue")
//   And discogsUsername is "testuser" (not masked)
//
// Scenario: User saves an API token
//   Given I PUT to /api/v1/settings with { "discogsPersonalAccessToken": "new-token-123" }
//   When the response is returned
//   Then the response status is 204
//   And the token is stored in the database
//
// Scenario: User saves theme preference
//   Given I PUT to /api/v1/settings with { "theme": "dark" }
//   When I subsequently GET /api/v1/settings
//   Then theme is "dark"
//
// Scenario: Partial update only changes specified fields
//   Given I first save discogsPersonalAccessToken = "token-1"
//   And I then save hardcoverApiToken = "token-2" (without including discogsPersonalAccessToken)
//   When I GET /api/v1/settings
//   Then discogsPersonalAccessToken is still masked (showing the first token)
//   And hardcoverApiToken is masked (showing the second token)
//
// Scenario: Settings endpoint requires authentication
//   Given I do not include an Authorization header
//   When I call GET /api/v1/settings
//   Then the response status is 401
//
// Scenario: Token masking shows first 4 and last 2 characters
//   Given a token "abcdefghij" is saved
//   When I GET /api/v1/settings
//   Then the masked token is "abcd••••ij"
//
// Scenario: Short tokens are fully masked
//   Given a token "short" (< 8 chars) is saved
//   When I GET /api/v1/settings
//   Then the masked token is "••••••"
//
// Scenario: BGG API token is masked in GET response (ABM-063)
//   Given "Bgg:ApiToken" is "bgg-secret-token-12345" in configuration
//   When I call GET /api/v1/settings
//   Then bggApiToken is masked (e.g., "bgg-••••45")
//
// Scenario: BGG API token can be saved via PUT (ABM-063)
//   Given I PUT to /api/v1/settings with { "bggApiToken": "new-bgg-token" }
//   When the response is returned
//   Then the response status is 204
//   And the token is stored in the database under key "Bgg:ApiToken"
//
// Scenario: GET returns empty bggApiToken when none configured (ABM-063)
//   Given no BGG token is in database or configuration
//   When I call GET /api/v1/settings
//   Then bggApiToken is ""
//
// Scenario: Feature flags reflect database-stored tokens
//   Given I save a Hardcover API token via PUT /api/v1/settings
//   And no Discogs token is configured
//   When I call GET /api/v1/config/features
//   Then hardcoverEnabled is true
//   And discogsEnabled is false
//
// Note: The feature flags test cannot be reliably implemented with the in-memory
// EF Core provider because the DbConfigurationSource requires a real PostgreSQL
// connection. The IOptionsSnapshot used by ConfigController reads from IConfiguration,
// which includes the DbConfigurationSource. In tests, the DbConfigurationSource
// fails silently (catch block), so it never provides DB-stored values. This scenario
// would require a full integration test against a real PostgreSQL database.

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Config;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Features.Settings;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AllByMyshelf.Integration.Api;

/// <summary>
/// Integration tests for GET /api/v1/settings and PUT /api/v1/settings (ABM-039).
/// </summary>
public class SettingsEndpointTests
{
    // ── GET /api/v1/settings — empty DB, no config ───────────────────────────

    [Fact]
    public async Task GetSettings_NoDbNoConfig_ReturnsEmptyStringsAndDefaultTheme()
    {
        // Arrange
        await using var factory = CreateFactory(bggToken: null, discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();
        body!.BggApiToken.Should().Be(string.Empty);
        body.BggUsername.Should().Be(string.Empty);
        body.DiscogsPersonalAccessToken.Should().Be(string.Empty);
        body.DiscogsUsername.Should().Be(string.Empty);
        body.HardcoverApiToken.Should().Be(string.Empty);
        body.Theme.Should().Be("os-default");
    }

    // ── GET /api/v1/settings — fallback to user-secrets ──────────────────────

    [Fact]
    public async Task GetSettings_TokensInConfig_DiscogsUsernameNotMasked()
    {
        // Arrange
        await using var factory = CreateFactory(
            discogsToken: "my-secret-token-value",
            hardcoverToken: null,
            discogsUsername: "testuser");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.DiscogsUsername.Should().Be("testuser");
    }

    [Fact]
    public async Task GetSettings_TokensInConfig_ReturnsMaskedTokens()
    {
        // Arrange
        await using var factory = CreateFactory(
            discogsToken: "my-secret-token-value",
            hardcoverToken: null,
            discogsUsername: "testuser");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.DiscogsPersonalAccessToken.Should().Be("my-s••••ue");
    }

    // ── GET /api/v1/settings — token masking (long tokens) ───────────────────

    [Fact]
    public async Task GetSettings_TokenLongerThan8Chars_ShowsFirst4AndLast2()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: "abcdefghij", hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.DiscogsPersonalAccessToken.Should().Be("abcd••••ij");
    }

    // ── GET /api/v1/settings — token masking (short tokens) ──────────────────

    [Fact]
    public async Task GetSettings_TokenShorterThan8Chars_FullyMasked()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: "short", hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.DiscogsPersonalAccessToken.Should().Be("••••••");
    }

    // ── GET /api/v1/settings — unauthenticated ───────────────────────────────

    [Fact]
    public async Task GetSettings_NoAuthHeader_Returns401()
    {
        // Arrange
        await using var factory = CreateFactoryWithoutAuth();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/v1/settings — save token ────────────────────────────────────

    [Fact]
    public async Task PutSettings_SaveNewToken_Returns204()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();
        var dto = new UpdateSettingsDto(
            BggApiToken: null,
            BggUsername: null,
            DiscogsPersonalAccessToken: "new-token-123",
            DiscogsUsername: null,
            HardcoverApiToken: null,
            Theme: null);

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/settings", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutSettings_SaveNewToken_TokenStoredInDatabase()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();
        var dto = new UpdateSettingsDto(
            BggApiToken: null,
            BggUsername: null,
            DiscogsPersonalAccessToken: "new-token-123",
            DiscogsUsername: null,
            HardcoverApiToken: null,
            Theme: null);

        // Act
        await client.PutAsJsonAsync("/api/v1/settings", dto);

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        var stored = await db.AppSettings.FindAsync("Discogs:PersonalAccessToken");
        stored.Should().NotBeNull();
        stored!.Value.Should().Be("new-token-123");
    }

    // ── PUT /api/v1/settings — save theme ────────────────────────────────────

    [Fact]
    public async Task PutSettings_SaveTheme_SubsequentGetReturnsNewTheme()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();
        var dto = new UpdateSettingsDto(
            BggApiToken: null,
            BggUsername: null,
            DiscogsPersonalAccessToken: null,
            DiscogsUsername: null,
            HardcoverApiToken: null,
            Theme: "dark");

        // Act
        await client.PutAsJsonAsync("/api/v1/settings", dto);
        var getResponse = await client.GetAsync("/api/v1/settings");
        var body = await getResponse.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.Theme.Should().Be("dark");
    }

    // ── PUT /api/v1/settings — partial update ────────────────────────────────

    [Fact]
    public async Task PutSettings_PartialUpdate_OnlySpecifiedFieldsChanged()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();

        // Save first token (8+ chars for masking: first 4 + "••••" + last 2)
        var dto1 = new UpdateSettingsDto(
            BggApiToken: null,
            BggUsername: null,
            DiscogsPersonalAccessToken: "discogs-token-123",
            DiscogsUsername: null,
            HardcoverApiToken: null,
            Theme: null);
        await client.PutAsJsonAsync("/api/v1/settings", dto1);

        // Save second token without including first
        var dto2 = new UpdateSettingsDto(
            BggApiToken: null,
            BggUsername: null,
            DiscogsPersonalAccessToken: null,
            DiscogsUsername: null,
            HardcoverApiToken: "hardcover-token-456",
            Theme: null);
        await client.PutAsJsonAsync("/api/v1/settings", dto2);

        // Act
        var getResponse = await client.GetAsync("/api/v1/settings");
        var body = await getResponse.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.DiscogsPersonalAccessToken.Should().Be("disc••••23"); // masked version of "discogs-token-123"
        body.HardcoverApiToken.Should().Be("hard••••56"); // masked version of "hardcover-token-456"
    }

    // ── GET /api/v1/settings — BGG API token masking (ABM-063) ───────────────

    [Fact]
    public async Task GetSettings_BggTokenInConfig_ReturnsMaskedToken()
    {
        // Arrange
        await using var factory = CreateFactory(
            bggToken: "bgg-secret-token-12345",
            discogsToken: null,
            hardcoverToken: null,
            bggUsername: "bgg-test-user");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.BggApiToken.Should().Be("bgg-••••45"); // first 4 + "••••" + last 2
    }

    [Fact]
    public async Task GetSettings_NoBggToken_ReturnsEmptyString()
    {
        // Arrange
        await using var factory = CreateFactory(bggToken: null, discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/settings");
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>();

        // Assert
        body!.BggApiToken.Should().Be(string.Empty);
    }

    // ── PUT /api/v1/settings — save BGG API token (ABM-063) ──────────────────

    [Fact]
    public async Task PutSettings_SaveBggToken_Returns204AndTokenStoredInDatabase()
    {
        // Arrange
        await using var factory = CreateFactory(bggToken: null, discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();
        var dto = new UpdateSettingsDto(
            BggApiToken: "new-bgg-token-xyz",
            BggUsername: null,
            DiscogsPersonalAccessToken: null,
            DiscogsUsername: null,
            HardcoverApiToken: null,
            Theme: null);

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/settings", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        var stored = await db.AppSettings.FindAsync("Bgg:ApiToken");
        stored.Should().NotBeNull();
        stored!.Value.Should().Be("new-bgg-token-xyz");
    }

    // ── Factory helper ────────────────────────────────────────────────────────

    private static SettingsFactory CreateFactory(
        string? bggToken = null,
        string? discogsToken = null,
        string? hardcoverToken = null,
        string? bggUsername = null,
        string? discogsUsername = null) => new(bggToken, discogsToken, hardcoverToken, bggUsername, discogsUsername);

    private static SettingsFactoryWithoutAuth CreateFactoryWithoutAuth() => new();

    /// <summary>
    /// Minimal factory that configures specific token values (or none) for settings tests.
    /// </summary>
    private sealed class SettingsFactory(
        string? bggToken,
        string? discogsToken,
        string? hardcoverToken,
        string? bggUsername,
        string? discogsUsername)
        : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"settings-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Auth0:Domain"] = "test.auth0.com",
                    ["Auth0:Audience"] = "https://test-api",
                    ["Bgg:ApiToken"] = bggToken,
                    ["Bgg:Username"] = bggUsername ?? (bggToken is not null ? "integration-test-bgg-user" : null),
                    ["Discogs:PersonalAccessToken"] = discogsToken,
                    ["Discogs:Username"] = discogsUsername ?? (discogsToken is not null ? "integration-test-user" : null),
                    ["Hardcover:ApiToken"] = hardcoverToken,
                };
                config.AddInMemoryCollection(values);
            });

            builder.ConfigureServices((context, services) =>
            {
                // Replace PostgreSQL with in-memory EF Core
                var existingDbOptions = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AllByMyshelfDbContext>))
                    .ToList();
                foreach (var d in existingDbOptions)
                    services.Remove(d);

                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AllByMyshelfDbContext>(options =>
                    options
                        .UseInternalServiceProvider(inMemoryServiceProvider)
                        .UseInMemoryDatabase(_dbName));

                // Register IConfigurationRoot for SettingsController
                services.AddSingleton<IConfigurationRoot>((IConfigurationRoot)context.Configuration);

                // Remove background services
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                BooksEndpointTests.RemoveBooksSyncServiceDescriptors(services);

                // Register no-op stubs
                services.AddSingleton<ISyncService>(new NoOpSyncService());
                services.AddSingleton<IBooksSyncService>(new NoOpBooksSyncService());

                // Replace JWT bearer with a no-op test handler so [Authorize] passes
                services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
                services
                    .AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });

            builder.UseEnvironment("Testing");
        }
    }

    /// <summary>
    /// Factory without authentication configured to test 401 responses.
    /// </summary>
    private sealed class SettingsFactoryWithoutAuth : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"settings-unauth-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Auth0:Domain"] = "test.auth0.com",
                    ["Auth0:Audience"] = "https://test-api",
                    ["Discogs:PersonalAccessToken"] = "test-token",
                    ["Discogs:Username"] = "test-user",
                    ["Hardcover:ApiToken"] = "test-token",
                };
                config.AddInMemoryCollection(values);
            });

            builder.ConfigureServices((context, services) =>
            {
                // Replace PostgreSQL with in-memory EF Core
                var existingDbOptions = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AllByMyshelfDbContext>))
                    .ToList();
                foreach (var d in existingDbOptions)
                    services.Remove(d);

                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AllByMyshelfDbContext>(options =>
                    options
                        .UseInternalServiceProvider(inMemoryServiceProvider)
                        .UseInMemoryDatabase(_dbName));

                // Register IConfigurationRoot for SettingsController
                services.AddSingleton<IConfigurationRoot>((IConfigurationRoot)context.Configuration);

                // Remove background services
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                BooksEndpointTests.RemoveBooksSyncServiceDescriptors(services);

                // Register no-op stubs
                services.AddSingleton<ISyncService>(new NoOpSyncService());
                services.AddSingleton<IBooksSyncService>(new NoOpBooksSyncService());

                // Do NOT configure authentication — this factory tests 401
            });

            builder.UseEnvironment("Testing");
        }
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class NoOpBooksSyncService : IBooksSyncService
    {
        public bool IsSyncRunning => false;
        public SyncStartResult TryStartSync() => SyncStartResult.Started;
    }

    private sealed class NoOpSyncService : ISyncService
    {
        public bool IsSyncRunning => false;
        public SyncProgressDto Progress => new(false, 0, null, "idle", 0);
        public SyncStartResult TryStartSync() => SyncStartResult.Started;
    }
}
