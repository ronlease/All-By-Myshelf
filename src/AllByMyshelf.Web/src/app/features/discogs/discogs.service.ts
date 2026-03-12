import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReleaseDetailDto {
  artist: string;
  coverImageUrl: string | null;
  discogsId: number;
  format: string;
  genre: string | null;
  highestPrice: number | null;
  id: string;
  lowestPrice: number | null;
  medianPrice: number | null;
  title: string;
  year: number | null;
}

export interface ReleaseDto {
  artist: string;
  format: string;
  genre: string | null;
  id: string;
  thumbnailUrl: string | null;
  title: string;
  year: number | null;
}

export interface RandomReleaseFilter {
  decade?: string;
  format?: string;
  genre?: string;
}

export interface SyncProgressDto {
  current: number;
  isRunning: boolean;
  retryAfterSeconds: number | null;
  status: 'idle' | 'syncing' | 'pausing' | 'resuming' | 'saving';
  total: number;
}

export interface CollectionFilter {
  artist?: string;
  format?: string;
  genre?: string;
  search?: string;
  title?: string;
  year?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class DiscogsService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getCollection(page: number, pageSize: number, filter?: CollectionFilter): Observable<PagedResult<ReleaseDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter?.artist) params = params.set('artist', filter.artist);
    if (filter?.format) params = params.set('format', filter.format);
    if (filter?.genre) params = params.set('genre', filter.genre);
    if (filter?.search) params = params.set('search', filter.search);
    if (filter?.title) params = params.set('title', filter.title);
    if (filter?.year) params = params.set('year', filter.year);

    return this.http.get<PagedResult<ReleaseDto>>(`${this.baseUrl}/api/v1/releases`, { params });
  }

  getRandomRelease(filter?: RandomReleaseFilter): Observable<ReleaseDetailDto> {
    let params = new HttpParams();
    if (filter?.decade) params = params.set('decade', filter.decade);
    if (filter?.format) params = params.set('format', filter.format);
    if (filter?.genre) params = params.set('genre', filter.genre);
    return this.http.get<ReleaseDetailDto>(`${this.baseUrl}/api/v1/releases/random`, { params });
  }

  getRelease(id: string): Observable<ReleaseDetailDto> {
    return this.http.get<ReleaseDetailDto>(`${this.baseUrl}/api/v1/releases/${id}`);
  }

  getSyncStatus(): Observable<SyncProgressDto> {
    return this.http.get<SyncProgressDto>(`${this.baseUrl}/api/v1/sync/status`);
  }

  triggerSync(): Observable<HttpResponse<unknown>> {
    return this.http.post<unknown>(`${this.baseUrl}/api/v1/sync`, null, { observe: 'response' });
  }
}
