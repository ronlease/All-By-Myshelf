// Feature: Statistics endpoint (ABM-020)
//
// Scenario: Returns 200 with collection value when releases exist with pricing
//   Given 2 releases in the database with LowestPrice 15.00 and 25.00
//   When GET /api/v1/statistics/collection-value is called with a valid auth token
//   Then the response status is 200
//   And the response body has TotalValue 40.00
//   And IncludedCount is 2
//   And ExcludedCount is 0
//
// Scenario: Returns 200 with zero value when no releases have pricing
//   Given 2 releases in the database with null LowestPrice
//   When GET /api/v1/statistics/collection-value is called with a valid auth token
//   Then the response status is 200
//   And IncludedCount is 0
//   And ExcludedCount is 2
//
// Scenario: Returns 401 when request is unauthenticated
//   When GET /api/v1/statistics/collection-value is called without an auth token
//   Then the response status is 401

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Statistics;
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
/// Integration tests for GET /api/v1/statistics/collection-value.
/// Each test spins up its own in-memory database so state never leaks between tests.
/// </summary>
public class StatisticsEndpointTests(StatisticsEndpointTests.StatisticsFactory factory) : IClassFixture<StatisticsEndpointTests.StatisticsFactory>
{
    private readonly StatisticsFactory _factory = factory;

    /// <summary>Seeds the in-memory database with the given releases, returns a fresh client.</summary>
    private HttpClient CreateClientWithSeededData(IEnumerable<Release> releases)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        // Clear any data from a previous test in the same factory instance.
        db.Releases.RemoveRange(db.Releases);
        db.Releases.AddRange(releases);
        db.SaveChanges();

        return client;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Release MakeReleaseWithPrice(int discogsId, decimal? lowestPrice) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiscogsId = discogsId,
            Artist = $"Artist {discogsId}",
            Title = $"Title {discogsId}",
            Year = 2000,
            Format = "Vinyl",
            LowestPrice = lowestPrice,
            LastSyncedAt = DateTimeOffset.UtcNow
        };

    // ── GET /api/v1/statistics/collection-value — 200 with pricing ───────────

    [Fact]
    public async Task GetCollectionValue_ReleasesWithPricing_Returns200WithCorrectValue()
    {
        // Arrange
        var releases = new[]
        {
            MakeReleaseWithPrice(1, 15.00m),
            MakeReleaseWithPrice(2, 25.00m)
        };
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/statistics/collection-value");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CollectionValueDto>();
        body.Should().NotBeNull();
        body!.TotalValue.Should().Be(40.00m);
        body.IncludedCount.Should().Be(2);
        body.ExcludedCount.Should().Be(0);
    }

    // ── GET /api/v1/statistics/collection-value — 200 with no pricing ────────

    [Fact]
    public async Task GetCollectionValue_ReleasesWithoutPricing_Returns200WithZeroValue()
    {
        // Arrange
        var releases = new[]
        {
            MakeReleaseWithPrice(101, null),
            MakeReleaseWithPrice(102, null)
        };
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/statistics/collection-value");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CollectionValueDto>();
        body.Should().NotBeNull();
        body!.TotalValue.Should().Be(0.00m);
        body.IncludedCount.Should().Be(0);
        body.ExcludedCount.Should().Be(2);
    }

    // ── GET /api/v1/statistics/collection-value — 401 unauthenticated ────────

    [Fact]
    public async Task GetCollectionValue_Unauthenticated_Returns401()
    {
        // Arrange — create a client without the test auth handler
        var unauthFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
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
                            .UseInMemoryDatabase($"statistics-unauth-{Guid.NewGuid()}"));

                    // Remove all SyncService registrations
                    ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                    services.AddSingleton<ISyncService>(new NoOpSyncService());
                });

                builder.UseEnvironment("Testing");
            });

        var client = unauthFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/statistics/collection-value");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that:
    /// - substitutes the EF Core in-memory provider for PostgreSQL
    /// - injects required Discogs configuration values to satisfy ValidateOnStart
    /// - replaces JWT bearer authentication with a no-op scheme so [Authorize] passes
    /// - removes the SyncService BackgroundService to avoid background work during tests
    /// </summary>
    public class StatisticsFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"statistics-integration-{Guid.NewGuid()}";

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

                // Build a dedicated EF Core internal service provider that contains only
                // the InMemory provider, preventing the "two providers registered" error.
                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AllByMyshelfDbContext>(options =>
                    options
                        .UseInternalServiceProvider(inMemoryServiceProvider)
                        .UseInMemoryDatabase(_dbName));

                // Remove all SyncService registrations, then register the per-scenario stub
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                services.AddSingleton<ISyncService>(new NoOpSyncService());

                // Replace JWT bearer with a test scheme that always authenticates
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });

            builder.UseEnvironment("Testing");
        }
    }

    private sealed class NoOpSyncService : ISyncService
    {
        public bool IsSyncRunning => false;
        public SyncProgressDto Progress => new(false, 0, null, "idle", 0);
        public SyncStartResult TryStartSync() => SyncStartResult.Started;
    }
}
