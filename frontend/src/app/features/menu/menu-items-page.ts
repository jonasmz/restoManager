import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { environment } from '../../core/config/environment';
import { apiErrorMessage } from '../../core/http/api-error';
import { MenuApiService } from './menu-api.service';
import { Category, MenuItem } from './menu.models';

@Component({
  selector: 'app-menu-items',
  imports: [RouterLink, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Platos</h1>
        <p class="text-secondary mb-0">Carta global. Abre un plato para editar su receta e impuestos.</p>
      </div>
      <a routerLink="/menu/items/new" class="btn btn-primary btn-sm"><i class="ti ti-plus me-1"></i>Nuevo</a>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="card-body pb-0">
        <select class="form-select form-select-sm mb-3" style="max-width: 16rem"
          [value]="categoryFilter()" (change)="setCategory($any($event.target).value)">
          <option value="">Todas las categorías</option>
          @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
        </select>
      </div>
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Categoría</th><th class="text-end">Precio</th><th>Disponible</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (m of rows(); track m.id) {
              <tr>
                <td>
                  @if (imageUrl(m); as src) {
                    <img [src]="src" [alt]="m.name" width="32" height="32"
                      class="rounded object-fit-cover me-2 align-middle" />
                  }
                  <a [routerLink]="['/menu/items', m.id]">{{ m.name }}</a>
                </td>
                <td class="text-secondary">{{ categoryName(m.categoryId) }}</td>
                <td class="text-end">{{ m.price | currency }}</td>
                <td>
                  @if (m.isAvailable) { <span class="badge bg-success-subtle text-success">Sí</span> }
                  @else { <span class="badge bg-secondary-subtle text-secondary">No</span> }
                </td>
                <td class="text-end"><a [routerLink]="['/menu/items', m.id]" class="btn btn-light btn-sm"><i class="ti ti-edit"></i></a></td>
              </tr>
            } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin platos.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class MenuItemsPage {
  private readonly api = inject(MenuApiService);

  protected readonly rows = signal<MenuItem[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly categoryFilter = signal('');
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.api.listCategories().subscribe((p) => this.categories.set(p.items));
    this.reload();
  }

  protected setCategory(value: string): void {
    this.categoryFilter.set(value);
    this.reload();
  }

  protected categoryName(id: number): string {
    return this.categories().find((c) => c.id === id)?.name ?? `#${id}`;
  }

  protected imageUrl(m: MenuItem): string | null {
    return m.imageUrl ? `${environment.businessApiUrl}${m.imageUrl}` : null;
  }

  private reload(): void {
    const categoryId = this.categoryFilter() ? Number(this.categoryFilter()) : undefined;
    this.api.listMenuItems({ categoryId }).subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
