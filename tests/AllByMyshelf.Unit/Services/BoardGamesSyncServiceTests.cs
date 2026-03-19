// Feature: BGG credential validation (ABM-064, ABM-065)
//
// Scenario: TryStartSync returns TokenNotConfigured when ApiToken is missing
//   Given BggOptions has ApiToken empty
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns TokenNotConfigured when Username is missing
//   Given BggOptions has Username empty but ApiToken configured
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns TokenNotConfigured when both are missing
//   Given BggOptions has empty ApiToken and Username
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns Started when both ApiToken and Username are configured
//   Given BggOptions has both ApiToken and Username configured
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
        var sut = CreateService(apiToken: string.Empty, username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenNull_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: null, username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenWhitespace_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "   ", username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — missing Username ─────────────────────────────────────

    [Fact]
    public void TryStartSync_UsernameEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_UsernameNull_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: null);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_UsernameWhitespace_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: "   ");

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
        var sut = CreateService(apiToken: string.Empty, username: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — both configured ──────────────────────────────────────

    [Fact]
    public void TryStartSync_BothApiTokenAndUsernameConfigured_ReturnsStarted()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGamesSyncService CreateService(string? apiToken, string? username)
    {
        var options = new Mock<IOptions<BggOptions>>();
        options.Setup(o => o.Value).Returns(new BggOptions
        {
            ApiToken = apiToken ?? string.Empty,
            Username = username ?? string.Empty
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
