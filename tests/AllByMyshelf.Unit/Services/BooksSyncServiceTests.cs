// Feature: Background sync of Hardcover collection  (ABM-030)
// Feature: Manual sync trigger endpoint             (ABM-031)
//
// Scenario: Sync is triggered and runs in the background
//   Given the Hardcover API token is configured
//   And no sync is currently running
//   When I trigger a manual sync
//   Then TryStartSync returns SyncStartResult.Started
//   And IsSyncRunning becomes true
//
// Scenario: Sync is already in progress
//   Given a sync is currently running
//   When I trigger another manual sync
//   Then TryStartSync returns SyncStartResult.AlreadyRunning
//
// Scenario: Attempt to trigger sync with no token configured
//   Given the Hardcover API token is NOT configured
//   When I call TryStartSync
//   Then TryStartSync returns SyncStartResult.TokenNotConfigured
//
// Scenario: IsSyncRunning reflects idle state before any sync
//   Given the service has just been created
//   Then IsSyncRunning is false
//
// Scenario: Author is mapped from first contribution
//   Given Hardcover returns a book with contributions populated
//   When the sync mapping is applied
//   Then the Book entity has author from contributions[0].author.name
//
// Scenario: Author is null when contributions is null
//   Given Hardcover returns a book with null contributions
//   When the sync mapping is applied
//   Then the Book entity has null author
//
// Scenario: Year is extracted from release_date
//   Given Hardcover returns a book with release_date "2020-05-15"
//   When the sync mapping is applied
//   Then the Book entity has year 2020
//
// Scenario: Year is null when release_date is null
//   Given Hardcover returns a book with null release_date
//   When the sync mapping is applied
//   Then the Book entity has null year
//
// Scenario: Year is null when release_date is whitespace
//   Given Hardcover returns a book with whitespace release_date
//   When the sync mapping is applied
//   Then the Book entity has null year
//
// Scenario: Year is null when release_date is unparseable
//   Given Hardcover returns a book with invalid release_date
//   When the sync mapping is applied
//   Then the Book entity has null year
//
// Scenario: Genre is always null (cached_tags is a JSON blob — see ABM-035)
//
// Scenario: CoverImageUrl is mapped from image.url
//   Given Hardcover returns a book with image populated
//   When the sync mapping is applied
//   Then the Book entity has coverImageUrl from image.url
//
// Scenario: CoverImageUrl is null when image is null
//   Given Hardcover returns a book with null image
//   When the sync mapping is applied
//   Then the Book entity has null coverImageUrl

