namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Abstract base class for all collection entities synced from external APIs.
/// Provides common identity, timestamp, and display properties.
/// </summary>
public abstract class CollectionEntityBase
{
    /// <summary>UTC timestamp of when this record was first created in the local database.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Primary key (application-generated GUID).</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp of the last sync that touched this record.</summary>
    public DateTimeOffset LastSyncedAt { get; set; }

    /// <summary>Display title as returned by the external API.</summary>
    public string Title { get; set; } = string.Empty;
}
