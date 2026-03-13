using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Business logic implementation for books.
/// </summary>
public class BooksService(IBooksRepository booksRepository) : IBooksService
{
    /// <inheritdoc/>
    public async Task<PagedResult<BookDto>> GetBooksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BookFilter? filter = null)
    {
        var (items, totalCount) = await booksRepository.GetPagedAsync(page, pageSize, cancellationToken, filter);

        var dtos = items.Select(b => new BookDto(
            Author: b.Author,
            CoverImageUrl: b.CoverImageUrl,
            Genre: b.Genre,
            HardcoverId: b.HardcoverId,
            Id: b.Id,
            Title: b.Title,
            Year: b.Year
        )).ToList();

        return new PagedResult<BookDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