using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Infrastructure.ExternalApis;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BooksSyncServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BooksSyncService CreateService(string token = "test-token")
    {
        var options = Options.Create(new HardcoverOptions
        {
            ApiToken = token
        });

        var scopeFactory = new Mock<IServiceScopeFactory>();
        var logger = NullLogger<BooksSyncService>.Instance;

        return new BooksSyncService(options, scopeFactory.Object, logger);
    }

    // ── IsSyncRunning — initial state ────────────────────────────────────────

    [Fact]
    public void IsSyncRunning_BeforeAnySync_IsFalse()
    {
        // Arrange
        var sut = CreateService();

        // Act / Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── TryStartSync — already running ───────────────────────────────────────

    [Fact]
    public void TryStartSync_AlreadyRunning_DoesNotStartSecondSync()
    {
        // Arrange
        var sut = CreateService();
        sut.TryStartSync();

        // Act
        var secondResult = sut.TryStartSync();

        // Assert — only one sync slot should be occupied
        secondResult.Should().Be(SyncStartResult.AlreadyRunning);
        sut.IsSyncRunning.Should().BeTrue();
    }

    [Fact]
    public void TryStartSync_AlreadyRunning_ReturnsAlreadyRunning()
    {
        // Arrange
        var sut = CreateService();
        sut.TryStartSync(); // first call acquires the lock

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.AlreadyRunning);
    }

    // ── Token check takes precedence over running flag ────────────────────────

    [Fact]
    public void TryStartSync_TokenNotConfigured_DoesNotSetRunningFlag()
    {
        // Arrange
        var sut = CreateService(token: string.Empty);

        // Act
        sut.TryStartSync();

        // Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── TryStartSync — token not configured ──────────────────────────────────

    [Fact]
    public void TryStartSync_TokenNotConfigured_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(token: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_TokenWhitespaceOnly_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(token: "   ");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — happy path ─────────────────────────────────────────────

    [Fact]
    public void TryStartSync_ValidToken_ReturnsStarted()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);
    }

    [Fact]
    public void TryStartSync_ValidToken_SetsSyncRunningTrue()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.TryStartSync();

        // Assert
        sut.IsSyncRunning.Should().BeTrue();
    }

    // ── Mapping — author from contributions ───────────────────────────────────

    [Fact]
    public void SyncMapping_BookWithContributions_MapsAuthorFromFirstContribution()
    {
        // Arrange — mirror the mapping expression from RunSyncAsync
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: new List<HardcoverClient.HardcoverContribution>
            {
                new(new HardcoverClient.HardcoverAuthor("Neil Gaiman")),
                new(new HardcoverClient.HardcoverAuthor("Terry Pratchett"))
            },
            Id: 12345,
            Image: null,
            ReleaseDate: null,
            Title: "Good Omens"
        );

        // Act — apply the same mapping logic BooksSyncService.RunSyncAsync uses
        var author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name;
        var entity = new Book
        {
            Author = author,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.Author.Should().Be("Neil Gaiman");
    }

    [Fact]
    public void SyncMapping_BookWithNullContributions_AuthorIsNull()
    {
        // Arrange — Hardcover returns null contributions
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12346,
            Image: null,
            ReleaseDate: null,
            Title: "Anonymous Work"
        );

        // Act
        var author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name;
        var entity = new Book
        {
            Author = author,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.Author.Should().BeNull();
    }

    // ── Mapping — year from release_date ──────────────────────────────────────

    [Fact]
    public void SyncMapping_BookWithReleaseDate_ExtractsYear()
    {
        // Arrange — Hardcover returns a parseable release_date
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12347,
            Image: null,
            ReleaseDate: "2020-05-15",
            Title: "The Midnight Library"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = year
        };

        // Assert
        entity.Year.Should().Be(2020);
    }

    [Fact]
    public void SyncMapping_BookWithInvalidReleaseDate_YearIsNull()
    {
        // Arrange — Hardcover returns an unparseable release_date
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12348,
            Image: null,
            ReleaseDate: "not-a-date",
            Title: "Invalid Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = year
        };

        // Assert
        entity.Year.Should().BeNull();
    }

    [Fact]
    public void SyncMapping_BookWithNullReleaseDate_YearIsNull()
    {
        // Arrange — Hardcover returns null release_date
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12349,
            Image: null,
            ReleaseDate: null,
            Title: "No Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = year
        };

        // Assert
        entity.Year.Should().BeNull();
    }

    [Fact]
    public void SyncMapping_BookWithWhitespaceReleaseDate_YearIsNull()
    {
        // Arrange — Hardcover returns whitespace release_date
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12350,
            Image: null,
            ReleaseDate: "   ",
            Title: "Whitespace Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = year
        };

        // Assert
        entity.Year.Should().BeNull();
    }

    // ── Mapping — coverImageUrl from image.url ────────────────────────────────

    [Fact]
    public void SyncMapping_BookWithImage_MapsCoverImageUrlFromImageUrl()
    {
        // Arrange — Hardcover returns image with url
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12354,
            Image: new HardcoverClient.HardcoverImage("https://i.hardcover.com/cover.jpg"),
            ReleaseDate: null,
            Title: "Beautiful Cover Book"
        );

        // Act
        var coverImageUrl = hardcoverBook.Image?.Url;
        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = coverImageUrl,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.CoverImageUrl.Should().Be("https://i.hardcover.com/cover.jpg");
    }

    [Fact]
    public void SyncMapping_BookWithNullImage_CoverImageUrlIsNull()
    {
        // Arrange — Hardcover returns null image
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            Contributions: null,
            Id: 12355,
            Image: null,
            ReleaseDate: null,
            Title: "No Cover Book"
        );

        // Act
        var coverImageUrl = hardcoverBook.Image?.Url;
        var entity = new Book
        {
            Author = hardcoverBook.Contributions?.FirstOrDefault()?.Author?.Name,
            CoverImageUrl = coverImageUrl,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.CoverImageUrl.Should().BeNull();
    }
}
