// Feature: Books repository - data mapping and storage (ABM-030)
//
// Scenario: Book entity has author from first contribution
//   Given a Book entity is constructed with author "Neil Gaiman"
//   When the entity is examined
//   Then the Author property is "Neil Gaiman"
//
// Scenario: Book entity has year extracted from release_date
//   Given a Book entity is constructed with year 2020
//   When the entity is examined
//   Then the Year property is 2020
//
// Scenario: Book entity has genre from first cached_tag
//   Given a Book entity is constructed with genre "Science Fiction"
//   When the entity is examined
//   Then the Genre property is "Science Fiction"
//
// Scenario: Book entity has coverImageUrl from image.url
//   Given a Book entity is constructed with coverImageUrl "https://i.hardcover.com/cover.jpg"
//   When the entity is examined
//   Then the CoverImageUrl property is "https://i.hardcover.com/cover.jpg"
//
// Scenario: New books are inserted on first sync
//   Given the local database contains no books
//   When UpsertCollectionAsync is called with a list of books
//   Then all books are saved to the database
//
// Scenario: Existing books are updated on subsequent sync
//   Given the local database already contains books from a previous sync
//   When UpsertCollectionAsync is called with updated data for the same HardcoverIds
//   Then the existing records are updated with the new values
//
// Scenario: Books removed from Hardcover are deleted from the database
//   Given the local database contains books B1 and B2
//   When UpsertCollectionAsync is called with only B1
//   Then B2 is removed from the database
//
// Scenario: Retrieve the first page of books
//   Given the database contains books
//   When GetPagedAsync is called with page=1, pageSize=N
//   Then up to N books are returned ordered by title
//
// Note: Filter tests (author, genre, title, year) require PostgreSQL ILike function
// and are covered by integration tests in BooksEndpointTests.cs

using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Unit.Repositories;

public class BooksRepositoryTests : IDisposable
{
    private readonly AllByMyshelfDbContext _db;
    private readonly BooksRepository _sut;

    public BooksRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AllByMyshelfDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AllByMyshelfDbContext(options);
        _sut = new BooksRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Book MakeBook(
        int hardcoverId,
        string author,
        string title,
        int? year = 2000,
        string? genre = "Fiction",
        string? coverImageUrl = null) =>
        new()
        {
            Author = author,
            CoverImageUrl = coverImageUrl,
            Genre = genre,
            HardcoverId = hardcoverId,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title,
            Year = year
        };

    // ── Entity mapping — author ───────────────────────────────────────────────

    [Fact]
    public void BookEntity_WithAuthor_AuthorPropertyIsSet()
    {
        // Arrange & Act
        var book = MakeBook(1, "Neil Gaiman", "American Gods");

        // Assert
        book.Author.Should().Be("Neil Gaiman");
    }

    // ── Entity mapping — coverImageUrl ────────────────────────────────────────

    [Fact]
    public void BookEntity_WithCoverImageUrl_CoverImageUrlPropertyIsSet()
    {
        // Arrange & Act
        var book = MakeBook(
            hardcoverId: 4,
            author: "Author",
            title: "Book",
            coverImageUrl: "https://i.hardcover.com/cover.jpg");

        // Assert
        book.CoverImageUrl.Should().Be("https://i.hardcover.com/cover.jpg");
    }

    // ── Entity mapping — genre ────────────────────────────────────────────────

    [Fact]
    public void BookEntity_WithGenre_GenrePropertyIsSet()
    {
        // Arrange & Act
        var book = MakeBook(3, "Author", "Book", genre: "Science Fiction");

        // Assert
        book.Genre.Should().Be("Science Fiction");
    }

    // ── Entity mapping — year ─────────────────────────────────────────────────

    [Fact]
    public void BookEntity_WithYear_YearPropertyIsSet()
    {
        // Arrange & Act
        var book = MakeBook(2, "Author", "Book", year: 2020);

        // Assert
        book.Year.Should().Be(2020);
    }

