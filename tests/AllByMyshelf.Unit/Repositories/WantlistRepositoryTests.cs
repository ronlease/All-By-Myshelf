// Feature: Wantlist repository - data storage and retrieval (ABM-037)
//
// Scenario: GetPagedAsync returns wantlist releases ordered by artist then title
//   Given the database contains wantlist releases
//   When GetPagedAsync is called
//   Then releases are returned ordered by artist name, then by title
//
// Scenario: GetPagedAsync returns empty list when database is empty
//   Given the database contains no wantlist releases
//   When GetPagedAsync is called
//   Then an empty list is returned with zero total count
//
// Scenario: GetPagedAsync paginates correctly
//   Given the database contains 25 wantlist releases
//   When GetPagedAsync is called with page=2, pageSize=10
//   Then 10 releases are returned starting from the 11th release
//
// Scenario: UpsertAsync inserts new wantlist releases
//   Given the database is empty
//   When UpsertAsync is called with new releases
//   Then all releases are saved to the database
//
// Scenario: UpsertAsync updates existing wantlist releases
//   Given the database contains existing wantlist releases
//   When UpsertAsync is called with updated data for the same Discogs IDs
//   Then the existing records are updated with new values
//
// Scenario: UpsertAsync sets AddedAt for new releases
//   Given the database is empty
//   When UpsertAsync is called with new releases
//   Then AddedAt is set to the current time
//
// Scenario: RemoveAbsentAsync removes releases not in the active set
//   Given the database contains wantlist releases with IDs 100, 101, 102
//   When RemoveAbsentAsync is called with active IDs {100, 102}
//   Then release 101 is removed from the database
//
// Scenario: RemoveAbsentAsync keeps releases in the active set
//   Given the database contains wantlist releases with IDs 100, 101
//   When RemoveAbsentAsync is called with active IDs {100, 101}
//   Then no releases are removed

using AllByMyshelf.Api.Features.Wantlist;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Unit.Repositories;

public class WantlistRepositoryTests : IDisposable
{
    private readonly AllByMyshelfDbContext _db;
    private readonly WantlistRepository _sut;

    public WantlistRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AllByMyshelfDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AllByMyshelfDbContext(options);
        _sut = new WantlistRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WantlistRelease MakeRelease(
        int discogsId,
        string artist,
        string title,
        int? year = 2000,
        string format = "Vinyl",
        string? genre = null) =>
        new()
        {
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

    // ── GetPagedAsync — empty database ────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_EmptyDatabase_ReturnsEmptyListAndZeroCount()
    {
        // Act
        var result = await _sut.GetPagedAsync(1, 20, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GetPagedAsync — ordering ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_WithData_ReturnsReleasesOrderedByArtistThenTitle()
    {
        // Arrange — add releases out of order
        _db.WantlistReleases.AddRange(
            MakeRelease(3, "Zebra", "First Album"),
            MakeRelease(1, "Alpha", "Second Album"),
            MakeRelease(2, "Alpha", "First Album")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Artist.Should().Be("Alpha");
        result.Items[0].Title.Should().Be("First Album");
        result.Items[1].Artist.Should().Be("Alpha");
        result.Items[1].Title.Should().Be("Second Album");
        result.Items[2].Artist.Should().Be("Zebra");
    }

    // ── GetPagedAsync — pagination ────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange — 25 releases, alphabetically by artist/title
        var releases = Enumerable.Range(1, 25)
            .Select(i => MakeRelease(i, $"Artist {i:D2}", $"Album {i:D2}"))
            .ToList();
        _db.WantlistReleases.AddRange(releases);
        await _db.SaveChangesAsync();

        // Act — request page 2 with pageSize 10
        var result = await _sut.GetPagedAsync(2, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        // Verify pagination: page 2 should skip the first 10 releases
        result.Items.First().Artist.Should().Be("Artist 11");
    }

    // ── RemoveAbsentAsync — removes missing releases ──────────────────────────

    [Fact]
    public async Task RemoveAbsentAsync_ReleasesNotInActiveSet_RemovesFromDatabase()
    {
        // Arrange — database has releases 100, 101, 102
        _db.WantlistReleases.AddRange(
            MakeRelease(100, "Artist A", "Album A"),
            MakeRelease(101, "Artist B", "Album B"),
            MakeRelease(102, "Artist C", "Album C")
        );
        await _db.SaveChangesAsync();

        // Act — active set contains only 100 and 102
        await _sut.RemoveAbsentAsync(new HashSet<int> { 100, 102 }, CancellationToken.None);

        // Assert — release 101 should be removed
        _db.WantlistReleases.Should().HaveCount(2);
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 100);
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 102);
        _db.WantlistReleases.Should().NotContain(r => r.DiscogsId == 101);
    }

    // ── RemoveAbsentAsync — keeps releases in active set ──────────────────────

    [Fact]
    public async Task RemoveAbsentAsync_ReleasesInActiveSet_KeepsInDatabase()
    {
        // Arrange
        _db.WantlistReleases.AddRange(
            MakeRelease(100, "Artist A", "Album A"),
            MakeRelease(101, "Artist B", "Album B")
        );
        await _db.SaveChangesAsync();

        // Act — active set contains both 100 and 101
        await _sut.RemoveAbsentAsync(new HashSet<int> { 100, 101 }, CancellationToken.None);

        // Assert — no releases should be removed
        _db.WantlistReleases.Should().HaveCount(2);
    }

    // ── UpsertAsync — insert new releases ─────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_NewReleases_AddsToDatabase()
    {
        // Arrange
        var newReleases = new[]
        {
            MakeRelease(100, "Artist A", "Album A"),
            MakeRelease(101, "Artist B", "Album B")
        };

        // Act
        await _sut.UpsertAsync(newReleases, CancellationToken.None);

        // Assert
        _db.WantlistReleases.Should().HaveCount(2);
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 100);
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 101);
    }

