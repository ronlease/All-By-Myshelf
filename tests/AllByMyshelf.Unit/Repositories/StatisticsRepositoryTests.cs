// Feature: Statistics repository - collection value calculation (ABM-020)
//
// Scenario: All releases have pricing data
//   Given 3 releases all with LowestPrice values (10.00, 20.00, 30.00)
//   When GetCollectionValueAsync is called
//   Then TotalValue is 60.00
//   And IncludedCount is 3
//   And ExcludedCount is 0
//
// Scenario: Some releases have no pricing data
//   Given 4 releases: 2 with LowestPrice (10.00, 20.00) and 2 with null LowestPrice
//   When GetCollectionValueAsync is called
//   Then TotalValue is 30.00
//   And IncludedCount is 2
//   And ExcludedCount is 2
//
// Scenario: No releases have pricing data
//   Given 3 releases all with null LowestPrice
//   When GetCollectionValueAsync is called
//   Then TotalValue is 0.00
//   And IncludedCount is 0
//   And ExcludedCount is 3
//
// Scenario: No releases exist
//   Given the collection is empty
//   When GetCollectionValueAsync is called
//   Then TotalValue is 0.00
//   And IncludedCount is 0
//   And ExcludedCount is 0
//
// Feature: Statistics repository - unified statistics (ABM-034)
//
// Scenario: Books with genres
//   Given 4 books: 3 with genre "Fiction" and 1 with genre "Biography"
//   When GetUnifiedStatisticsAsync is called
//   Then Books.GenreBreakdown has Fiction first with count 3
//   And Biography second with count 1
//
// Scenario: Empty database
//   Given no releases or books exist
//   When GetUnifiedStatisticsAsync is called
//   Then Records.TotalCount is 0
//   And Books.TotalCount is 0
//   And all breakdowns are empty
//
// Scenario: Records with formats
//   Given 4 releases: 3 with format "LP" and 1 with format "CD"
//   When GetUnifiedStatisticsAsync is called
//   Then Records.FormatBreakdown has LP first with count 3
//   And CD second with count 1
//
// Scenario: Records with years
//   Given 3 releases with years 1975, 1978, 1992
//   When GetUnifiedStatisticsAsync is called
//   Then Records.DecadeBreakdown has "1970s" with count 2
//   And "1990s" with count 1
//   And "1970s" appears first (sorted by label)
//
// Scenario: Records and books together
//   Given 2 releases and 3 books exist
//   When GetUnifiedStatisticsAsync is called
//   Then Records.TotalCount is 2
//   And Books.TotalCount is 3

using AllByMyshelf.Api.Features.Statistics;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Unit.Repositories;

public class StatisticsRepositoryTests : IDisposable
{
    private readonly AllByMyshelfDbContext _db;
    private readonly StatisticsRepository _sut;

    public StatisticsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AllByMyshelfDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AllByMyshelfDbContext(options);
        _sut = new StatisticsRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Book MakeBook(int hardcoverId, string title, string? genre = null) =>
        new()
        {
            Author = "Author",
            Genre = genre,
            HardcoverId = hardcoverId,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title
        };

    private static Release MakeRelease(int discogsId, string? format = null, string? genre = null, int? year = null) =>
        new()
        {
            Artist = $"Artist {discogsId}",
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
            Artist = $"Artist {discogsId}",
            DiscogsId = discogsId,
            Format = "Vinyl",
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            LowestPrice = lowestPrice,
            Title = $"Title {discogsId}",
            Year = 2000
        };

    // ── GetCollectionValueAsync — all releases have pricing ───────────────────

    [Fact]
    public async Task GetCollectionValueAsync_AllReleasesHavePrice_ReturnsSumWithCorrectCounts()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeReleaseWithPrice(1, 10.00m),
            MakeReleaseWithPrice(2, 20.00m),
            MakeReleaseWithPrice(3, 30.00m)
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCollectionValueAsync(CancellationToken.None);

