namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a book persisted from a Hardcover collection sync.
/// </summary>
public class Book
{
    /// <summary>Author names as returned by Hardcover.</summary>
    public List<string> Authors { get; set; } = [];

    /// <summary>Cover image URL as returned by Hardcover; null when not provided.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; set; }

    /// <summary>Hardcover book ID used for upsert matching.</summary>
    public int HardcoverId { get; set; }

    /// <summary>Primary key (application-generated GUID).</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp of the last sync that touched this record.</summary>
    public DateTimeOffset LastSyncedAt { get; set; }

    /// <summary>URL slug used for Hardcover book page links.</summary>
    public string? Slug { get; set; }

    /// <summary>Book title as returned by Hardcover.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Publication year extracted from release_date; null when Hardcover does not provide one.</summary>
    public int? Year { get; set; }
}
