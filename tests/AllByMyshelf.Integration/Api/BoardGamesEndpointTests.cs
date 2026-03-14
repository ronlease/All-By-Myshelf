// Feature: BoardGameGeek integration endpoint (ABM-056)
//
// Scenario: Retrieve a single board game by ID
//   Given the database contains a board game with a known GUID
//   When I request GET /api/v1/boardgames/{id}
//   Then the response is HTTP 200 OK
//   And the response body contains the board game detail
//
// Scenario: Request a board game that does not exist
//   Given the database does not contain a board game with the specified ID
//   When I request GET /api/v1/boardgames/{id}
//   Then the response is HTTP 404 Not Found
//
// Scenario: Retrieve the first page of board games
//   Given the database contains board games
//   When I request GET /api/v1/boardgames?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains up to 20 board games
//
// Scenario: Database contains no board games
//   Given no sync has been run and the database contains no board games
//   When I request GET /api/v1/boardgames?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains an empty board games array
//   And the total record count is 0
//
// Scenario: Retrieve a random board game
//   Given the database contains board games
//   When I request GET /api/v1/boardgames/random
//   Then the response is HTTP 200 OK
//   And the response body contains a board game
//
// Scenario: Retrieve random board game from empty database
//   Given the database contains no board games
//   When I request GET /api/v1/boardgames/random
//   Then the response is HTTP 404 Not Found

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Bgg;
using AllByMyshelf.Api.Features.Discogs;
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
/// Integration tests for GET /api/v1/boardgames and related endpoints.
/// Each test spins up its own in-memory database so state never leaks between tests.
/// </summary>
public class BoardGamesEndpointTests
{
    // ── Factory helper ────────────────────────────────────────────────────────

    private static HttpClient CreateClient(IBoardGamesSyncService syncService)
    {
        var factory = new BoardGamesFactory(syncService);
        return factory.CreateClient();
    }

    /// <summary>Seeds the in-memory database with the given board games, returns a fresh client.</summary>
    private static HttpClient CreateClientWithSeededData(IEnumerable<BoardGame> boardGames, IBoardGamesSyncService? syncService = null)
    {
        syncService ??= new NoOpBoardGamesSyncService();
        var factory = new BoardGamesFactory(syncService);
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        // Clear any data from a previous test in the same factory instance.
        db.BoardGames.RemoveRange(db.BoardGames);
        db.BoardGames.AddRange(boardGames);
        db.SaveChanges();

        return client;
    }

    // ── GET /api/v1/boardgames/{id} — existing board game ────────────────────

    [Fact]
    public async Task GetBoardGame_ExistingId_Returns200WithBoardGameDetail()
    {
        // Arrange
        var boardGame = MakeBoardGame(1, "Catan", "Klaus Teuber", "Strategy", 1995);
        var client = CreateClientWithSeededData(new[] { boardGame });

        // Act
        var response = await client.GetAsync($"/api/v1/boardgames/{boardGame.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BoardGameDetailDto>();
        body.Should().NotBeNull();
        body!.BggId.Should().Be(1);
        body.Designer.Should().Be("Klaus Teuber");
        body.Genre.Should().Be("Strategy");
        body.Id.Should().Be(boardGame.Id);
        body.Title.Should().Be("Catan");
        body.YearPublished.Should().Be(1995);
    }

    // ── GET /api/v1/boardgames/{id} — non-existent board game ────────────────

    [Fact]
    public async Task GetBoardGame_NonExistentId_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<BoardGame>());

        // Act
        var response = await client.GetAsync($"/api/v1/boardgames/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/boardgames — empty database ──────────────────────────────

    [Fact]
    public async Task GetBoardGames_EmptyDatabase_Returns200WithEmptyArray()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<BoardGame>());

