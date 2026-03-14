import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BreakdownItemDto, StatisticsService, UnifiedStatisticsDto } from './statistics.service';

export interface CategorySection {
  breakdowns: { groupByField: string; items: BreakdownItemDto[]; route: string; title: string }[];
  icon: string;
  name: string;
  summary: string;
  totalCount: number;
}

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './statistics.component.html',
  styleUrl: './statistics.component.scss',
})
export class StatisticsComponent implements OnInit {
  categories = computed<CategorySection[]>(() => {
    const stats = this.statistics();
    if (!stats) return [];

    const sections: CategorySection[] = [];

    if (stats.boardGames.totalCount > 0) {
      const breakdowns: CategorySection['breakdowns'] = [];
      if (stats.boardGames.genreBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'genre', items: stats.boardGames.genreBreakdown, route: '/board-games', title: 'By Genre' });
      }
      sections.push({
        breakdowns,
        icon: 'extension',
        name: 'Board Games',
        summary: `${stats.boardGames.totalCount} game${stats.boardGames.totalCount === 1 ? '' : 's'}`,
        totalCount: stats.boardGames.totalCount,
      });
    }

    if (stats.books.totalCount > 0) {
      const breakdowns: CategorySection['breakdowns'] = [];
      if (stats.books.authorBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'author', items: stats.books.authorBreakdown, route: '/books', title: 'By Author' });
      }
      if (stats.books.decadeBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'decade', items: stats.books.decadeBreakdown, route: '/books', title: 'By Decade' });
      }
      if (stats.books.genreBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'genre', items: stats.books.genreBreakdown, route: '/books', title: 'By Genre' });
      }
      sections.push({
        breakdowns,
        icon: 'menu_book',
        name: 'Books',
        summary: `${stats.books.totalCount} book${stats.books.totalCount === 1 ? '' : 's'}`,
        totalCount: stats.books.totalCount,
      });
    }

    if (stats.records.totalCount > 0) {
      const value = Math.round(stats.records.totalValue);
      let valueSummary = `$${value.toLocaleString()} estimated value`;
      if (stats.records.excludedFromValueCount > 0) {
        valueSummary += ` (${stats.records.excludedFromValueCount} without pricing)`;
      }

      const breakdowns: CategorySection['breakdowns'] = [];
      if (stats.records.decadeBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'decade', items: stats.records.decadeBreakdown, route: '/', title: 'By Decade' });
      }
      if (stats.records.formatBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'format', items: stats.records.formatBreakdown, route: '/', title: 'By Format' });
      }
      if (stats.records.genreBreakdown.length > 0) {
        breakdowns.push({ groupByField: 'genre', items: stats.records.genreBreakdown, route: '/', title: 'By Genre' });
      }
      sections.push({
        breakdowns,
        icon: 'album',
        name: 'Music',
        summary: `${stats.records.totalCount} release${stats.records.totalCount === 1 ? '' : 's'} · ${valueSummary}`,
        totalCount: stats.records.totalCount,
      });
    }

    return sections;
  });
  collapsedCategories = signal<Set<string>>(new Set());
  error = signal(false);
  loading = signal(true);
  private readonly router = inject(Router);
  statistics = signal<UnifiedStatisticsDto | null>(null);
  private readonly statisticsService = inject(StatisticsService);

  isCategoryExpanded(name: string): boolean {
    return !this.collapsedCategories().has(name);
  }

  ngOnInit(): void {
    this.statisticsService.getAll().subscribe({
      next: (data) => {
        this.statistics.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onLabelClick(route: string, groupByField: string, label: string): void {
    this.router.navigate([route], { queryParams: { expand: label, groupBy: groupByField } });
  }

  retry(): void {
    this.error.set(false);
    this.loading.set(true);
    this.ngOnInit();
  }

  toggleCategory(name: string): void {
    const current = new Set(this.collapsedCategories());
    if (current.has(name)) {
      current.delete(name);
    } else {
      current.add(name);
    }
    this.collapsedCategories.set(current);
  }
}
