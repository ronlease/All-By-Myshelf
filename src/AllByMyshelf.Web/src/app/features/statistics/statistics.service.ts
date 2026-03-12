import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CollectionValueDto {
  excludedCount: number;
  includedCount: number;
  totalValue: number;
}

@Injectable({ providedIn: 'root' })
export class StatisticsService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getCollectionValue(): Observable<CollectionValueDto> {
    return this.http.get<CollectionValueDto>(`${this.baseUrl}/api/v1/statistics/collection-value`);
  }
}
