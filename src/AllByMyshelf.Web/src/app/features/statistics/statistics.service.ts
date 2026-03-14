import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BookStatisticsDto {
  authorBreakdown: BreakdownItemDto[];
  decadeBreakdown: BreakdownItemDto[];
  genreBreakdown: BreakdownItemDto[];
  totalCount: number;
}

export interface BreakdownItemDto {
  count: number;
  label: string;
}

export interface CollectionValueDto {
  excludedCount: number;
  includedCount: number;
  totalValue: number;
}

export interface RecordStatisticsDto {
  decadeBreakdown: BreakdownItemDto[];
  excludedFromValueCount: number;
  formatBreakdown: BreakdownItemDto[];
  genreBreakdown: BreakdownItemDto[];
  totalCount: number;
  totalValue: number;
}

export interface UnifiedStatisticsDto {
  books: BookStatisticsDto;
  records: RecordStatisticsDto;
}

@Injectable({ providedIn: 'root' })
export class StatisticsService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly http = inject(HttpClient);

  getAll(): Observable<UnifiedStatisticsDto> {
    return this.http.get<UnifiedStatisticsDto>(`${this.baseUrl}/api/v1/statistics`);
  }

  getCollectionValue(): Observable<CollectionValueDto> {
    return this.http.get<CollectionValueDto>(`${this.baseUrl}/api/v1/statistics/collection-value`);
  }
}
