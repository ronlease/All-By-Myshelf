using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Data access contract for board games.
/// </summary>
public interface IBoardGamesRepository
{
    /// <summary>
    /// Returns a single board game by its application-generated ID.
    /// </summary>
    /// <param name="id">Application-generated GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The board game if found, otherwise null.</returns>
    Task<BoardGame?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a paginated list of board games with optional filtering.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <returns>A tuple containing the list of board games and the total count.</returns>
    Task<(IReadOnlyList<BoardGame> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BoardGameFilter? filter = null);

    /// <summary>
    /// Returns a single randomly selected board game. Returns null if no board games exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BoardGame?> GetRandomAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inserts or updates the provided board games, removing any board games not present in the incoming collection.
    /// </summary>
    /// <param name="boardGames">Board games to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertCollectionAsync(IEnumerable<BoardGame> boardGames, CancellationToken cancellationToken);
}
