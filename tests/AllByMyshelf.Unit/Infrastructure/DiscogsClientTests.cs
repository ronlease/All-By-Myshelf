// Feature: Discogs personal access token configuration  (ABM-001)
// Feature: Background sync of Discogs collection        (ABM-002)
// Feature: Release detail view                          (ABM-012)
//
// Scenario: Application starts with a valid token and GetCollectionAsync succeeds
//   Given a valid personal access token and username are configured
//   And the Discogs API returns a single page of releases
//   When GetCollectionAsync is called
//   Then all releases from that page are returned
//
// Scenario: GetCollectionAsync paginates across multiple pages
//   Given the Discogs API returns 2 pages of releases
//   When GetCollectionAsync is called
//   Then releases from both pages are returned as a flat list
//
// Scenario: GetCollectionAsync stops when an empty releases array is returned
//   Given the Discogs API returns an empty releases list on the first page
//   When GetCollectionAsync is called
//   Then an empty list is returned
//
// Scenario: GetCollectionAsync throws when token is missing
//   Given the Discogs personal access token is empty
//   When GetCollectionAsync is called
//   Then an InvalidOperationException is thrown mentioning the token
//
// Scenario: GetCollectionAsync throws when username is missing
//   Given the Discogs username is empty
//   When GetCollectionAsync is called
//   Then an InvalidOperationException is thrown mentioning the username
//
// Scenario: Sync respects Discogs API rate limits
//   Given a sync is running
//   When the Discogs API returns 429 Too Many Requests on the first attempt
//   Then the client retries the request
//   And eventually returns the releases
//
// Scenario: GetCollectionAsync sends the Authorization header
//   Given a valid token is configured
//   When GetCollectionAsync is called
//   Then each HTTP request carries the correct Discogs token authorization header
//
// Scenario: GetReleaseDetailAsync returns genre correctly mapped
//   Given the Discogs release detail API returns a full response
//   When GetReleaseDetailAsync is called
//   Then the returned detail contains genre
//
// Scenario: GetReleaseDetailAsync handles missing optional fields gracefully
//   Given the Discogs release detail API returns a response with no genres
//   When GetReleaseDetailAsync is called
//   Then the returned detail has an empty collection for genres
//
// Scenario: GetReleaseDetailAsync returns null on a non-success HTTP response
//   Given the Discogs release detail API returns a 404 response
//   When GetReleaseDetailAsync is called
//   Then null is returned and no exception is thrown
//
// Scenario: GetReleaseDetailAsync retries on 429 rate limit before succeeding
//   Given the Discogs release detail API returns 429 on the first attempt
//   When GetReleaseDetailAsync is called
//   Then the client retries and returns the detail on the subsequent success response
//
// Scenario: GetMarketplaceStatsAsync returns all price fields correctly
//   Given the Discogs marketplace stats API returns a full response with highest, lowest, and median prices
//   When GetMarketplaceStatsAsync is called
//   Then all three price values are mapped correctly
//
// Scenario: GetMarketplaceStatsAsync returns null on a non-success HTTP response
//   Given the Discogs marketplace stats API returns a 500 server error
//   When GetMarketplaceStatsAsync is called
//   Then null is returned and no exception is thrown
//
// Scenario: GetMarketplaceStatsAsync retries on 429 rate limit before succeeding
//   Given the Discogs marketplace stats API returns 429 on the first attempt
//   When GetMarketplaceStatsAsync is called
//   Then the client retries and returns the stats on the subsequent success response

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AllByMyshelf.Api.Features.Discogs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Infrastructure;

