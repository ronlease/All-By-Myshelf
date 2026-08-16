import { Sort } from '@angular/material/sort';

/**
 * A single column in a multi-column sort order.
 * The first entry is the most significant.
 */
export interface SortColumn {
  active: string;
  direction: 'asc' | 'desc';
}

/**
 * Promotes the newly sorted column to the front of the sort order, keeping the
 * previous columns behind it as tie-breakers.
 */
export function applySortChange(columns: readonly SortColumn[], sort: Sort): SortColumn[] {
  const direction = (sort.direction as 'asc' | 'desc') || 'asc';
  return [{ active: sort.active, direction }, ...columns.filter((c) => c.active !== sort.active)];
}

/**
 * Compares two rendered cell values, ordering numerically when both sides are
 * numbers so that "10" sorts after "2" rather than before it.
 */
export function compareCellValues(a: string, b: string): number {
  const left = Number(a);
  const right = Number(b);
  const bothNumeric =
    a.trim() !== '' && b.trim() !== '' && !Number.isNaN(left) && !Number.isNaN(right);

  return bothNumeric ? left - right : a.localeCompare(b);
}

/**
 * Reads a persisted sort order, falling back to the supplied default when nothing
 * is stored or the stored value is unusable.
 */
export function readSortColumns(storageKey: string, fallback: SortColumn[]): SortColumn[] {
  const saved = localStorage.getItem(storageKey);
  if (!saved) return fallback;

  try {
    const parsed = JSON.parse(saved);
    const usable =
      Array.isArray(parsed) &&
      parsed.every(
        (c) =>
          typeof c?.active === 'string' && (c.direction === 'asc' || c.direction === 'desc'),
      );

    return usable && parsed.length > 0 ? (parsed as SortColumn[]) : fallback;
  } catch {
    return fallback;
  }
}

/**
 * Returns a new list ordered by the given sort columns. The input is not mutated.
 */
export function sortItems<T>(
  items: T[],
  columns: readonly SortColumn[],
  columnValue: (item: T, col: string) => string,
): T[] {
  if (columns.length === 0) return items;

  return [...items].sort((a, b) => {
    for (const column of columns) {
      const comparison = compareCellValues(
        columnValue(a, column.active),
        columnValue(b, column.active),
      );

      if (comparison !== 0) {
        return column.direction === 'asc' ? comparison : -comparison;
      }
    }

    return 0;
  });
}

/**
 * Persists a sort order so it survives a page reload.
 */
export function writeSortColumns(storageKey: string, columns: readonly SortColumn[]): void {
  localStorage.setItem(storageKey, JSON.stringify(columns));
}
