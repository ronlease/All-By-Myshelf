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
            Slug: b.Slug,
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

    /// <inheritdoc/>
    public async Task<BookDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await booksRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
            return null;

        return new BookDetailDto
        {
            Author = book.Author,
            CoverImageUrl = book.CoverImageUrl,
            Genre = book.Genre,
            HardcoverId = book.HardcoverId,
            Id = book.Id,
            Slug = book.Slug,
            Title = book.Title,
            Year = book.Year
        };
    }

    /// <inheritdoc/>
    public async Task<BookDto?> GetRandomAsync(CancellationToken cancellationToken)
    {
        var book = await booksRepository.GetRandomAsync(cancellationToken);
        if (book is null)
            return null;

        return new BookDto(
            Author: book.Author,
            CoverImageUrl: book.CoverImageUrl,
            Genre: book.Genre,
            HardcoverId: book.HardcoverId,
            Id: book.Id,
            Slug: book.Slug,
            Title: book.Title,
            Year: book.Year
        );
    }
}
