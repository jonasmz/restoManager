import { computed, inject, Injectable, signal } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { OrgApiService } from '../../features/org/org-api.service';
import { Branch } from '../../features/org/org.models';

const STORAGE_KEY = 'rm.branch';

/**
 * Sucursal activa de la sesión (regla transversal §1). Se persiste en
 * `localStorage` y se envía en cada llamada a la Business API vía interceptor
 * (`X-Branch-Id`). Por defecto, la primera sucursal del usuario.
 */
@Injectable({ providedIn: 'root' })
export class BranchContextService {
  private readonly api = inject(OrgApiService);
  private readonly auth = inject(AuthService);

  private readonly _branches = signal<Branch[]>([]);
  private readonly _activeId = signal<number | null>(readStored());

  readonly branches = this._branches.asReadonly();
  readonly activeId = this._activeId.asReadonly();
  readonly activeBranch = computed(() => this._branches().find((b) => b.id === this._activeId()) ?? null);
  readonly ready = computed(() => this._branches().length > 0);

  /** Carga la lista de sucursales visibles y fija una activa si aún no hay. */
  load(): void {
    this.api.listBranches().subscribe({
      next: (page) => {
        this._branches.set(page.items);
        const current = this._activeId();
        if (current === null || !page.items.some((b) => b.id === current)) {
          const fallback = this.auth.currentUser()?.branchIds[0] ?? page.items[0]?.id ?? null;
          this.setActive(fallback);
        }
      },
      error: () => this._branches.set([]),
    });
  }

  setActive(id: number | null): void {
    this._activeId.set(id);
    try {
      if (id === null) {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, String(id));
      }
    } catch {
      /* almacenamiento no disponible */
    }
  }

  clear(): void {
    this._branches.set([]);
    this.setActive(null);
  }
}

function readStored(): number | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const n = raw ? Number(raw) : NaN;
    return Number.isInteger(n) && n > 0 ? n : null;
  } catch {
    return null;
  }
}
