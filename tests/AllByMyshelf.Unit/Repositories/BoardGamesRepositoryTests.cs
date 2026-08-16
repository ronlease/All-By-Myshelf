// Feature: Board games repository - upsert and storage (ABM-064)
//
// Scenario: New board games are inserted on first sync
//   Given the local database contains no board games
//   When UpsertCollectionAsync is called with a list of board games
//   Then all board games are saved to the database
//
// Scenario: Existing board games are updated on subsequent sync
//   Given the local database already contains board games from a previous sync
//   When UpsertCollectionAsync is called with updated data for the same BoardGameGeekIds
//   Then the existing records are updated in place with the new values
//   And no duplicate records are created
//
// Scenario: Board games removed from BoardGameGeek are deleted from the database
//   Given the local database contains board games G1 and G2
//   When UpsertCollectionAsync is called with only G1
//   Then G2 is removed from the database
//
// Scenario: Duplicate BoardGameGeekIds in the incoming payload are collapsed
//   Given BoardGameGeek returns the same BoardGameGeekId twice with different titles
//   When UpsertCollectionAsync is called
//   Then only one record is stored
//   And it holds the values from the last occurrence
//
// Scenario: An empty incoming collection clears the local database
//   Given the local database contains board games
//   When UpsertCollectionAsync is called with an empty list
//   Then all board games are removed
//
// Note: Filter tests (designer, genre, player count, title, year) require the
// PostgreSQL ILike function and are covered by integration tests in
// BoardGamesEndpointTests.cs

using AllByMyshelf.Api.Features.BoardGameGeek;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Unit.Repositories;

public class BoardGamesRepositoryTests : IDisposable
{
    private readonly AllByMyshelfDbContext _db;
    private readonly BoardGamesRepository _sut;

