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
//
// Feature: Recently added releases          (ABM-021)
//
// Scenario: GET /api/v1/releases/recent returns releases added within the last 30 days
//   Given releases were added at various dates
//   When I request GET /api/v1/releases/recent
//   Then the response is HTTP 200 OK
//   And only releases with AddedAt within the last 30 days are returned
//   And they are ordered by AddedAt descending (newest first)
//
// Scenario: GET /api/v1/releases/recent returns an empty array when no recent releases
//   Given no releases have been added within the last 30 days
//   When I request GET /api/v1/releases/recent
//   Then the response is HTTP 200 OK
//   And the response body is an empty array
//
// Scenario: GET /api/v1/releases/recent returns at most 10 releases
//   Given more than 10 releases were added within the last 30 days
//   When I request GET /api/v1/releases/recent
//   Then the response is HTTP 200 OK
//   And the response body contains at most 10 releases
//
// Feature: Collection maintenance view      (ABM-029)
//
// Scenario: GET /api/v1/releases/maintenance returns releases with incomplete data
//   Given some releases have null Genre, Year, or price fields
//   When I request GET /api/v1/releases/maintenance
//   Then the response is HTTP 200 OK
//   And each result includes the list of missing field names
//
// Scenario: GET /api/v1/releases/maintenance returns an empty array when all data is complete
//   Given all releases have every field populated
//   When I request GET /api/v1/releases/maintenance
//   Then the response is HTTP 200 OK
//   And the response body is an empty array
//
// Scenario: GET /api/v1/releases/maintenance does not include complete releases
//   Given some releases are complete and some are not
//   When I request GET /api/v1/releases/maintenance
//   Then only incomplete releases appear in the response
//
// Feature: Random release picker              (ABM-025)
//
// Scenario: GET /api/v1/releases/random returns a release when database has releases
//   Given the database contains releases
//   When I request GET /api/v1/releases/random
//   Then the response is HTTP 200 OK
//   And the response body contains a ReleaseDetailDto
//
// Scenario: GET /api/v1/releases/random returns 404 when database is empty
//   Given the database contains no releases
//   When I request GET /api/v1/releases/random
//   Then the response is HTTP 404 Not Found
//
// Feature: Pagination validation
//
// Scenario: GET /api/v1/releases?page=-1 defaults invalid page to 1
//   Given the database contains releases
//   When I request GET /api/v1/releases?page=-1
//   Then the response is HTTP 200 OK
//   And the response indicates page 1
//
// Note: Search and filter tests (search, genre, artist, etc.) cannot be tested
// with the EF Core in-memory provider because they rely on PostgreSQL-specific
// EF.Functions.ILike. These features require integration tests against a real
// PostgreSQL database or unit tests with mocked repositories.

