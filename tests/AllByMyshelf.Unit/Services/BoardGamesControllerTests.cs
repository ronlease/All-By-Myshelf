// Feature: Board games API endpoints (ABM-056)
//
// Scenario: GetBoardGames returns paginated list
//   Given the service returns a paged result of board games
//   When GET /api/v1/boardgames is called
//   Then the controller returns 200 OK with the paged result
//
// Scenario: GetBoardGame returns single board game detail
//   Given a board game exists with the specified ID
//   When GET /api/v1/boardgames/{id} is called
//   Then the controller returns 200 OK with the board game detail
//
// Scenario: GetBoardGame returns 404 when not found
//   Given no board game exists with the specified ID
//   When GET /api/v1/boardgames/{id} is called
//   Then the controller returns 404 Not Found
//
// Scenario: GetRandom returns a random board game
//   Given board games exist in the collection
//   When GET /api/v1/boardgames/random is called
//   Then the controller returns 200 OK with a random board game
//
// Scenario: GetRandom returns 404 when collection is empty
//   Given no board games exist in the collection
//   When GET /api/v1/boardgames/random is called
//   Then the controller returns 404 Not Found
//
// Scenario: TriggerSync returns 202 when sync starts
//   Given the sync service is not running
//   When POST /api/v1/boardgames/sync is called
//   Then the controller returns 202 Accepted
//
// Scenario: TriggerSync returns 409 when sync already running
//   Given a sync is already in progress
//   When POST /api/v1/boardgames/sync is called
//   Then the controller returns 409 Conflict
//
// Scenario: TriggerSync returns 503 when token not configured
//   Given the BGG username is not configured
//   When POST /api/v1/boardgames/sync is called
//   Then the controller returns 503 Service Unavailable

using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Bgg;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BoardGamesControllerTests
{
    private readonly Mock<IBoardGamesService> _boardGamesService = new();
    private readonly Mock<IBoardGamesSyncService> _syncService = new();

    private BoardGamesController CreateController() =>
        new(_boardGamesService.Object, _syncService.Object);

    [Fact]
    public async Task GetBoardGame_Found_ReturnsOkWithDetail()
    {
        var id = Guid.NewGuid();
        var detail = new BoardGameDetailDto { Id = id, Title = "Chess" };
        _boardGamesService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await CreateController().GetBoardGame(id);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(detail);
    }

    [Fact]
    public async Task GetBoardGame_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _boardGamesService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardGameDetailDto?)null);

        var result = await CreateController().GetBoardGame(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetBoardGames_ReturnsOkWithPagedResult()
    {
        var paged = new PagedResult<BoardGameDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 };
        _boardGamesService.Setup(s => s.GetBoardGamesAsync(1, 20, It.IsAny<CancellationToken>(), It.IsAny<BoardGameFilter>()))
            .ReturnsAsync(paged);

        var result = await CreateController().GetBoardGames();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetRandom_Found_ReturnsOkWithBoardGame()
    {
        var dto = new BoardGameDto(0, [], null, Guid.NewGuid(), null, null, null, "Catan", null);
        _boardGamesService.Setup(s => s.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await CreateController().GetRandom();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetRandom_NotFound_Returns404()
    {
        _boardGamesService.Setup(s => s.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardGameDto?)null);

        var result = await CreateController().GetRandom();

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetSyncStatus_ReturnsOk()
    {
        _syncService.Setup(s => s.IsSyncRunning).Returns(false);

        var result = CreateController().GetSyncStatus();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void TriggerSync_AlreadyRunning_Returns409()
    {
        _syncService.Setup(s => s.TryStartSync()).Returns(SyncStartResult.AlreadyRunning);

        var result = CreateController().TriggerSync();

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public void TriggerSync_Started_Returns202()
    {
        _syncService.Setup(s => s.TryStartSync()).Returns(SyncStartResult.Started);

        var result = CreateController().TriggerSync();

        var statusResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(202);
    }

    [Fact]
    public void TriggerSync_TokenNotConfigured_Returns503()
    {
        _syncService.Setup(s => s.TryStartSync()).Returns(SyncStartResult.TokenNotConfigured);

        var result = CreateController().TriggerSync();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }
}
