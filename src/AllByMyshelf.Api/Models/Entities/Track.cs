namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a single track within a release tracklist, persisted from Discogs.
/// </summary>
public sealed class Track
{
    /// <summary>Artist names for this track; empty for single-artist albums.</summary>
    public List<string> Artists { get; set; } = [];

    /// <summary>Primary key (application-generated GUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Track position as returned by Discogs (e.g. "A1", "1", "B2").</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent release.</summary>
    public Guid ReleaseId { get; set; }

    /// <summary>Track title as returned by Discogs.</summary>
    public string Title { get; set; } = string.Empty;
}
