namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a vinyl release persisted from a Discogs collection sync.
/// </summary>
public class Release
{
    /// <summary>Primary key (application-generated GUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Discogs release ID used for upsert matching.</summary>
    public int DiscogsId { get; set; }

    /// <summary>Artist name as returned by Discogs.</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>Release title as returned by Discogs.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Release year; null when Discogs does not provide one.</summary>
    public int? Year { get; set; }

    /// <summary>Primary format description (e.g. "Vinyl", "CD").</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the last sync that touched this record.</summary>
    public DateTimeOffset LastSyncedAt { get; set; }
}
