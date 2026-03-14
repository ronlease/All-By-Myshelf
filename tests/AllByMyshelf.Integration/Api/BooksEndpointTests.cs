// Feature: Book detail endpoint (ABM-049)
//
// Scenario: Retrieve a single book by ID
//   Given the database contains a book with a known GUID
//   When I request GET /api/v1/books/{id}
//   Then the response is HTTP 200 OK
//   And the response body contains the book detail
//
// Scenario: Request a book that does not exist
//   Given the database does not contain a book with the specified ID
//   When I request GET /api/v1/books/{id}
//   Then the response is HTTP 404 Not Found
//
// Feature: Paginated books collection endpoint  (ABM-032)
// Feature: Manual Hardcover sync trigger endpoint (ABM-031)
//
// Scenario: Retrieve the first page of books
//   Given the database contains books
//   When I request GET /api/v1/books?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains up to 20 books
//   And each book includes author, title, year, and genre
//   And the response includes total record count and total page count
//
// Scenario: Retrieve a subsequent page
//   Given the database contains more than 20 books
//   When I request GET /api/v1/books?page=2&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains books from the second page
//   And the books on page 2 do not overlap with those on page 1
//
// Scenario: Request a page beyond the available data
//   Given the database contains 15 books
//   When I request GET /api/v1/books?page=5&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains an empty books array
//   And the total record count still reflects 15
//
// Scenario: Database contains no books
//   Given no sync has been run and the database contains no books
//   When I request GET /api/v1/books?page=1&pageSize=20
//   Then the response is HTTP 200 OK
//   And the response body contains an empty books array
//   And the total record count is 0
//
// Scenario: Successfully trigger a Hardcover sync
//   Given the Hardcover API token is configured
//   And no sync is currently running
//   When I send POST /api/v1/books/sync
//   Then the response is HTTP 202 Accepted
//   And the response body includes a message confirming the sync has started
//
// Scenario: Attempt to trigger sync while one is already running
//   Given a sync is currently in progress
//   When I send POST /api/v1/books/sync
//   Then the response is HTTP 409 Conflict
//   And the response body explains that a sync is already in progress
//
// Scenario: Attempt to trigger sync with no token configured
//   Given the Hardcover API token is NOT configured
//   When I send POST /api/v1/books/sync
//   Then the response is HTTP 503 Service Unavailable
//   And the response body explains that the Hardcover token is not configured
//
// Feature: Pagination validation
//
// Scenario: GET /api/v1/books?page=-1 defaults invalid page to 1
//   Given the database contains books
//   When I request GET /api/v1/books?page=-1
//   Then the response is HTTP 200 OK
//   And the response indicates page 1
//
// Note: Filter tests (author, genre, etc.) cannot be tested with the EF Core
// in-memory provider because they rely on PostgreSQL-specific EF.Functions.ILike.
// These features require integration tests against a real PostgreSQL database
// or unit tests with mocked repositories.

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
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
/// Integration tests for GET /api/v1/books and POST /api/v1/books/sync.
/// Each test spins up its own in-memory database so state never leaks between tests.
/// </summary>
public class BooksEndpointTests
{
    // ── Factory helper ────────────────────────────────────────────────────────

    private static HttpClient CreateClient(IBooksSyncService syncService)
    {
        var factory = new BooksFactory(syncService);
        return factory.CreateClient();
    }

    /// <summary>Seeds the in-memory database with the given books, returns a fresh client.</summary>
    private static HttpClient CreateClientWithSeededData(IEnumerable<Book> books, IBooksSyncService? syncService = null)
    {
        syncService ??= new NoOpBooksSyncService();
        var factory = new BooksFactory(syncService);
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllByMyshelfDbContext>();
        // Clear any data from a previous test in the same factory instance.
        db.Books.RemoveRange(db.Books);
        db.Books.AddRange(books);
        db.SaveChanges();

        return client;
    }

    // ── GET /api/v1/books/{id} — existing book ────────────────────────────────

