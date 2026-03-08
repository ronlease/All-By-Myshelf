// Feature: Paginated collection endpoint  (ABM-004)
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

    // ── GetReleasesAsync — pagination metadata ────────────────────────────────

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

    // ── GetReleasesAsync — page size cap ─────────────────────────────────────

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
}
