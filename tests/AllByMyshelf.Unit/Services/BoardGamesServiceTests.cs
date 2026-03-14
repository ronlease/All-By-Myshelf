// Feature: BoardGameGeek service - business logic for board games (ABM-056)
//
// Scenario: GetByIdAsync returns mapped BoardGameDetailDto when board game exists
//   Given the repository returns a board game entity for the given ID
//   When GetByIdAsync is called
//   Then the returned BoardGameDetailDto maps all fields from the board game entity
//
// Scenario: GetByIdAsync returns null when board game does not exist
//   Given the repository returns null for the given ID
//   When GetByIdAsync is called
//   Then null is returned
//
// Scenario: GetRandomAsync returns a board game when repository has board games
//   Given the repository returns a board game
//   When GetRandomAsync is called
//   Then the returned dto maps all fields from the board game entity
//
// Scenario: GetRandomAsync returns null when repository returns null
//   Given the repository returns null (empty database)
//   When GetRandomAsync is called
//   Then null is returned

using AllByMyshelf.Api.Features.Bgg;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BoardGamesServiceTests
{
    private readonly Mock<IBoardGamesRepository> _repositoryMock;
    private readonly BoardGamesService _sut;

    public BoardGamesServiceTests()
    {
        _repositoryMock = new Mock<IBoardGamesRepository>(MockBehavior.Strict);
        _sut = new BoardGamesService(_repositoryMock.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGame MakeBoardGame(
        int bggId,
        string title,
        string? designer = null,
        string? genre = null,
        int? yearPublished = null,
        string? coverImageUrl = null,
        string? description = null,
        int? minPlayers = null,
        int? maxPlayers = null,
        int? minPlaytime = null,
        int? maxPlaytime = null,
        string? thumbnailUrl = null) =>
        new()
        {
            BggId = bggId,
            CoverImageUrl = coverImageUrl,
            Description = description,
            Designer = designer,
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            MaxPlayers = maxPlayers,
            MaxPlaytime = maxPlaytime,
            MinPlayers = minPlayers,
            MinPlaytime = minPlaytime,
            ThumbnailUrl = thumbnailUrl,
            Title = title,
            YearPublished = yearPublished
        };

    // ── GetByIdAsync — existing board game ────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_RepositoryReturnsBoardGame_ReturnsMappedDto()
    {
        // Arrange
        var boardGame = MakeBoardGame(
            bggId: 1,
            title: "Catan",
            designer: "Klaus Teuber",
            genre: "Strategy",
            yearPublished: 1995,
            coverImageUrl: "https://cf.geekdo-images.com/cover.jpg",
            description: "Trade, build, settle",
            minPlayers: 3,
            maxPlayers: 4,
            minPlaytime: 60,
            maxPlaytime: 120,
            thumbnailUrl: "https://cf.geekdo-images.com/thumb.jpg"
        );

        _repositoryMock
            .Setup(r => r.GetByIdAsync(boardGame.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boardGame);

        // Act
        var result = await _sut.GetByIdAsync(boardGame.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.BggId.Should().Be(1);
        result.CoverImageUrl.Should().Be("https://cf.geekdo-images.com/cover.jpg");
        result.Description.Should().Be("Trade, build, settle");
        result.Designer.Should().Be("Klaus Teuber");
        result.Genre.Should().Be("Strategy");
        result.Id.Should().Be(boardGame.Id);
        result.MaxPlayers.Should().Be(4);
        result.MaxPlaytime.Should().Be(120);
        result.MinPlayers.Should().Be(3);
        result.MinPlaytime.Should().Be(60);
        result.ThumbnailUrl.Should().Be("https://cf.geekdo-images.com/thumb.jpg");
        result.Title.Should().Be("Catan");
        result.YearPublished.Should().Be(1995);
    }

    // ── GetByIdAsync — non-existent board game ────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardGame?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetRandomAsync — with board game ──────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_RepositoryReturnsBoardGame_ReturnsMappedDto()
    {
        // Arrange
        var boardGame = MakeBoardGame(
            bggId: 1,
            title: "Catan",
            designer: "Klaus Teuber",
            genre: "Strategy",
            yearPublished: 1995,
            thumbnailUrl: "https://cf.geekdo-images.com/thumb.jpg",
            minPlayers: 3,
            maxPlayers: 4
        );

        _repositoryMock
            .Setup(r => r.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(boardGame);

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.BggId.Should().Be(1);
        result.Designer.Should().Be("Klaus Teuber");
        result.Genre.Should().Be("Strategy");
        result.MaxPlayers.Should().Be(4);
        result.MinPlayers.Should().Be(3);
        result.ThumbnailUrl.Should().Be("https://cf.geekdo-images.com/thumb.jpg");
        result.Title.Should().Be("Catan");
        result.YearPublished.Should().Be(1995);
    }

    // ── GetRandomAsync — empty database ───────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardGame?)null);

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