    [Fact]
    public async Task GetBook_ExistingId_Returns200WithBookDetail()
    {
        // Arrange
        var book = MakeBook(1, "Neil Gaiman", "American Gods", 2001, "Fantasy");
        var client = CreateClientWithSeededData(new[] { book });

        // Act
        var response = await client.GetAsync($"/api/v1/books/{book.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BookDetailDto>();
        body.Should().NotBeNull();
        body!.Author.Should().Be("Neil Gaiman");
        body.Genre.Should().Be("Fantasy");
        body.HardcoverId.Should().Be(1);
        body.Id.Should().Be(book.Id);
        body.Title.Should().Be("American Gods");
        body.Year.Should().Be(2001);
    }

    // ── GET /api/v1/books/{id} — non-existent book ─────────────────────────

    [Fact]
    public async Task GetBook_NonExistentId_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Book>());

        // Act
        var response = await client.GetAsync($"/api/v1/books/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/books — empty database ────────────────────────────────────

    [Fact]
    public async Task GetBooks_EmptyDatabase_Returns200WithEmptyArrayAndZeroCount()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Book>());

        // Act
        var response = await client.GetAsync("/api/v1/books?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    // ── GET /api/v1/books — first page ────────────────────────────────────────

    [Fact]
    public async Task GetBooks_FirstPage_EachBookContainsAuthorTitleYearGenre()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[]
        {
            MakeBook(1, "Neil Gaiman", "American Gods", 2001, "Fantasy")
        });

        // Act
        var response = await client.GetAsync("/api/v1/books?page=1&pageSize=20");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();

