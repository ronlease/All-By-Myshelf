// Feature: BGG API Token Authentication (ABM-063)
//
// Scenario: TryStartSync returns TokenNotConfigured when ApiToken is missing
//   Given BggOptions has ApiToken empty
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns TokenNotConfigured when both are missing
//   Given BggOptions has empty ApiToken and Username
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns Started when ApiToken is configured
//   Given BggOptions has ApiToken configured
//   When TryStartSync is called
//   Then SyncStartResult.Started is returned

using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.Bgg;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BoardGamesSyncServiceTests
{
    // ── TryStartSync — missing ApiToken ───────────────────────────────────────

    [Fact]
    public void TryStartSync_ApiTokenEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenNull_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: null);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenWhitespace_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "   ");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — both missing ───────────────────────────────────────────

    [Fact]
    public void TryStartSync_BothApiTokenAndUsernameEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — ApiToken configured ──────────────────────────────────

    [Fact]
    public void TryStartSync_ApiTokenConfigured_ReturnsStarted()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGamesSyncService CreateService(string? apiToken)
    {
        var options = new Mock<IOptions<BggOptions>>();
        options.Setup(o => o.Value).Returns(new BggOptions
        {
            ApiToken = apiToken ?? string.Empty
        });

        // Create a minimal service provider with no actual services registered.
        // TryStartSync does not execute the sync loop, so we don't need a real scope factory.
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new BoardGamesSyncService(
            options.Object,
            scopeFactory,
            NullLogger<BoardGamesSyncService>.Instance);
    }
}
