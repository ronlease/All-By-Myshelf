// Feature: SyncServiceBase background sync orchestration
//
// Scenario: Successful sync sets and clears the running flag
//   Given a sync service with a valid token
//   When TryStartSync is called and the sync completes
//   Then IsSyncRunning becomes true during sync and false after
//
// Scenario: TryStartSync returns AlreadyRunning when sync is in progress
//   Given a sync is already running
//   When TryStartSync is called a second time
//   Then SyncStartResult.AlreadyRunning is returned
//
// Scenario: Failed sync clears the running flag
//   Given RunSyncAsync throws an exception
//   When the sync loop processes the error
//   Then IsSyncRunning is reset to false
//
// Scenario: OnSyncCompleted is called after each sync
//   Given a sync service that overrides OnSyncCompleted
//   When a sync completes
//   Then OnSyncCompleted is invoked

using AllByMyshelf.Api.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AllByMyshelf.Unit.Services;

public class SyncServiceBaseTests
{
    // ── Successful sync ──────────────────────────────────────────────────────

    [Fact]
    public async Task TryStartSync_SuccessfulSync_ClearsRunningFlagAfterCompletion()
    {
        // Arrange
        var sut = new TestSyncService(tokenConfigured: true);
        await sut.StartAsync(CancellationToken.None);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);

        // Wait for the sync to complete
        await sut.WaitForSyncCompletionAsync();
        sut.IsSyncRunning.Should().BeFalse();
        sut.RunSyncCallCount.Should().Be(1);
    }

    // ── Already running ──────────────────────────────────────────────────────

    [Fact]
    public async Task TryStartSync_AlreadyRunning_ReturnsAlreadyRunning()
    {
        // Arrange — create a service that blocks during sync
        var sut = new TestSyncService(tokenConfigured: true, blockDuringSync: true);
        await sut.StartAsync(CancellationToken.None);

        var firstResult = sut.TryStartSync();
        firstResult.Should().Be(SyncStartResult.Started);

        // Wait until the sync is actually running
        await sut.WaitForSyncStartedAsync();

        // Act
        var secondResult = sut.TryStartSync();

        // Assert
        secondResult.Should().Be(SyncStartResult.AlreadyRunning);

        // Cleanup — unblock and stop
        sut.UnblockSync();
        await sut.WaitForSyncCompletionAsync();
    }

    // ── Failed sync clears flag ──────────────────────────────────────────────

    [Fact]
    public async Task TryStartSync_SyncThrows_ClearsRunningFlagAfterError()
    {
        // Arrange
        var sut = new TestSyncService(tokenConfigured: true, throwOnSync: true);
        await sut.StartAsync(CancellationToken.None);

        // Act
        var result = sut.TryStartSync();
        result.Should().Be(SyncStartResult.Started);

        await sut.WaitForSyncCompletionAsync();

        // Assert
        sut.IsSyncRunning.Should().BeFalse();
    }

    // ── OnSyncCompleted ──────────────────────────────────────────────────────

    [Fact]
    public async Task TryStartSync_OnSyncCompleted_IsCalledAfterSync()
    {
        // Arrange
        var sut = new TestSyncService(tokenConfigured: true);
        await sut.StartAsync(CancellationToken.None);

        // Act
        sut.TryStartSync();
        await sut.WaitForSyncCompletionAsync();

        // Assert
        sut.OnSyncCompletedCallCount.Should().Be(1);
    }

    // ── IsSyncRunning during sync ────────────────────────────────────────────

    [Fact]
    public async Task IsSyncRunning_TrueDuringSync()
    {
        // Arrange
        var sut = new TestSyncService(tokenConfigured: true, blockDuringSync: true);
        await sut.StartAsync(CancellationToken.None);

        // Act
        sut.TryStartSync();
        await sut.WaitForSyncStartedAsync();

        // Assert
        sut.IsSyncRunning.Should().BeTrue();

        // Cleanup
        sut.UnblockSync();
        await sut.WaitForSyncCompletionAsync();
    }

    // ── Test double ──────────────────────────────────────────────────────────

    private sealed class TestSyncService(
        bool tokenConfigured,
        bool blockDuringSync = false,
        bool throwOnSync = false) : SyncServiceBase
    {
        private readonly SemaphoreSlim _blockSignal = new(0, 1);
        private readonly SemaphoreSlim _completionSignal = new(0, 1);
        private readonly SemaphoreSlim _startedSignal = new(0, 1);

        public int OnSyncCompletedCallCount { get; private set; }
        public int RunSyncCallCount { get; private set; }

        protected override bool IsTokenConfigured => tokenConfigured;
        protected override ILogger Logger => NullLogger.Instance;
        protected override string LogName => "Test";

        protected override void OnSyncCompleted()
        {
            OnSyncCompletedCallCount++;
            _completionSignal.Release();
        }

        protected override async Task RunSyncAsync(CancellationToken cancellationToken)
        {
            RunSyncCallCount++;
            _startedSignal.Release();

            if (throwOnSync)
                throw new InvalidOperationException("Test sync failure");

            if (blockDuringSync)
                await _blockSignal.WaitAsync(cancellationToken);
        }

        public void UnblockSync() => _blockSignal.Release();
        public Task WaitForSyncCompletionAsync() => _completionSignal.WaitAsync(TimeSpan.FromSeconds(5));
        public Task WaitForSyncStartedAsync() => _startedSignal.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
