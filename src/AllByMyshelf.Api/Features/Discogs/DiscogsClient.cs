using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Typed HTTP client for the Discogs API.
/// Handles authentication, pagination, and rate-limit back-off.
/// </summary>
public class DiscogsClient(HttpClient httpClient, IOptions<DiscogsOptions> options, ILogger<DiscogsClient> logger)
{
    private readonly DiscogsOptions _options = options.Value;

    /// <summary>Fired when a rate-limit response is received. Argument is the number of seconds to wait.</summary>
    public event Action<int>? OnRateLimitPause;

    /// <summary>Fired when execution resumes after a rate-limit delay.</summary>
    public event Action? OnRateLimitResume;

    private async Task<HttpResponseMessage> FetchWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Discogs token={_options.PersonalAccessToken}");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                logger.LogWarning("Discogs rate limit hit. Backing off for {Seconds}s before retrying {Url}.",
                    retryAfter.TotalSeconds, url);
                OnRateLimitPause?.Invoke((int)retryAfter.TotalSeconds);
                await Task.Delay(retryAfter, cancellationToken);
                OnRateLimitResume?.Invoke();
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }
    }

    /// <summary>
    /// Fetches every release from the user's flat Discogs collection (all pages).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A flat list of all <see cref="DiscogsRelease"/> records.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Discogs token or username is not configured.
    /// </exception>
    public async Task<IReadOnlyList<DiscogsRelease>> GetCollectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
            throw new InvalidOperationException("Discogs personal access token is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("Discogs username is not configured.");

        var releases = new List<DiscogsRelease>();
        var page = 1;

        while (true)
        {
            var url = $"/users/{_options.Username}/collection/folders/0/releases?per_page=100&page={page}";
            var response = await FetchWithRetryAsync(url, cancellationToken);

            var pageData = await response.Content.ReadFromJsonAsync<DiscogsCollectionPage>(cancellationToken: cancellationToken);
            if (pageData?.Releases is null || pageData.Releases.Count == 0)
                break;

            releases.AddRange(pageData.Releases);

            if (page >= pageData.Pagination.Pages)
                break;

            page++;
        }

        return releases;
    }

    /// <summary>
    /// Fetches extended detail for a single Discogs release.
    /// Returns null gracefully when the release is not found or the response cannot be parsed.
    /// </summary>
    /// <param name="discogsId">The Discogs release ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DiscogsReleaseDetail"/> containing genre, or null on failure.</returns>
    public async Task<DiscogsReleaseDetail?> GetReleaseDetailAsync(int discogsId, CancellationToken cancellationToken)
    {
        var url = $"/releases/{discogsId}";
        try
        {
            var response = await FetchWithRetryAsync(url, cancellationToken);
            return await response.Content.ReadFromJsonAsync<DiscogsReleaseDetail>(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch release detail for Discogs ID {DiscogsId}. Skipping detail fields.", discogsId);
            return null;
        }
    }

    /// <summary>
    /// Fetches marketplace pricing statistics for a single Discogs release.
    /// Returns null gracefully on failure so the sync can continue without pricing data.
    /// </summary>
    /// <param name="discogsId">The Discogs release ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DiscogsMarketplaceStats"/> with low/median/high prices, or null on failure.</returns>
    public async Task<DiscogsMarketplaceStats?> GetMarketplaceStatsAsync(int discogsId, CancellationToken cancellationToken)
    {
        var url = $"/marketplace/stats/{discogsId}?curr_abbr=USD";
        try
        {
            var response = await FetchWithRetryAsync(url, cancellationToken);
            return await response.Content.ReadFromJsonAsync<DiscogsMarketplaceStats>(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch marketplace stats for Discogs ID {DiscogsId}. Skipping pricing.", discogsId);
            return null;
        }
    }
}

// ── Response model types ──────────────────────────────────────────────────────

/// <summary>A single page of collection results from the Discogs API.</summary>
public class DiscogsCollectionPage
{
    [JsonPropertyName("pagination")]
    public DiscogsPagination Pagination { get; init; } = new();

    [JsonPropertyName("releases")]
    public List<DiscogsRelease> Releases { get; init; } = [];
}

/// <summary>Pagination metadata from a Discogs collection response.</summary>
public class DiscogsPagination
{
    [JsonPropertyName("items")]
    public int Items { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pages")]
    public int Pages { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }
}

/// <summary>A single release entry within a Discogs collection page.</summary>
public class DiscogsRelease
{
    [JsonPropertyName("basic_information")]
    public DiscogsBasicInformation BasicInformation { get; init; } = new();

    [JsonPropertyName("id")]
    public int Id { get; init; }
}

/// <summary>Basic metadata for a Discogs release.</summary>
public class DiscogsBasicInformation
{
    [JsonPropertyName("artists")]
    public List<DiscogsArtist> Artists { get; init; } = [];

    [JsonPropertyName("cover_image")]
    public string? CoverImage { get; init; }

    [JsonPropertyName("formats")]
    public List<DiscogsFormat> Formats { get; init; } = [];

    [JsonPropertyName("thumb")]
    public string? Thumb { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; init; }
}

/// <summary>An artist reference within a Discogs release.</summary>
public class DiscogsArtist
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>A format descriptor within a Discogs release.</summary>
public class DiscogsFormat
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

// ── Release detail response models ────────────────────────────────────────────

/// <summary>Extended detail for a single Discogs release (GET /releases/{id}).</summary>
public class DiscogsReleaseDetail
{
    [JsonPropertyName("genres")]
    public List<string> Genres { get; init; } = [];
}

/// <summary>Marketplace pricing statistics for a release (GET /marketplace/stats/{id}).</summary>
public class DiscogsMarketplaceStats
{
    [JsonPropertyName("highest_price")]
    public DiscogsPrice? HighestPrice { get; init; }

    [JsonPropertyName("lowest_price")]
    public DiscogsPrice? LowestPrice { get; init; }

    [JsonPropertyName("median_price")]
    public DiscogsPrice? MedianPrice { get; init; }
}

/// <summary>A price value with currency from the Discogs marketplace.</summary>
public class DiscogsPrice
{
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public decimal Value { get; init; }
}
