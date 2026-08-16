import {
  applySortChange,
  compareCellValues,
  readSortColumns,
  SortColumn,
  sortItems,
  writeSortColumns,
} from './table-sort';

interface Row {
  players: string;
  title: string;
  year: string;
}

const columnValue = (row: Row, col: string): string => (row as unknown as Record<string, string>)[col] ?? '';

describe('table-sort', () => {
  describe('compareCellValues', () => {
    it('orders numeric values numerically rather than lexicographically', () => {
      expect(compareCellValues('2', '10')).toBeLessThan(0);
    });

    it('orders non-numeric values alphabetically', () => {
      expect(compareCellValues('Apple', 'Banana')).toBeLessThan(0);
    });

    it('treats a numeric and a placeholder value as text', () => {
      expect(compareCellValues('1999', '—')).not.toBe(0);
    });

    it('reports equal values as equal', () => {
      expect(compareCellValues('Same', 'Same')).toBe(0);
    });
  });

  describe('sortItems', () => {
    const rows: Row[] = [
      { players: '4', title: 'Catan', year: '1995' },
      { players: '2', title: 'Patchwork', year: '2014' },
      { players: '10', title: 'Codenames', year: '2015' },
    ];

    it('sorts ascending by a single column', () => {
      const sorted = sortItems(rows, [{ active: 'title', direction: 'asc' }], columnValue);

      expect(sorted.map((r) => r.title)).toEqual(['Catan', 'Codenames', 'Patchwork']);
    });

    it('sorts descending when the direction is desc', () => {
      const sorted = sortItems(rows, [{ active: 'title', direction: 'desc' }], columnValue);

      expect(sorted.map((r) => r.title)).toEqual(['Patchwork', 'Codenames', 'Catan']);
    });

    it('sorts a numeric column numerically', () => {
      const sorted = sortItems(rows, [{ active: 'players', direction: 'asc' }], columnValue);

      expect(sorted.map((r) => r.players)).toEqual(['2', '4', '10']);
    });

    it('falls back to later columns to break ties', () => {
      const tied: Row[] = [
        { players: '2', title: 'Zebra', year: '2000' },
        { players: '2', title: 'Alpha', year: '2000' },
      ];

      const sorted = sortItems(
        tied,
        [
          { active: 'players', direction: 'asc' },
          { active: 'title', direction: 'asc' },
        ],
        columnValue,
      );

      expect(sorted.map((r) => r.title)).toEqual(['Alpha', 'Zebra']);
    });

    it('returns the list untouched when no sort columns are active', () => {
      expect(sortItems(rows, [], columnValue)).toBe(rows);
    });

    it('does not mutate the original list', () => {
      const original = [...rows];
      sortItems(rows, [{ active: 'title', direction: 'asc' }], columnValue);

      expect(rows).toEqual(original);
    });
  });

  describe('applySortChange', () => {
    const existing: SortColumn[] = [
      { active: 'artist', direction: 'asc' },
      { active: 'title', direction: 'asc' },
    ];

    it('promotes the newly sorted column to the front', () => {
      const result = applySortChange(existing, { active: 'year', direction: 'desc' });

      expect(result[0]).toEqual({ active: 'year', direction: 'desc' });
    });

    it('keeps the previous columns behind as tie-breakers', () => {
      const result = applySortChange(existing, { active: 'year', direction: 'desc' });

      expect(result.map((c) => c.active)).toEqual(['year', 'artist', 'title']);
    });

    it('does not duplicate a column that was already in the order', () => {
      const result = applySortChange(existing, { active: 'title', direction: 'desc' });

      expect(result.map((c) => c.active)).toEqual(['title', 'artist']);
    });

    it('defaults a cleared direction to ascending', () => {
      const result = applySortChange(existing, { active: 'year', direction: '' });

      expect(result[0].direction).toBe('asc');
    });
  });

  describe('readSortColumns and writeSortColumns', () => {
    const key = 'test-sort-columns';

    beforeEach(() => localStorage.removeItem(key));
    afterEach(() => localStorage.removeItem(key));

    it('returns the fallback when nothing is stored', () => {
      const fallback: SortColumn[] = [{ active: 'title', direction: 'asc' }];

      expect(readSortColumns(key, fallback)).toEqual(fallback);
    });

    it('round-trips a persisted sort order', () => {
      const columns: SortColumn[] = [{ active: 'year', direction: 'desc' }];
      writeSortColumns(key, columns);

      expect(readSortColumns(key, [])).toEqual(columns);
    });

    it('returns the fallback when the stored value is not valid JSON', () => {
      localStorage.setItem(key, 'not json');
      const fallback: SortColumn[] = [{ active: 'title', direction: 'asc' }];

      expect(readSortColumns(key, fallback)).toEqual(fallback);
    });

    it('returns the fallback when the stored value has the wrong shape', () => {
      localStorage.setItem(key, JSON.stringify([{ nope: true }]));
      const fallback: SortColumn[] = [{ active: 'title', direction: 'asc' }];

      expect(readSortColumns(key, fallback)).toEqual(fallback);
    });
  });
});
