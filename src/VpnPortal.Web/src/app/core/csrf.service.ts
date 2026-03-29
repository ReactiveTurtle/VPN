import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class CsrfService {
  private readonly http = inject(HttpClient);
  private tokenRequest$?: Observable<string>;

  ensureToken(): Observable<string> {
    this.tokenRequest$ ??= this.http
      .get<{ token: string }>('/api/csrf/token', { withCredentials: true })
      .pipe(
        map((response) => response.token),
        shareReplay(1)
      );

    return this.tokenRequest$;
  }

  reset(): void {
    this.tokenRequest$ = undefined;
  }
}
