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

    private static Release MakeReleaseWithPrice(int discogsId, decimal? lowestPrice) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiscogsId = discogsId,
            Artist = $"Artist {discogsId}",
            Title = $"Title {discogsId}",
            Year = 2000,
            Format = "Vinyl",
            LowestPrice = lowestPrice,
            LastSyncedAt = DateTimeOffset.UtcNow
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
}