    public BoardGamesRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AllByMyshelfDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AllByMyshelfDbContext(options);
        _sut = new BoardGamesRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── UpsertCollectionAsync — insert ────────────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_EmptyDatabase_InsertsAllBoardGames()
    {
        // Arrange
        var incoming = new[]
        {
            CreateBoardGame(boardGameGeekId: 1, title: "Gloomhaven"),
            CreateBoardGame(boardGameGeekId: 2, title: "Wingspan")
        };

        // Act
        await _sut.UpsertCollectionAsync(incoming, CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.OrderBy(b => b.BoardGameGeekId).ToListAsync();
        stored.Should().HaveCount(2);
        stored.Select(b => b.Title).Should().Equal("Gloomhaven", "Wingspan");
    }

    [Fact]
    public async Task UpsertCollectionAsync_NewBoardGame_PersistsAllMappedFields()
    {
        // Arrange
        var incoming = new[]
        {
            CreateBoardGame(
                boardGameGeekId: 42,
                title: "Brass: Birmingham",
                designers: ["Gavan Brown", "Matt Tolman"],
                genre: "Economic")
        };

        // Act
        await _sut.UpsertCollectionAsync(incoming, CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.SingleAsync();
        stored.BoardGameGeekId.Should().Be(42);
        stored.CoverImageUrl.Should().Be("https://example.test/cover-42.jpg");
        stored.Description.Should().Be("Description for 42.");
        stored.Designers.Should().Equal("Gavan Brown", "Matt Tolman");
        stored.Genre.Should().Be("Economic");
        stored.MaxPlayers.Should().Be(4);
        stored.MaxPlaytime.Should().Be(120);
        stored.MinPlayers.Should().Be(2);
        stored.MinPlaytime.Should().Be(60);
        stored.ThumbnailUrl.Should().Be("https://example.test/thumb-42.jpg");
        stored.YearPublished.Should().Be(2018);
    }

    // ── UpsertCollectionAsync — update ────────────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_ExistingBoardGameGeekId_UpdatesInPlace()
    {
        // Arrange — seed a previous sync
        var original = CreateBoardGame(boardGameGeekId: 7, title: "Old Title", genre: "Old Genre");
        _db.BoardGames.Add(original);
        await _db.SaveChangesAsync();

        var updated = CreateBoardGame(
            boardGameGeekId: 7,
            title: "New Title",
            designers: ["Uwe Rosenberg"],
            genre: "New Genre");

        // Act
        await _sut.UpsertCollectionAsync([updated], CancellationToken.None);

        // Assert — one record, updated values
        var stored = await _db.BoardGames.SingleAsync();
        stored.Title.Should().Be("New Title");
        stored.Genre.Should().Be("New Genre");
        stored.Designers.Should().Equal("Uwe Rosenberg");
    }

    [Fact]
    public async Task UpsertCollectionAsync_ExistingBoardGameGeekId_KeepsOriginalRowIdentity()
    {
        // Arrange — the update must not replace the row, so the original Id survives
        var original = CreateBoardGame(boardGameGeekId: 7, title: "Old Title");
        _db.BoardGames.Add(original);
        await _db.SaveChangesAsync();
        var originalId = original.Id;

        // Act
        await _sut.UpsertCollectionAsync(
            [CreateBoardGame(boardGameGeekId: 7, title: "New Title")],
            CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.SingleAsync();
        stored.Id.Should().Be(originalId);
    }

    // ── UpsertCollectionAsync — remove ────────────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_BoardGameNoLongerInCollection_IsRemoved()
    {
        // Arrange — two games stored, only one still owned
        _db.BoardGames.AddRange(
            CreateBoardGame(boardGameGeekId: 1, title: "Kept"),
            CreateBoardGame(boardGameGeekId: 2, title: "Sold"));
        await _db.SaveChangesAsync();

        // Act
        await _sut.UpsertCollectionAsync(
            [CreateBoardGame(boardGameGeekId: 1, title: "Kept")],
            CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].BoardGameGeekId.Should().Be(1);
    }

    [Fact]
    public async Task UpsertCollectionAsync_EmptyIncomingCollection_RemovesEverything()
    {
        // Arrange
        _db.BoardGames.AddRange(
            CreateBoardGame(boardGameGeekId: 1, title: "One"),
            CreateBoardGame(boardGameGeekId: 2, title: "Two"));
        await _db.SaveChangesAsync();

        // Act
        await _sut.UpsertCollectionAsync([], CancellationToken.None);

        // Assert
        (await _db.BoardGames.CountAsync()).Should().Be(0);
    }

    // ── UpsertCollectionAsync — deduplication ─────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_DuplicateBoardGameGeekIds_StoresLastOccurrenceOnly()
    {
        // Arrange — BoardGameGeek occasionally returns the same game twice
        var incoming = new[]
        {
            CreateBoardGame(boardGameGeekId: 9, title: "First Copy"),
            CreateBoardGame(boardGameGeekId: 9, title: "Second Copy")
        };

        // Act
        await _sut.UpsertCollectionAsync(incoming, CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.SingleAsync();
        stored.Title.Should().Be("Second Copy");
    }

    // ── UpsertCollectionAsync — mixed insert, update and delete ───────────────

    [Fact]
    public async Task UpsertCollectionAsync_MixedChanges_AppliesInsertUpdateAndDeleteTogether()
    {
        // Arrange — 1 stays (updated), 2 is removed, 3 is new
        _db.BoardGames.AddRange(
            CreateBoardGame(boardGameGeekId: 1, title: "Stays"),
            CreateBoardGame(boardGameGeekId: 2, title: "Goes"));
        await _db.SaveChangesAsync();

        var incoming = new[]
        {
            CreateBoardGame(boardGameGeekId: 1, title: "Stays Updated"),
            CreateBoardGame(boardGameGeekId: 3, title: "Brand New")
        };

        // Act
        await _sut.UpsertCollectionAsync(incoming, CancellationToken.None);

        // Assert
        var stored = await _db.BoardGames.OrderBy(b => b.BoardGameGeekId).ToListAsync();
        stored.Should().HaveCount(2);
        stored.Select(b => b.BoardGameGeekId).Should().Equal(1, 3);
        stored[0].Title.Should().Be("Stays Updated");
        stored[1].Title.Should().Be("Brand New");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGame CreateBoardGame(
        int boardGameGeekId,
        string title,
        List<string>? designers = null,
        string? genre = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new BoardGame
        {
            BoardGameGeekId = boardGameGeekId,
            CoverImageUrl = $"https://example.test/cover-{boardGameGeekId}.jpg",
            CreatedAt = now,
            Description = $"Description for {boardGameGeekId}.",
            Designers = designers ?? [],
            Genre = genre,
            Id = Guid.NewGuid(),
            LastSyncedAt = now,
            MaxPlayers = 4,
            MaxPlaytime = 120,
            MinPlayers = 2,
            MinPlaytime = 60,
            ThumbnailUrl = $"https://example.test/thumb-{boardGameGeekId}.jpg",
            Title = title,
            YearPublished = 2018
        };
    }
}
