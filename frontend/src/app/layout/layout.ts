import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../core/auth/auth.service';
import { BranchContextService } from '../core/branch/branch-context.service';
import { LayoutService } from './layout.service';
import { NAV_GROUPS } from './nav-items';

/**
 * Shell de la aplicación: portado del template `requirements/inapp/` (topbar 60px
 * fijo, sidebar 240/60px, overlay móvil). Los grupos del menú lateral son
 * plegables y su estado se persiste (LayoutService); el grupo que contiene la
 * ruta activa se muestra siempre desplegado.
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
  private readonly router = inject(Router);

  private readonly currentUrl = signal(this.router.url);

  constructor() {
    this.branch.load();
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => this.currentUrl.set(e.urlAfterRedirects));
  }

  protected onBranchChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.branch.setActive(Number.isInteger(value) && value > 0 ? value : null);
    window.location.reload();
  }

  /**
   * Grupos visibles para el rol, con un flag `expanded`: desplegado si el usuario
   * no lo plegó, si contiene la ruta activa, o si el sidebar está en modo icono.
   */
  protected readonly groups = computed(() => {
    const url = this.currentUrl();
    const iconRail = this.layout.collapsed();
    return NAV_GROUPS.map((group) => {
      const items = group.items.filter((item) => this.auth.hasAnyRole(item.roles));
      const hasActive = items.some((item) => isActive(url, item.route, item.exact));
      return {
        title: group.title,
        items,
        hasActive,
        expanded: iconRail || hasActive || !this.layout.isGroupCollapsed(group.title),
      };
    }).filter((group) => group.items.length > 0);
  });

  protected toggleGroup(title: string): void {
    this.layout.toggleGroup(title);
  }

  protected logout(): void {
    this.auth.logout();
  }
}

function isActive(url: string, route: string, exact?: boolean): boolean {
  const path = url.split(/[?#]/)[0];
  return exact ? path === route : path === route || path.startsWith(route + '/');
}
