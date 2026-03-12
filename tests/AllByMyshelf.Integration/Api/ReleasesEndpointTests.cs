// Feature: Paginated collection endpoint  (ABM-004)
// Feature: Release detail view             (ABM-012)
//
// Scenario: Retrieve the first page of releases
//   Given the database contains releases
//   When I request GET /api/v1/releases?page=1&pageSize=25
//   Then the response is HTTP 200 OK
//   And the response body contains up to 25 releases
//   And each release includes artist, title, year, and format
//   And the response includes total record count and total page count
//
// Scenario: Retrieve a subsequent page
//   Given the database contains more than 25 releases
//   When I request GET /api/v1/releases?page=2&pageSize=25
//   Then the response is HTTP 200 OK
//   And the response body contains releases from the second page
//   And the releases on page 2 do not overlap with those on page 1
//
// Scenario: Request a page beyond the available data
//   Given the database contains 30 releases
//   When I request GET /api/v1/releases?page=5&pageSize=25
//   Then the response is HTTP 200 OK
//   And the response body contains an empty releases array
//   And the total record count still reflects 30
//
// Scenario: Database contains no releases
//   Given no sync has been run and the database contains no releases
//   When I request GET /api/v1/releases?page=1&pageSize=25
//   Then the response is HTTP 200 OK
//   And the response body contains an empty releases array
//   And the total record count is 0
//
// Scenario: GET /api/v1/releases/{id} returns 200 with full ReleaseDetailDto when the release exists
//   Given the database contains a release with all detail fields populated
//   When I request GET /api/v1/releases/{id}
//   Then the response is HTTP 200 OK
//   And the body contains all fields: artist, title, year, format, genre
//
// Scenario: GET /api/v1/releases/{id} returns 404 when the release does not exist
//   Given the database does not contain a release with the requested id
//   When I request GET /api/v1/releases/{id}
//   Then the response is HTTP 404 Not Found
//
// Scenario: Detail view fields are present in the response after a resync
//   Given a release was synced with genre populated
//   When I request GET /api/v1/releases/{id}
//   Then the response body includes the detail field with its stored value

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Services;
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
/// Integration tests for GET /api/v1/releases.
/// Each test spins up its own in-memory database so state never leaks between tests.
/// </summary>
public class ReleasesEndpointTests(ReleasesEndpointTests.ReleasesFactory factory) : IClassFixture<ReleasesEndpointTests.ReleasesFactory>
{
    private readonly ReleasesFactory _factory = factory;

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

    // ── GET /api/v1/releases/{id} — 200 with full detail ─────────────────────

    [Fact]
    public async Task GetRelease_ExistingId_ResponseIncludesAllDetailFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeDetailedRelease(id, discogsId: 5002);
        var client = CreateClientWithSeededData(new[] { release });

        // Act
        var response = await client.GetAsync($"/api/v1/releases/{id}");
        var body = await response.Content.ReadFromJsonAsync<ReleaseDetailDto>();

