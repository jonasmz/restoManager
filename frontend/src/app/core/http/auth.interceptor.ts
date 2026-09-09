import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, Subject, switchMap, take, throwError } from 'rxjs';

import { environment } from '../config/environment';
import { AuthService } from '../auth/auth.service';
import { TokenStorage } from '../auth/token-storage';

// Estado compartido: mientras se renueva el token, los 401 concurrentes esperan.
let refreshing = false;
const refreshed$ = new Subject<string | null>();

/**
 * Adjunta el bearer a las llamadas a la Business API. Ante un `401`, intenta
 * `refresh` una sola vez (encolando los 401 concurrentes) y reintenta; si el
 * refresh falla, cierra la sesión y redirige a `/auth/signin`.
 * Las llamadas a la propia Auth API pasan sin tocar.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.startsWith(environment.authApiUrl)) {
    return next(req);
  }

  const auth = inject(AuthService);
  const storage = inject(TokenStorage);
  const router = inject(Router);

  const withToken = (request: HttpRequest<unknown>, token: string | null) =>
    token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(withToken(req, storage.access)).pipe(
    catchError((error: unknown) => {
      const is401 = error instanceof HttpErrorResponse && error.status === 401;
      if (!is401 || !storage.refresh) {
        return throwError(() => error);
      }

      if (refreshing) {
        return refreshed$.pipe(
          take(1),
          switchMap((token) => (token ? next(withToken(req, token)) : throwError(() => error))),
        );
      }

      refreshing = true;
      return auth.refresh().pipe(
        switchMap((token) => {
          refreshing = false;
          refreshed$.next(token);
          return next(withToken(req, token));
        }),
        catchError((refreshError: unknown) => {
          refreshing = false;
          refreshed$.next(null);
          auth.clearSession();
          void router.navigate(['/auth/signin']);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
