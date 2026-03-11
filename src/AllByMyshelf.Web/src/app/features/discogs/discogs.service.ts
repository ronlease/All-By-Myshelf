import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReleaseDetailDto {
  artist: string;
  discogsId: number;
  format: string;
  genre: string | null;
  id: string;
  title: string;
  year: number | null;
}

export interface ReleaseDto {
  artist: string;
  format: string;
  id: string;
  title: string;
  year: number | null;
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

  getCollection(page: number, pageSize: number): Observable<PagedResult<ReleaseDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<ReleaseDto>>(`${this.baseUrl}/api/v1/releases`, { params });
  }

  getRelease(id: string): Observable<ReleaseDetailDto> {
    return this.http.get<ReleaseDetailDto>(`${this.baseUrl}/api/v1/releases/${id}`);
  }

  triggerSync(): Observable<HttpResponse<unknown>> {
    return this.http.post<unknown>(`${this.baseUrl}/api/v1/sync`, null, { observe: 'response' });
  }
}
