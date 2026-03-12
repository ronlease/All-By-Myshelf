using System.Text.Json;
using System.Text.Json.Serialization;
using AllByMyshelf.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Infrastructure.ExternalApis;

/// <summary>
/// Typed HTTP client for the Hardcover GraphQL API.
/// Handles authentication and fetches read books from the user's collection.
/// </summary>
public class HardcoverClient(
    IHttpClientFactory httpClientFactory,
    IOptions<HardcoverOptions> options,
    ILogger<HardcoverClient> logger)
{
    private readonly HardcoverOptions _options = options.Value;

    /// <summary>
    /// Fetches all books marked as "read" (status_id = 3) from the authenticated user's Hardcover collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="HardcoverBook"/> records.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Hardcover API token is not configured.
    /// </exception>
    public async Task<List<HardcoverBook>> GetReadBooksAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
            throw new InvalidOperationException("Hardcover API token is not configured.");

        var client = httpClientFactory.CreateClient("Hardcover");

        var query = new
        {
            query = @"
                {
                    me {
                        user_books(where: {status_id: {_eq: 3}}) {
                            book {
                                id
                                title
                                contributions {
                                    author {
                                        name
                                    }
                                }
                                release_date
                                cached_tags
                                image {
                                    url
                                }
                            }
                        }
                    }
                }"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.hardcover.app/v1/graphql");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiToken.Trim()}");
        request.Content = JsonContent.Create(query);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("Hardcover raw response: {Response}", raw);

        var result = JsonSerializer.Deserialize<HardcoverGraphQLResponse>(raw, options);

        var userBooks = result?.Data?.Me?.FirstOrDefault()?.UserBooks;
        if (userBooks is null)
        {
            logger.LogWarning("Hardcover GraphQL response did not contain expected data. Raw: {Raw}", raw);
            return new List<HardcoverBook>();
        }

        return userBooks
            .Where(ub => ub.Book is not null)
            .Select(ub => ub.Book!)
            .ToList();
    }

    #region Response Models

    private record HardcoverGraphQLResponse(
        [property: JsonPropertyName("data")] HardcoverData? Data);

    private record HardcoverData(
        [property: JsonPropertyName("me")] List<HardcoverMe>? Me);

    private record HardcoverMe(
        [property: JsonPropertyName("user_books")] List<HardcoverUserBook>? UserBooks);

    private record HardcoverUserBook(
        [property: JsonPropertyName("book")] HardcoverBook? Book);

    /// <summary>
    /// Represents a book returned by the Hardcover GraphQL API.
    /// </summary>
    public record HardcoverBook(
        [property: JsonPropertyName("cached_tags")] List<string>? CachedTags,
        [property: JsonPropertyName("contributions")] List<HardcoverContribution>? Contributions,
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("image")] HardcoverImage? Image,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("title")] string? Title);

    /// <summary>
    /// Represents a contribution (author, illustrator, etc.) for a Hardcover book.
    /// </summary>
    public record HardcoverContribution(
        [property: JsonPropertyName("author")] HardcoverAuthor? Author);

    /// <summary>
    /// Represents an author for a Hardcover book.
    /// </summary>
    public record HardcoverAuthor(
        [property: JsonPropertyName("name")] string? Name);

    /// <summary>
    /// Represents an image for a Hardcover book.
    /// </summary>
    public record HardcoverImage(
        [property: JsonPropertyName("url")] string? Url);

    #endregion
}
