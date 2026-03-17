using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Wantlist;

/// <summary>
/// EF Core implementation of <see cref="IWantlistRepository"/>.
/// </summary>
public class WantlistRepository(AllByMyshelfDbContext db) : IWantlistRepository
{
    /// <inheritdoc/>
    public async Task<(IReadOnlyList<WantlistRelease> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.WantlistReleases;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Sort by first artist in memory since we can't sort by array in EF
        items = items
            .OrderBy(w => w.Artists.FirstOrDefault() ?? string.Empty)
            .ThenBy(w => w.Title)
            .ToList();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task RemoveAbsentAsync(IReadOnlySet<int> activeDiscogsIds, CancellationToken cancellationToken)
    {
        var toRemove = await db.WantlistReleases
            .Where(w => !activeDiscogsIds.Contains(w.DiscogsId))
            .ToListAsync(cancellationToken);

        db.WantlistReleases.RemoveRange(toRemove);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(IEnumerable<WantlistRelease> releases, CancellationToken cancellationToken)
    {
        var incoming = releases.ToList();
        var incomingIds = incoming.Select(r => r.DiscogsId).ToHashSet();

        // Load all existing records in one query.
        var existing = await db.WantlistReleases.ToDictionaryAsync(w => w.DiscogsId, cancellationToken);

        // Upsert each incoming record.
        foreach (var release in incoming)
        {
            if (existing.TryGetValue(release.DiscogsId, out var existingRelease))
            {
                // Update in-place so EF tracks the change.
                existingRelease.Artists = release.Artists;
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
                release.AddedAt = DateTimeOffset.UtcNow;
                await db.WantlistReleases.AddAsync(release, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