    // ── UpsertAsync — sets AddedAt for new releases ───────────────────────────

    [Fact]
    public async Task UpsertAsync_NewReleases_SetsAddedAtToCurrentTime()
    {
        // Arrange
        var beforeUpsert = DateTimeOffset.UtcNow;
        var newRelease = MakeRelease(100, "Artist A", "Album A");
        newRelease.AddedAt = null; // Clear AddedAt to simulate new release

        // Act
        await _sut.UpsertAsync(new[] { newRelease }, CancellationToken.None);

        // Assert
        var saved = _db.WantlistReleases.Single();
        saved.AddedAt.Should().NotBeNull();
        saved.AddedAt.Should().BeOnOrAfter(beforeUpsert);
    }

    // ── UpsertAsync — update existing releases ────────────────────────────────

    [Fact]
    public async Task UpsertAsync_ExistingReleases_UpdatesInPlace()
    {
        // Arrange — database has release 100
        var existingRelease = MakeRelease(100, "Old Artist", "Old Album", genre: "Old Genre");
        _db.WantlistReleases.Add(existingRelease);
        await _db.SaveChangesAsync();

        // Act — update release 100
        var updatedRelease = MakeRelease(100, "New Artist", "New Album", genre: "New Genre");
        await _sut.UpsertAsync(new[] { updatedRelease }, CancellationToken.None);

        // Assert
        _db.WantlistReleases.Should().HaveCount(1);
        var release = _db.WantlistReleases.Single();
        release.DiscogsId.Should().Be(100);
        release.Artist.Should().Be("New Artist");
        release.Title.Should().Be("New Album");
        release.Genre.Should().Be("New Genre");
    }

    // ── UpsertAsync — mixed insert and update ─────────────────────────────────

    [Fact]
    public async Task UpsertAsync_MixedNewAndExisting_UpdatesAndAdds()
    {
        // Arrange — database has release 100
        _db.WantlistReleases.Add(MakeRelease(100, "Old Artist", "Old Album"));
        await _db.SaveChangesAsync();

        // Act — update 100, add 101
        var releases = new[]
        {
            MakeRelease(100, "Updated Artist", "Updated Album"),
            MakeRelease(101, "New Artist", "New Album")
        };
        await _sut.UpsertAsync(releases, CancellationToken.None);

        // Assert
        _db.WantlistReleases.Should().HaveCount(2);
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 100 && r.Artist == "Updated Artist");
        _db.WantlistReleases.Should().Contain(r => r.DiscogsId == 101 && r.Artist == "New Artist");
    }
}
