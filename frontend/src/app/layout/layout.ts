import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../core/auth/auth.service';
import { BranchContextService } from '../core/branch/branch-context.service';
import { LayoutService } from './layout.service';
import { NAV_GROUPS } from './nav-items';

/**
 * Shell de la aplicación: portado fielmente del template
 * `requirements/inapp/` (topbar 60px fijo, sidebar 240/60px, overlay móvil).
 */
@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './layout.html',
})
export class Layout {
  protected readonly layout = inject(LayoutService);
  protected readonly auth = inject(AuthService);
  protected readonly branch = inject(BranchContextService);

  constructor() {
    this.branch.load();
  }

  protected onBranchChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.branch.setActive(Number.isInteger(value) && value > 0 ? value : null);
    window.location.reload();
  }

  /** Grupos del sidebar con solo los ítems que el rol del usuario puede ver. */
  protected readonly navGroups = computed(() =>
    NAV_GROUPS.map((group) => ({
      ...group,
      items: group.items.filter((item) => this.auth.hasAnyRole(item.roles)),
    })).filter((group) => group.items.length > 0),
  );

  protected logout(): void {
    this.auth.logout();
  }
}
