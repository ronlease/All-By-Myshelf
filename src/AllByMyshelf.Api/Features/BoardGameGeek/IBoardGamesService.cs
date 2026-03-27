using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Business logic contract for board games.
/// </summary>
public interface IBoardGamesService
{
    /// <summary>
    /// Returns the full detail for a single board game by its application-generated ID.
    /// </summary>
    /// <param name="id">Application-generated GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The board game detail if found, otherwise null.</returns>
    Task<BoardGameDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a paginated list of board games with optional filtering.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <returns>A paginated result containing board game DTOs.</returns>
    Task<PagedResult<BoardGameDto>> GetBoardGamesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BoardGameFilter? filter = null);

    /// <summary>
    /// Returns a single randomly selected board game, or null if no board games exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BoardGameDto?> GetRandomAsync(CancellationToken cancellationToken);
}
