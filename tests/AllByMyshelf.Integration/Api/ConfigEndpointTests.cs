// Feature: Optional external API integrations (ABM-036)
//
// Scenario: Features endpoint returns both integrations enabled when both tokens are configured
//   Given both Discogs and Hardcover tokens are in configuration
//   When I call GET /api/v1/config/features
//   Then discogsEnabled is true
//   And hardcoverEnabled is true
//
// Scenario: Features endpoint returns only Discogs enabled when only Discogs is configured
//   Given only the Discogs token is in configuration
//   When I call GET /api/v1/config/features
//   Then discogsEnabled is true
//   And hardcoverEnabled is false
//
// Scenario: Features endpoint returns only Hardcover enabled when only Hardcover is configured
//   Given only the Hardcover token is in configuration
//   When I call GET /api/v1/config/features
//   Then discogsEnabled is false
//   And hardcoverEnabled is true
//
// Scenario: Features endpoint returns both disabled when no tokens are configured
//   Given neither Discogs nor Hardcover tokens are in configuration
//   When I call GET /api/v1/config/features
//   Then discogsEnabled is false
//   And hardcoverEnabled is false
//
// Scenario: Application starts successfully with no tokens configured
//   Given neither token is present in configuration
//   When the application starts
//   Then the application starts without error
//   And GET /health returns 200

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Config;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Infrastructure.Data;
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
/// Integration tests for GET /api/v1/config/features and optional integration startup (ABM-036).
/// </summary>
public class ConfigEndpointTests
{
    // ── GET /api/v1/config/features — both tokens configured ─────────────────

    [Fact]
    public async Task GetFeatures_BothTokensConfigured_ReturnsBothEnabled()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: "discogs-token", hardcoverToken: "hardcover-token");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeaturesDto>();
        body!.DiscogsEnabled.Should().BeTrue();
        body.HardcoverEnabled.Should().BeTrue();
    }

    // ── GET /api/v1/config/features — only Discogs configured ────────────────

    [Fact]
    public async Task GetFeatures_OnlyDiscogsConfigured_ReturnsDiscogsEnabledHardcoverDisabled()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: "discogs-token", hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeaturesDto>();
        body!.DiscogsEnabled.Should().BeTrue();
        body.HardcoverEnabled.Should().BeFalse();
    }

    // ── GET /api/v1/config/features — only Hardcover configured ──────────────

    [Fact]
    public async Task GetFeatures_OnlyHardcoverConfigured_ReturnsHardcoverEnabledDiscogsDisabled()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: "hardcover-token");
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeaturesDto>();
        body!.DiscogsEnabled.Should().BeFalse();
        body.HardcoverEnabled.Should().BeTrue();
    }

    // ── GET /api/v1/config/features — no tokens configured ───────────────────

    [Fact]
    public async Task GetFeatures_NoTokensConfigured_ReturnsBothDisabled()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeaturesDto>();
        body!.DiscogsEnabled.Should().BeFalse();
        body.HardcoverEnabled.Should().BeFalse();
    }

    // ── Startup — no tokens configured → health still 200 ────────────────────

    [Fact]
    public async Task ApplicationStartsSuccessfully_WithNoTokensConfigured()
    {
        // Arrange
        await using var factory = CreateFactory(discogsToken: null, hardcoverToken: null);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Factory helper ────────────────────────────────────────────────────────

    private static ConfigFactory CreateFactory(string? discogsToken, string? hardcoverToken) =>
        new(discogsToken, hardcoverToken);

    /// <summary>
    /// Minimal factory that configures specific token values (or none) for features tests.
    /// </summary>
    private sealed class ConfigFactory(string? discogsToken, string? hardcoverToken)
        : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"config-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Auth0:Domain"] = "test.auth0.com",
                    ["Auth0:Audience"] = "https://test-api",
                    ["Discogs:Username"] = discogsToken is not null ? "integration-test-user" : null,
                    ["Discogs:PersonalAccessToken"] = discogsToken,
                    ["Hardcover:ApiToken"] = hardcoverToken,
                };
                config.AddInMemoryCollection(values);
            });

            builder.ConfigureServices(services =>
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
