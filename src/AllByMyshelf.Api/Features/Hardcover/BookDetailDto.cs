namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Full detail representation of a single book, returned by the GET /api/v1/books/{id} endpoint.
/// </summary>
public class BookDetailDto
{
    /// <summary>List of author names.</summary>
    public IReadOnlyList<string> Authors { get; init; } = [];

    /// <summary>Cover image URL; null when not available.</summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>Primary genre; null when not available.</summary>
    public string? Genre { get; init; }

    /// <summary>Hardcover book ID.</summary>
    public int HardcoverId { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>URL slug for Hardcover book page links.</summary>
    public string? Slug { get; init; }

    /// <summary>Book title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Publication year; null when not available.</summary>
    public int? Year { get; init; }
}