        // Act
        var response = await client.GetAsync("/api/v1/boardgames?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<BoardGameDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/boardgames — first page ──────────────────────────────────

    [Fact]
    public async Task GetBoardGames_FirstPage_Returns200WithCorrectCount()
    {
        // Arrange
        var boardGames = new[]
        {
            MakeBoardGame(1, "Catan", "Klaus Teuber", "Strategy", 1995),
            MakeBoardGame(2, "Ticket to Ride", "Alan R. Moon", "Family", 2004),
            MakeBoardGame(3, "Pandemic", "Matt Leacock", "Cooperative", 2008),
            MakeBoardGame(4, "Carcassonne", "Klaus-Jürgen Wrede", "Strategy", 2000),
            MakeBoardGame(5, "Azul", "Michael Kiesling", "Abstract", 2017)
        };
        var client = CreateClientWithSeededData(boardGames);

        // Act
        var response = await client.GetAsync("/api/v1/boardgames?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<BoardGameDto>>();
        body!.Items.Should().HaveCount(5);
        body.TotalCount.Should().Be(5);
    }

    // ── GET /api/v1/boardgames/random — with board game ──────────────────────

    [Fact]
    public async Task GetRandom_DatabaseHasBoardGames_Returns200()
    {
        // Arrange
        var boardGame = MakeBoardGame(1, "Catan", "Klaus Teuber", "Strategy", 1995);
        var client = CreateClientWithSeededData(new[] { boardGame });

        // Act
        var response = await client.GetAsync("/api/v1/boardgames/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BoardGameDto>();
        body.Should().NotBeNull();
        body!.BggId.Should().Be(1);
        body.Title.Should().Be("Catan");
    }

    // ── GET /api/v1/boardgames/random — empty database ───────────────────────

    [Fact]
    public async Task GetRandom_EmptyDatabase_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<BoardGame>());

        // Act
        var response = await client.GetAsync("/api/v1/boardgames/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGame MakeBoardGame(int bggId, string title, string? designer = null, string? genre = null, int? yearPublished = null) =>
        new()
        {
            BggId = bggId,
            CoverImageUrl = null,
            Description = null,
            Designer = designer,
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            MaxPlayers = null,
            MaxPlaytime = null,
            MinPlayers = null,
            MinPlaytime = null,
            ThumbnailUrl = null,
            Title = title,
            YearPublished = yearPublished
        };

    /// <summary>
    /// Removes the service registrations that Program.cs creates for BoardGamesSyncService.
    /// Note: RemoveSyncServiceDescriptors should be called first to also remove SyncService.
    /// </summary>
    internal static void RemoveBoardGamesSyncServiceDescriptors(IServiceCollection services)
    {
        // Remove concrete BoardGamesSyncService singleton and IBoardGamesSyncService forwarding lambda
        services.RemoveAll<BoardGamesSyncService>();
        services.RemoveAll<IBoardGamesSyncService>();

        // Note: IHostedService factory lambda is already removed by RemoveSyncServiceDescriptors
        // which removes all factory-based IHostedService registrations (both SyncService and
        // BoardGamesSyncService use this pattern).
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that:
    /// - substitutes the EF Core in-memory provider for PostgreSQL
    /// - injects required configuration values to satisfy ValidateOnStart
    /// - replaces JWT bearer authentication with a no-op scheme so [Authorize] passes
    /// - removes the BoardGamesSyncService BackgroundService to avoid background work during tests
    /// </summary>
    private sealed class BoardGamesFactory(IBoardGamesSyncService syncService) : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"boardgames-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Satisfy ValidateOnStart for configuration options
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Hardcover:ApiToken"] = "integration-test-token",
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
                        .UseInMemoryDatabase(_dbName));

                // Remove all SyncService and BoardGamesSyncService registrations
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                RemoveBoardGamesSyncServiceDescriptors(services);
                services.AddSingleton(syncService);

                // Replace JWT bearer with a test scheme that always authenticates
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });

            builder.UseEnvironment("Testing");
        }
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class NoOpBoardGamesSyncService : IBoardGamesSyncService
    {
        public bool IsSyncRunning => false;
        public SyncStartResult TryStartSync() => SyncStartResult.Started;
    }
}
