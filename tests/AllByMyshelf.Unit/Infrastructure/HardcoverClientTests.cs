// Feature: Hardcover personal library sync
//
// Scenario: User retrieves their read books successfully
//   Given the user is authenticated with a valid Hardcover API token
//   And the Hardcover me query returns a valid user ID
//   And the user_books query returns a list of books
//   When GetReadBooksAsync is called
//   Then all read books are returned
//
// Scenario: GetReadBooksAsync returns empty when user ID cannot be resolved
//   Given the Hardcover me query returns null data
//   When GetReadBooksAsync is called
//   Then an empty list is returned
//
// Scenario: GetReadBooksAsync returns empty when user_books returns null data
//   Given the me query returns a valid user ID
//   And the user_books query returns null data
//   When GetReadBooksAsync is called
//   Then an empty list is returned
//
// Scenario: GetReadBooksAsync sends authorization header from options
//   Given a valid API token is configured
//   When GetReadBooksAsync is called
//   Then each HTTP request includes the authorization header with the token
//
// Scenario: GetReadBooksAsync handles paginated results
//   Given the user_books query returns 500 books on the first page
//   And the user_books query returns 200 books on the second page
//   When GetReadBooksAsync is called
//   Then all 700 books are returned
//   And two HTTP requests are made with correct offsets
//
// Scenario: PostQueryAsync throws on non-success HTTP response
//   Given the Hardcover API returns a 500 Internal Server Error
//   When PostQueryAsync is called
//   Then an HttpRequestException is thrown

using System.Net;
using System.Text.Json;
using AllByMyshelf.Api.Features.Hardcover;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Infrastructure;

