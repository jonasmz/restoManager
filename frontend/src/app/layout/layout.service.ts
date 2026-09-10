import { Injectable, signal } from '@angular/core';

const GROUPS_KEY = 'rm.sidebarCollapsedGroups';

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

  /** Títulos de grupos del menú que el usuario dejó plegados (se persiste). */
  private readonly collapsedGroups = signal<ReadonlySet<string>>(loadCollapsedGroups());

  toggleCollapsed(): void {
    this.collapsed.update((v) => !v);
  }

  openMobile(): void {
    this.mobileOpen.set(true);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  isGroupCollapsed(title: string): boolean {
    return this.collapsedGroups().has(title);
  }

  toggleGroup(title: string): void {
    const next = new Set(this.collapsedGroups());
    if (next.has(title)) {
      next.delete(title);
    } else {
      next.add(title);
    }
    this.collapsedGroups.set(next);
    saveCollapsedGroups(next);
  }
}

function loadCollapsedGroups(): ReadonlySet<string> {
  try {
    const raw = localStorage.getItem(GROUPS_KEY);
    return new Set(raw ? (JSON.parse(raw) as string[]) : []);
  } catch {
    return new Set();
  }
}

function saveCollapsedGroups(groups: ReadonlySet<string>): void {
  try {
    localStorage.setItem(GROUPS_KEY, JSON.stringify([...groups]));
  } catch {
    /* almacenamiento no disponible */
  }
}
