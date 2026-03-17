// Feature: Books service - business logic for books (ABM-032, ABM-036, ABM-049)
//
// Scenario: GetByIdAsync returns mapped BookDetailDto when book exists
//   Given the repository returns a book entity for the given ID
//   When GetByIdAsync is called
//   Then the returned BookDetailDto maps all fields from the book entity
//
// Scenario: GetByIdAsync returns null when book does not exist
//   Given the repository returns null for the given ID
//   When GetByIdAsync is called
//   Then null is returned
//
// Scenario: GetRandomAsync returns a book when repository has books
//   Given the repository returns a book
//   When GetRandomAsync is called
//   Then the returned dto maps all fields from the book entity
//
// Scenario: GetRandomAsync returns null when repository returns null
//   Given the repository returns null (empty database)
//   When GetRandomAsync is called
//   Then null is returned
//
// Scenario: GetRandomAsync maps all book fields correctly
//   Given the repository returns a book with all fields populated
//   When GetRandomAsync is called
//   Then all fields are mapped to the dto

using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BooksServiceTests
{
    private readonly Mock<IBooksRepository> _repositoryMock;
    private readonly BooksService _sut;

    public BooksServiceTests()
    {
        _repositoryMock = new Mock<IBooksRepository>(MockBehavior.Strict);
        _sut = new BooksService(_repositoryMock.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Book MakeBook(int hardcoverId, string? author, string title, int? year = 2000,
        string? genre = "Fiction", string? coverImageUrl = null) =>
        new()
        {
            Authors = author is not null ? [author] : [],
            CoverImageUrl = coverImageUrl,
            Genre = genre,
            HardcoverId = hardcoverId,
            Id = Guid.NewGuid(),
            LastSyncedAt = DateTimeOffset.UtcNow,
            Title = title,
            Year = year
        };

    // ── GetByIdAsync — existing book ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_RepositoryReturnsBook_ReturnsMappedBookDetailDto()
    {
        // Arrange
        var book = MakeBook(1, "Neil Gaiman", "American Gods", 2001, "Fantasy",
            "https://i.hardcover.com/cover.jpg");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _sut.GetByIdAsync(book.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Authors.Should().BeEquivalentTo(new[] { "Neil Gaiman" });
        result.CoverImageUrl.Should().Be("https://i.hardcover.com/cover.jpg");
        result.Genre.Should().Be("Fantasy");
        result.HardcoverId.Should().Be(1);
        result.Id.Should().Be(book.Id);
        result.Title.Should().Be("American Gods");
        result.Year.Should().Be(2001);
    }

    // ── GetByIdAsync — non-existent book ────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetRandomAsync — with book ────────────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_RepositoryReturnsBook_ReturnsMappedBookDto()
    {
        // Arrange
        var book = MakeBook(1, "Neil Gaiman", "American Gods", 2001, "Fantasy",
            "https://i.hardcover.com/cover.jpg");

        _repositoryMock
            .Setup(r => r.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.HardcoverId.Should().Be(1);
        result.Authors.Should().BeEquivalentTo(new[] { "Neil Gaiman" });
        result.Title.Should().Be("American Gods");
        result.Year.Should().Be(2001);
        result.Genre.Should().Be("Fantasy");
        result.CoverImageUrl.Should().Be("https://i.hardcover.com/cover.jpg");
    }

    // ── GetRandomAsync — empty database ───────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ── GetRandomAsync — null fields ──────────────────────────────────────────

    [Fact]
    public async Task GetRandomAsync_BookWithNullFields_MapsNullFieldsCorrectly()
    {
        // Arrange
        var book = MakeBook(2, author: null!, "Book Without Author", year: null, genre: null,
            coverImageUrl: null);

        _repositoryMock
            .Setup(r => r.GetRandomAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _sut.GetRandomAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Authors.Should().BeEmpty();
        result.Year.Should().BeNull();
        result.Genre.Should().BeNull();
        result.CoverImageUrl.Should().BeNull();
    }
}