        // Assert
        var item = body!.Items.Single();
        item.Author.Should().Be("Neil Gaiman");
        item.Title.Should().Be("American Gods");
        item.Year.Should().Be(2001);
        item.Genre.Should().Be("Fantasy");
    }

    [Fact]
    public async Task GetBooks_FirstPage_Returns200WithBooksAndCorrectTotalCount()
    {
        // Arrange
        var books = Enumerable.Range(1, 30)
            .Select(i => MakeBook(i, $"Author {i:D2}", $"Book {i}"))
            .ToList();
        var client = CreateClientWithSeededData(books);

        // Act
        var response = await client.GetAsync("/api/v1/books?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        body!.Items.Should().HaveCount(20);
        body.TotalCount.Should().Be(30);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(20);
    }

    // ── GET /api/v1/books — default parameters ────────────────────────────────

    [Fact]
    public async Task GetBooks_NoQueryParameters_Returns200UsingDefaults()
    {
        // Arrange
        var client = CreateClientWithSeededData(new[] { MakeBook(1, "Author", "Book") });

        // Act
        var response = await client.GetAsync("/api/v1/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/v1/books?page=-1 — invalid page defaults to 1 ────────────────

    [Fact]
    public async Task GetBooks_InvalidPageNumber_DefaultsToPage1()
    {
        // Arrange
        var books = Enumerable.Range(1, 5)
            .Select(i => MakeBook(i, $"Author {i}", $"Book {i}"))
            .ToList();
        var client = CreateClientWithSeededData(books);

        // Act
        var response = await client.GetAsync("/api/v1/books?page=-1&pageSize=20");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.Items.Should().HaveCount(5);
    }

    // ── GET /api/v1/books — page beyond data ──────────────────────────────────

    [Fact]
    public async Task GetBooks_PageBeyondAvailableData_Returns200WithEmptyItemsAndCorrectCount()
    {
        // Arrange — 15 books, request page 5 of pageSize 20
        var books = Enumerable.Range(1, 15)
            .Select(i => MakeBook(i, $"Author {i:D2}", $"Book {i}"))
            .ToList();
        var client = CreateClientWithSeededData(books);

        // Act
        var response = await client.GetAsync("/api/v1/books?page=5&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(15);
    }

    // ── GET /api/v1/books — second page ───────────────────────────────────────

    [Fact]
    public async Task GetBooks_SecondPage_Returns200WithNonOverlappingItems()
    {
        // Arrange — 30 books, alphabetical titles
        var books = Enumerable.Range(1, 30)
            .Select(i => MakeBook(i, $"Author {i:D2}", $"Book {i:D2}"))
            .ToList();
        var client = CreateClientWithSeededData(books);

        // Act
        var page1Response = await client.GetAsync("/api/v1/books?page=1&pageSize=20");
        var page2Response = await client.GetAsync("/api/v1/books?page=2&pageSize=20");

        // Assert
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();

        page2!.Items.Should().NotBeEmpty();
        var page1Titles = page1!.Items.Select(i => i.Title).ToHashSet();
        page2.Items.Select(i => i.Title).Should().NotIntersectWith(page1Titles);
    }

    // ── GET /api/v1/books/random — random book ───────────────────────────────

    [Fact]
    public async Task GetRandom_DatabaseHasBooks_Returns200WithBook()
    {
        // Arrange
        var book = MakeBook(1, "Neil Gaiman", "American Gods", 2001, "Fantasy");
        var client = CreateClientWithSeededData(new[] { book });

        // Act
        var response = await client.GetAsync("/api/v1/books/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BookDto>();
        body.Should().NotBeNull();
        body!.Author.Should().Be("Neil Gaiman");
        body.Title.Should().Be("American Gods");
    }

    [Fact]
    public async Task GetRandom_EmptyDatabase_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Book>());

        // Act
        var response = await client.GetAsync("/api/v1/books/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/v1/books/sync — 202 Accepted ───────────────────────────────

    [Fact]
    public async Task TriggerSync_NoSyncRunning_ResponseBodyContainsStartedMessage()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.Started);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().ContainEquivalentOf("sync started");
    }

    [Fact]
    public async Task TriggerSync_NoSyncRunning_Returns202Accepted()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.Started);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ── POST /api/v1/books/sync — 409 Conflict ───────────────────────────────

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_IsSyncRunningRemainsTrue()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.AlreadyRunning, isSyncRunning: true);
        var client = CreateClient(syncService);

        // Act
        await client.PostAsync("/api/v1/books/sync", null);

        // Assert — the stub was not reset; it still reports running
        syncService.IsSyncRunning.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_ResponseBodyExplainsConflict()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.AlreadyRunning);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert — body should describe the conflict
        body.Should().ContainEquivalentOf("already");
    }

    [Fact]
    public async Task TriggerSync_SyncAlreadyRunning_Returns409Conflict()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.AlreadyRunning);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /api/v1/books/sync — 503 Service Unavailable ────────────────────

    [Fact]
    public async Task TriggerSync_TokenNotConfigured_ResponseBodyExplainsMissingToken()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.TokenNotConfigured);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.Should().ContainEquivalentOf("token");
    }

    [Fact]
    public async Task TriggerSync_TokenNotConfigured_Returns503ServiceUnavailable()
    {
        // Arrange
        var syncService = new StubBooksSyncService(SyncStartResult.TokenNotConfigured);
        var client = CreateClient(syncService);

        // Act
        var response = await client.PostAsync("/api/v1/books/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Book MakeBook(int hardcoverId, string author, string title, int? year = 2000,
        string genre = "Fiction") =>
        new()
        {
            Author = author,
            CoverImageUrl = null,
            Genre = genre,
            HardcoverId = hardcoverId,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title,
            Year = year
        };

    /// <summary>
    /// Removes the three service registrations that Program.cs creates for BooksSyncService.
    /// Note: RemoveSyncServiceDescriptors should be called first to also remove SyncService.
    /// </summary>
    internal static void RemoveBooksSyncServiceDescriptors(IServiceCollection services)
    {
        // Remove concrete BooksSyncService singleton and IBooksSyncService forwarding lambda
        services.RemoveAll<BooksSyncService>();
        services.RemoveAll<IBooksSyncService>();

        // Note: IHostedService factory lambda is already removed by RemoveSyncServiceDescriptors
        // which removes all factory-based IHostedService registrations (both SyncService and
        // BooksSyncService use this pattern).
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that:
    /// - substitutes the EF Core in-memory provider for PostgreSQL
    /// - injects required Hardcover and Discogs configuration values to satisfy ValidateOnStart
    /// - replaces JWT bearer authentication with a no-op scheme so [Authorize] passes
    /// - removes the BooksSyncService BackgroundService to avoid background work during tests
    /// </summary>
    private sealed class BooksFactory(IBooksSyncService syncService) : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"books-integration-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Satisfy ValidateOnStart for HardcoverOptions and DiscogsOptions
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

                // Remove all SyncService and BooksSyncService registrations
                ReleasesEndpointTests.RemoveSyncServiceDescriptors(services);
                RemoveBooksSyncServiceDescriptors(services);
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

    private sealed class NoOpBooksSyncService : IBooksSyncService
    {
        public bool IsSyncRunning => false;
        public SyncStartResult TryStartSync() => SyncStartResult.Started;
    }

    private sealed class StubBooksSyncService(SyncStartResult result, bool isSyncRunning = false)
        : IBooksSyncService
    {
        public bool IsSyncRunning { get; } = isSyncRunning;
        public SyncStartResult TryStartSync() => result;
    }
}
