using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReleasesRepository"/>.
/// </summary>
public class ReleasesRepository(AllByMyshelfDbContext db) : IReleasesRepository
{
    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Release> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Releases
            .OrderBy(r => r.Artist)
            .ThenBy(r => r.Title);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Releases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
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
                existingRelease.Title = release.Title;
                existingRelease.Year = release.Year;
                existingRelease.Format = release.Format;
                existingRelease.Label = release.Label;
                existingRelease.Country = release.Country;
                existingRelease.Genre = release.Genre;
                existingRelease.Notes = release.Notes;
                existingRelease.Styles = release.Styles;
                existingRelease.LastSyncedAt = release.LastSyncedAt;
            }
            else
            {
                await db.Releases.AddAsync(release, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
