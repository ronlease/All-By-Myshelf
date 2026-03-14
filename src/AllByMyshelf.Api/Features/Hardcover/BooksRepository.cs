using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// EF Core implementation of <see cref="IBooksRepository"/>.
/// </summary>
public class BooksRepository(AllByMyshelfDbContext db) : IBooksRepository
{
    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BookFilter? filter = null)
    {
        IQueryable<Book> query = db.Books;

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Author))
                query = query.Where(b => b.Author != null && EF.Functions.ILike(b.Author, $"%{filter.Author}%"));

            if (!string.IsNullOrWhiteSpace(filter.Genre))
                query = query.Where(b => b.Genre != null && EF.Functions.ILike(b.Genre, $"%{filter.Genre}%"));

            if (!string.IsNullOrWhiteSpace(filter.Title))
                query = query.Where(b => EF.Functions.ILike(b.Title, $"%{filter.Title}%"));

            if (!string.IsNullOrWhiteSpace(filter.Year))
                query = query.Where(b => b.Year != null && EF.Functions.ILike(b.Year.Value.ToString(), $"%{filter.Year}%"));
        }

        query = query.OrderBy(b => b.Title);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Book?> GetRandomAsync(CancellationToken cancellationToken)
    {
        var count = await db.Books.CountAsync(cancellationToken);
        if (count == 0) return null;

        var skip = Random.Shared.Next(0, count);
        return await db.Books
            .OrderBy(b => b.Id)
            .Skip(skip)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpsertCollectionAsync(IEnumerable<Book> books, CancellationToken cancellationToken)
    {
        var incoming = books.ToList();
        var incomingIds = incoming.Select(b => b.HardcoverId).ToHashSet();

        // Load all existing records in one query.
        var existing = await db.Books.ToDictionaryAsync(b => b.HardcoverId, cancellationToken);

        // Remove records that no longer exist in the Hardcover collection.
        var toRemove = existing.Values
            .Where(b => !incomingIds.Contains(b.HardcoverId))
            .ToList();
        db.Books.RemoveRange(toRemove);

        // Upsert each incoming record.
        foreach (var book in incoming)
        {
            if (existing.TryGetValue(book.HardcoverId, out var existingBook))
            {
                // Update in-place so EF tracks the change.
                existingBook.Author = book.Author;
                existingBook.CoverImageUrl = book.CoverImageUrl;
                existingBook.Genre = book.Genre;
                existingBook.LastSyncedAt = book.LastSyncedAt;
                existingBook.Title = book.Title;
                existingBook.Year = book.Year;
            }
            else
            {
                await db.Books.AddAsync(book, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
