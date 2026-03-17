import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SettingsDto {
  bggApiToken: string;
  bggUsername: string;
  discogsPersonalAccessToken: string;
  discogsUsername: string;
  hardcoverApiToken: string;
  theme: string;
}

export interface UpdateSettingsDto {
  bggApiToken?: string;
  bggUsername?: string;
  discogsPersonalAccessToken?: string;
  discogsUsername?: string;
  hardcoverApiToken?: string;
  theme?: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getSettings(): Observable<SettingsDto> {
    return this.http.get<SettingsDto>(`${this.baseUrl}/api/v1/settings`);
  }

  updateSettings(dto: UpdateSettingsDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/v1/settings`, dto);
  }
}