        // Assert
        body.Should().NotBeNull();
        body!.Genre.Should().Be("Jazz");
    }

    [Fact]
    public async Task GetRelease_ExistingId_Returns200WithFullReleaseDetailDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeDetailedRelease(id, discogsId: 5001);
        var client = CreateClientWithSeededData(new[] { release });

        // Act
        var response = await client.GetAsync($"/api/v1/releases/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ReleaseDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(id);
        body.DiscogsId.Should().Be(5001);
        body.Artist.Should().Be("John Coltrane");
        body.Title.Should().Be("A Love Supreme");
        body.Year.Should().Be(1964);
        body.Format.Should().Be("Vinyl");
    }

    // ── GET /api/v1/releases/{id} — 404 when not found ───────────────────────

    [Fact]
    public async Task GetRelease_IdNotInDatabase_Returns404EvenWhenOtherReleasesExist()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[]
        {
            MakeRelease(1, "Artist", "Album")
        });
        var unknownId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/releases/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/releases/{id} — nullable detail fields ───────────────────

    [Fact]
    public async Task GetRelease_ReleaseWithNoDetailFields_Returns200WithNullDetailFields()
    {
        // Arrange — release without detail fields (e.g. synced before ABM-012)
        var id = Guid.NewGuid();
        var release = new Release
        {
            Id = id,
            DiscogsId = 5003,
            Artist = "Miles Davis",
            Title = "Kind of Blue",
            Year = 1959,
            Format = "Vinyl",
            Genre = null,
            LastSyncedAt = DateTimeOffset.UtcNow
        };
        var client = CreateClientWithSeededData(new[] { release });

        // Act
        var response = await client.GetAsync($"/api/v1/releases/{id}");
        var body = await response.Content.ReadFromJsonAsync<ReleaseDetailDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Genre.Should().BeNull();
    }

    [Fact]
    public async Task GetRelease_UnknownId_Returns404()
    {
        // Arrange — database is empty; any Guid will be unknown
        var client = CreateClientWithSeededData(Array.Empty<Release>());
        var unknownId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/releases/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/releases — empty database ─────────────────────────────────

    [Fact]
    public async Task GetReleases_EmptyDatabase_Returns200WithEmptyArrayAndZeroCount()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Release>());

        // Act
        var response = await client.GetAsync("/api/v1/releases?page=1&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/releases — first page ────────────────────────────────────

    [Fact]
    public async Task GetReleases_FirstPage_EachReleaseContainsArtistTitleYearFormat()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[]
        {
            MakeRelease(1, "John Coltrane", "A Love Supreme", 1964, "Vinyl")
        });

        // Act
        var response = await client.GetAsync("/api/v1/releases?page=1&pageSize=25");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();

        // Assert
        var item = body!.Items.Single();
        item.Artist.Should().Be("John Coltrane");
        item.Title.Should().Be("A Love Supreme");
        item.Year.Should().Be(1964);
        item.Format.Should().Be("Vinyl");
    }

    [Fact]
    public async Task GetReleases_FirstPage_Returns200WithReleasesAndCorrectTotalCount()
    {
        // Arrange
        var releases = Enumerable.Range(1, 30)
            .Select(i => MakeRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/releases?page=1&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();
        body!.Items.Should().HaveCount(25);
        body.TotalCount.Should().Be(30);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(25);
    }

    // ── GET /api/v1/releases — default parameters ─────────────────────────────

    [Fact]
    public async Task GetReleases_NoQueryParameters_Returns200UsingDefaults()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[] { MakeRelease(1, "Artist", "Album") });

        // Act
        var response = await client.GetAsync("/api/v1/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/releases — page beyond data ───────────────────────────────

    [Fact]
    public async Task GetReleases_PageBeyondAvailableData_Returns200WithEmptyItemsAndCorrectCount()
    {
        // Arrange — 30 releases, request page 5 of pageSize 25
        var releases = Enumerable.Range(1, 30)
            .Select(i => MakeRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/releases?page=5&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(30);
    }

    // ── GET /api/v1/releases — second page ───────────────────────────────────

    [Fact]
    public async Task GetReleases_SecondPage_Returns200WithNonOverlappingItems()
    {
        // Arrange — 30 releases, alphabetical artists A01..A30
        var releases = Enumerable.Range(1, 30)
            .Select(i => MakeRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var page1Response = await client.GetAsync("/api/v1/releases?page=1&pageSize=25");
        var page2Response = await client.GetAsync("/api/v1/releases?page=2&pageSize=25");

        // Assert
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();

        page2!.Items.Should().NotBeEmpty();
        var page1Titles = page1!.Items.Select(i => i.Title).ToHashSet();
        page2.Items.Select(i => i.Title).Should().NotIntersectWith(page1Titles);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Release MakeDetailedRelease(Guid id, int discogsId) =>
        new()
        {
            Id = id,
            DiscogsId = discogsId,
            Artist = "John Coltrane",
            Title = "A Love Supreme",
            Year = 1964,
            Format = "Vinyl",
            Genre = "Jazz",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

    private static Release MakeRelease(int discogsId, string artist, string title, int? year = 2000,
        string format = "Vinyl") =>
        new()
        {
            Id = Guid.NewGuid(),
            DiscogsId = discogsId,
            Artist = artist,
            Title = title,
            Year = year,
            Format = format,
            LastSyncedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Removes the three service registrations that Program.cs creates for SyncService.
    /// Uses a two-pass approach: first remove all typed registrations, then remove the
    /// IHostedService factory lambda that closes over SyncService.
    /// </summary>
    internal static void RemoveSyncServiceDescriptors(IServiceCollection services)
    {
        // Remove concrete SyncService singleton and ISyncService forwarding lambda
        services.RemoveAll<SyncService>();
        services.RemoveAll<ISyncService>();

        // Remove the IHostedService factory lambda: it has no ImplementationType (it's a
        // factory delegate) — but it is the only IHostedService registered by the app, so
        // removing all factory-backed IHostedService descriptors is safe here.
        var hostedServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == null)
            .ToList();
        foreach (var d in hostedServiceDescriptors)
            services.Remove(d);
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that:
    /// - substitutes the EF Core in-memory provider for PostgreSQL
    /// - injects required Discogs configuration values to satisfy ValidateOnStart
    /// - replaces JWT bearer authentication with a no-op scheme so [Authorize] passes
    /// - removes the SyncService BackgroundService to avoid background work during tests
    /// </summary>
    public class ReleasesFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"releases-integration-{Guid.NewGuid()}";

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
                // In EF Core 10, AddDbContext registers the options as a keyed singleton
                // whose ServiceType is DbContextOptions<T>. Removing that descriptor and
                // re-adding with UseInMemoryDatabase is the standard WebApplicationFactory
                // pattern. We must also force EF Core to build a fresh internal service
                // provider scoped to the in-memory provider only; we do that by enabling
                // internal service provider management per context via EnableSensitiveDataLogging
                // (which is a no-op here) and supplying a dedicated InMemory service provider.
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
                RemoveSyncServiceDescriptors(services);
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