        // Assert
        result.TotalValue.Should().Be(60.00m);
        result.IncludedCount.Should().Be(3);
        result.ExcludedCount.Should().Be(0);
    }

    // ── GetCollectionValueAsync — empty database ──────────────────────────────

    [Fact]
    public async Task GetCollectionValueAsync_EmptyDatabase_ReturnsZeroValuesAndCounts()
    {
        // Act
        var result = await _sut.GetCollectionValueAsync(CancellationToken.None);

        // Assert
        result.TotalValue.Should().Be(0.00m);
        result.IncludedCount.Should().Be(0);
        result.ExcludedCount.Should().Be(0);
    }

    // ── GetCollectionValueAsync — no releases have pricing ────────────────────

    [Fact]
    public async Task GetCollectionValueAsync_NoReleasesHavePrice_ReturnsZeroValueWithCorrectCounts()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeReleaseWithPrice(101, null),
            MakeReleaseWithPrice(102, null),
            MakeReleaseWithPrice(103, null)
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCollectionValueAsync(CancellationToken.None);

        // Assert
        result.TotalValue.Should().Be(0.00m);
        result.IncludedCount.Should().Be(0);
        result.ExcludedCount.Should().Be(3);
    }

    // ── GetCollectionValueAsync — some releases have pricing ──────────────────

    [Fact]
    public async Task GetCollectionValueAsync_SomeReleasesHavePrice_CalculatesCorrectTotalAndCounts()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeReleaseWithPrice(201, 10.00m),
            MakeReleaseWithPrice(202, 20.00m),
            MakeReleaseWithPrice(203, null),
            MakeReleaseWithPrice(204, null)
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCollectionValueAsync(CancellationToken.None);

        // Assert
        result.TotalValue.Should().Be(30.00m);
        result.IncludedCount.Should().Be(2);
        result.ExcludedCount.Should().Be(2);
    }

    // ── GetUnifiedStatisticsAsync — books with genres ─────────────────────────

    [Fact]
    public async Task GetUnifiedStatisticsAsync_BooksWithGenres_ReturnsGenreBreakdownSortedByCountDesc()
    {
        // Arrange
        _db.Books.AddRange(
            MakeBook(1, "Book 1", "Fiction"),
            MakeBook(2, "Book 2", "Fiction"),
            MakeBook(3, "Book 3", "Fiction"),
            MakeBook(4, "Book 4", "Biography")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnifiedStatisticsAsync(CancellationToken.None);

        // Assert
        result.Books.GenreBreakdown.Should().HaveCount(2);
        result.Books.GenreBreakdown[0].Label.Should().Be("Fiction");
        result.Books.GenreBreakdown[0].Count.Should().Be(3);
        result.Books.GenreBreakdown[1].Label.Should().Be("Biography");
        result.Books.GenreBreakdown[1].Count.Should().Be(1);
    }

    // ── GetUnifiedStatisticsAsync — empty database ────────────────────────────

    [Fact]
    public async Task GetUnifiedStatisticsAsync_EmptyDatabase_ReturnsZeroCounts()
    {
        // Act
        var result = await _sut.GetUnifiedStatisticsAsync(CancellationToken.None);

        // Assert
        result.Records.TotalCount.Should().Be(0);
        result.Records.TotalValue.Should().Be(0);
        result.Records.ExcludedFromValueCount.Should().Be(0);
        result.Records.DecadeBreakdown.Should().BeEmpty();
        result.Records.FormatBreakdown.Should().BeEmpty();
        result.Records.GenreBreakdown.Should().BeEmpty();
        result.Books.TotalCount.Should().Be(0);
        result.Books.GenreBreakdown.Should().BeEmpty();
    }

    // ── GetUnifiedStatisticsAsync — records and books ─────────────────────────

    [Fact]
    public async Task GetUnifiedStatisticsAsync_RecordsAndBooks_ReturnsBothStatistics()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeRelease(1),
            MakeRelease(2)
        );
        _db.Books.AddRange(
            MakeBook(1, "Book 1"),
            MakeBook(2, "Book 2"),
            MakeBook(3, "Book 3")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnifiedStatisticsAsync(CancellationToken.None);

        // Assert
        result.Records.TotalCount.Should().Be(2);
        result.Books.TotalCount.Should().Be(3);
    }

    // ── GetUnifiedStatisticsAsync — records with formats ──────────────────────

    [Fact]
    public async Task GetUnifiedStatisticsAsync_RecordsWithFormats_ReturnsFormatBreakdownSortedByCountDesc()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeRelease(1, format: "LP"),
            MakeRelease(2, format: "LP"),
            MakeRelease(3, format: "LP"),
            MakeRelease(4, format: "CD")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnifiedStatisticsAsync(CancellationToken.None);

        // Assert
        result.Records.FormatBreakdown.Should().HaveCount(2);
        result.Records.FormatBreakdown[0].Label.Should().Be("LP");
        result.Records.FormatBreakdown[0].Count.Should().Be(3);
        result.Records.FormatBreakdown[1].Label.Should().Be("CD");
        result.Records.FormatBreakdown[1].Count.Should().Be(1);
    }

    // ── GetUnifiedStatisticsAsync — records with years ────────────────────────

    [Fact]
    public async Task GetUnifiedStatisticsAsync_RecordsWithYears_ReturnsDecadeBreakdownSortedByLabel()
    {
        // Arrange
        _db.Releases.AddRange(
            MakeRelease(1, year: 1975),
            MakeRelease(2, year: 1978),
            MakeRelease(3, year: 1992)
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnifiedStatisticsAsync(CancellationToken.None);

        // Assert
        result.Records.DecadeBreakdown.Should().HaveCount(2);
        result.Records.DecadeBreakdown[0].Label.Should().Be("1970s");
        result.Records.DecadeBreakdown[0].Count.Should().Be(2);
        result.Records.DecadeBreakdown[1].Label.Should().Be("1990s");
        result.Records.DecadeBreakdown[1].Count.Should().Be(1);
    }
}
