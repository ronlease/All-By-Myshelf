// Feature: Background sync of Discogs collection  (ABM-002)
// Feature: Manual sync trigger endpoint           (ABM-005)
//
// Scenario: Sync is triggered and runs in the background
//   Given the Discogs personal access token is configured
//   And no sync is currently running
//   When I trigger a manual sync
//   Then TryStartSync returns SyncStartResult.Started
//   And IsSyncRunning becomes true
//
// Scenario: Sync is already in progress
//   Given a sync is currently running
//   When I trigger another manual sync
//   Then TryStartSync returns SyncStartResult.AlreadyRunning
//
// Scenario: Attempt to trigger sync with no token configured
//   Given the Discogs personal access token is NOT configured
//   When I call TryStartSync
//   Then TryStartSync returns SyncStartResult.TokenNotConfigured
//
// Scenario: IsSyncRunning reflects idle state before any sync
//   Given the service has just been created
//   Then IsSyncRunning is false

using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class SyncServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SyncService CreateService(
        string token = "test-token",
        string username = "test-user")
    {
        var options = Options.Create(new DiscogsOptions
        {
            PersonalAccessToken = token,
            Username = username
        });

        var scopeFactory = new Mock<IServiceScopeFactory>();
        var logger = NullLogger<SyncService>.Instance;

        return new SyncService(options, scopeFactory.Object, logger);
    }

    // ── IsSyncRunning — initial state ────────────────────────────────────────

    [Fact]
    public void IsSyncRunning_BeforeAnySync_IsFalse()
    {
        // Arrange
        var sut = CreateService();

        // Act / Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── TryStartSync — already running ───────────────────────────────────────

    [Fact]
    public void TryStartSync_AlreadyRunning_DoesNotStartSecondSync()
    {
        // Arrange
        var sut = CreateService();
        sut.TryStartSync();

        // Act
        var secondResult = sut.TryStartSync();

        // Assert — only one sync slot should be occupied
        secondResult.Should().Be(SyncStartResult.AlreadyRunning);
        sut.IsSyncRunning.Should().BeTrue();
    }

    [Fact]
    public void TryStartSync_AlreadyRunning_ReturnsAlreadyRunning()
    {
        // Arrange
        var sut = CreateService();
        sut.TryStartSync(); // first call acquires the lock

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.AlreadyRunning);
    }

    // ── Token check takes precedence over running flag ────────────────────────

    [Fact]
    public void TryStartSync_TokenNotConfigured_DoesNotSetRunningFlag()
    {
        // Arrange
        var sut = CreateService(token: string.Empty);

        // Act
        sut.TryStartSync();

        // Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── TryStartSync — token not configured ──────────────────────────────────

    [Fact]
    public void TryStartSync_TokenNotConfigured_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(token: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_TokenWhitespaceOnly_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(token: "   ");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — happy path ─────────────────────────────────────────────

    [Fact]
    public void TryStartSync_ValidToken_ReturnsStarted()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);
    }

    [Fact]
    public void TryStartSync_ValidToken_SetsSyncRunningTrue()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.TryStartSync();

        // Assert
        sut.IsSyncRunning.Should().BeTrue();
    }
}
