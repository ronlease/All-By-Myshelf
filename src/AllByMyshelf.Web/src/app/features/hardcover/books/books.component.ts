import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Observable } from 'rxjs';
import { BookDto, HardcoverService } from '../hardcover.service';
import { CollectionBaseComponent } from '../../../shared/collection-base.component';

@Component({
  selector: 'app-books',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './books.component.html',
})
export class BooksComponent extends CollectionBaseComponent<BookDto> {
  protected allItems = signal<BookDto[]>([]);
  protected readonly collectionKey = 'books';
  protected readonly displayedColumns = ['thumbnail', 'author', 'title', 'genre', 'year'];
  protected readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Author', value: 'author' },
    { label: 'Decade', value: 'decade' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  private readonly hardcoverService = inject(HardcoverService);
  protected readonly pageSize = 25;

  // Alias for template compatibility
  get allBooks() {
    return this.allItems;
  }

  protected applySearch(books: BookDto[]): BookDto[] {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return books;

    return books.filter(b =>
      b.title.toLowerCase().includes(term) ||
      b.authors.some(a => a.toLowerCase().includes(term)) ||
      (b.genre ?? '').toLowerCase().includes(term) ||
      (b.year?.toString() ?? '').includes(term)
    );
  }

  protected columnValue(b: BookDto, col: string): string {
    switch (col) {
      case 'author': return b.authors.length > 0 ? b.authors.join(', ') : '—';
      case 'genre': return b.genre ?? '—';
      case 'title': return b.title;
      case 'year': return b.year?.toString() ?? '—';
      default: return '';
    }
  }

  protected detailRoute(book: BookDto): string {
    return `/books/${book.id}`;
  }

  // Alias for template compatibility
  get filteredBooks(): BookDto[] {
    return this.filteredItems;
  }

  protected getDecadeKey(book: BookDto): string {
    return book.year != null ? `${Math.floor(book.year / 10) * 10}s` : '—';
  }

  // Alias for template compatibility
  get groupedBooks(): { items: BookDto[]; key: string }[] {
    return this.groupedItems;
  }

  protected loadAll(): void {
    this.loading.set(true);
    this.hardcoverService.getBooks(1, 10000).subscribe({
      next: (result) => {
        this.allItems.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load books.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  // Alias for template compatibility
  get pagedBooks(): BookDto[] {
    return this.pagedItems;
  }

  protected syncCompletedObservable(): Observable<void> {
    return this.syncState.booksSyncCompleted$;
  }
}
