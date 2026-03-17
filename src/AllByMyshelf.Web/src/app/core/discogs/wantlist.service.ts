import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../features/discogs/discogs.service';

export interface WantlistReleaseDto {
  artists: string[];
  coverImageUrl: string | null;
  discogsId: number;
  format: string;
  genre: string | null;
  id: string;
  thumbnailUrl: string | null;
  title: string;
  year: number | null;
}

@Injectable({ providedIn: 'root' })
export class WantlistService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getWantlist(page: number, pageSize: number): Observable<PagedResult<WantlistReleaseDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<WantlistReleaseDto>>(`${this.baseUrl}/api/v1/wantlist`, { params });
  }
}
