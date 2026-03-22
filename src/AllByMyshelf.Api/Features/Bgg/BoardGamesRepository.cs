using AllByMyshelf.Api.Infrastructure;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// EF Core implementation of <see cref="IBoardGamesRepository"/>.
/// </summary>
public class BoardGamesRepository(AllByMyshelfDbContext db) : IBoardGamesRepository
{
    /// <inheritdoc/>
    public async Task<BoardGame?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.BoardGames
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<BoardGame> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BoardGameFilter? filter = null)
    {
        IQueryable<BoardGame> query = db.BoardGames;

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Designer))
            {
                var escapedDesigner = InputSanitizer.EscapeLikePattern(filter.Designer);
                query = query.Where(b => b.Designers.Any(d => EF.Functions.ILike(d, $"%{escapedDesigner}%")));
            }

            if (!string.IsNullOrWhiteSpace(filter.Genre))
            {
                var escapedGenre = InputSanitizer.EscapeLikePattern(filter.Genre);
                query = query.Where(b => b.Genre != null && EF.Functions.ILike(b.Genre, $"%{escapedGenre}%"));
            }

            if (filter.PlayerCount.HasValue)
                query = query.Where(b => b.MinPlayers <= filter.PlayerCount && b.MaxPlayers >= filter.PlayerCount);

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                var escapedTitle = InputSanitizer.EscapeLikePattern(filter.Title);
                query = query.Where(b => EF.Functions.ILike(b.Title, $"%{escapedTitle}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Year))
            {
                var escapedYear = InputSanitizer.EscapeLikePattern(filter.Year);
                query = query.Where(b => b.YearPublished != null && EF.Functions.ILike(b.YearPublished.Value.ToString(), $"%{escapedYear}%"));
            }
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
    public async Task<BoardGame?> GetRandomAsync(CancellationToken cancellationToken)
    {
        var count = await db.BoardGames.CountAsync(cancellationToken);
        if (count == 0) return null;

        var skip = Random.Shared.Next(0, count);
        return await db.BoardGames
            .OrderBy(b => b.Id)
            .Skip(skip)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpsertCollectionAsync(IEnumerable<BoardGame> boardGames, CancellationToken cancellationToken)
    {
        // Deduplicate by BggId — keep the last occurrence if the API returns duplicates.
        var incoming = boardGames
            .GroupBy(b => b.BggId)
            .Select(g => g.Last())
            .ToList();
        var incomingIds = incoming.Select(b => b.BggId).ToHashSet();

        // Load all existing records in one query.
        var existing = await db.BoardGames.ToDictionaryAsync(b => b.BggId, cancellationToken);

        // Remove records that no longer exist in the BGG collection.
        var toRemove = existing.Values
            .Where(b => !incomingIds.Contains(b.BggId))
            .ToList();
        db.BoardGames.RemoveRange(toRemove);

        // Upsert each incoming record.
        foreach (var boardGame in incoming)
        {
            if (existing.TryGetValue(boardGame.BggId, out var existingBoardGame))
            {
                // Update in-place so EF tracks the change.
                existingBoardGame.CoverImageUrl = boardGame.CoverImageUrl;
                existingBoardGame.Description = boardGame.Description;
                existingBoardGame.Designers = boardGame.Designers;
                existingBoardGame.Genre = boardGame.Genre;
                existingBoardGame.LastSyncedAt = boardGame.LastSyncedAt;
                existingBoardGame.MaxPlayers = boardGame.MaxPlayers;
                existingBoardGame.MaxPlaytime = boardGame.MaxPlaytime;
                existingBoardGame.MinPlayers = boardGame.MinPlayers;
                existingBoardGame.MinPlaytime = boardGame.MinPlaytime;
                existingBoardGame.ThumbnailUrl = boardGame.ThumbnailUrl;
                existingBoardGame.Title = boardGame.Title;
                existingBoardGame.YearPublished = boardGame.YearPublished;
            }
            else
            {
                await db.BoardGames.AddAsync(boardGame, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
