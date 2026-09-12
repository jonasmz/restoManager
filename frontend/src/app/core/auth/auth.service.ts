import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { map, Observable, tap, throwError } from 'rxjs';

import { environment } from '../config/environment';
import { CurrentUser, currentUserFromPayload, currentUserFromToken } from './current-user';
import { decodeJwt, isExpired } from './jwt';
import { TokenStorage } from './token-storage';

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorage);
  private readonly router = inject(Router);
  private readonly base = environment.authApiUrl;

  private readonly _currentUser = signal<CurrentUser | null>(this.restore());
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  login(email: string, password: string): Observable<void> {
    return this.http
      .post<TokenResponse>(`${this.base}/login`, { email, password })
      .pipe(
        tap((res) => this.applyTokens(res)),
        map(() => undefined),
      );
  }

  /** Renueva el par de tokens con el refresh guardado. Emite el nuevo access token. */
  refresh(): Observable<string> {
    const refreshToken = this.storage.refresh;
    if (!refreshToken) {
      return throwError(() => new Error('No hay refresh token.'));
    }
    return this.http
      .post<TokenResponse>(`${this.base}/refresh`, { refreshToken })
      .pipe(
        tap((res) => this.applyTokens(res)),
        map((res) => res.accessToken),
      );
  }

  logout(): void {
    const refreshToken = this.storage.refresh;
    if (refreshToken) {
      this.http.post(`${this.base}/logout`, { refreshToken }).subscribe({ error: () => undefined });
    }
    this.clearSession();
    void this.router.navigate(['/auth/signin']);
  }

  /** Limpia el estado local sin llamar al backend (lo usa el interceptor al fallar el refresh). */
  clearSession(): void {
    this.storage.clear();
    this._currentUser.set(null);
  }

  hasRole(role: string): boolean {
    return this._currentUser()?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: readonly string[] | undefined): boolean {
    if (!roles || roles.length === 0) {
      return true;
    }
    const mine = this._currentUser()?.roles ?? [];
    return roles.some((role) => mine.includes(role));
  }

  private restore(): CurrentUser | null {
    const token = this.storage.access;
    if (!token) {
      return null;
    }
    const payload = decodeJwt(token);
    // Si expiró pero hay refresh, el interceptor lo renovará en la primera petición.
    return isExpired(payload) ? null : currentUserFromPayload(payload);
  }

  private applyTokens(res: TokenResponse): void {
    this.storage.set(res.accessToken, res.refreshToken);
    this._currentUser.set(currentUserFromToken(res.accessToken));
  }
}
