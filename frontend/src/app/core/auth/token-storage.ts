import { Injectable } from '@angular/core';

const ACCESS_KEY = 'rm.access';
const REFRESH_KEY = 'rm.refresh';

/** Persistencia de tokens en `localStorage` (decisión de Fase 1). Tolerante a fallos. */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  get access(): string | null {
    return this.read(ACCESS_KEY);
  }

  get refresh(): string | null {
    return this.read(REFRESH_KEY);
  }

  set(access: string, refresh: string): void {
    this.write(ACCESS_KEY, access);
    this.write(REFRESH_KEY, refresh);
  }

  clear(): void {
    try {
      localStorage.removeItem(ACCESS_KEY);
      localStorage.removeItem(REFRESH_KEY);
    } catch {
      /* almacenamiento no disponible */
    }
  }

  private read(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private write(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      /* almacenamiento no disponible */
    }
  }
}
