import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const userGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.refreshSession().pipe(
    map((user) => user.role === 'User' ? true : router.createUrlTree(['/login'])),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.refreshSession().pipe(
    map((user) => user.role === 'SuperAdmin' ? true : router.createUrlTree(['/admin/login'])),
    catchError(() => of(router.createUrlTree(['/admin/login'])))
  );
};
