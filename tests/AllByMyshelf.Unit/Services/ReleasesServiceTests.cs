// Feature: Paginated collection endpoint  (ABM-004)
// Feature: Release detail view             (ABM-012)
// Feature: Album art URL storage           (ABM-011)
//
// Scenario: Retrieve the first page of releases
//   Given the database contains releases
//   When GetReleasesAsync is called with page=1, pageSize=25
//   Then the service returns up to 25 releases
//   And each release includes artist, title, year, and format
//   And the result carries total record count and total page count
//
// Scenario: Retrieve a subsequent page
//   Given the database contains more than 25 releases
//   When GetReleasesAsync is called with page=2, pageSize=25
//   Then the result items are from the second page
//
// Scenario: Request a page beyond the available data
//   Given the database contains 30 releases
//   When GetReleasesAsync is called with page=5, pageSize=25
//   Then the result items list is empty
//   And TotalCount still reflects 30
//
// Scenario: Database contains no releases
//   Given the repository returns an empty collection
//   When GetReleasesAsync is called
//   Then the result items list is empty
//   And TotalCount is 0
//
// Scenario: PageSize is capped at 100
//   Given the caller requests pageSize=200
//   When GetReleasesAsync is called
//   Then the repository is queried with pageSize=100
//
// Scenario: GetByIdAsync returns a fully mapped ReleaseDetailDto when the release exists
//   Given the repository returns a release with all fields populated
//   When GetByIdAsync is called with its Guid
//   Then the returned dto maps all fields including genre
//
// Scenario: GetByIdAsync returns null when the release is not found
//   Given the repository returns null for an unknown Guid
//   When GetByIdAsync is called
//   Then null is returned
//
// Scenario: GetByIdAsync maps nullable detail fields as null when they are null on the entity
//   Given the repository returns a release whose detail fields are all null
//   When GetByIdAsync is called
//   Then the returned dto has null for genre
//
// Scenario: GetReleasesAsync maps ThumbnailUrl from entity to ReleaseDto
//   Given the repository returns a release with ThumbnailUrl populated
//   When GetReleasesAsync is called
//   Then the returned ReleaseDto has a matching ThumbnailUrl
//
// Scenario: GetReleasesAsync maps null ThumbnailUrl from entity to ReleaseDto
//   Given the repository returns a release with ThumbnailUrl null
//   When GetReleasesAsync is called
//   Then the returned ReleaseDto has null ThumbnailUrl
//
// Scenario: GetByIdAsync maps CoverImageUrl from entity to ReleaseDetailDto
//   Given the repository returns a release with CoverImageUrl populated
//   When GetByIdAsync is called
//   Then the returned ReleaseDetailDto has a matching CoverImageUrl
//
// Scenario: GetByIdAsync maps null CoverImageUrl from entity to ReleaseDetailDto
//   Given the repository returns a release with CoverImageUrl null
//   When GetByIdAsync is called
//   Then the returned ReleaseDetailDto has null CoverImageUrl

