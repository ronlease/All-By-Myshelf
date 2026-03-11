// Feature: Persisting synced releases to the database  (ABM-003)
// Feature: Release detail view                          (ABM-012)
//
// Scenario: New releases are inserted on first sync
//   Given the local database contains no releases
//   When UpsertCollectionAsync is called with a list of releases
//   Then all releases are saved to the database
//   And each release has artist, title, year, and format persisted
//
// Scenario: Existing releases are updated on subsequent sync
//   Given the local database already contains releases from a previous sync
//   When UpsertCollectionAsync is called with updated data for the same DiscogsIds
//   Then the existing records are updated with the new values
//   And no duplicate records are created
//
// Scenario: Releases removed from Discogs are deleted from the database
//   Given the local database contains releases R1 and R2
//   When UpsertCollectionAsync is called with only R1
//   Then R2 is removed from the database
//   And R1 remains
//
// Scenario: Retrieve the first page of releases
//   Given the database contains releases
//   When GetPagedAsync is called with page=1, pageSize=N
//   Then up to N releases are returned ordered by artist then title
//   And TotalCount equals the total number of records
//
// Scenario: Retrieve a subsequent page — no overlap with page 1
//   Given the database contains more records than pageSize
//   When GetPagedAsync is called with page=2
//   Then the items are distinct from page 1
//
// Scenario: GetByIdAsync returns the correct release when found
//   Given the database contains a release with a known Guid
//   When GetByIdAsync is called with that Guid
//   Then the matching release is returned
//
// Scenario: GetByIdAsync returns null when not found
//   Given the database does not contain a release with the requested Guid
//   When GetByIdAsync is called
//   Then null is returned
//
// Scenario: Detail fields are persisted and retrieved correctly through upsert
//   Given a release with genre populated
//   When UpsertCollectionAsync is called
//   Then the detail field is stored and retrievable via GetByIdAsync

using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Unit.Repositories;

public class ReleasesRepositoryTests : IDisposable
{
    private readonly AllByMyshelfDbContext _db;
    private readonly ReleasesRepository _sut;

    public ReleasesRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AllByMyshelfDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AllByMyshelfDbContext(options);
        _sut = new ReleasesRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Release MakeRelease(int discogsId, string artist = "Artist", string title = "Title",
        int? year = 2000, string format = "Vinyl") =>
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

