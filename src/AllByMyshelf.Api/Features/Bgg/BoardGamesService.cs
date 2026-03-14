using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Business logic implementation for board games.
/// </summary>
public class BoardGamesService(IBoardGamesRepository boardGamesRepository) : IBoardGamesService
{
    /// <inheritdoc/>
    public async Task<BoardGameDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var boardGame = await boardGamesRepository.GetByIdAsync(id, cancellationToken);
        if (boardGame is null)
            return null;

        return new BoardGameDetailDto
        {
            BggId = boardGame.BggId,
            CoverImageUrl = boardGame.CoverImageUrl,
            Description = boardGame.Description,
            Designer = boardGame.Designer,
            Genre = boardGame.Genre,
            Id = boardGame.Id,
            MaxPlayers = boardGame.MaxPlayers,
            MaxPlaytime = boardGame.MaxPlaytime,
            MinPlayers = boardGame.MinPlayers,
            MinPlaytime = boardGame.MinPlaytime,
            ThumbnailUrl = boardGame.ThumbnailUrl,
            Title = boardGame.Title,
            YearPublished = boardGame.YearPublished
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BoardGameDto>> GetBoardGamesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BoardGameFilter? filter = null)
    {
        var (items, totalCount) = await boardGamesRepository.GetPagedAsync(page, pageSize, cancellationToken, filter);

        var dtos = items.Select(b => new BoardGameDto(
            BggId: b.BggId,
            Designer: b.Designer,
            Genre: b.Genre,
            Id: b.Id,
            MaxPlayers: b.MaxPlayers,
            MinPlayers: b.MinPlayers,
            ThumbnailUrl: b.ThumbnailUrl,
            Title: b.Title,
            YearPublished: b.YearPublished
        )).ToList();

        return new PagedResult<BoardGameDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<BoardGameDto?> GetRandomAsync(CancellationToken cancellationToken)
    {
        var boardGame = await boardGamesRepository.GetRandomAsync(cancellationToken);
        if (boardGame is null)
            return null;

        return new BoardGameDto(
            BggId: boardGame.BggId,
            Designer: boardGame.Designer,
            Genre: boardGame.Genre,
            Id: boardGame.Id,
            MaxPlayers: boardGame.MaxPlayers,
            MinPlayers: boardGame.MinPlayers,
            ThumbnailUrl: boardGame.ThumbnailUrl,
            Title: boardGame.Title,
            YearPublished: boardGame.YearPublished
        );
    }
}