using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Repositories;
using AllByMyshelf.Api.Services;
using FluentAssertions;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class ReleasesServiceTests
{
    private readonly Mock<IReleasesRepository> _repositoryMock;
    private readonly ReleasesService _sut;

    public ReleasesServiceTests()
    {
        _repositoryMock = new Mock<IReleasesRepository>(MockBehavior.Strict);
        _sut = new ReleasesService(_repositoryMock.Object);
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

    private static Release MakeRelease(int discogsId, string artist = "Artist", string title = "Title") =>
        new()
        {
            Id = Guid.NewGuid(),
            DiscogsId = discogsId,
            Artist = artist,
            Title = title,
            Year = 2000 + discogsId,
            Format = "Vinyl",
            LastSyncedAt = DateTimeOffset.UtcNow
        };

    // ── GetByIdAsync — found: all fields mapped ───────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingRelease_ReturnsMappedReleaseDetailDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeDetailedRelease(id, discogsId: 555);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.DiscogsId.Should().Be(555);
        result.Artist.Should().Be("John Coltrane");
        result.Title.Should().Be("A Love Supreme");
        result.Year.Should().Be(1964);
        result.Format.Should().Be("Vinyl");
        result.Genre.Should().Be("Jazz");
    }

    // ── GetByIdAsync — nullable detail fields map correctly when null ─────────

    [Fact]
    public async Task GetByIdAsync_NullDetailFields_MapsNullsToDto()
    {
        // Arrange — a release where detail fields were never populated by sync
        var id = Guid.NewGuid();
        var release = new Release
        {
            Id = id,
            DiscogsId = 666,
            Artist = "Unknown Artist",
            Title = "Untitled",
            Year = null,
            Format = "Vinyl",
            Genre = null,
            LastSyncedAt = DateTimeOffset.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Year.Should().BeNull();
        result.Genre.Should().BeNull();
    }

    // ── GetByIdAsync — not found ──────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Release?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetReleasesAsync — pagination metadata ────────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_BeyondAvailableData_ReturnsEmptyItemsWithCorrectTotalCount()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(5, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release>(), 30));

        // Act
        var result = await _sut.GetReleasesAsync(5, 25, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(30);
    }

    // ── GetReleasesAsync — empty database ────────────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_EmptyDatabase_ReturnsEmptyItemsAndZeroTotalCount()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release>(), 0));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GetReleasesAsync — multiple items on page ─────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_MultipleReleases_ReturnsMappedDtosForEach()
    {
        // Arrange
        var releases = Enumerable.Range(1, 3)
            .Select(i => MakeRelease(i, $"Artist {i}", $"Album {i}"))
            .ToList();

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((releases, 3));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Select(d => d.Artist).Should().BeEquivalentTo("Artist 1", "Artist 2", "Artist 3");
    }

    // ── GetReleasesAsync — page size cap ─────────────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_PageSizeExactly100_IsNotCapped()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release>(), 0));

        // Act
        var result = await _sut.GetReleasesAsync(1, 100, CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetReleasesAsync_PageSizeOver100_CapsAt100()
    {
        // Arrange — repository must be called with exactly 100, not 200
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release>(), 0));

        // Act
        var result = await _sut.GetReleasesAsync(1, 200, CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(100);
        _repositoryMock.Verify(r => r.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReleasesAsync_ReturnsCorrectPageAndPageSizeInResult()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release>(), 60));

        // Act
        var result = await _sut.GetReleasesAsync(2, 25, CancellationToken.None);

        // Assert
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(25);
        result.TotalCount.Should().Be(60);
    }

    // ── GetReleasesAsync — mapping ────────────────────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_WithReleases_MapsArtistTitleYearFormat()
    {
        // Arrange
        var release = MakeRelease(1, artist: "Miles Davis", title: "Kind of Blue");
        release.Year = 1959;
        release.Format = "Vinyl";

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release> { release }, 1));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var dto = result.Items.Single();
        dto.Artist.Should().Be("Miles Davis");
        dto.Title.Should().Be("Kind of Blue");
        dto.Year.Should().Be(1959);
        dto.Format.Should().Be("Vinyl");
    }

    [Fact]
    public async Task GetReleasesAsync_WithReleases_MapsNullYearCorrectly()
    {
        // Arrange
        var release = MakeRelease(2);
        release.Year = null;

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release> { release }, 1));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Single().Year.Should().BeNull();
    }

    // ── GetReleasesAsync — ThumbnailUrl mapping ───────────────────────────────

    [Fact]
    public async Task GetReleasesAsync_ReleasesWithThumbnailUrl_MapsThumbnailUrlToDto()
    {
        // Arrange
        var release = MakeRelease(10);
        release.ThumbnailUrl = "https://i.discogs.com/thumb.jpg";

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release> { release }, 1));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Single().ThumbnailUrl.Should().Be("https://i.discogs.com/thumb.jpg");
    }

    [Fact]
    public async Task GetReleasesAsync_ReleasesWithNullThumbnailUrl_MapsNullThumbnailUrlToDto()
    {
        // Arrange
        var release = MakeRelease(11);
        release.ThumbnailUrl = null;

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Release> { release }, 1));

        // Act
        var result = await _sut.GetReleasesAsync(1, 25, CancellationToken.None);

        // Assert
        result.Items.Single().ThumbnailUrl.Should().BeNull();
    }

    // ── GetByIdAsync — CoverImageUrl mapping ─────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReleaseWithCoverImageUrl_MapsCoverImageUrlToDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeDetailedRelease(id, discogsId: 777);
        release.CoverImageUrl = "https://i.discogs.com/cover.jpg";

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CoverImageUrl.Should().Be("https://i.discogs.com/cover.jpg");
    }

    [Fact]
    public async Task GetByIdAsync_ReleaseWithNullCoverImageUrl_MapsNullCoverImageUrlToDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var release = MakeDetailedRelease(id, discogsId: 778);
        release.CoverImageUrl = null;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CoverImageUrl.Should().BeNull();
    }
}