    // ── GetByIdAsync — found ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_KnownId_ReturnsCorrectRelease()
    {
        // Arrange
        var target = MakeRelease(901, artist: "Charles Mingus", title: "The Black Saint");
        _db.Releases.AddRange(target, MakeRelease(902, artist: "Other", title: "Other Album"));
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(target.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(target.Id);
        result.DiscogsId.Should().Be(901);
        result.Artist.Should().Be("Charles Mingus");
        result.Title.Should().Be("The Black Saint");
    }

    // ── GetByIdAsync — not found ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_EmptyDatabase_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        // Arrange
        _db.Releases.Add(MakeRelease(1000));
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetPagedAsync — ordering ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_EmptyDatabase_ReturnsEmptyItemsAndZeroCount()
    {
        // Act
        var (items, totalCount) = await _sut.GetPagedAsync(1, 25, CancellationToken.None);

        // Assert
        items.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_PageBeyondData_ReturnsEmptyItems()
    {
        // Arrange
        _db.Releases.AddRange(Enumerable.Range(1, 3).Select(i => MakeRelease(i)));
        await _db.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _sut.GetPagedAsync(5, 25, CancellationToken.None);

        // Assert
        items.Should().BeEmpty();
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_ReleasesExist_ReturnsResultsOrderedByArtistThenTitle()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeRelease(1, artist: "Coltrane", title: "Ballads"),
            MakeRelease(2, artist: "Coltrane", title: "A Love Supreme"),
            MakeRelease(3, artist: "Davis",    title: "Kind of Blue")
        );
        await _db.SaveChangesAsync();

        // Act
        var (items, _) = await _sut.GetPagedAsync(1, 10, CancellationToken.None);

        // Assert
        items.Select(r => r.Title).Should().ContainInOrder("A Love Supreme", "Ballads", "Kind of Blue");
    }

    // ── GetPagedAsync — total count ───────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectTotalCount()
    {
        // Arrange
        _db.Releases.AddRange(Enumerable.Range(1, 5).Select(i => MakeRelease(i)));
        await _db.SaveChangesAsync();

        // Act
        var (_, totalCount) = await _sut.GetPagedAsync(1, 2, CancellationToken.None);

        // Assert
        totalCount.Should().Be(5);
    }

    // ── GetPagedAsync — pagination ────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsCorrectSlice()
    {
        // Arrange — 5 releases ordered alphabetically by artist: A1..A5
        _db.Releases.AddRange(Enumerable.Range(1, 5)
            .Select(i => MakeRelease(i, artist: $"Artist {i:D2}")));
        await _db.SaveChangesAsync();

        // Act
        var (page1Items, _) = await _sut.GetPagedAsync(1, 3, CancellationToken.None);
        var (page2Items, _) = await _sut.GetPagedAsync(2, 3, CancellationToken.None);

        // Assert — no overlap between pages
        var page1Ids = page1Items.Select(r => r.DiscogsId).ToHashSet();
        page2Items.Select(r => r.DiscogsId).Should().NotIntersectWith(page1Ids);
    }

    // ── UpsertCollectionAsync — insert on first sync ──────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_AllReleasesRemoved_EmptiesDatabase()
    {
        // Arrange
        _db.Releases.AddRange(MakeRelease(801), MakeRelease(802));
        await _db.SaveChangesAsync();

        // Act
        await _sut.UpsertCollectionAsync(Array.Empty<Release>(), CancellationToken.None);

        // Assert
        var count = await _db.Releases.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task UpsertCollectionAsync_DetailFieldsNull_PersistsNullsCorrectly()
    {
        // Arrange — detail fields explicitly null (no detail sync occurred)
        var release = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 1200,
            Artist = "Miles Davis",
            Title = "Bitches Brew",
            Year = 1970,
            Format = "Vinyl",
            Genre = null,
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.UpsertCollectionAsync(new[] { release }, CancellationToken.None);

        // Assert
        var stored = await _sut.GetByIdAsync(release.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Genre.Should().BeNull();
    }

    [Fact]
    public async Task UpsertCollectionAsync_EmptyDatabase_InsertsAllReleases()
    {
        // Arrange
        var releases = new[]
        {
            MakeRelease(101, "Coltrane", "A Love Supreme"),
            MakeRelease(102, "Davis", "Kind of Blue")
        };

        // Act
        await _sut.UpsertCollectionAsync(releases, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.ToListAsync();
        stored.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpsertCollectionAsync_EmptyDatabase_PersistsAllFields()
    {
        // Arrange
        var syncTime = DateTimeOffset.UtcNow;
        var release = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 200,
            Artist = "Miles Davis",
            Title = "Bitches Brew",
            Year = 1970,
            Format = "Vinyl",
            LastSyncedAt = syncTime
        };

        // Act
        await _sut.UpsertCollectionAsync(new[] { release }, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.SingleAsync();
        stored.Artist.Should().Be("Miles Davis");
        stored.Title.Should().Be("Bitches Brew");
        stored.Year.Should().Be(1970);
        stored.Format.Should().Be("Vinyl");
        stored.DiscogsId.Should().Be(200);
    }

    [Fact]
    public async Task UpsertCollectionAsync_EmptyDatabase_PersistsNullYearCorrectly()
    {
        // Arrange
        var release = MakeRelease(300, year: null);

        // Act
        await _sut.UpsertCollectionAsync(new[] { release }, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.SingleAsync();
        stored.Year.Should().BeNull();
    }

    // ── UpsertCollectionAsync — update on subsequent sync ────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_ExistingRelease_DoesNotCreateDuplicate()
    {
        // Arrange
        _db.Releases.Add(MakeRelease(600));
        await _db.SaveChangesAsync();

        // Act
        await _sut.UpsertCollectionAsync(new[] { MakeRelease(600, artist: "Updated") }, CancellationToken.None);

        // Assert
        var count = await _db.Releases.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpsertCollectionAsync_ExistingRelease_UpdatesFieldsInPlace()
    {
        // Arrange — seed with old data
        var original = MakeRelease(401, artist: "Old Artist", title: "Old Title", year: 1990, format: "CD");
        _db.Releases.Add(original);
        await _db.SaveChangesAsync();

        var updated = new Release
        {
            Id = Guid.NewGuid(),        // intentionally different — upsert should not create a duplicate
            DiscogsId = 401,
            Artist = "New Artist",
            Title = "New Title",
            Year = 2020,
            Format = "Vinyl",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.UpsertCollectionAsync(new[] { updated }, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.ToListAsync();
        stored.Should().HaveCount(1);
        stored.Single().Artist.Should().Be("New Artist");
        stored.Single().Title.Should().Be("New Title");
        stored.Single().Year.Should().Be(2020);
        stored.Single().Format.Should().Be("Vinyl");
    }

    [Fact]
    public async Task UpsertCollectionAsync_ExistingRelease_UpdatesLastSyncedAt()
    {
        // Arrange
        var original = MakeRelease(500);
        _db.Releases.Add(original);
        await _db.SaveChangesAsync();

        var newSyncTime = DateTimeOffset.UtcNow.AddHours(1);
        var incoming = MakeRelease(500);
        incoming.LastSyncedAt = newSyncTime;

        // Act
        await _sut.UpsertCollectionAsync(new[] { incoming }, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.SingleAsync();
        stored.LastSyncedAt.Should().BeCloseTo(newSyncTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpsertCollectionAsync_ExistingReleaseUpdatedWithDetailFields_OverwritesPreviousNulls()
    {
        // Arrange — seed a release without detail fields, then upsert with them populated
        var original = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 1300,
            Artist = "Ornette Coleman",
            Title = "The Shape of Jazz to Come",
            Year = 1959,
            Format = "Vinyl",
            Genre = null,
            LastSyncedAt = DateTimeOffset.UtcNow
        };
        _db.Releases.Add(original);
        await _db.SaveChangesAsync();

        var updated = new Release
        {
            Id = Guid.NewGuid(), // different Guid — upsert matches on DiscogsId
            DiscogsId = 1300,
            Artist = "Ornette Coleman",
            Title = "The Shape of Jazz to Come",
            Year = 1959,
            Format = "Vinyl",
            Genre = "Jazz",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.UpsertCollectionAsync(new[] { updated }, CancellationToken.None);

        // Assert — should still be one record, now populated with detail fields
        var stored = await _sut.GetByIdAsync(original.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Genre.Should().Be("Jazz");
    }

    // ── UpsertCollectionAsync — removal of stale records ─────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_ReleaseAbsentFromIncoming_RemovesItFromDatabase()
    {
        // Arrange — two releases in DB, only one comes back from Discogs
        _db.Releases.AddRange(MakeRelease(701), MakeRelease(702));
        await _db.SaveChangesAsync();

        // Act
        await _sut.UpsertCollectionAsync(new[] { MakeRelease(701) }, CancellationToken.None);

        // Assert
        var stored = await _db.Releases.ToListAsync();
        stored.Should().HaveCount(1);
        stored.Single().DiscogsId.Should().Be(701);
    }

    // ── UpsertCollectionAsync — detail fields persisted and retrieved ──────────

    [Fact]
    public async Task UpsertCollectionAsync_WithDetailFields_PersistsAllDetailFieldsCorrectly()
    {
        // Arrange
        var release = new Release
        {
            Id = Guid.NewGuid(),
            DiscogsId = 1100,
            Artist = "John Coltrane",
            Title = "A Love Supreme",
            Year = 1964,
            Format = "Vinyl",
            Genre = "Jazz",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.UpsertCollectionAsync(new[] { release }, CancellationToken.None);

        // Assert
        var stored = await _sut.GetByIdAsync(release.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Genre.Should().Be("Jazz");
    }
}
