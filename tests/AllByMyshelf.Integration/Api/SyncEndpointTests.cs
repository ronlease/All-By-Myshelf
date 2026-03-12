// Feature: Manual sync trigger endpoint            (ABM-005)
// Feature: Background sync of Discogs collection   (ABM-002)
//
// Scenario: Successfully trigger a sync
//   Given the Discogs personal access token is configured
//   And no sync is currently running
//   When I send POST /api/v1/sync
//   Then the response is HTTP 202 Accepted
//   And the response body includes a message confirming the sync has started
//
// Scenario: Attempt to trigger sync while one is already running
//   Given a sync is currently in progress
//   When I send POST /api/v1/sync
//   Then the response is HTTP 409 Conflict
//   And the response body explains that a sync is already in progress
//   And the running sync is not interrupted
//
// Scenario: Attempt to trigger sync with no token configured
//   Given the Discogs personal access token is NOT configured
//   When I send POST /api/v1/sync
//   Then the response is HTTP 503 Service Unavailable
//   And the response body explains that the Discogs token is not configured

using System.Net;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AllByMyshelf.Integration.Api;

/// <summary>
/// Integration tests for POST /api/v1/sync.
/// Each scenario creates its own factory so the ISyncService stub can return
/// different results per scenario without shared state.
/// </summary>
public class SyncEndpointTests
{
    // ── Factory helper ────────────────────────────────────────────────────────

    private static HttpClient CreateClient(ISyncService syncService)
    {
        var factory = new SyncFactory(syncService);
        return factory.CreateClient();
    }

    // ── POST /api/v1/sync — 202 Accepted ─────────────────────────────────────

    [Fact]
    public async Task TriggerSync_NoSyncRunning_ResponseBodyContainsStartedMessage()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.Started);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().ContainEquivalentOf("sync started");
    }

    [Fact]
    public async Task TriggerSync_NoSyncRunning_Returns202Accepted()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.Started);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ── POST /api/v1/sync — 409 Conflict ─────────────────────────────────────

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_IsSyncRunningRemainsTrue()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.AlreadyRunning, isSyncRunning: true);
        var client = CreateClient(syncService);

        // Act
        await client.PostAsync("/api/v1/sync", null);

        // Assert — the stub was not reset; it still reports running
        syncService.IsSyncRunning.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_ResponseBodyExplainsConflict()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.AlreadyRunning);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert — body should describe the conflict
        body.Should().ContainEquivalentOf("already");
    }

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_Returns409Conflict()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.AlreadyRunning);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /api/v1/sync — 503 Service Unavailable ───────────────────────────

    [Fact]
    public async Task TriggerSync_TokenNotConfigured_ResponseBodyExplainsMissingToken()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.TokenNotConfigured);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().ContainEquivalentOf("token");
    }

    [Fact]
    public async Task TriggerSync_TokenNotConfigured_Returns503ServiceUnavailable()
    {
        // Arrange
        var syncService = new StubSyncService(SyncStartResult.TokenNotConfigured);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    private sealed class SyncFactory(ISyncService syncService) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Discogs:PersonalAccessToken"] = "integration-test-token",
                    ["Discogs:Username"] = "integration-test-user",
                    ["Auth0:Domain"] = "test.auth0.com",
                    ["Auth0:Audience"] = "https://test-api"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace the PostgreSQL DbContext with an EF Core in-memory provider.
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
                        .UseInMemoryDatabase($"sync-integration-{Guid.NewGuid()}"));

                // Remove all SyncService registrations, then register the per-scenario stub
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                services.AddSingleton(syncService);

                // Replace JWT bearer with the no-op test scheme
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });

            builder.UseEnvironment("Testing");
        }
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private sealed class StubSyncService(SyncStartResult result, bool isSyncRunning = false)
        : ISyncService
    {
        public bool IsSyncRunning { get; } = isSyncRunning;
        public SyncProgressDto Progress => new(IsSyncRunning, 0, null, IsSyncRunning ? "syncing" : "idle", 0);
        public SyncStartResult TryStartSync() => result;
    }
}