using System.Net;
using System.Net.Http.Json;
using AllByMyshelf.Api.Common;
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
        body.Artists.Should().BeEquivalentTo(new[] { "John Coltrane" });
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
            Artists = new List<string> { "Miles Davis" },
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

    // ── GET /api/v1/releases?page=-1 — invalid page defaults to 1 ─────────────

    [Fact]
    public async Task GetReleases_InvalidPageNumber_DefaultsToPage1()
    {
        // Arrange
        var releases = Enumerable.Range(1, 5)
            .Select(i => MakeRelease(i, $"Artist {i}", $"Album {i}"))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/releases?page=-1&pageSize=25");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReleaseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.Items.Should().HaveCount(5);
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
        item.Artists.Should().BeEquivalentTo(new[] { "John Coltrane" });
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

    // ── GET /api/v1/releases/random — random release ──────────────────────────

    [Fact]
    public async Task GetRandom_DatabaseHasReleases_Returns200WithRelease()
    {
        // Arrange
        var release = MakeDetailedRelease(Guid.NewGuid(), 1001);
        var client = CreateClientWithSeededData(new[] { release });

        // Act
        var response = await client.GetAsync("/api/v1/releases/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ReleaseDetailDto>();
        body.Should().NotBeNull();
        body!.Artists.Should().BeEquivalentTo(new[] { "John Coltrane" });
    }

    [Fact]
    public async Task GetRandom_EmptyDatabase_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Release>());

        // Act
        var response = await client.GetAsync("/api/v1/releases/random");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/releases/recent — recently added ──────────────────────────

    [Fact]
    public async Task GetRecentlyAdded_ReleasesAddedWithinLast30Days_ReturnsOnlyRecentReleases()
    {
        // Arrange
        var recent = MakeRelease(1, "Artist A", "Recent Album", addedAt: DateTimeOffset.UtcNow.AddDays(-5));
        var old = MakeRelease(2, "Artist B", "Old Album", addedAt: DateTimeOffset.UtcNow.AddDays(-60));
        var client = CreateClientWithSeededData(new[] { recent, old });

        // Act
        var response = await client.GetAsync("/api/v1/releases/recent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ReleaseDto>>();
        body.Should().HaveCount(1);
        body![0].Artists.Should().BeEquivalentTo(new[] { "Artist A" });
    }

    [Fact]
    public async Task GetRecentlyAdded_NoRecentReleases_ReturnsEmptyArray()
    {
        // Arrange — all releases added more than 30 days ago
        var old = MakeRelease(1, "Artist", "Old Album", addedAt: DateTimeOffset.UtcNow.AddDays(-60));
        var client = CreateClientWithSeededData(new[] { old });

        // Act
        var response = await client.GetAsync("/api/v1/releases/recent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ReleaseDto>>();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentlyAdded_MoreThan10RecentReleases_ReturnsAtMost10()
    {
        // Arrange — 15 releases all added recently
        var releases = Enumerable.Range(1, 15)
            .Select(i => MakeRelease(i, $"Artist {i}", $"Album {i}",
                addedAt: DateTimeOffset.UtcNow.AddDays(-i)))
            .ToList();
        var client = CreateClientWithSeededData(releases);

        // Act
        var response = await client.GetAsync("/api/v1/releases/recent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ReleaseDto>>();
        body.Should().HaveCountLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetRecentlyAdded_ReturnsNewestFirst()
    {
        // Arrange
        var newest = MakeRelease(1, "Artist A", "Newest", addedAt: DateTimeOffset.UtcNow.AddDays(-1));
        var middle = MakeRelease(2, "Artist B", "Middle", addedAt: DateTimeOffset.UtcNow.AddDays(-10));
        var oldest = MakeRelease(3, "Artist C", "Oldest", addedAt: DateTimeOffset.UtcNow.AddDays(-20));
        var client = CreateClientWithSeededData(new[] { oldest, newest, middle });

        // Act
        var response = await client.GetAsync("/api/v1/releases/recent");
        var body = await response.Content.ReadFromJsonAsync<List<ReleaseDto>>();

        // Assert
        body.Should().HaveCount(3);
        body![0].Title.Should().Be("Newest");
        body[1].Title.Should().Be("Middle");
        body[2].Title.Should().Be("Oldest");
    }

    // ── GET /api/v1/releases/duplicates — duplicate releases ─────────────────

    [Fact]
    public async Task GetDuplicates_ReleasesWithSameArtistAndTitle_ReturnsGroupedDuplicates()
    {
        // Arrange — two releases with same artist and title but different Discogs IDs
        var release1 = new Release
        {
            Artists = new List<string> { "John Coltrane" },
            DiscogsId = 100,
            Format = "Vinyl",
            Genre = "Jazz",
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = "A Love Supreme",
            Year = 1964
        };
        var release2 = new Release
        {
            Artists = new List<string> { "John Coltrane" },
            DiscogsId = 200,
            Format = "CD",
            Genre = "Jazz",
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = "A Love Supreme",
            Year = 1965
        };
        var client = CreateClientWithSeededData(new[] { release1, release2 });

        // Act
        var response = await client.GetAsync("/api/v1/releases/duplicates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DuplicateGroupDto>>();
        body.Should().HaveCount(1);
        body![0].Artists.Should().BeEquivalentTo(new[] { "John Coltrane" });
        body[0].Title.Should().Be("A Love Supreme");
        body[0].Releases.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDuplicates_NoDuplicates_ReturnsEmptyArray()
    {
        // Arrange — releases with different titles
        var release1 = MakeRelease(1, "Artist A", "Album A");
        var release2 = MakeRelease(2, "Artist B", "Album B");
        var client = CreateClientWithSeededData(new[] { release1, release2 });

        // Act
        var response = await client.GetAsync("/api/v1/releases/duplicates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DuplicateGroupDto>>();
        body.Should().BeEmpty();
    }

    // ── PUT /api/v1/releases/{id}/notes-rating — update notes and rating ─────

    [Fact]
    public async Task UpdateNotesAndRating_ValidData_Returns204()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeRelease(1, "Artist", "Album");
        release.Id = id;
        var client = CreateClientWithSeededData(new[] { release });
        var dto = new { notes = "Great album!", rating = 5 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/releases/{id}/notes-rating", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateNotesAndRating_ValidData_UpdatesDatabase()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeRelease(1, "Artist", "Album");
        release.Id = id;
        var factory = _factory; // Need to access factory to verify DB state
        var client = CreateClientWithSeededData(new[] { release });
        var dto = new { notes = "Great album!", rating = 5 };

        // Act
        await client.PutAsJsonAsync($"/api/v1/releases/{id}/notes-rating", dto);

        // Assert — verify by reading back
        var getResponse = await client.GetAsync($"/api/v1/releases/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<ReleaseDetailDto>();
        body!.Notes.Should().Be("Great album!");
        body.Rating.Should().Be(5);
    }

    [Fact]
    public async Task UpdateNotesAndRating_InvalidRating_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeRelease(1, "Artist", "Album");
        release.Id = id;
        var client = CreateClientWithSeededData(new[] { release });
        var dto = new { notes = (string?)null, rating = 6 }; // rating > 5 is invalid

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/releases/{id}/notes-rating", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateNotesAndRating_RatingZero_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeRelease(1, "Artist", "Album");
        release.Id = id;
        var client = CreateClientWithSeededData(new[] { release });
        var dto = new { notes = (string?)null, rating = 0 }; // rating < 1 is invalid

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/releases/{id}/notes-rating", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateNotesAndRating_UnknownId_Returns404()
    {
        // Arrange
        var client = CreateClientWithSeededData(Array.Empty<Release>());
        var unknownId = Guid.NewGuid();
        var dto = new { notes = "Notes", rating = (int?)null };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/releases/{unknownId}/notes-rating", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateNotesAndRating_NullRating_Returns204()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeRelease(1, "Artist", "Album");
        release.Id = id;
        var client = CreateClientWithSeededData(new[] { release });
        var dto = new { notes = "Just notes", rating = (int?)null };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/releases/{id}/notes-rating", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── GET /api/v1/releases/maintenance — incomplete releases ──────────────

    [Fact]
    public async Task GetMaintenance_ReleasesWithMissingFields_ReturnsIncompleteWithFieldNames()
    {
        // Arrange — release missing Genre and Year
        var incomplete = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 100,
            Artists = new List<string> { "Artist" },
            Format = "Vinyl",
            Genre = null,
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = "Incomplete Album",
            Year = null
        };
        var client = CreateClientWithSeededData(new[] { incomplete });

        // Act
        var response = await client.GetAsync("/api/v1/releases/maintenance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MaintenanceReleaseDto>>();
        body.Should().HaveCount(1);
        body![0].MissingFields.Should().Contain("Genre");
        body[0].MissingFields.Should().Contain("Year");
        body[0].MissingFields.Should().Contain("Cover Art");
        body[0].MissingFields.Should().NotContain("Pricing");
    }

    [Fact]
    public async Task GetMaintenance_AllReleasesComplete_ReturnsEmptyArray()
    {
        // Arrange — fully populated release
        var complete = MakeCompleteRelease(1);
        var client = CreateClientWithSeededData(new[] { complete });

        // Act
        var response = await client.GetAsync("/api/v1/releases/maintenance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MaintenanceReleaseDto>>();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMaintenance_MixedReleases_ReturnsOnlyIncomplete()
    {
        // Arrange
        var complete = MakeCompleteRelease(1);
        var incomplete = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 200,
            Artists = new List<string> { "Incomplete Artist" },
            CoverImageUrl = null,
            Format = "Vinyl",
            Genre = "Rock",
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = "Missing Cover",
            Year = 2020
        };
        var client = CreateClientWithSeededData(new[] { complete, incomplete });

        // Act
        var response = await client.GetAsync("/api/v1/releases/maintenance");
        var body = await response.Content.ReadFromJsonAsync<List<MaintenanceReleaseDto>>();

        // Assert
        body.Should().HaveCount(1);
        body![0].Artists.Should().BeEquivalentTo(new[] { "Incomplete Artist" });
        body[0].MissingFields.Should().Contain("Cover Art");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Release MakeCompleteRelease(int discogsId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CoverImageUrl = "https://example.com/cover.jpg",
            DiscogsId = discogsId,
            Artists = new List<string> { "Complete Artist" },
            Format = "Vinyl",
            Genre = "Rock",
            HighestPrice = 30m,
            LastSyncedAt = DateTimeOffset.UtcNow,
            LowestPrice = 10m,
            MedianPrice = 20m,
            Title = "Complete Album",
            Year = 2020
        };

    private static Release MakeDetailedRelease(Guid id, int discogsId) =>
        new()
        {
            Id = id,
            DiscogsId = discogsId,
            Artists = new List<string> { "John Coltrane" },
            Title = "A Love Supreme",
            Year = 1964,
            Format = "Vinyl",
            Genre = "Jazz",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

    private static Release MakeRelease(int discogsId, string artist, string title, int? year = 2000,
        string format = "Vinyl", DateTimeOffset? addedAt = null, string? genre = null) =>
        new()
        {
            AddedAt = addedAt,
            Artists = new List<string> { artist },
            DiscogsId = discogsId,
            Format = format,
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title,
            Year = year
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
