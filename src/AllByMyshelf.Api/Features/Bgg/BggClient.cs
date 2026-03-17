using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Client for the BoardGameGeek XML API.
/// Sends an Authorization: Bearer header when an API token is configured.
/// </summary>
public class BggClient(HttpClient httpClient, IOptions<BggOptions> options, ILogger<BggClient> logger)
{
    private readonly BggOptions _options = options.Value;

    private const int MaxRetries = 5;

    /// <summary>
    /// Fetches all board games marked as owned from the specified username's BGG collection.
    /// The BGG collection API sometimes returns HTTP 202 (request queued) which requires retrying with exponential backoff.
    /// </summary>
    /// <param name="username">The BGG username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of collection items with basic game info and stats.</returns>
    public async Task<IReadOnlyList<BggCollectionItem>> GetCollectionAsync(string username, CancellationToken cancellationToken)
    {
        var url = $"/xmlapi2/collection?username={Uri.EscapeDataString(username)}&own=1&stats=1&subtype=boardgame";

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuthHeader(request);
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                logger.LogInformation("BGG returned 202, retrying in {Delay}s (attempt {Attempt}/{Max})", delay.TotalSeconds, attempt + 1, MaxRetries);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCollection(xml);
        }

        throw new InvalidOperationException("BGG collection request failed after max retries.");
    }

    /// <summary>
    /// Fetches detailed information (description, designer, category) for the specified game IDs.
    /// Accepts a batch of IDs as a comma-separated list.
    /// </summary>
    /// <param name="ids">BGG game IDs to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of detailed game information.</returns>
    public async Task<IReadOnlyList<BggThingDetail>> GetThingDetailsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var idList = string.Join(",", ids);
        var url = $"/xmlapi2/thing?id={idList}&stats=1";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuthHeader(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseThingDetails(xml);
    }

    private void ApplyAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        }
    }

    private static IReadOnlyList<BggCollectionItem> ParseCollection(string xml)
    {
        var doc = XDocument.Parse(xml);
        var items = new List<BggCollectionItem>();

        foreach (var item in doc.Descendants("item"))
        {
            var bggId = int.Parse(item.Attribute("objectid")?.Value ?? "0");
            var name = item.Element("name")?.Value;
            var yearPublished = int.TryParse(item.Element("yearpublished")?.Value, out var y) ? y : (int?)null;
            var thumbnail = item.Element("thumbnail")?.Value;
            var image = item.Element("image")?.Value;
            var minPlayers = int.TryParse(item.Element("stats")?.Attribute("minplayers")?.Value, out var minP) ? minP : (int?)null;
            var maxPlayers = int.TryParse(item.Element("stats")?.Attribute("maxplayers")?.Value, out var maxP) ? maxP : (int?)null;
            var minPlaytime = int.TryParse(item.Element("stats")?.Attribute("minplaytime")?.Value, out var minT) ? minT : (int?)null;
            var maxPlaytime = int.TryParse(item.Element("stats")?.Attribute("maxplaytime")?.Value, out var maxT) ? maxT : (int?)null;

            if (bggId > 0 && !string.IsNullOrWhiteSpace(name))
            {
                items.Add(new BggCollectionItem(bggId, image, maxPlayers, maxPlaytime, minPlayers, minPlaytime, name, thumbnail, yearPublished));
            }
        }

        return items;
    }

    private static IReadOnlyList<BggThingDetail> ParseThingDetails(string xml)
    {
        var doc = XDocument.Parse(xml);
        var details = new List<BggThingDetail>();

        foreach (var item in doc.Descendants("item"))
        {
            var id = int.TryParse(item.Attribute("id")?.Value, out var i) ? i : 0;
            var description = item.Element("description")?.Value;

            var designers = item.Elements("link")
                .Where(l => l.Attribute("type")?.Value == "boardgamedesigner")
                .Select(l => l.Attribute("value")?.Value!)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var category = item.Elements("link")
                .FirstOrDefault(l => l.Attribute("type")?.Value == "boardgamecategory")
                ?.Attribute("value")?.Value;

            if (id > 0)
            {
                details.Add(new BggThingDetail(category, description, designers, id));
            }
        }

        return details;
    }
}

/// <summary>
/// Represents a single item from the BGG collection API response.
/// Contains basic game info and player/playtime stats.
/// </summary>
/// <param name="BggId">BGG game ID.</param>
/// <param name="CoverImageUrl">Full image URL.</param>
/// <param name="MaxPlayers">Maximum number of players.</param>
/// <param name="MaxPlaytime">Maximum playtime in minutes.</param>
/// <param name="MinPlayers">Minimum number of players.</param>
/// <param name="MinPlaytime">Minimum playtime in minutes.</param>
/// <param name="Name">Game title.</param>
/// <param name="ThumbnailUrl">Thumbnail image URL.</param>
/// <param name="YearPublished">Year the game was published.</param>
public record BggCollectionItem(
    int BggId,
    string? CoverImageUrl,
    int? MaxPlayers,
    int? MaxPlaytime,
    int? MinPlayers,
    int? MinPlaytime,
    string Name,
    string? ThumbnailUrl,
    int? YearPublished);

/// <summary>
/// Represents detailed information fetched from the BGG thing API.
/// Contains enrichment data like description, designers, and category.
/// </summary>
/// <param name="Category">Primary category/genre.</param>
/// <param name="Description">Game description.</param>
/// <param name="Designers">List of designer names.</param>
/// <param name="Id">BGG game ID.</param>
public record BggThingDetail(
    string? Category,
    string? Description,
    List<string> Designers,
    int Id);
