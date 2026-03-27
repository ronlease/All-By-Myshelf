import { inject, Injectable, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { BggService } from '../../features/bgg/bgg.service';
import { DiscogsService, SyncOptionsDto, SyncProgressDto } from '../../features/discogs/discogs.service';
import { HardcoverService } from '../../features/hardcover/hardcover.service';

@Injectable({ providedIn: 'root' })
export class SyncStateService {
  private readonly bggService = inject(BggService);
  readonly booksSyncCompleted$ = new Subject<void>();
  booksSyncing = signal(false);
  private booksSyncTimer?: ReturnType<typeof setTimeout>;
  private readonly dialog = inject(MatDialog);
  private readonly discogsService = inject(DiscogsService);
  readonly discogsSyncCompleted$ = new Subject<void>();
  discogsSyncProgress = signal<SyncProgressDto | null>(null);
  discogsSyncing = signal(false);
  private discogsSyncTimer?: ReturnType<typeof setTimeout>;
  readonly gamesSyncCompleted$ = new Subject<void>();
  gamesSyncing = signal(false);
  private gamesSyncTimer?: ReturnType<typeof setTimeout>;
  private readonly hardcoverService = inject(HardcoverService);
  private pauseStartTime = 0;
  private pauseTotalSeconds = 0;
  private readonly snackBar = inject(MatSnackBar);

  get discogsSyncStatusLabel(): string {
    const p = this.discogsSyncProgress();
    if (!p) return 'Syncing...';
    switch (p.status) {
      case 'pausing': {
        const elapsed = Math.floor((Date.now() - this.pauseStartTime) / 1000);
        const remaining = Math.max(0, this.pauseTotalSeconds - Math.floor(elapsed / 5) * 5);
        return `Pausing ${remaining}s (rate limit)`;
      }
      case 'resuming':
        return 'Resuming…';
      case 'saving':
        return 'Saving…';
      case 'saving wantlist':
        return 'Saving wantlist…';
      case 'syncing wantlist':
        return 'Syncing wantlist…';
      default: {
        const phaseLabel = p.phase === 'details' ? ' (details)' :
                           p.phase === 'pricing' ? ' (pricing)' :
                           p.phase === 'wantlist' ? ' (wantlist)' : '';
        return p.total > 0
          ? `Syncing ${p.current} of ${p.total}${phaseLabel}…`
          : 'Syncing…';
      }
    }
  }

  private pollBoardGamesSyncStatus(): void {
    const poll = () => {
      this.bggService.getSyncStatus().subscribe({
        next: (status) => {
          if (status.isRunning) {
            this.gamesSyncTimer = setTimeout(poll, 2000);
          } else {
            this.gamesSyncing.set(false);
            this.snackBar.open('Board game sync completed.', 'Dismiss', { duration: 3000 });
            this.gamesSyncCompleted$.next();
          }
        },
        error: () => {
          this.gamesSyncTimer = setTimeout(poll, 3000);
        },
      });
    };
    poll();
  }

  private pollBooksSyncStatus(): void {
    const poll = () => {
      this.hardcoverService.getSyncStatus().subscribe({
        next: (status) => {
          if (status.isRunning) {
            this.booksSyncTimer = setTimeout(poll, 2000);
          } else {
            this.booksSyncing.set(false);
            this.snackBar.open('Book sync completed.', 'Dismiss', { duration: 3000 });
            this.booksSyncCompleted$.next();
          }
        },
        error: () => {
          this.booksSyncTimer = setTimeout(poll, 3000);
        },
      });
    };
    poll();
  }

  private pollDiscogsSyncStatus(): void {
    const poll = () => {
      this.discogsService.getSyncStatus().subscribe({
        next: (progress) => {
          if (progress.status === 'pausing' && this.discogsSyncProgress()?.status !== 'pausing') {
            this.pauseStartTime = Date.now();
            this.pauseTotalSeconds = progress.retryAfterSeconds ?? 60;
          }
          this.discogsSyncProgress.set(progress);
          if (progress.isRunning) {
            this.discogsSyncTimer = setTimeout(poll, 1000);
          } else {
            this.discogsSyncing.set(false);
            this.discogsSyncProgress.set(null);
            this.snackBar.open('Collection sync completed.', 'Dismiss', { duration: 3000 });
            this.discogsSyncCompleted$.next();
          }
        },
        error: () => {
          this.discogsSyncTimer = setTimeout(poll, 2000);
        },
      });
    };
    poll();
  }

  startBoardGamesSync(): void {
    if (this.gamesSyncing()) return;
    this.gamesSyncing.set(true);
    this.bggService.triggerSync().subscribe({
      next: (response) => {
        if (response.status === 202) {
          this.snackBar.open('Board game sync started.', 'Dismiss', { duration: 3000 });
          this.pollBoardGamesSyncStatus();
        } else {
          this.gamesSyncing.set(false);
        }
      },
      error: (err) => {
        this.gamesSyncing.set(false);
        if (err.status === 409) {
          this.snackBar.open('A board game sync is already in progress.', 'Dismiss', { duration: 5000 });
        } else if (err.status === 503) {
          this.snackBar.open('BGG username is not configured.', 'Dismiss', { duration: 5000 });
        } else {
          this.snackBar.open('An unexpected error occurred.', 'Dismiss', { duration: 5000 });
        }
      },
    });
  }

  startBooksSync(): void {
    if (this.booksSyncing()) return;
    this.booksSyncing.set(true);
    this.hardcoverService.triggerSync().subscribe({
      next: (response) => {
        if (response.status === 202) {
          this.snackBar.open('Book sync started.', 'Dismiss', { duration: 3000 });
          this.pollBooksSyncStatus();
        } else {
          this.booksSyncing.set(false);
        }
      },
      error: (err) => {
        this.booksSyncing.set(false);
        if (err.status === 409) {
          this.snackBar.open('A book sync is already in progress.', 'Dismiss', { duration: 5000 });
        } else if (err.status === 503) {
          this.snackBar.open('Hardcover API key is not configured.', 'Dismiss', { duration: 5000 });
        } else {
          this.snackBar.open('An unexpected error occurred.', 'Dismiss', { duration: 5000 });
        }
      },
    });
  }

  async startDiscogsSync(): Promise<void> {
    if (this.discogsSyncing()) return;

    const { SyncOptionsDialogComponent } = await import(
      '../../features/discogs/sync-options-dialog/sync-options-dialog.component'
    );

    const dialogRef = this.dialog.open(SyncOptionsDialogComponent, {
      width: '420px',
    });

    dialogRef.afterClosed().subscribe((options: SyncOptionsDto | null) => {
      if (!options) return;

      this.discogsSyncing.set(true);
      this.discogsService.triggerSync(options).subscribe({
        next: (response) => {
          if (response.status === 202) {
            this.snackBar.open('Sync started.', 'Dismiss', { duration: 3000 });
            this.pollDiscogsSyncStatus();
          } else {
            this.discogsSyncing.set(false);
          }
        },
        error: (err) => {
          this.discogsSyncing.set(false);
          if (err.status === 409) {
            this.snackBar.open('A sync is already in progress.', 'Dismiss', { duration: 5000 });
          } else if (err.status === 503) {
            this.snackBar.open('Discogs token is not configured.', 'Dismiss', { duration: 5000 });
          } else {
            this.snackBar.open('An unexpected error occurred.', 'Dismiss', { duration: 5000 });
          }
        },
      });
    });
  }
}
