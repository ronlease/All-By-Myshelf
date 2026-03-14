// Feature: Paginated wantlist endpoint  (ABM-037)
//
// Scenario: Retrieve the first page of wantlist releases
//   Given the database contains wantlist releases
//   When I request GET /api/v1/wantlist?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains up to 20 wantlist releases
//   And each release includes artist, title, year, format, and genre
//   And the response includes total record count and total page count
//
// Scenario: Retrieve a subsequent page
//   Given the database contains more than 20 wantlist releases
//   When I request GET /api/v1/wantlist?page=2&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains wantlist releases from the second page
//   And the wantlist releases on page 2 do not overlap with those on page 1
//
// Scenario: Request a page beyond the available data
//   Given the database contains 15 wantlist releases
//   When I request GET /api/v1/wantlist?page=5&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains an empty wantlist releases array
//   And the total record count still reflects 15
//
// Scenario: Database contains no wantlist releases
//   Given no sync has been run and the database contains no wantlist releases
//   When I request GET /api/v1/wantlist?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains an empty wantlist releases array
//   And the total record count is 0
//
// Scenario: GET /api/v1/wantlist?page=-1 defaults invalid page to 1
//   Given the database contains wantlist releases
//   When I request GET /api/v1/wantlist?page=-1
//   Then the response is HTTP 200 OK
//   And the response indicates page 1

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Features.Wantlist;
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
/// Integration tests for GET /api/v1/wantlist.
/// Each test spins up its own in-memory database so state never leaks between tests.
/// </summary>
public class WantlistEndpointTests
{
    // ── Factory helper ────────────────────────────────────────────────────────

    /// <summary>Seeds the in-memory database with the given wantlist releases, returns a fresh client.</summary>
    private static HttpClient CreateClientWithSeededData(IEnumerable<WantlistRelease> releases)
    {
        var factory = new WantlistFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        db.WantlistReleases.RemoveRange(db.WantlistReleases);
        db.WantlistReleases.AddRange(releases);
        db.SaveChanges();

        return client;
    }

    // ── GET /api/v1/wantlist — empty database ─────────────────────────────────

    [Fact]
    public async Task GetWantlist_EmptyDatabase_Returns200WithEmptyArrayAndZeroCount()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<WantlistRelease>());

        // Act
        var response = await client.GetAsync("/api/v1/wantlist?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/wantlist — first page ─────────────────────────────────────

    [Fact]
    public async Task GetWantlist_FirstPage_EachReleaseContainsArtistTitleYearFormatGenre()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[]
        {
            MakeWantlistRelease(1, "John Coltrane", "A Love Supreme", 1964, "Vinyl", "Jazz")
        });

        // Act
        var response = await client.GetAsync("/api/v1/wantlist?page=1&pageSize=20");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();

        // Assert
        var item = body!.Items.Single();
        item.Artist.Should().Be("John Coltrane");
        item.Title.Should().Be("A Love Supreme");
        item.Year.Should().Be(1964);
        item.Format.Should().Be("Vinyl");
        item.Genre.Should().Be("Jazz");
    }

    [Fact]
    public async Task GetWantlist_FirstPage_Returns200WithReleasesAndCorrectTotalCount()
    {
        // Arrange
        var releases = Enumerable.Range(1, 30)
            .Select(i => MakeWantlistRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/wantlist?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();
        body!.Items.Should().HaveCount(20);
        body.TotalCount.Should().Be(30);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(20);
    }

    // ── GET /api/v1/wantlist — default parameters ─────────────────────────────

    [Fact]
    public async Task GetWantlist_NoQueryParameters_Returns200UsingDefaults()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[] { MakeWantlistRelease(1, "Artist", "Album") });

        // Act
        var response = await client.GetAsync("/api/v1/wantlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/wantlist?page=-1 — invalid page defaults to 1 ─────────────

    [Fact]
    public async Task GetWantlist_InvalidPageNumber_DefaultsToPage1()
    {
        // Arrange
        var releases = Enumerable.Range(1, 5)
            .Select(i => MakeWantlistRelease(i, $"Artist {i}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/wantlist?page=-1&pageSize=20");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.Items.Should().HaveCount(5);
    }

    // ── GET /api/v1/wantlist — page beyond data ───────────────────────────────

    [Fact]
    public async Task GetWantlist_PageBeyondAvailableData_Returns200WithEmptyItemsAndCorrectCount()
    {
        // Arrange — 15 releases, request page 5 of pageSize 20
        var releases = Enumerable.Range(1, 15)
            .Select(i => MakeWantlistRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/wantlist?page=5&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(15);
    }

    // ── GET /api/v1/wantlist — second page ────────────────────────────────────

    [Fact]
    public async Task GetWantlist_SecondPage_Returns200WithNonOverlappingItems()
    {
        // Arrange — 30 releases, alphabetical artists A01..A30
        var releases = Enumerable.Range(1, 30)
            .Select(i => MakeWantlistRelease(i, $"Artist {i:D2}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var page1Response = await client.GetAsync("/api/v1/wantlist?page=1&pageSize=20");
        var page2Response = await client.GetAsync("/api/v1/wantlist?page=2&pageSize=20");

        // Assert
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<WantlistReleaseDto>>();

        page2!.Items.Should().NotBeEmpty();
        var page1Titles = page1!.Items.Select(i => i.Title).ToHashSet();
        page2.Items.Select(i => i.Title).Should().NotIntersectWith(page1Titles);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WantlistRelease MakeWantlistRelease(int discogsId, string artist, string title, int? year = 2000,
        string format = "Vinyl", string? genre = null) =>
        new()
        {
            AddedAt = DateTimeOffset.UtcNow,
            Artist = artist,
            CoverImageUrl = null,
            DiscogsId = discogsId,
            Format = format,
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            ThumbnailUrl = null,
            Title = title,
            Year = year
        };

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory for wantlist endpoint tests.
    /// </summary>
    private sealed class WantlistFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"wantlist-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
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

                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                BooksEndpointTests.RemoveBooksSyncServiceDescriptors(services);

                services.AddSingleton<ISyncService>(new NoOpSyncService());
                services.AddSingleton<IBooksSyncService>(new NoOpBooksSyncService());

                services.AddAuthentication(TestAuthHandler.SchemeName)
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
