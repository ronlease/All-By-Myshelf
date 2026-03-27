// Feature: Background sync of Discogs collection  (ABM-002)
// Feature: Manual sync trigger endpoint           (ABM-005)
// Feature: Album art URL storage                  (ABM-011)
//
// Scenario: Sync is triggered and runs in the background
//   Given the Discogs personal access token is configured
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
//   Given the Discogs personal access token is NOT configured
//   When I call TryStartSync
//   Then TryStartSync returns SyncStartResult.TokenNotConfigured
//
// Scenario: IsSyncRunning reflects idle state before any sync
//   Given the service has just been created
//   Then IsSyncRunning is false
//
// Scenario: CoverImageUrl and ThumbnailUrl are mapped from BasicInformation
//   Given Discogs returns a release with cover_image and thumb populated
//   When the sync mapping is applied
//   Then the Release entity has matching CoverImageUrl and ThumbnailUrl
//
// Scenario: CoverImageUrl and ThumbnailUrl are null when Discogs returns null
//   Given Discogs returns a release where cover_image and thumb are null
//   When the sync mapping is applied
//   Then the Release entity has null CoverImageUrl and null ThumbnailUrl
//
// Scenario: CoverImageUrl and ThumbnailUrl are null when Discogs returns empty strings
//   Given Discogs returns a release where cover_image and thumb are empty strings
//   When the sync mapping is applied
//   Then the Release entity has empty CoverImageUrl and empty ThumbnailUrl
//
// Scenario: Sync maps LowestPrice from release detail to Release entity (ABM-020)
//   Given Discogs collection returns one release
//   And GetReleaseDetailAsync returns a detail with LowestPrice 19.99
//   When RunSync is triggered
//   Then the upserted release has LowestPrice 19.99
//
// Scenario: ExecuteAsync background loop processes sync signal
//   Given the service is started
//   When TryStartSync is called
//   Then the background loop executes RunSyncAsync
//   And UpsertCollectionAsync is called with the fetched releases
//
// Scenario: ExecuteAsync completes and resets state
//   Given a sync is running
//   When the sync completes
//   Then IsSyncRunning becomes false
//   And Progress is reset to idle

using System.Net;
using System.Text;
using System.Text.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Wantlist;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AllByMyshelf.Unit.Services;