public class HardcoverClientTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HardcoverClient CreateClient(
        HttpMessageHandler handler,
        string apiToken = "test-token")
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory
            .Setup(f => f.CreateClient("Hardcover"))
            .Returns(new HttpClient(handler));

        var optionsSnapshot = new Mock<IOptionsSnapshot<HardcoverOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(new HardcoverOptions
        {
            ApiToken = apiToken
        });

        return new HardcoverClient(
            mockFactory.Object,
            optionsSnapshot.Object,
            NullLogger<HardcoverClient>.Instance);
    }

    private static StringContent MeResponse(int? userId)
    {
        object payload;
        if (userId.HasValue)
        {
            payload = new { data = new { me = new[] { new { id = userId.Value } } } };
        }
        else
        {
            payload = new { data = new { me = Array.Empty<object>() } };
        }

        return new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8);
    }

    private static StringContent UserBooksResponse(int bookCount, int startId = 1)
    {
        var books = Enumerable.Range(startId, bookCount).Select(i => new
        {
            book = new
            {
                id = i,
                title = $"Book {i}",
                contributions = (object?)null,
                image = (object?)null,
                release_date = (string?)null,
                cached_tags = (object?)null
            }
        });

        var payload = new
        {
            data = new { user_books = books.ToArray() }
        };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8);
    }

    private static StringContent UserBooksNullDataResponse()
    {
        var payload = new { data = new { user_books = (object?)null } };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8);
    }

    // ── Authorization header ──────────────────────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_ValidToken_SendsAuthorizationHeader()
    {
        // Arrange
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(123) };
        var booksResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(1) };
        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, booksResponse });
        var handler = new CapturingQueuedHandler(responses);
        var sut = CreateClient(handler, apiToken: "my-secret-token");

        // Act
        await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        handler.CapturedRequests.Should().HaveCount(2);
        foreach (var request in handler.CapturedRequests)
        {
            request.Headers.TryGetValues("authorization", out var values);
            values.Should().ContainSingle("my-secret-token");
        }
    }

    [Fact]
    public async Task GetReadBooksAsync_TokenWithWhitespace_TrimsTokenBeforeSending()
    {
        // Arrange
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(456) };
        var booksResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(1) };
        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, booksResponse });
        var handler = new CapturingQueuedHandler(responses);
        var sut = CreateClient(handler, apiToken: "  token-with-spaces  ");

        // Act
        await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        handler.CapturedRequests.Should().HaveCount(2);
        var firstRequest = handler.CapturedRequests.First();
        firstRequest.Headers.TryGetValues("authorization", out var values);
        values.Should().ContainSingle("token-with-spaces");
    }

    // ── Happy path — single page ──────────────────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_MeQueryAndUserBooksSucceed_ReturnsBooks()
    {
        // Arrange
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(100) };
        var booksResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(3) };
        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, booksResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(b => b.Title != null && b.Title.StartsWith("Book "));
    }

    [Fact]
    public async Task GetReadBooksAsync_SinglePageWithMappedFields_MapsAllFieldsCorrectly()
    {
        // Arrange
        var mePayload = new { data = new { me = new[] { new { id = 200 } } } };
        var booksPayload = new
        {
            data = new
            {
                user_books = new[]
                {
                    new
                    {
                        book = new
                        {
                            id = 777,
                            title = "The Hobbit",
                            contributions = new[]
                            {
                                new { author = new { name = "J.R.R. Tolkien" } }
                            },
                            image = new { url = "https://example.com/hobbit.jpg" },
                            release_date = "1937-09-21",
                            cached_tags = (object?)null
                        }
                    }
                }
            }
        };

        var meContent = new StringContent(JsonSerializer.Serialize(mePayload), System.Text.Encoding.UTF8);
        var booksContent = new StringContent(JsonSerializer.Serialize(booksPayload), System.Text.Encoding.UTF8);

        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = meContent },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = booksContent }
        });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var book = result.Single();
        book.Id.Should().Be(777);
        book.Title.Should().Be("The Hobbit");
        book.Contributions.Should().ContainSingle(c => c.Author!.Name == "J.R.R. Tolkien");
        book.Image!.Url.Should().Be("https://example.com/hobbit.jpg");
        book.ReleaseDate.Should().Be("1937-09-21");
    }

    // ── Me query returns null data ────────────────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_MeQueryReturnsNullData_ReturnsEmptyList()
    {
        // Arrange — me query returns null user ID
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(null) };
        var handler = new QueuedResponseHandler(new Queue<HttpResponseMessage>(new[] { meResponse }));
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        handler.CallCount.Should().Be(1); // Only me query called, user_books is skipped
    }

    [Fact]
    public async Task GetReadBooksAsync_MeQueryReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var payload = new { data = new { me = Array.Empty<object>() } };
        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8);
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var handler = new QueuedResponseHandler(new Queue<HttpResponseMessage>(new[] { meResponse }));
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ── User_books returns null data ──────────────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_UserBooksReturnsNullData_ReturnsEmptyList()
    {
        // Arrange
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(300) };
        var booksResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksNullDataResponse() };
        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, booksResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        handler.CallCount.Should().Be(2); // me + one user_books attempt
    }

    [Fact]
    public async Task GetReadBooksAsync_UserBooksContainsNullBooks_FiltersOutNullBooks()
    {
        // Arrange
        var mePayload = new { data = new { me = new[] { new { id = 350 } } } };
        var booksPayload = new
        {
            data = new
            {
                user_books = new object[]
                {
                    new { book = new { id = 1, title = "Book 1", contributions = (object?)null, image = (object?)null, release_date = (string?)null, cached_tags = (object?)null } },
                    new { book = (object?)null }, // Null book entry
                    new { book = new { id = 2, title = "Book 2", contributions = (object?)null, image = (object?)null, release_date = (string?)null, cached_tags = (object?)null } }
                }
            }
        };

        var meContent = new StringContent(JsonSerializer.Serialize(mePayload), System.Text.Encoding.UTF8);
        var booksContent = new StringContent(JsonSerializer.Serialize(booksPayload), System.Text.Encoding.UTF8);

        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = meContent },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = booksContent }
        });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.Title != null);
    }

    // ── Pagination — multiple pages ───────────────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_PaginatedResults_ReturnsAllBooks()
    {
        // Arrange — first page returns 500 books (full page), second page returns 200 books (last page)
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(400) };
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(500, startId: 1) };
        var page2Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(200, startId: 501) };

        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, page1Response, page2Response });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(700);
        handler.CallCount.Should().Be(3); // me + 2 user_books pages
    }

    [Fact]
    public async Task GetReadBooksAsync_ThreePages_IssuesCorrectNumberOfRequests()
    {
        // Arrange — 3 pages: 500 + 500 + 100
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(500) };
        var page1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(500, 1) };
        var page2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(500, 501) };
        var page3 = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(100, 1001) };

        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, page1, page2, page3 });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1100);
        handler.CallCount.Should().Be(4); // me + 3 pages
    }

    [Fact]
    public async Task GetReadBooksAsync_PaginatedResults_StopsWhenPageSizeLessThan500()
    {
        // Arrange — page 1 has 499 books, which is less than 500, so pagination stops
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(600) };
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = UserBooksResponse(499) };

        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, page1Response });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        var result = await sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(499);
        handler.CallCount.Should().Be(2); // me + 1 user_books page
    }

    // ── PostQueryAsync throws on non-success ──────────────────────────────────

    [Fact]
    public async Task GetReadBooksAsync_MeQueryReturnsServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };
        var handler = new QueuedResponseHandler(new Queue<HttpResponseMessage>(new[] { errorResponse }));
        var sut = CreateClient(handler);

        // Act
        Func<Task> act = () => sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetReadBooksAsync_UserBooksQueryReturnsServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var meResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = MeResponse(700) };
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };
        var responses = new Queue<HttpResponseMessage>(new[] { meResponse, errorResponse });
        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler);

        // Act
        Func<Task> act = () => sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetReadBooksAsync_NotFoundResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found")
        };
        var handler = new QueuedResponseHandler(new Queue<HttpResponseMessage>(new[] { notFoundResponse }));
        var sut = CreateClient(handler);

        // Act
        Func<Task> act = () => sut.GetReadBooksAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

/// <summary>Captures all requests and returns responses from a queue in order.</summary>
internal sealed class CapturingQueuedHandler(Queue<HttpResponseMessage> responses)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> CapturedRequests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequests.Add(request);
        return Task.FromResult(responses.Dequeue());
    }
}
