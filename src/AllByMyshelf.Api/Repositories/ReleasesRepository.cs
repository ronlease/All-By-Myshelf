using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReleasesRepository"/>.
/// </summary>
public class ReleasesRepository(AllByMyshelfDbContext db) : IReleasesRepository
{
    /// <inheritdoc/>
    public async Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Releases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Release> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken, ReleaseFilter? filter = null)
    {
        IQueryable<Release> query = db.Releases;

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = $"%{filter.Search}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.Artist, term) ||
                    EF.Functions.ILike(r.Format, term) ||
                    EF.Functions.ILike(r.Title, term) ||
                    (r.Genre != null && EF.Functions.ILike(r.Genre, term)) ||
                    (r.Year != null && EF.Functions.ILike(r.Year.Value.ToString(), term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Artist))
                query = query.Where(r => EF.Functions.ILike(r.Artist, $"%{filter.Artist}%"));

            if (!string.IsNullOrWhiteSpace(filter.Format))
                query = query.Where(r => EF.Functions.ILike(r.Format, $"%{filter.Format}%"));

            if (!string.IsNullOrWhiteSpace(filter.Genre))
                query = query.Where(r => r.Genre != null && EF.Functions.ILike(r.Genre, $"%{filter.Genre}%"));

            if (!string.IsNullOrWhiteSpace(filter.Title))
                query = query.Where(r => EF.Functions.ILike(r.Title, $"%{filter.Title}%"));

            if (!string.IsNullOrWhiteSpace(filter.Year))
                query = query.Where(r => r.Year != null && EF.Functions.ILike(r.Year.Value.ToString(), $"%{filter.Year}%"));
        }

        query = query.OrderBy(r => r.Artist).ThenBy(r => r.Title);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task UpsertCollectionAsync(IEnumerable<Release> releases, CancellationToken cancellationToken)
    {
        var incoming = releases.ToList();
        var incomingIds = incoming.Select(r => r.DiscogsId).ToHashSet();

        // Load all existing records in one query.
        var existing = await db.Releases.ToDictionaryAsync(r => r.DiscogsId, cancellationToken);

        // Remove records that no longer exist in the Discogs collection.
        var toRemove = existing.Values
            .Where(r => !incomingIds.Contains(r.DiscogsId))
            .ToList();
        db.Releases.RemoveRange(toRemove);

        // Upsert each incoming record.
        foreach (var release in incoming)
        {
            if (existing.TryGetValue(release.DiscogsId, out var existingRelease))
            {
                // Update in-place so EF tracks the change.
                existingRelease.Artist = release.Artist;
                existingRelease.CoverImageUrl = release.CoverImageUrl;
                existingRelease.Format = release.Format;
                existingRelease.Genre = release.Genre;
                existingRelease.LastSyncedAt = release.LastSyncedAt;
                existingRelease.ThumbnailUrl = release.ThumbnailUrl;
                existingRelease.Title = release.Title;
                existingRelease.Year = release.Year;
            }
            else
            {
                await db.Releases.AddAsync(release, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
