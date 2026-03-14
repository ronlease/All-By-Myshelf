import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BggService, BoardGameDetailDto } from '../bgg.service';

@Component({
  selector: 'app-board-game-detail',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    RouterModule,
  ],
  templateUrl: './board-game-detail.component.html',
  styleUrl: './board-game-detail.component.scss',
})
export class BoardGameDetailComponent implements OnInit {
  private readonly bggService = inject(BggService);
  error = signal(false);
  game = signal<BoardGameDetailDto | null>(null);
  loading = signal(true);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  formatPlaytime(game: BoardGameDetailDto): string {
    if (game.minPlaytime === null && game.maxPlaytime === null) return '—';
    if (game.minPlaytime === game.maxPlaytime) return `${game.minPlaytime} min`;
    return `${game.minPlaytime ?? '?'}–${game.maxPlaytime ?? '?'} min`;
  }

  formatPlayers(game: BoardGameDetailDto): string {
    if (game.minPlayers === null && game.maxPlayers === null) return '—';
    if (game.minPlayers === game.maxPlayers) return game.minPlayers?.toString() ?? '—';
    return `${game.minPlayers ?? '?'}–${game.maxPlayers ?? '?'}`;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.bggService.getBoardGame(id).subscribe({
      next: (detail) => {
        this.game.set(detail);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onBackClick(): void {
    this.router.navigate(['/board-games']);
  }
}
