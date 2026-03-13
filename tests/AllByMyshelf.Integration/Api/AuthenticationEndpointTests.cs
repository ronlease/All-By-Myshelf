// Feature: API authentication enforcement (ABM-028)
//
// Scenario: Request without Authorization header is rejected
//   Given I do not include an Authorization header
//   When I make a GET request to "/api/v1/releases"
//   Then the response status code is 401 Unauthorized
//
// Scenario: All API endpoints require authentication by default
//   Given no Authorization header
//   When I make a GET request to each endpoint under /api/v1/
//   Then each returns 401
//
// Scenario: Unauthorized response includes WWW-Authenticate header
//   Given I make an unauthenticated request to a protected endpoint
//   When the response is returned with status 401
//   Then the response includes a WWW-Authenticate header
//
// Scenario: Health check endpoint remains accessible without authentication
//   Given I make a GET request to "/health" without an Authorization header
//   Then the response status code is 200

using System.Net;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AllByMyshelf.Integration.Api;

/// <summary>
/// Integration tests for API authentication enforcement (ABM-028).
/// Tests verify that all API endpoints require authentication by default,
/// and that the health check endpoint remains publicly accessible.
/// </summary>
public class AuthenticationEndpointTests(AuthenticationEndpointTests.UnauthenticatedFactory factory)
    : IClassFixture<AuthenticationEndpointTests.UnauthenticatedFactory>
{
    private readonly UnauthenticatedFactory _factory = factory;

    // ── GET /api/v1/* — 401 Unauthorized ──────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/releases")]
    [InlineData("/api/v1/books")]
    [InlineData("/api/v1/sync/status")]
    [InlineData("/api/v1/statistics/collection-value")]
    public async Task UnauthenticatedRequest_ProtectedEndpoint_Returns401(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ProtectedEndpoint_Returns401WithWwwAuthenticateHeader()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().NotBeNullOrEmpty();
    }

    // ── GET /health — 200 OK (anonymous) ──────────────────────────────────────

    [Fact]
    public async Task HealthCheck_WithoutAuthentication_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that:
    /// - satisfies ValidateOnStart with fake config values for Discogs, Hardcover, Auth0
    /// - replaces PostgreSQL with in-memory EF Core
    /// - removes background services and registers no-op stubs
    /// - does NOT replace JWT Bearer with TestAuthHandler (so real 401s are returned)
    /// </summary>
    public class UnauthenticatedFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"auth-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Satisfy ValidateOnStart for DiscogsOptions and HardcoverOptions
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Discogs:PersonalAccessToken"] = "integration-test-token",
                    ["Discogs:Username"] = "integration-test-user",
                    ["Hardcover:ApiToken"] = "integration-test-token",
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
                        .UseInMemoryDatabase(_dbName));

                // Remove all SyncService and BooksSyncService registrations
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                BooksEndpointTests.RemoveBooksSyncServiceDescriptors(services);

                // Register no-op stubs
                services.AddSingleton<ISyncService>(new NoOpSyncService());
                services.AddSingleton<IBooksSyncService>(new NoOpBooksSyncService());

                // NOTE: We do NOT replace JWT bearer authentication here, so real 401s are returned
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
