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
//
// Feature: Unified statistics endpoint (ABM-034)
//
// Scenario: Returns 200 with both records and books statistics
//   Given 2 releases and 3 books exist in the database
//   When GET /api/v1/statistics is called with a valid auth token
//   Then the response status is 200
//   And Records.TotalCount is 2
//   And Books.TotalCount is 3
//
// Scenario: Returns 200 with zero counts when database is empty
//   Given no releases or books exist
//   When GET /api/v1/statistics is called with a valid auth token
//   Then the response status is 200
//   And Records.TotalCount is 0
//   And Books.TotalCount is 0
//
// Scenario: Returns 200 with correct breakdowns for records
//   Given releases with various formats, genres, and years
//   When GET /api/v1/statistics is called with a valid auth token
//   Then the response status is 200
//   And Records.FormatBreakdown is populated
//   And Records.GenreBreakdown is populated
//   And Records.DecadeBreakdown is populated

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

    /// <summary>Seeds the in-memory database with the given releases and books, returns a fresh client.</summary>
    private HttpClient CreateClientWithSeededData(IEnumerable<Release> releases, IEnumerable<Book> books)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        // Clear any data from a previous test in the same factory instance.
        db.Releases.RemoveRange(db.Releases);
        db.Books.RemoveRange(db.Books);
        db.Releases.AddRange(releases);
        db.Books.AddRange(books);
        db.SaveChanges();

        return client;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Book MakeBook(int hardcoverId, string title, string? genre = null) =>
        new()
        {
            Authors = new List<string> { "Author" },
            Genre = genre,
            HardcoverId = hardcoverId,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title
        };

    private static Release MakeRelease(int discogsId, string? format = null, string? genre = null, int? year = null) =>
        new()
        {
            Artists = new List<string> { $"Artist {discogsId}" },
            DiscogsId = discogsId,
            Format = format ?? "Vinyl",
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            LowestPrice = null,
            Title = $"Title {discogsId}",
            Year = year
        };

    private static Release MakeReleaseWithPrice(int discogsId, decimal? lowestPrice) =>
        new()
        {
            Artists = new List<string> { $"Artist {discogsId}" },
            DiscogsId = discogsId,
            Format = "Vinyl",
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            LowestPrice = lowestPrice,
            Title = $"Title {discogsId}",
            Year = 2000
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

    // ── GET /api/v1/statistics — empty database ───────────────────────────────

    [Fact]
    public async Task GetUnifiedStatistics_EmptyDatabase_Returns200WithZeroCounts()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Release>(), Array.Empty<Book>());

        // Act
        var response = await client.GetAsync("/api/v1/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UnifiedStatisticsDto>();
        body.Should().NotBeNull();
        body!.Records.TotalCount.Should().Be(0);
        body.Books.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/statistics — records with breakdowns ──────────────────────

    [Fact]
    public async Task GetUnifiedStatistics_RecordsWithBreakdowns_ReturnsCorrectBreakdowns()
    {
        // Arrange
        var releases = new[]
        {
            MakeRelease(1, format: "LP", genre: "Rock", year: 1975),
            MakeRelease(2, format: "LP", genre: "Jazz", year: 1978),
            MakeRelease(3, format: "CD", genre: "Rock", year: 1992)
        };
        var client = CreateClientWithSeededData(releases, Array.Empty<Book>());

        // Act
        var response = await client.GetAsync("/api/v1/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UnifiedStatisticsDto>();
        body.Should().NotBeNull();
        body!.Records.FormatBreakdown.Should().NotBeEmpty();
        body.Records.GenreBreakdown.Should().NotBeEmpty();
        body.Records.DecadeBreakdown.Should().NotBeEmpty();
    }

    // ── GET /api/v1/statistics — with releases and books ──────────────────────

    [Fact]
    public async Task GetUnifiedStatistics_WithReleasesAndBooks_Returns200WithBothSections()
    {
        // Arrange
        var releases = new[]
        {
            MakeRelease(1),
            MakeRelease(2)
        };
        var books = new[]
        {
            MakeBook(1, "Book 1"),
            MakeBook(2, "Book 2"),
            MakeBook(3, "Book 3")
        };
        var client = CreateClientWithSeededData(releases, books);

        // Act
        var response = await client.GetAsync("/api/v1/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UnifiedStatisticsDto>();
        body.Should().NotBeNull();
        body!.Records.TotalCount.Should().Be(2);
        body.Books.TotalCount.Should().Be(3);
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

                // Remove all SyncService and BooksSyncService registrations
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                BooksEndpointTests.RemoveBooksSyncServiceDescriptors(services);
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
        public SyncProgressDto Progress => new(0, false, null, null, SyncConstants.Statuses.Idle, 0);
        public SyncOptionsDto SyncOptions => new();
        public Task<SyncEstimateDto> GetEstimateAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new SyncEstimateDto(0, 0, 0));
        public SyncStartResult TryStartSync(SyncOptionsDto? options = null) => SyncStartResult.Started;
    }
}
