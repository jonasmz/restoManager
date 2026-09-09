import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

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
  protected readonly navGroups = NAV_GROUPS;
}