public class DiscogsClientTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DiscogsClient CreateClient(
        HttpMessageHandler handler,
        string token = "my-token",
        string username = "my-user")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.discogs.com")
        };

        var optionsSnapshot = new Mock<IOptionsSnapshot<DiscogsOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(new DiscogsOptions
        {
            PersonalAccessToken = token,
            Username = username
        });

        return new DiscogsClient(httpClient, optionsSnapshot.Object, NullLogger<DiscogsClient>.Instance);
    }

    private static StringContent EmptyPage() =>
        new StringContent(
            JsonSerializer.Serialize(new
            {
                pagination = new { page = 1, pages = 1, per_page = 100, items = 0 },
                releases = Array.Empty<object>()
            }),
            System.Text.Encoding.UTF8,
            "application/json");

    private static StringContent JsonPage(int page, int totalPages, int releaseCount)
    {
        var releases = Enumerable.Range(1, releaseCount).Select(i => new
        {
            id = (page - 1) * 100 + i,
            basic_information = new
            {
                title = $"Album {i}",
                year = 2000 + i,
                artists = new[] { new { name = $"Artist {i}" } },
                formats = new[] { new { name = "Vinyl" } }
            }
        });

        var payload = new
        {
            pagination = new { page, pages = totalPages, per_page = 100, items = totalPages * releaseCount },
            releases
        };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");
    }

    // ── Configuration guard — missing token ───────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_TokenEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, EmptyPage()), token: string.Empty);

        // Act
        Func<Task> act = () => sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*token*");
    }

    [Fact]
    public async Task GetCollectionAsync_TokenWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, EmptyPage()), token: "   ");

        // Act
        Func<Task> act = () => sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Configuration guard — missing username ────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_UsernameEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, EmptyPage()), username: string.Empty);

        // Act
        Func<Task> act = () => sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*username*");
    }

    // ── Empty results ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_EmptyReleasesArray_ReturnsEmptyList()
    {
        // Arrange
        var handler = new StaticResponseHandler(HttpStatusCode.OK, EmptyPage());
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ── Rate-limit retry ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_RateLimited_RetriesAndEventuallyReturnsReleases()
    {
        // Arrange — first response is 429 with Retry-After: 0s, second is success
        var rateLimitResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        rateLimitResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
            TimeSpan.FromSeconds(0));

        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonPage(page: 1, totalPages: 1, releaseCount: 2)
        };

        var responses = new Queue<HttpResponseMessage>(new[] { rateLimitResponse, successResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        handler.CallCount.Should().Be(2); // one 429 + one success
    }

    [Fact]
    public async Task GetCollectionAsync_RateLimitedTwice_RetriesUntilSuccess()
    {
        // Arrange — two 429s then success
        var makeRateLimit = () =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return r;
        };

        var responses = new Queue<HttpResponseMessage>(new[]
        {
            makeRateLimit(),
            makeRateLimit(),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(1, 1, 1) }
        });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        handler.CallCount.Should().Be(3);
    }

    // ── Single page — happy path ──────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_SinglePage_MapsReleaseFieldsCorrectly()
    {
        // Arrange — one release with known values
        var payload = new
        {
            pagination = new { page = 1, pages = 1, per_page = 100, items = 1 },
            releases = new[]
            {
                new
                {
                    id = 999,
                    basic_information = new
                    {
                        title = "Blue Train",
                        year = 1957,
                        artists = new[] { new { name = "John Coltrane" } },
                        formats = new[] { new { name = "Vinyl" } }
                    }
                }
            }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, content));

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var release = result.Single();
        release.Id.Should().Be(999);
        release.BasicInformation.Title.Should().Be("Blue Train");
        release.BasicInformation.Year.Should().Be(1957);
        release.BasicInformation.Artists.Should().ContainSingle(a => a.Name == "John Coltrane");
        release.BasicInformation.Formats.Should().ContainSingle(f => f.Name == "Vinyl");
    }

    [Fact]
    public async Task GetCollectionAsync_SinglePage_ReturnsAllReleasesFromPage()
    {
        // Arrange
        var handler = new StaticResponseHandler(HttpStatusCode.OK, JsonPage(page: 1, totalPages: 1, releaseCount: 5));
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(5);
    }

    // ── Pagination — multiple pages ───────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_ThreePages_IssuesThreeHttpRequests()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(1, 3, 2) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(2, 3, 2) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(3, 3, 2) }
        });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(6);
        handler.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task GetCollectionAsync_TwoPages_ReturnsCombinedReleasesFromBothPages()
    {
        // Arrange — page 1 returns 3 releases, page 2 returns 2 releases
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(page: 1, totalPages: 2, releaseCount: 3) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonPage(page: 2, totalPages: 2, releaseCount: 2) }
        });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(5);
    }

    // ── Authorization header ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_ValidToken_SendsDiscogsAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = EmptyPage() });
        var sut = CreateClient(capturingHandler, token: "super-secret-token");

        // Act
        await sut.GetCollectionAsync(CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        // The client calls request.Headers.Add("Authorization", ...) which HttpClient
        // parses into the typed Authorization header.
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Discogs");
        authHeader.Parameter.Should().Be("token=super-secret-token");
    }

    // ── GetReleaseDetailAsync — happy path ────────────────────────────────────

    [Fact]
    public async Task GetReleaseDetailAsync_FullResponse_MapsAllFieldsCorrectly()
    {
        // Arrange
        var payload = new
        {
            genres = new[] { "Jazz" }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, content));

        // Act
        var result = await sut.GetReleaseDetailAsync(12345, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Genres.Should().ContainSingle(g => g == "Jazz");
    }

    // ── GetReleaseDetailAsync — missing optional fields ───────────────────────

    [Fact]
    public async Task GetReleaseDetailAsync_NoGenres_ReturnsEmptyGenresCollection()
    {
        // Arrange
        var payload = new
        {
            genres = Array.Empty<string>()
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, content));

        // Act
        var result = await sut.GetReleaseDetailAsync(42, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Genres.Should().BeEmpty();
    }

    // ── GetReleaseDetailAsync — non-success response ──────────────────────────

    [Fact]
    public async Task GetReleaseDetailAsync_NotFoundResponse_ReturnsNull()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.NotFound,
            new StringContent(string.Empty)));

        // Act
        var result = await sut.GetReleaseDetailAsync(99999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReleaseDetailAsync_ServerError_ReturnsNullWithoutThrowing()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.InternalServerError,
            new StringContent(string.Empty)));

        // Act
        Func<Task> act = () => sut.GetReleaseDetailAsync(1, CancellationToken.None);

        // Assert — must not throw; must return null
        await act.Should().NotThrowAsync();
        var result = await sut.GetReleaseDetailAsync(1, CancellationToken.None);
        result.Should().BeNull();
    }

    // ── GetReleaseDetailAsync — rate-limit retry ──────────────────────────────

    [Fact]
    public async Task GetReleaseDetailAsync_RateLimited_RetriesAndReturnsDetail()
    {
        // Arrange — first response is 429, second is success
        var rateLimitResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        rateLimitResponse.Headers.RetryAfter =
            new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(0));

        var detailPayload = new
        {
            genres = new[] { "Jazz" }
        };
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(detailPayload),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        var responses = new Queue<HttpResponseMessage>(new[] { rateLimitResponse, successResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReleaseDetailAsync(77, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Genres.Should().ContainSingle(g => g == "Jazz");
        handler.CallCount.Should().Be(2); // one 429 + one success
    }

    // ── GetMarketplaceStatsAsync — happy path ─────────────────────────────────

    [Fact]
    public async Task GetMarketplaceStatsAsync_FullResponse_MapsAllPrices()
    {
        // Arrange
        var payload = new
        {
            lowest_price = new { currency = "USD", value = 15.99m },
            median_price = new { currency = "USD", value = 25.50m },
            highest_price = new { currency = "USD", value = 49.99m }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.OK, content));

        // Act
        var result = await sut.GetMarketplaceStatsAsync(12345, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.LowestPrice.Should().NotBeNull();
        result.LowestPrice!.Currency.Should().Be("USD");
        result.LowestPrice.Value.Should().Be(15.99m);
        result!.MedianPrice.Should().NotBeNull();
        result.MedianPrice!.Currency.Should().Be("USD");
        result.MedianPrice.Value.Should().Be(25.50m);
        result!.HighestPrice.Should().NotBeNull();
        result.HighestPrice!.Currency.Should().Be("USD");
        result.HighestPrice.Value.Should().Be(49.99m);
    }

    // ── GetMarketplaceStatsAsync — non-success response ───────────────────────

    [Fact]
    public async Task GetMarketplaceStatsAsync_ServerError_ReturnsNull()
    {
        // Arrange
        var sut = CreateClient(new StaticResponseHandler(HttpStatusCode.InternalServerError,
            new StringContent(string.Empty)));

        // Act
        Func<Task> act = () => sut.GetMarketplaceStatsAsync(1, CancellationToken.None);

        // Assert — must not throw; must return null
        await act.Should().NotThrowAsync();
        var result = await sut.GetMarketplaceStatsAsync(1, CancellationToken.None);
        result.Should().BeNull();
    }

    // ── GetMarketplaceStatsAsync — rate-limit retry ───────────────────────────

    [Fact]
    public async Task GetMarketplaceStatsAsync_RateLimited_RetriesAndReturnsStats()
    {
        // Arrange — first response is 429, second is success
        var rateLimitResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        rateLimitResponse.Headers.RetryAfter =
            new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(0));

        var statsPayload = new
        {
            lowest_price = new { currency = "USD", value = 10.00m },
            median_price = new { currency = "USD", value = 20.00m },
            highest_price = new { currency = "USD", value = 30.00m }
        };
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(statsPayload),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        var responses = new Queue<HttpResponseMessage>(new[] { rateLimitResponse, successResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetMarketplaceStatsAsync(99, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.LowestPrice!.Value.Should().Be(10.00m);
        result.MedianPrice!.Value.Should().Be(20.00m);
        result.HighestPrice!.Value.Should().Be(30.00m);
        handler.CallCount.Should().Be(2); // one 429 + one success
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

/// <summary>Captures the last request and returns a fixed response.</summary>
internal sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(response);
    }
}

/// <summary>Returns responses from a queue in order.</summary>
internal sealed class QueuedResponseHandler(Queue<HttpResponseMessage> responses)
    : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(responses.Dequeue());
    }
}

/// <summary>Always returns the same pre-built response.</summary>
internal sealed class StaticResponseHandler(HttpStatusCode statusCode, HttpContent content)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode) { Content = content });
}
