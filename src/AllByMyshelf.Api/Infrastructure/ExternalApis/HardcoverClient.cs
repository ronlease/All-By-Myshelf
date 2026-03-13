using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllByMyshelf.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Infrastructure.ExternalApis;

/// <summary>
/// Client for the Hardcover GraphQL API.
/// </summary>
public class HardcoverClient(
    IHttpClientFactory httpClientFactory,
    IOptions<HardcoverOptions> options,
    ILogger<HardcoverClient> logger)
{
    private const string ApiUrl = "https://api.hardcover.app/v1/graphql";
    private readonly HardcoverOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Fetches all books marked as "read" (status_id = 3) from the authenticated user's Hardcover library.
    /// Uses two queries: one to resolve the current user's ID, then one to fetch their read books.
    /// </summary>
    public async Task<List<HardcoverBook>> GetReadBooksAsync(CancellationToken cancellationToken)
    {
        var userId = await GetCurrentUserIdAsync(cancellationToken);
        if (userId is null)
        {
            logger.LogWarning("Could not resolve Hardcover user ID — skipping book sync.");
            return [];
        }

        return await GetReadBooksByUserIdAsync(userId.Value, cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var query = new { query = "{ me { id } }" };
        var raw = await PostQueryAsync(query, cancellationToken);
        logger.LogDebug("Hardcover me response: {Raw}", raw);

        var result = JsonSerializer.Deserialize<MeResponse>(raw, JsonOptions);
        var id = result?.Data?.Me?.FirstOrDefault()?.Id;

        if (id is null)
            logger.LogWarning("Hardcover me query did not return a user ID. Raw: {Raw}", raw);

        return id;
    }

    private async Task<List<HardcoverBook>> GetReadBooksByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        const int pageSize = 500;
        var allBooks = new List<HardcoverBook>();
        var offset = 0;

        while (true)
        {
            var query = new
            {
                query = $@"
                    {{
                        user_books(
                            where: {{user_id: {{_eq: {userId}}}, status_id: {{_eq: 3}}}}
                            distinct_on: book_id
                            limit: {pageSize}
                            offset: {offset}
                        ) {{
                            book {{
                                cached_tags
                                contributions {{
                                    author {{
                                        name
                                    }}
                                }}
                                id
                                image {{
                                    url
                                }}
                                release_date
                                title
                            }}
                        }}
                    }}"
            };

            var raw = await PostQueryAsync(query, cancellationToken);
            logger.LogDebug("Hardcover user_books response (offset {Offset}): {Raw}", offset, raw);

            var result = JsonSerializer.Deserialize<UserBooksResponse>(raw, JsonOptions);
            var page = result?.Data?.UserBooks;

            if (page is null)
            {
                logger.LogWarning("Hardcover user_books query returned no data. Raw: {Raw}", raw);
                break;
            }

            var books = page
                .Where(ub => ub.Book is not null)
                .Select(ub => ub.Book!)
                .ToList();

            allBooks.AddRange(books);

            if (books.Count < pageSize)
                break;

            offset += pageSize;
        }

        logger.LogInformation("Hardcover sync fetched {Count} read books for user {UserId}.", allBooks.Count, userId);
        return allBooks;
    }

    private async Task<string> PostQueryAsync(object query, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Hardcover");

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.TryAddWithoutValidation("authorization", _options.ApiToken.Trim());
        request.Content = JsonContent.Create(query);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // ── Response models ──────────────────────────────────────────────────────

    private record MeResponse(
        [property: JsonPropertyName("data")] MeData? Data);

    private record MeData(
        [property: JsonPropertyName("me")] List<MeUser>? Me);

    private record MeUser(
        [property: JsonPropertyName("id")] int? Id);

    private record UserBooksResponse(
        [property: JsonPropertyName("data")] UserBooksData? Data);

    private record UserBooksData(
        [property: JsonPropertyName("user_books")] List<HardcoverUserBook>? UserBooks);

    private record HardcoverUserBook(
        [property: JsonPropertyName("book")] HardcoverBook? Book);

    public record HardcoverBook(
        [property: JsonPropertyName("cached_tags")] JsonElement? CachedTags,
        [property: JsonPropertyName("contributions")] List<HardcoverContribution>? Contributions,
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("image")] HardcoverImage? Image,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("title")] string? Title);

    public record HardcoverContribution(
        [property: JsonPropertyName("author")] HardcoverAuthor? Author);

    public record HardcoverAuthor(
        [property: JsonPropertyName("name")] string? Name);

    public record HardcoverImage(
        [property: JsonPropertyName("url")] string? Url);
}
