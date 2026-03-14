import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BookDto {
  author: string | null;
  coverImageUrl: string | null;
  genre: string | null;
  hardcoverId: number;
  id: string;
  title: string;
  year: number | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class HardcoverService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getBooks(page: number, pageSize: number): Observable<PagedResult<BookDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<BookDto>>(`${this.baseUrl}/api/v1/books`, { params });
  }

  getRandomBook(): Observable<BookDto> {
    return this.http.get<BookDto>(`${this.baseUrl}/api/v1/books/random`);
  }

  getSyncStatus(): Observable<{ isRunning: boolean }> {
    return this.http.get<{ isRunning: boolean }>(`${this.baseUrl}/api/v1/books/sync/status`);
  }

  triggerSync(): Observable<HttpResponse<string>> {
    return this.http.post(`${this.baseUrl}/api/v1/books/sync`, null, {
      observe: 'response',
      responseType: 'text'
    });
  }
}
