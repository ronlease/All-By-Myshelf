namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Named constants for sync modes, phases, and statuses used by <see cref="SyncService"/>
/// and its DTOs to avoid magic strings.
/// </summary>
public static class SyncConstants
{
    /// <summary>Sync mode values for <see cref="SyncOptionsDto.Mode"/>.</summary>
    public static class Modes
    {
        public const string Full = "full";
        public const string Incremental = "incremental";
        public const string Stale = "stale";
    }

    /// <summary>Sync phase values for <see cref="SyncProgressDto.Phase"/>.</summary>
    public static class Phases
    {
        public const string Collection = "collection";
        public const string Details = "details";
        public const string Pricing = "pricing";
        public const string Saving = "saving";
        public const string Wantlist = "wantlist";
    }

    /// <summary>Sync status values for <see cref="SyncProgressDto.Status"/>.</summary>
    public static class Statuses
    {
        public const string Idle = "idle";
        public const string Pausing = "pausing";
        public const string Resuming = "resuming";
        public const string Saving = "saving";
        public const string SavingWantlist = "saving wantlist";
        public const string Syncing = "syncing";
        public const string SyncingWantlist = "syncing wantlist";
    }
}
