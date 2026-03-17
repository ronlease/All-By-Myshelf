import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BoardGameDetailDto {
  bggId: number;
  coverImageUrl: string | null;
  description: string | null;
  designers: string[];
  genre: string | null;
  id: string;
  maxPlayers: number | null;
  maxPlaytime: number | null;
  minPlayers: number | null;
  minPlaytime: number | null;
  thumbnailUrl: string | null;
  title: string;
  yearPublished: number | null;
}

export interface BoardGameDto {
  bggId: number;
  designers: string[];
  genre: string | null;
  id: string;
  maxPlayers: number | null;
  minPlayers: number | null;
  thumbnailUrl: string | null;
  title: string;
  yearPublished: number | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class BggService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getBoardGame(id: string): Observable<BoardGameDetailDto> {
    return this.http.get<BoardGameDetailDto>(`${this.baseUrl}/api/v1/boardgames/${id}`);
  }

  getBoardGames(page: number, pageSize: number): Observable<PagedResult<BoardGameDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<BoardGameDto>>(`${this.baseUrl}/api/v1/boardgames`, { params });
  }

  getRandomBoardGame(): Observable<BoardGameDto> {
    return this.http.get<BoardGameDto>(`${this.baseUrl}/api/v1/boardgames/random`);
  }

  getSyncStatus(): Observable<{ isRunning: boolean }> {
    return this.http.get<{ isRunning: boolean }>(`${this.baseUrl}/api/v1/boardgames/sync/status`);
  }

  triggerSync(): Observable<HttpResponse<string>> {
    return this.http.post(`${this.baseUrl}/api/v1/boardgames/sync`, null, {
      observe: 'response',
      responseType: 'text'
    });
  }
}
