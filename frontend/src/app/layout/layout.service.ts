import { Injectable, signal } from '@angular/core';

/**
 * Estado del shell (sidebar). Reemplaza a
 * `requirements/inapp/src/assets/js/sidebar.js` sin manipular el DOM:
 * el template reacciona a estas señales.
 */
@Injectable({ providedIn: 'root' })
export class LayoutService {
  /** Sidebar colapsado (240px ↔ 60px) en escritorio. */
  readonly collapsed = signal(false);
  /** Sidebar visible como off-canvas en móvil (≤992px). */
  readonly mobileOpen = signal(false);

  toggleCollapsed(): void {
    this.collapsed.update((v) => !v);
  }

  openMobile(): void {
    this.mobileOpen.set(true);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }
}
