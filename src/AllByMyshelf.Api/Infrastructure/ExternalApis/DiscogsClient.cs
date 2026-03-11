using System.Net;
using System.Text.Json.Serialization;
using AllByMyshelf.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Infrastructure.ExternalApis;

/// <summary>
/// Typed HTTP client for the Discogs API.
/// Handles authentication, pagination, and rate-limit back-off.
/// </summary>
public class DiscogsClient(HttpClient httpClient, IOptions<DiscogsOptions> options, ILogger<DiscogsClient> logger)
{
    private readonly DiscogsOptions _options = options.Value;

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
    /// <returns>A <see cref="DiscogsReleaseDetail"/> containing label, country, genre, notes, and styles, or null on failure.</returns>
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
                await Task.Delay(retryAfter, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
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
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pages")]
    public int Pages { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("items")]
    public int Items { get; init; }
}

/// <summary>A single release entry within a Discogs collection page.</summary>
public class DiscogsRelease
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("basic_information")]
    public DiscogsBasicInformation BasicInformation { get; init; } = new();
}

/// <summary>Basic metadata for a Discogs release.</summary>
public class DiscogsBasicInformation
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; init; }

    [JsonPropertyName("artists")]
    public List<DiscogsArtist> Artists { get; init; } = [];

    [JsonPropertyName("formats")]
    public List<DiscogsFormat> Formats { get; init; } = [];
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
    [JsonPropertyName("labels")]
    public List<DiscogsLabel> Labels { get; init; } = [];

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; init; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("styles")]
    public List<string> Styles { get; init; } = [];
}

/// <summary>A label reference within a Discogs release detail response.</summary>
public class DiscogsLabel
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
