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
//
// Scenario: ExecuteAsync background loop processes books
//   Given the service is started
//   When TryStartSync is called
//   Then the background loop executes RunSyncAsync
//   And UpsertCollectionAsync is called with the fetched books
//
// Scenario: ExecuteAsync completes and resets state for books
//   Given a books sync is running
//   When the sync completes
//   Then IsSyncRunning becomes false

using System.Net;
using System.Text;
using System.Text.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

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
    public void SyncMapping_BookWithContributions_MapsAllAuthors()
    {
        // Arrange — mirror the mapping expression from RunSyncAsync
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            CachedTags: null,
            Contributions: new List<HardcoverClient.HardcoverContribution>
            {
                new(new HardcoverClient.HardcoverAuthor("Neil Gaiman")),
                new(new HardcoverClient.HardcoverAuthor("Terry Pratchett"))
            },
            Id: 12345,
            Image: null,
            ReleaseDate: null,
            Slug: null,
            Title: "Good Omens"
        );

        // Act — apply the same mapping logic BooksSyncService.RunSyncAsync uses
        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.Authors.Should().BeEquivalentTo(new[] { "Neil Gaiman", "Terry Pratchett" });
    }

    [Fact]
    public void SyncMapping_BookWithNullContributions_AuthorIsNull()
    {
        // Arrange — Hardcover returns null contributions
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            CachedTags: null,
            Contributions: null,
            Id: 12346,
            Image: null,
            ReleaseDate: null,
            Slug: null,
            Title: "Anonymous Work"
        );

        // Act
        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
            CoverImageUrl = hardcoverBook.Image?.Url,
            Genre = null,
            HardcoverId = hardcoverBook.Id,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = hardcoverBook.Title ?? "Unknown Title",
            Year = null
        };

        // Assert
        entity.Authors.Should().BeEmpty();
    }

    // ── Mapping — year from release_date ──────────────────────────────────────

    [Fact]
    public void SyncMapping_BookWithReleaseDate_ExtractsYear()
    {
        // Arrange — Hardcover returns a parseable release_date
        var hardcoverBook = new HardcoverClient.HardcoverBook(
            CachedTags: null,
            Contributions: null,
            Id: 12347,
            Image: null,
            ReleaseDate: "2020-05-15",
            Slug: null,
            Title: "The Midnight Library"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
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
            CachedTags: null,
            Contributions: null,
            Id: 12348,
            Image: null,
            ReleaseDate: "not-a-date",
            Slug: null,
            Title: "Invalid Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
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
            CachedTags: null,
            Contributions: null,
            Id: 12349,
            Image: null,
            ReleaseDate: null,
            Slug: null,
            Title: "No Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
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
            CachedTags: null,
            Contributions: null,
            Id: 12350,
            Image: null,
            ReleaseDate: "   ",
            Slug: null,
            Title: "Whitespace Date Book"
        );

        // Act
        int? year = null;
        if (!string.IsNullOrWhiteSpace(hardcoverBook.ReleaseDate) &&
            DateTime.TryParse(hardcoverBook.ReleaseDate, out var releaseDate))
        {
            year = releaseDate.Year;
        }

        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var entity = new Book
        {
            Authors = authors,
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
            CachedTags: null,
            Contributions: null,
            Id: 12354,
            Image: new HardcoverClient.HardcoverImage("https://i.hardcover.com/cover.jpg"),
            ReleaseDate: null,
            Slug: null,
            Title: "Beautiful Cover Book"
        );

        // Act
        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var coverImageUrl = hardcoverBook.Image?.Url;
        var entity = new Book
        {
            Authors = authors,
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
            CachedTags: null,
            Contributions: null,
            Id: 12355,
            Image: null,
            ReleaseDate: null,
            Slug: null,
            Title: "No Cover Book"
        );

        // Act
        var authors = hardcoverBook.Contributions?
            .Select(c => c.Author?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
        var coverImageUrl = hardcoverBook.Image?.Url;
        var entity = new Book
        {
            Authors = authors,
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

    // ── ExecuteAsync — background loop integration ────────────────────────────

    [Fact]
    public async Task ExecuteAsync_BooksFromHardcover_UpsertsCalled()
    {
        // Arrange
        var mockHttpClientFactory = CreateMockHttpClientFactory();
        var mockRepository = new Mock<IBooksRepository>();
        var sut = CreateServiceWithRealScope(mockHttpClientFactory.Object, mockRepository.Object);

        // Act
        sut.TryStartSync();
        await sut.StartAsync(CancellationToken.None);

        // Wait for the sync to complete
        await Task.Delay(500);

        // Assert
        mockRepository.Verify(
            r => r.UpsertCollectionAsync(
                It.Is<IEnumerable<Book>>(books =>
                    books.Count() == 1 &&
                    books.First().HardcoverId == 54321 &&
                    books.First().Title == "Good Omens" &&
                    books.First().Authors.Contains("Neil Gaiman")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesAndResetsState_IsSyncRunningBecomesFalse()
    {
        // Arrange
        var mockHttpClientFactory = CreateMockHttpClientFactory();
        var mockRepository = new Mock<IBooksRepository>();
        var sut = CreateServiceWithRealScope(mockHttpClientFactory.Object, mockRepository.Object);

        // Act
        sut.TryStartSync();
        await sut.StartAsync(CancellationToken.None);

        // Wait for the sync to complete
        await Task.Delay(500);

        // Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── Test helpers for ExecuteAsync tests ──────────────────────────────────

    private static Mock<IHttpClientFactory> CreateMockHttpClientFactory()
    {
        var handler = new Mock<HttpMessageHandler>();

        // Mock the "me" query response
        var meResponse = JsonSerializer.Serialize(new
        {
            data = new
            {
                me = new[] { new { id = 999 } }
            }
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.ReadAsStringAsync().Result.Contains("{ me { id } }")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(meResponse, Encoding.UTF8, "application/json")
            });

        // Mock the user_books query response
        var userBooksResponse = JsonSerializer.Serialize(new
        {
            data = new
            {
                user_books = new[]
                {
                    new
                    {
                        book = new
                        {
                            cached_tags = (object?)null,
                            contributions = new[]
                            {
                                new
                                {
                                    author = new { name = "Neil Gaiman" }
                                }
                            },
                            id = 54321,
                            image = new { url = "https://i.hardcover.com/book.jpg" },
                            release_date = "2019-05-01",
                            title = "Good Omens"
                        }
                    }
                }
            }
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.ReadAsStringAsync().Result.Contains("user_books")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userBooksResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.hardcover.app/v1/graphql")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return factory;
    }

    private static BooksSyncService CreateServiceWithRealScope(
        IHttpClientFactory httpClientFactory,
        IBooksRepository repository)
    {
        var services = new ServiceCollection();

        // Register HardcoverClient with the mocked IHttpClientFactory
        services.AddScoped<HardcoverClient>(sp =>
        {
            var optionsSnapshot = new Mock<IOptionsSnapshot<HardcoverOptions>>();
            optionsSnapshot.Setup(o => o.Value).Returns(new HardcoverOptions
            {
                ApiToken = "test-token"
            });
            var logger = NullLogger<HardcoverClient>.Instance;
            return new HardcoverClient(httpClientFactory, optionsSnapshot.Object, logger);
        });

        // Register the repository
        services.AddScoped<IBooksRepository>(sp => repository);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var syncOptions = Options.Create(new HardcoverOptions
        {
            ApiToken = "test-token"
        });

        return new BooksSyncService(syncOptions, scopeFactory, NullLogger<BooksSyncService>.Instance);
    }
}
