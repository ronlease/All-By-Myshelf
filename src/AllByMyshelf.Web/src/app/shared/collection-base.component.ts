import { Directive, OnDestroy, OnInit, signal, WritableSignal, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PageEvent } from '@angular/material/paginator';
import { Subscription } from 'rxjs';
import { SyncStateService } from '../core/sync/sync-state.service';

export interface GroupByOption {
  label: string;
  value: string;
}

/**
 * Abstract base class for collection list components.
 * Provides shared filtering, grouping, pagination, and search logic.
 */
@Directive()
export abstract class CollectionBaseComponent<T> implements OnInit, OnDestroy {
  protected readonly activatedRoute = inject(ActivatedRoute);
  protected abstract readonly collectionKey: string;
  currentPage = signal(1);
  protected abstract readonly displayedColumns: readonly string[];
  expandedGroups = signal<Set<string>>(new Set());
  groupByField = signal('');
  protected abstract readonly groupByOptions: readonly GroupByOption[];
  loading = signal(true);
  protected abstract readonly pageSize: number;
  protected readonly router = inject(Router);
  searchTerm = signal('');
  protected searchTimer?: ReturnType<typeof setTimeout>;
  protected readonly snackBar = inject(MatSnackBar);
  protected subscription?: Subscription;
  protected readonly syncState = inject(SyncStateService);

  /**
   * Get the value of a column for a given item.
   * Used for grouping and filtering.
   */
  protected abstract columnValue(item: T, col: string): string;

  /**
   * Get the detail route path for the given item (e.g., '/books' for books).
   */
  protected abstract detailRoute(item: T): string;

  /**
   * All items in the collection.
   */
  protected abstract allItems: WritableSignal<T[]>;

  /**
   * Filtered items after applying search term.
   */
  get filteredItems(): T[] {
    return this.applySearch(this.allItems());
  }

  /**
   * Grouped items after filtering.
   */
  get groupedItems(): { items: T[]; key: string }[] {
    const field = this.groupByField();
    if (!field) return [];

    const map = new Map<string, T[]>();
    for (const item of this.filteredItems) {
      let key: string;
      if (field === 'decade') {
        key = this.getDecadeKey(item);
      } else {
        key = this.columnValue(item, field);
      }
      const group = map.get(key) ?? [];
      group.push(item);
      map.set(key, group);
    }

    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, items]) => ({ items, key }));
  }

  /**
   * Paged items from the filtered list.
   */
  get pagedItems(): T[] {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredItems.slice(start, start + this.pageSize);
  }

  /**
   * Total count of filtered items.
   */
  get totalFilteredCount(): number {
    return this.filteredItems.length;
  }

  /**
   * Check if a group is expanded.
   */
  isGroupExpanded(key: string): boolean {
    return this.expandedGroups().has(key);
  }

  /**
   * Load all items from the API.
   * Must set loading state and update allItems signal.
   */
  protected abstract loadAll(): void;

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  ngOnInit(): void {
    localStorage.setItem('last-collection', this.collectionKey);

    const params = this.activatedRoute.snapshot.queryParams;
    if (params['groupBy']) {
      this.groupByField.set(params['groupBy']);
    }
    if (params['expand']) {
      this.expandedGroups.set(new Set([params['expand']]));
    }

    this.loadAll();
    this.subscription = this.syncCompletedObservable().subscribe(() => {
      this.loadAll();
    });
  }

  onGroupByChange(): void {
    this.expandedGroups.set(new Set());
  }

  onGroupCollapse(key: string): void {
    const current = new Set(this.expandedGroups());
    current.delete(key);
    this.expandedGroups.set(current);
  }

  onGroupExpand(key: string): void {
    const current = new Set(this.expandedGroups());
    current.add(key);
    this.expandedGroups.set(current);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
  }

  /**
   * Navigate to the detail page for the given item.
   */
  onRowClick(item: T): void {
    this.router.navigate([this.detailRoute(item)]);
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.currentPage.set(1), 300);
  }

  /**
   * Apply search term filter to items.
   * Subclasses can override to customize search logic.
   */
  protected abstract applySearch(items: T[]): T[];

  /**
   * Get the decade key for an item (e.g., '1980s').
   * Subclasses must implement based on their year field.
   */
  protected abstract getDecadeKey(item: T): string;

  /**
   * Get the sync completed observable for this collection type.
   */
  protected abstract syncCompletedObservable(): import('rxjs').Observable<void>;
}
