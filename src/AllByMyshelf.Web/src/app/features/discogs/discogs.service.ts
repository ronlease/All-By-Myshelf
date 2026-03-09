import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReleaseDto {
  id: number;
  artist: string;
  title: string;
  year: number | null;
  format: string;
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
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getCollection(page: number, pageSize: number): Observable<PagedResult<ReleaseDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<ReleaseDto>>(`${this.baseUrl}/api/v1/releases`, { params });
  }

  triggerSync(): Observable<HttpResponse<unknown>> {
    return this.http.post<unknown>(`${this.baseUrl}/api/v1/sync`, null, { observe: 'response' });
  }
}