    // ── GetPagedAsync — empty database ────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_EmptyDatabase_ReturnsEmptyListAndZeroCount()
    {
        // Act
        var result = await _sut.GetPagedAsync(1, 20, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GetPagedAsync — with data ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_WithData_ReturnsBooks()
    {
        // Arrange
        _db.Books.AddRange(
            MakeBook(1, "Author A", "Book A"),
            MakeBook(2, "Author B", "Book B")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 20, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // ── GetRandomAsync — empty database ───────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_EmptyDatabase_ReturnsNull()
    {
        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetRandomAsync — with data ────────────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_WithData_ReturnsBook()
    {
        // Arrange
        _db.Books.AddRange(
            MakeBook(1, "Author A", "Book A"),
            MakeBook(2, "Author B", "Book B"),
            MakeBook(3, "Author C", "Book C")
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.HardcoverId.Should().BeOneOf(1, 2, 3);
    }

    [Fact]
    public async Task GetRandomAsync_SingleBook_ReturnsThatBook()
    {
        // Arrange
        var book = MakeBook(42, "Single Author", "Single Book");
        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.HardcoverId.Should().Be(42);
        result.Title.Should().Be("Single Book");
    }

    // ── GetPagedAsync — with pagination ───────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange — 25 books, alphabetically by title
        var books = Enumerable.Range(1, 25)
            .Select(i => MakeBook(i, $"Author {i}", $"Book {i:D2}"))
            .ToList();
        _db.Books.AddRange(books);
        await _db.SaveChangesAsync();

        // Act — request page 2 with pageSize 10
        var result = await _sut.GetPagedAsync(2, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        // Verify pagination: page 2 should skip the first 10 books
        result.Items.First().Title.Should().Be("Book 11");
    }

    // ── UpsertCollectionAsync — add new books ─────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_NewBooks_AddsToDatabase()
    {
        // Arrange
        var newBooks = new[]
        {
            MakeBook(100, "Author A", "Book A"),
            MakeBook(101, "Author B", "Book B")
        };

        // Act
        await _sut.UpsertCollectionAsync(newBooks, CancellationToken.None);

        // Assert
        _db.Books.Should().HaveCount(2);
        _db.Books.Should().Contain(b => b.HardcoverId == 100);
        _db.Books.Should().Contain(b => b.HardcoverId == 101);
    }

    // ── UpsertCollectionAsync — remove missing books ──────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_RemovesMissingBooks()
    {
        // Arrange — database has books 100, 101
        _db.Books.AddRange(
            MakeBook(100, "Old Author", "Old Book A"),
            MakeBook(101, "Old Author", "Old Book B")
        );
        await _db.SaveChangesAsync();

        // Act — upsert only book 100 (book 101 should be removed)
        var updatedBooks = new[] { MakeBook(100, "New Author", "Updated Book A") };
        await _sut.UpsertCollectionAsync(updatedBooks, CancellationToken.None);

        // Assert
        _db.Books.Should().HaveCount(1);
        _db.Books.Should().Contain(b => b.HardcoverId == 100);
        _db.Books.Should().NotContain(b => b.HardcoverId == 101);
    }

    // ── UpsertCollectionAsync — update and add ────────────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_UpdatesExistingAndAddsNew()
    {
        // Arrange — database has books 100, 101
        _db.Books.AddRange(
            MakeBook(100, "Old Author A", "Old Book A"),
            MakeBook(101, "Old Author B", "Old Book B")
        );
        await _db.SaveChangesAsync();

        // Act — update book 100, add book 102, remove book 101
        var updatedBooks = new[]
        {
            MakeBook(100, "New Author A", "Updated Book A"),
            MakeBook(102, "New Author C", "New Book C")
        };
        await _sut.UpsertCollectionAsync(updatedBooks, CancellationToken.None);

        // Assert
        _db.Books.Should().HaveCount(2);
        _db.Books.Should().Contain(b => b.HardcoverId == 100 && b.Author == "New Author A");
        _db.Books.Should().Contain(b => b.HardcoverId == 102);
        _db.Books.Should().NotContain(b => b.HardcoverId == 101);

        var book100 = _db.Books.Single(b => b.HardcoverId == 100);
        book100.Title.Should().Be("Updated Book A");
    }

    // ── UpsertCollectionAsync — update existing books ─────────────────────────

    [Fact]
    public async Task UpsertCollectionAsync_UpdatesExistingBooks()
    {
        // Arrange — database has book 100
        var existingBook = MakeBook(100, "Old Author", "Old Book");
        _db.Books.Add(existingBook);
        await _db.SaveChangesAsync();

        // Act — update book 100
        var updatedBook = MakeBook(100, "New Author", "New Book");
        await _sut.UpsertCollectionAsync(new[] { updatedBook }, CancellationToken.None);

        // Assert
        _db.Books.Should().HaveCount(1);
        var book = _db.Books.Single();
        book.HardcoverId.Should().Be(100);
        book.Author.Should().Be("New Author");
        book.Title.Should().Be("New Book");
    }
}
