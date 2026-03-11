import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { DiscogsService, ReleaseDetailDto } from '../discogs.service';

@Component({
  selector: 'app-release-detail',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
    RouterModule,
  ],
  templateUrl: './release-detail.component.html',
})
export class ReleaseDetailComponent implements OnInit {
  private readonly discogsService = inject(DiscogsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  error = signal(false);
  loading = signal(true);
  release = signal<ReleaseDetailDto | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.discogsService.getRelease(id).subscribe({
      next: (detail) => {
        this.release.set(detail);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }
}
