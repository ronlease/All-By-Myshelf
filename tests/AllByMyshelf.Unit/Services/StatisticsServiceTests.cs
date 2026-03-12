// Feature: Statistics service - collection value (ABM-020)
//
// Scenario: Returns collection value from repository
//   Given the repository returns a CollectionValueDto with IncludedCount 5, ExcludedCount 2, TotalValue 127.50
//   When GetCollectionValueAsync is called
//   Then the result matches the repository's return value
//
// Scenario: Passes cancellation token to repository
//   Given a cancellation token
//   When GetCollectionValueAsync is called with the token
//   Then the repository receives the same token

using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Repositories;
using AllByMyshelf.Api.Services;
using FluentAssertions;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class StatisticsServiceTests
{
    private readonly Mock<IStatisticsRepository> _repositoryMock;
    private readonly StatisticsService _sut;

    public StatisticsServiceTests()
    {
        _repositoryMock = new Mock<IStatisticsRepository>(MockBehavior.Strict);
        _sut = new StatisticsService(_repositoryMock.Object);
    }

    // ── GetCollectionValueAsync — delegation to repository ────────────────────

    [Fact]
    public async Task GetCollectionValueAsync_RepositoryReturnsValue_ReturnsMatchingDto()
    {
        // Arrange
        var expectedDto = new CollectionValueDto(
            ExcludedCount: 2,
            IncludedCount: 5,
            TotalValue: 127.50m
        );

        _repositoryMock
            .Setup(r => r.GetCollectionValueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _sut.GetCollectionValueAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedDto);
        result.IncludedCount.Should().Be(5);
        result.ExcludedCount.Should().Be(2);
        result.TotalValue.Should().Be(127.50m);
    }

    // ── GetCollectionValueAsync — cancellation token ──────────────────────────

    [Fact]
    public async Task GetCollectionValueAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(r => r.GetCollectionValueAsync(token))
            .ReturnsAsync(new CollectionValueDto(ExcludedCount: 0, IncludedCount: 0, TotalValue: 0m));

        // Act
        await _sut.GetCollectionValueAsync(token);

        // Assert
        _repositoryMock.Verify(r => r.GetCollectionValueAsync(token), Times.Once);
    }
}
