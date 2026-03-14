import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { WantlistService, WantlistReleaseDto } from '../../../core/discogs/wantlist.service';
import { FormatIconPipe } from '../format-icon.pipe';

@Component({
  selector: 'app-wantlist',
  standalone: true,
  imports: [
    CommonModule,
    FormatIconPipe,
    MatCardModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './wantlist.component.html',
  styleUrl: './wantlist.component.scss',
})
export class WantlistComponent implements OnInit {
  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'artist', 'title', 'year', 'format', 'genre'];
  loading = signal(true);
  readonly pageSize = 25;
  releases = signal<WantlistReleaseDto[]>([]);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  totalCount = signal(0);
  private readonly wantlistService = inject(WantlistService);

  ngOnInit(): void {
    this.loadWantlist();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.loadWantlist();
  }

  onRowClick(release: WantlistReleaseDto): void {
    this.router.navigate(['/releases', release.id]);
  }

  private loadWantlist(): void {
    this.loading.set(true);
    this.wantlistService.getWantlist(this.currentPage(), this.pageSize).subscribe({
      next: (result) => {
        this.releases.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load wantlist.', 'Dismiss', { duration: 5000 });
      },
    });
  }
}
