namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a vinyl release persisted from a Discogs collection sync.
/// </summary>
public class Release
{
    /// <summary>Artist name as returned by Discogs.</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>Country of release; null when not populated by sync.</summary>
    public string? Country { get; set; }

    /// <summary>Discogs release ID used for upsert matching.</summary>
    public int DiscogsId { get; set; }

    /// <summary>Primary format description (e.g. "Vinyl", "CD").</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; set; }

    /// <summary>Primary key (application-generated GUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Record label; null when not populated by sync.</summary>
    public string? Label { get; set; }

    /// <summary>UTC timestamp of the last sync that touched this record.</summary>
    public DateTimeOffset LastSyncedAt { get; set; }

    /// <summary>Personal notes from Discogs; null when not populated by sync.</summary>
    public string? Notes { get; set; }

    /// <summary>Comma-separated list of styles; null when not populated by sync.</summary>
    public string? Styles { get; set; }

    /// <summary>Release title as returned by Discogs.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Release year; null when Discogs does not provide one.</summary>
    public int? Year { get; set; }
}
