import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FeaturesDto {
  discogsEnabled: boolean;
  hardcoverEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class FeaturesService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  private readonly features$: Observable<FeaturesDto> = this.http
    .get<FeaturesDto>(`${this.baseUrl}/api/v1/config/features`)
    .pipe(shareReplay(1));

  getFeatures(): Observable<FeaturesDto> {
    return this.features$;
  }
}
