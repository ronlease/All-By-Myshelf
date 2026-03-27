using AllByMyshelf.Api.Infrastructure;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// EF Core implementation of <see cref="IReleasesRepository"/>.
/// </summary>
public class ReleasesRepository(AllByMyshelfDbContext db) : IReleasesRepository
{
    /// <inheritdoc/>
    public async Task<Dictionary<int, Release>> GetAllByDiscogsIdAsync(CancellationToken cancellationToken)
    {
        return await db.Releases
            .Include(r => r.Tracks)
            .AsNoTracking()
            .ToDictionaryAsync(r => r.DiscogsId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Releases
            .Include(r => r.Tracks)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(List<string> Artists, string Title, List<Release> Releases)>> GetDuplicatesAsync(CancellationToken cancellationToken)
    {
        var allReleases = await db.Releases
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return allReleases
            .GroupBy(r => new { Artists = string.Join("|", r.Artists.OrderBy(a => a)).ToLower(), Title = r.Title.ToLower() })
            .Where(g => g.Count() > 1)
            .OrderBy(g => string.Join(", ", g.First().Artists))
            .ThenBy(g => g.First().Title)
            .Select(g => (
                g.First().Artists,
                g.First().Title,
                g.OrderBy(r => r.Year).ThenBy(r => r.DiscogsId).ToList()
            ))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Release>> GetIncompleteReleasesAsync(CancellationToken cancellationToken)
    {
        var releases = await db.Releases
            .Where(r =>
                r.CoverImageUrl == null || r.CoverImageUrl == "" ||
                r.Genre == null ||
                r.Year == null || r.Year == 0)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return releases
            .OrderBy(r => string.Join(", ", r.Artists))
            .ThenBy(r => r.Title)
            .ToList();
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
                var escapedSearch = InputSanitizer.EscapeLikePattern(filter.Search);
                var term = $"%{escapedSearch}%";
                query = query.Where(r =>
                    r.Artists.Any(a => EF.Functions.ILike(a, term)) ||
                    EF.Functions.ILike(r.Format, term) ||
                    EF.Functions.ILike(r.Title, term) ||
                    (r.Genre != null && EF.Functions.ILike(r.Genre, term)) ||
                    (r.Year != null && EF.Functions.ILike(r.Year.Value.ToString(), term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Artist))
            {
                var escapedArtist = InputSanitizer.EscapeLikePattern(filter.Artist);
                query = query.Where(r => r.Artists.Any(a => EF.Functions.ILike(a, $"%{escapedArtist}%")));
            }

            if (!string.IsNullOrWhiteSpace(filter.Format))
            {
                var escapedFormat = InputSanitizer.EscapeLikePattern(filter.Format);
                query = query.Where(r => EF.Functions.ILike(r.Format, $"%{escapedFormat}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Genre))
            {
                var escapedGenre = InputSanitizer.EscapeLikePattern(filter.Genre);
                query = query.Where(r => r.Genre != null && EF.Functions.ILike(r.Genre, $"%{escapedGenre}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                var escapedTitle = InputSanitizer.EscapeLikePattern(filter.Title);
                query = query.Where(r => EF.Functions.ILike(r.Title, $"%{escapedTitle}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Year))
            {
                var escapedYear = InputSanitizer.EscapeLikePattern(filter.Year);
                query = query.Where(r => r.Year != null && EF.Functions.ILike(r.Year.Value.ToString(), $"%{escapedYear}%"));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Sort by first artist in memory since we can't sort by array in EF
        items = items
            .OrderBy(r => r.Artists.FirstOrDefault() ?? string.Empty)
            .ThenBy(r => r.Title)
            .ToList();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Release>> GetRecentlyAddedAsync(
        int count, int days, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        return await db.Releases
            .Where(r => r.AddedAt != null && r.AddedAt >= cutoff)
            .OrderByDescending(r => r.AddedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Release?> GetRandomAsync(RandomReleaseFilter? filter, CancellationToken cancellationToken)
    {
        IQueryable<Release> query = db.Releases;

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Decade) &&
                filter.Decade.EndsWith('s') &&
                int.TryParse(filter.Decade[..^1], out var decadeStart))
            {
                query = query.Where(r => r.Year >= decadeStart && r.Year < decadeStart + 10);
            }

            if (!string.IsNullOrWhiteSpace(filter.Format))
            {
                var escapedFormat = InputSanitizer.EscapeLikePattern(filter.Format);
                query = query.Where(r => EF.Functions.ILike(r.Format, $"%{escapedFormat}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Genre))
            {
                var escapedGenre = InputSanitizer.EscapeLikePattern(filter.Genre);
                query = query.Where(r => r.Genre != null && EF.Functions.ILike(r.Genre, $"%{escapedGenre}%"));
            }
        }

        var count = await query.CountAsync(cancellationToken);
        if (count == 0) return null;

        var skip = Random.Shared.Next(0, count);
        return await query
            .OrderBy(r => r.Id)
            .Skip(skip)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateNotesAndRatingAsync(Guid id, string? notes, int? rating, CancellationToken cancellationToken)
    {
        var release = await db.Releases.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (release is null)
            return false;

        release.Notes = notes;
        release.Rating = rating;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task UpdateResyncedReleaseAsync(Release release, CancellationToken cancellationToken)
    {
        var existing = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == release.Id, cancellationToken);

        if (existing is null) return;

        existing.DetailSyncedAt = release.DetailSyncedAt;
        existing.Genre = release.Genre;
        existing.HighestPrice = release.HighestPrice;
        existing.LowestPrice = release.LowestPrice;
        existing.MedianPrice = release.MedianPrice;
        existing.TrackArtists = release.TrackArtists;

        // Replace tracks.
        db.Tracks.RemoveRange(existing.Tracks);
        foreach (var track in release.Tracks)
        {
            track.ReleaseId = existing.Id;
            await db.Tracks.AddAsync(track, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpsertCollectionAsync(IEnumerable<Release> releases, CancellationToken cancellationToken)
    {
        // Deduplicate by DiscogsId — keep the last occurrence if the API returns duplicates.
        var incoming = releases
            .GroupBy(r => r.DiscogsId)
            .Select(g => g.Last())
            .ToList();
        var incomingIds = incoming.Select(r => r.DiscogsId).ToHashSet();

        // Load all existing records (with tracks) in one query.
        var existing = await db.Releases
            .Include(r => r.Tracks)
            .ToDictionaryAsync(r => r.DiscogsId, cancellationToken);

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
                existingRelease.Artists = release.Artists;
                existingRelease.CoverImageUrl = release.CoverImageUrl;
                existingRelease.DetailSyncedAt = release.DetailSyncedAt;
                existingRelease.Format = release.Format;
                existingRelease.Genre = release.Genre;
                existingRelease.HighestPrice = release.HighestPrice;
                existingRelease.LastSyncedAt = release.LastSyncedAt;
                existingRelease.LowestPrice = release.LowestPrice;
                existingRelease.MedianPrice = release.MedianPrice;
                existingRelease.ThumbnailUrl = release.ThumbnailUrl;
                existingRelease.Title = release.Title;
                existingRelease.TrackArtists = release.TrackArtists;
                existingRelease.Year = release.Year;

                // Replace tracks: remove old, add new.
                if (release.Tracks.Count > 0)
                {
                    db.Tracks.RemoveRange(existingRelease.Tracks);
                    foreach (var track in release.Tracks)
                    {
                        track.ReleaseId = existingRelease.Id;
                        await db.Tracks.AddAsync(track, cancellationToken);
                    }
                }
            }
            else
            {
                release.AddedAt = DateTimeOffset.UtcNow;
                await db.Releases.AddAsync(release, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