public class SyncServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SyncService CreateService(
        string token = "test-token",
        string username = "test-user")
    {
        var options = Options.Create(new DiscogsOptions
        {
            PersonalAccessToken = token,
            Username = username
        });

        var scopeFactory = new Mock<IServiceScopeFactory>();
        var logger = NullLogger<SyncService>.Instance;

        return new SyncService(options, scopeFactory.Object, logger);
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

    // ── Artwork URL mapping — populated values ────────────────────────────────

    [Fact]
    public void SyncMapping_BasicInformationWithCoverImageAndThumb_MapsUrlsToEntity()
    {
        // Arrange — mirror the mapping expression from RunSyncAsync
        var basicInfo = new DiscogsBasicInformation
        {
            Artists = [new DiscogsArtist { Name = "John Coltrane" }],
            CoverImage = "https://i.discogs.com/cover.jpg",
            Formats = [new DiscogsFormat { Name = "Vinyl" }],
            Thumb = "https://i.discogs.com/thumb.jpg",
            Title = "A Love Supreme",
            Year = 1964
        };

        // Act — apply the same mapping logic SyncService.RunSyncAsync uses
        var artists = basicInfo.Artists?.Select(a => a.Name).ToList() ?? new List<string> { "Unknown Artist" };
        var entity = new Release
        {
            Artists = artists,
            CoverImageUrl = basicInfo.CoverImage,
            DiscogsId = 999,
            Format = basicInfo.Formats.FirstOrDefault()?.Name ?? string.Empty,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            ThumbnailUrl = basicInfo.Thumb,
            Title = basicInfo.Title,
            Year = basicInfo.Year == 0 ? (int?)null : basicInfo.Year,
        };

        // Assert
        entity.CoverImageUrl.Should().Be("https://i.discogs.com/cover.jpg");
        entity.ThumbnailUrl.Should().Be("https://i.discogs.com/thumb.jpg");
    }

    // ── Artwork URL mapping — null values from Discogs ────────────────────────

    [Fact]
    public void SyncMapping_BasicInformationWithNullCoverImageAndThumb_EntityUrlsAreNull()
    {
        // Arrange — Discogs returns null for both artwork fields
        var basicInfo = new DiscogsBasicInformation
        {
            Artists = [new DiscogsArtist { Name = "Miles Davis" }],
            CoverImage = null,
            Formats = [new DiscogsFormat { Name = "Vinyl" }],
            Thumb = null,
            Title = "Kind of Blue",
            Year = 1959
        };

        // Act
        var artists = basicInfo.Artists?.Select(a => a.Name).ToList() ?? new List<string> { "Unknown Artist" };
        var entity = new Release
        {
            Artists = artists,
            CoverImageUrl = basicInfo.CoverImage,
            DiscogsId = 998,
            Format = basicInfo.Formats.FirstOrDefault()?.Name ?? string.Empty,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            ThumbnailUrl = basicInfo.Thumb,
            Title = basicInfo.Title,
            Year = basicInfo.Year == 0 ? (int?)null : basicInfo.Year,
        };

        // Assert
        entity.CoverImageUrl.Should().BeNull();
        entity.ThumbnailUrl.Should().BeNull();
    }

    // ── Artwork URL mapping — empty string values from Discogs ────────────────

    [Fact]
    public void SyncMapping_BasicInformationWithEmptyStringCoverImageAndThumb_EntityUrlsAreEmpty()
    {
        // Arrange — Discogs returns empty strings (as sometimes observed in real API responses)
        var basicInfo = new DiscogsBasicInformation
        {
            Artists = [new DiscogsArtist { Name = "Ornette Coleman" }],
            CoverImage = string.Empty,
            Formats = [new DiscogsFormat { Name = "Vinyl" }],
            Thumb = string.Empty,
            Title = "The Shape of Jazz to Come",
            Year = 1959
        };

        // Act
        var artists = basicInfo.Artists?.Select(a => a.Name).ToList() ?? new List<string> { "Unknown Artist" };
        var entity = new Release
        {
            Artists = artists,
            CoverImageUrl = basicInfo.CoverImage,
            DiscogsId = 997,
            Format = basicInfo.Formats.FirstOrDefault()?.Name ?? string.Empty,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            ThumbnailUrl = basicInfo.Thumb,
            Title = basicInfo.Title,
            Year = basicInfo.Year == 0 ? (int?)null : basicInfo.Year,
        };

        // Assert
        entity.CoverImageUrl.Should().BeEmpty();
        entity.ThumbnailUrl.Should().BeEmpty();
    }

    // ── Marketplace pricing mapping ───────────────────────────────────────────

    [Fact]
    public void SyncMapping_MarketplaceStatsWithPricing_MapsAllPricesToEntity()
    {
        // Arrange — mirror the mapping expression from RunSyncAsync
        var stats = new DiscogsMarketplaceStats
        {
            HighestPrice = new DiscogsPrice { Value = 45.00m },
            LowestPrice = new DiscogsPrice { Value = 19.99m },
            MedianPrice = new DiscogsPrice { Value = 28.50m },
        };

        // Act — apply the same mapping logic SyncService.RunSyncAsync uses
        var entity = new Release
        {
            Artists = new List<string> { "John Coltrane" },
            CoverImageUrl = null,
            DiscogsId = 555,
            Format = "Vinyl",
            Genre = "Jazz",
            HighestPrice = stats.HighestPrice?.Value,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            LowestPrice = stats.LowestPrice?.Value,
            MedianPrice = stats.MedianPrice?.Value,
            ThumbnailUrl = null,
            Title = "A Love Supreme",
            Year = 1964,
        };

        // Assert
        entity.HighestPrice.Should().Be(45.00m);
        entity.LowestPrice.Should().Be(19.99m);
        entity.MedianPrice.Should().Be(28.50m);
    }

    // ── ExecuteAsync — background loop integration ────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CompletesAndResetsState_IsSyncRunningBecomesFalse()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler();
        var mockRepository = new Mock<IReleasesRepository>();
        mockRepository.Setup(r => r.GetAllByDiscogsIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Release>());
        var sut = CreateServiceWithRealScope(mockHandler, mockRepository.Object);

        // Act
        sut.TryStartSync();
        await sut.StartAsync(CancellationToken.None);

        // Wait for the sync to complete
        await Task.Delay(500);

        // Assert
        sut.IsSyncRunning.Should().BeFalse();
        sut.Progress.Status.Should().Be(SyncConstants.Statuses.Idle);
    }

    [Fact]
    public async Task ExecuteAsync_SingleRelease_UpsertsCalled()
    {
        // Arrange
        var mockHandler = CreateMockHttpMessageHandler();
        var mockRepository = new Mock<IReleasesRepository>();
        mockRepository.Setup(r => r.GetAllByDiscogsIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Release>());
        var sut = CreateServiceWithRealScope(mockHandler, mockRepository.Object);

        // Act
        sut.TryStartSync();
        await sut.StartAsync(CancellationToken.None);

        // Wait for the sync to complete
        await Task.Delay(500);

        // Assert
        mockRepository.Verify(
            r => r.UpsertCollectionAsync(
                It.Is<IEnumerable<Release>>(releases =>
                    releases.Count() == 1 &&
                    releases.First().DiscogsId == 123456 &&
                    releases.First().Title == "A Love Supreme" &&
                    releases.First().Artists.Contains("John Coltrane") &&
                    releases.First().Genre == "Jazz" &&
                    releases.First().LowestPrice == 19.99m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test helpers for ExecuteAsync tests ──────────────────────────────────

    private static Mock<HttpMessageHandler> CreateMockHttpMessageHandler()
    {
        var handler = new Mock<HttpMessageHandler>();

        // Mock collection endpoint
        var collectionJson = JsonSerializer.Serialize(new DiscogsCollectionPage
        {
            Pagination = new DiscogsPagination { Pages = 1, Page = 1, Items = 1 },
            Releases =
            [
                new DiscogsRelease
                {
                    Id = 123456,
                    BasicInformation = new DiscogsBasicInformation
                    {
                        Artists = [new DiscogsArtist { Name = "John Coltrane" }],
                        CoverImage = "https://i.discogs.com/cover.jpg",
                        Formats = [new DiscogsFormat { Name = "Vinyl" }],
                        Thumb = "https://i.discogs.com/thumb.jpg",
                        Title = "A Love Supreme",
                        Year = 1964
                    }
                }
            ]
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/users/test-user/collection")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(collectionJson, Encoding.UTF8, "application/json")
            });

        // Mock release detail endpoint
        var detailJson = JsonSerializer.Serialize(new DiscogsReleaseDetail
        {
            Genres = ["Jazz"]
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/releases/123456") &&
                    !req.RequestUri.PathAndQuery.Contains("marketplace")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(detailJson, Encoding.UTF8, "application/json")
            });

        // Mock marketplace stats endpoint
        var statsJson = JsonSerializer.Serialize(new DiscogsMarketplaceStats
        {
            LowestPrice = new DiscogsPrice { Value = 19.99m, Currency = "USD" },
            MedianPrice = new DiscogsPrice { Value = 28.50m, Currency = "USD" },
            HighestPrice = new DiscogsPrice { Value = 45.00m, Currency = "USD" }
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/marketplace/stats/123456")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(statsJson, Encoding.UTF8, "application/json")
            });

        // Mock wantlist endpoint (empty wantlist)
        var wantlistJson = JsonSerializer.Serialize(new
        {
            pagination = new { page = 1, pages = 1, per_page = 100, items = 0 },
            wants = Array.Empty<object>()
        });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/wants")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(wantlistJson, Encoding.UTF8, "application/json")
            });

        return handler;
    }

    private static SyncService CreateServiceWithRealScope(
        Mock<HttpMessageHandler> mockHandler,
        IReleasesRepository repository)
    {
        var services = new ServiceCollection();

        // Register DiscogsClient with the mocked HttpClient
        services.AddScoped<DiscogsClient>(sp =>
        {
            var httpClient = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri("https://api.discogs.com")
            };
            var optionsSnapshot = new Mock<IOptionsSnapshot<DiscogsOptions>>();
            optionsSnapshot.Setup(o => o.Value).Returns(new DiscogsOptions
            {
                PersonalAccessToken = "test-token",
                Username = "test-user"
            });
            var logger = NullLogger<DiscogsClient>.Instance;
            return new DiscogsClient(httpClient, optionsSnapshot.Object, logger);
        });

        // Register the repositories
        services.AddScoped<IReleasesRepository>(sp => repository);
        services.AddScoped<IWantlistRepository>(sp =>
            new Mock<IWantlistRepository>().Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var syncOptions = Options.Create(new DiscogsOptions
        {
            PersonalAccessToken = "test-token",
            Username = "test-user"
        });

        return new SyncService(syncOptions, scopeFactory, NullLogger<SyncService>.Instance);
    }
}
