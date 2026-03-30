import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, switchMap, tap } from 'rxjs';
import { SessionUser } from './models';
import { PortalApiService } from './portal-api.service';
import { CsrfService } from './csrf.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(PortalApiService);
  private readonly csrf = inject(CsrfService);

  readonly currentUser = signal<SessionUser | null>(null);

  loginUser(login: string, password: string): Observable<SessionUser> {
    return this.csrf.ensureToken().pipe(
      switchMap(() => this.api.loginUser({ login, password })),
      tap((user) => this.currentUser.set(user)),
      switchMap((user) => {
        this.csrf.reset();
        return this.csrf.ensureToken().pipe(map(() => user));
      })
    );
  }

  loginAdmin(login: string, password: string): Observable<SessionUser> {
    return this.csrf.ensureToken().pipe(
      switchMap(() => this.api.loginAdmin({ login, password })),
      tap((user) => this.currentUser.set(user)),
      switchMap((user) => {
        this.csrf.reset();
        return this.csrf.ensureToken().pipe(map(() => user));
      })
    );
  }

  refreshSession(): Observable<SessionUser> {
    return this.api.getCurrentSession().pipe(tap((user) => this.currentUser.set(user)));
  }

  logout(): Observable<void> {
    return this.api.logout().pipe(tap(() => {
      this.currentUser.set(null);
      this.csrf.reset();
    }));
  }
}
