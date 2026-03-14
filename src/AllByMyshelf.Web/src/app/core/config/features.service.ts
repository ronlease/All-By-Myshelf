import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, ReplaySubject, shareReplay, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FeaturesDto {
  bggEnabled: boolean;
  discogsEnabled: boolean;
  hardcoverEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class FeaturesService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  private readonly refreshTrigger$ = new ReplaySubject<void>(1);
  private readonly features$: Observable<FeaturesDto> = this.refreshTrigger$.pipe(
    switchMap(() =>
      this.http
        .get<FeaturesDto>(`${this.baseUrl}/api/v1/config/features`)
    ),
    shareReplay(1),
  );

  constructor() {
    this.refreshTrigger$.next();
  }

  getFeatures(): Observable<FeaturesDto> {
    return this.features$;
  }

  refresh(): void {
    this.refreshTrigger$.next();
  }
}
