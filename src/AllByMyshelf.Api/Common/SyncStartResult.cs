namespace AllByMyshelf.Api.Common;

/// <summary>
/// Represents the outcome of attempting to start a background sync.
/// </summary>
public enum SyncStartResult
{
    /// <summary>A new sync was successfully enqueued.</summary>
    Started,

    /// <summary>A sync was already in progress; no new sync was started.</summary>
    AlreadyRunning,

    /// <summary>The API token is not configured; sync cannot proceed.</summary>
    TokenNotConfigured
}
