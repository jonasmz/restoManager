import { CurrencyPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { CartService } from './cart.service';
import { CatalogApiService } from './catalog-api.service';
import { CatalogItem, PublicCatalog } from './catalog.models';

/** Quita acentos y pasa a minúsculas para comparar sin sensibilidad. */
function fold(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Carta pública accesible por QR (Fase 11). Ruta `/carta/:slug`, fuera del shell y sin
 * autenticación. Filtro por categoría, buscador y carrito de estimación (solo cliente).
 */
@Component({
  selector: 'app-catalog',
  imports: [FormsModule, CurrencyPipe, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    .catalog-img { height: 180px; object-fit: cover; background: var(--bs-gray-100); }
    .catalog-img-placeholder { height: 180px; display: grid; place-items: center; background: var(--bs-gray-100); color: var(--bs-gray-400); }
    .cart-panel { position: sticky; top: 1rem; }
    .cart-fab { position: fixed; right: 1rem; bottom: 1rem; z-index: 1030; }
    @media (min-width: 992px) { .cart-fab { display: none; } }
    .cart-drawer { position: fixed; inset: 0 0 0 auto; width: min(22rem, 100%); z-index: 1050; overflow-y: auto; }
    .cart-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.4); z-index: 1040; }
  `,
  template: `
    <div class="min-vh-100 bg-light">
      <div class="container py-4" style="max-width: 72rem">
        @if (loading()) {
          <p class="text-secondary">Cargando la carta…</p>
        } @else if (error()) {
          <div class="alert alert-danger">{{ error() }}</div>
        } @else if (catalog(); as c) {
          <header class="mb-4">
            <h1 class="fs-2 mb-1">{{ c.restaurant.name }}</h1>
            <p class="text-secondary mb-0">{{ c.branch.name }} · {{ c.branch.address }}</p>
          </header>

          <div class="row g-4">
            <!-- Carta -->
            <div class="col-lg-8">
              <div class="card mb-3">
                <div class="card-body d-flex flex-wrap gap-2">
                  <input
                    type="search"
                    class="form-control"
                    style="min-width: 12rem; flex: 1 1 12rem"
                    placeholder="Buscar plato o ingrediente…"
                    [ngModel]="query()"
                    (ngModelChange)="query.set($event)"
                  />
                  <select
                    class="form-select"
                    style="min-width: 10rem; flex: 0 1 14rem"
                    [ngModel]="activeCategory()"
                    (ngModelChange)="activeCategory.set(+$event)"
                  >
                    <option [value]="0">Todas las categorías</option>
                    @for (cat of c.categories; track cat.id) {
                      <option [value]="cat.id">{{ cat.name }}</option>
                    }
                  </select>
                </div>
              </div>

              @if (filtered().length === 0) {
                <p class="text-secondary">No hay platos que coincidan con la búsqueda.</p>
              }

              <div class="row g-3">
                @for (item of filtered(); track item.id) {
                  <div class="col-12 col-sm-6">
                    <div class="card h-100">
                      @if (image(item); as src) {
                        <img [src]="src" [alt]="item.name" class="card-img-top catalog-img" loading="lazy" />
                      } @else {
                        <div class="card-img-top catalog-img-placeholder"><i class="ti ti-tools-kitchen-2" style="font-size: 2rem"></i></div>
                      }
                      <div class="card-body d-flex flex-column">
                        <span class="badge bg-primary-subtle text-primary align-self-start mb-1">{{ item.categoryName }}</span>
                        <h2 class="fs-6 mb-1">{{ item.name }}</h2>
                        @if (item.description) { <p class="text-secondary small mb-2">{{ item.description }}</p> }
                        @if (item.ingredients.length) {
                          <p class="text-secondary small mb-2"><i class="ti ti-leaf me-1"></i>{{ item.ingredients.join(', ') }}</p>
                        }
                        <div class="mt-auto d-flex justify-content-between align-items-center">
                          <span class="fw-semibold">{{ item.price | currency }}</span>
                          <button type="button" class="btn btn-primary btn-sm" (click)="cart.add(item)">
                            <i class="ti ti-plus me-1"></i>Agregar
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>

            <!-- Carrito (escritorio) -->
            <div class="col-lg-4 d-none d-lg-block">
              <div class="cart-panel">
                <ng-container [ngTemplateOutlet]="cartCard"></ng-container>
              </div>
            </div>
          </div>

          <!-- Carrito (móvil) -->
          <button type="button" class="btn btn-primary cart-fab shadow" (click)="cartOpen.set(true)">
            <i class="ti ti-shopping-cart me-1"></i>{{ cart.count() }} · {{ cart.total() | currency }}
          </button>
          @if (cartOpen()) {
            <button type="button" class="cart-backdrop d-lg-none border-0 p-0" aria-label="Cerrar pedido"
              (click)="cartOpen.set(false)"></button>
            <div class="cart-drawer bg-light p-3 d-lg-none">
              <div class="d-flex justify-content-end">
                <button type="button" class="btn btn-light btn-sm mb-2" (click)="cartOpen.set(false)"><i class="ti ti-x"></i></button>
              </div>
              <ng-container [ngTemplateOutlet]="cartCard"></ng-container>
            </div>
          }

          <ng-template #cartCard>
            <div class="card">
              <div class="card-body">
                <h2 class="fs-6 mb-3"><i class="ti ti-shopping-cart me-1"></i>Mi pedido estimado</h2>
                @if (cart.lines().length === 0) {
                  <p class="text-secondary small mb-0">Todavía no agregaste nada.</p>
                } @else {
                  <ul class="list-unstyled mb-3">
                    @for (line of cart.lines(); track line.itemId) {
                      <li class="d-flex align-items-center gap-2 mb-2">
                        <div class="flex-grow-1">
                          <div class="small">{{ line.name }}</div>
                          <div class="text-secondary small">{{ line.price | currency }} c/u</div>
                        </div>
                        <div class="btn-group btn-group-sm" role="group">
                          <button type="button" class="btn btn-light" (click)="cart.dec(line.itemId)"><i class="ti ti-minus"></i></button>
                          <span class="btn btn-light disabled">{{ line.qty }}</span>
                          <button type="button" class="btn btn-light" (click)="cart.inc(line.itemId)"><i class="ti ti-plus"></i></button>
                        </div>
                        <span class="small fw-semibold" style="min-width: 4.5rem; text-align: right">{{ line.price * line.qty | currency }}</span>
                      </li>
                    }
                  </ul>
                  <div class="d-flex justify-content-between border-top pt-2">
                    <span class="fw-semibold">Total estimado</span>
                    <span class="fw-semibold">{{ cart.total() | currency }}</span>
                  </div>
                  <button type="button" class="btn btn-link btn-sm text-secondary px-0 mt-1" (click)="cart.clear()">Vaciar</button>
                }
                <p class="text-secondary mt-2 mb-0" style="font-size: .75rem">
                  Total estimado, impuestos incluidos. No es un pedido.
                </p>
              </div>
            </div>
          </ng-template>
        }
      </div>
    </div>
  `,
})
export class CatalogPage implements OnInit {
  readonly slug = input.required<string>();

  private readonly api = inject(CatalogApiService);
  protected readonly cart = inject(CartService);

  protected readonly catalog = signal<PublicCatalog | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly query = signal('');
  protected readonly activeCategory = signal(0);
  protected readonly cartOpen = signal(false);

  protected readonly filtered = computed<CatalogItem[]>(() => {
    const cat = this.activeCategory();
    const q = fold(this.query().trim());
    return (this.catalog()?.items ?? []).filter((item) => {
      if (cat && item.categoryId !== cat) {
        return false;
      }
      if (!q) {
        return true;
      }
      const haystack = fold(`${item.name} ${item.description} ${item.ingredients.join(' ')}`);
      return haystack.includes(q);
    });
  });

  ngOnInit(): void {
    const slug = this.slug();
    this.cart.use(slug);
    this.api.getCatalog(slug).subscribe({
      next: (c) => {
        this.catalog.set(c);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          err && typeof err === 'object' && 'status' in err && err.status === 404
            ? 'No encontramos esta carta. Verificá el enlace o el código QR.'
            : apiErrorMessage(err),
        );
        this.loading.set(false);
      },
    });
  }

  protected image(item: CatalogItem): string | null {
    return this.api.imageUrl(item.imageUrl);
  }
}
