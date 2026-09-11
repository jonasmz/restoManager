import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import { TokenStorage } from '../../core/auth/token-storage';

interface CreatedUserId {
  id: number;
}

/**
 * Llamadas administrativas a la Auth API (crear login de empleado, listar roles). Van al
 * mismo origin que `AuthService.login`, así que `authInterceptor` no les adjunta el bearer
 * (está pensado para login/refresh, que no lo tienen) — acá sí hace falta, porque
 * `POST /auth/users` y `GET /auth/roles` requieren un admin logueado.
 */
@Injectable({ providedIn: 'root' })
export class AuthAdminApiService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorage);
  private readonly base = environment.authApiUrl;

  listRoles(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/roles`, { headers: this.authHeaders() });
  }

  createUser(body: {
    email: string;
    password: string;
    role: string;
    employeeId: number;
    branchIds: number[];
  }): Observable<CreatedUserId> {
    return this.http.post<CreatedUserId>(`${this.base}/users`, body, { headers: this.authHeaders() });
  }

  private authHeaders(): Record<string, string> {
    const token = this.storage.access;
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
}
