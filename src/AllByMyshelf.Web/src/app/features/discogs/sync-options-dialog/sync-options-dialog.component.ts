import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { DiscogsService, SyncEstimateDto, SyncOptionsDto } from '../discogs.service';

@Component({
  selector: 'app-sync-options-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
  ],
  templateUrl: './sync-options-dialog.component.html',
  styleUrl: './sync-options-dialog.component.scss',
})
export class SyncOptionsDialogComponent implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<SyncOptionsDialogComponent>);
  private readonly discogsService = inject(DiscogsService);
  estimate = signal<SyncEstimateDto | null>(null);
  includeDetails = true;
  includePricing = true;
  includeWantlist = true;
  loading = signal(true);
  mode: 'incremental' | 'full' | 'stale' = 'incremental';
  staleDays = 30;

  ngOnInit(): void {
    this.discogsService.getSyncEstimate().subscribe({
      next: (est) => {
        this.estimate.set(est);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  onStart(): void {
    const options: SyncOptionsDto = {
      includeDetails: this.includeDetails,
      includePricing: this.includePricing,
      includeWantlist: this.includeWantlist,
      mode: this.mode,
      staleDays: this.staleDays,
    };
    this.dialogRef.close(options);
  }
}
