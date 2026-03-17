namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Public representation of a book for API responses.
/// </summary>
/// <param name="Authors">List of author names.</param>
/// <param name="CoverImageUrl">Cover image URL; null when not available.</param>
/// <param name="Genre">Primary genre; null when not available.</param>
/// <param name="HardcoverId">Hardcover book ID.</param>
/// <param name="Id">Application-generated GUID.</param>
/// <param name="Slug">URL slug for Hardcover book page links; null when not available.</param>
/// <param name="Title">Book title.</param>
/// <param name="Year">Publication year; null when not available.</param>
public record BookDto(
    List<string> Authors,
    string? CoverImageUrl,
    string? Genre,
    int HardcoverId,
    Guid Id,
    string? Slug,
    string Title,
    int? Year);
