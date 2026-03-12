import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';

export interface RecordStore {
  address: string;
  name: string;
}

interface NominatimResult {
  lat: string;
  lon: string;
}

interface OverpassElement {
  tags?: {
    'addr:city'?: string;
    'addr:housenumber'?: string;
    'addr:postcode'?: string;
    'addr:state'?: string;
    'addr:street'?: string;
    name?: string;
  };
}

interface OverpassResponse {
  elements: OverpassElement[];
}

@Injectable({ providedIn: 'root' })
export class StoreFinderService {
  private readonly EXCLUDED_STORES = [
    'amazon',
    'best buy',
    'fye',
    'guitar center',
    "musician's friend",
    'musicians friend',
    'sam ash',
    'sweetwater',
    'target',
    'walmart',
  ];
  private readonly http = inject(HttpClient);

  findStores(query: string): Observable<RecordStore[]> {
    return this.geocode(query).pipe(
      switchMap((coords) => this.queryOverpass(coords.lat, coords.lon)),
      map((response) => this.parseStores(response)),
    );
  }

  private buildAddress(tags: OverpassElement['tags']): string {
    if (!tags) return '';

    const parts: string[] = [];
    if (tags['addr:housenumber']) parts.push(tags['addr:housenumber']);
    if (tags['addr:street']) parts.push(tags['addr:street']);

    const street = parts.join(' ');
    const cityStateZip: string[] = [];
    if (tags['addr:city']) cityStateZip.push(tags['addr:city']);
    if (tags['addr:state']) cityStateZip.push(tags['addr:state']);
    if (tags['addr:postcode']) cityStateZip.push(tags['addr:postcode']);

    const city = cityStateZip.join(', ');

    return [street, city].filter(Boolean).join(', ');
  }

  private geocode(query: string): Observable<{ lat: string; lon: string }> {
    const headers = new HttpHeaders({
      'User-Agent': 'AllByMyshelf/1.0',
    });

    return this.http
      .get<NominatimResult[]>(
        `https://nominatim.openstreetmap.org/search`,
        {
          params: {
            q: query,
            countrycodes: 'us',
            format: 'json',
            limit: '1',
          },
          headers,
        },
      )
      .pipe(
        map((results) => {
          if (!results.length) {
            throw new Error('Location not found');
          }
          return { lat: results[0].lat, lon: results[0].lon };
        }),
      );
  }

  private isExcludedStore(name: string): boolean {
    const lowerName = name.toLowerCase();
    return this.EXCLUDED_STORES.some((excluded) => lowerName.includes(excluded));
  }

  private parseStores(response: OverpassResponse): RecordStore[] {
    return response.elements
      .filter((element) => element.tags?.name)
      .filter((element) => !this.isExcludedStore(element.tags!.name!))
      .map((element) => ({
        name: element.tags!.name!,
        address: this.buildAddress(element.tags),
      }));
  }

  private queryOverpass(lat: string, lon: string): Observable<OverpassResponse> {
    const around = `(around:40000,${lat},${lon})`;
    const query = `[out:json];(node[shop=music]${around};node[shop=records]${around};node[shop=vinyl]${around};way[shop=music]${around};way[shop=records]${around};way[shop=vinyl]${around};);out;`;
    return this.http.post<OverpassResponse>(
      'https://overpass-api.de/api/interpreter',
      `data=${encodeURIComponent(query)}`,
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/x-www-form-urlencoded',
        }),
      },
    );
  }
}
