import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { MenuApiService } from '../menu/menu-api.service';
import { Category, MenuItem } from '../menu/menu.models';
import { SalesApiService } from './sales-api.service';
import { Discount, Order, PAYMENT_METHODS, PaymentMethod } from './sales.models';

@Component({
  selector: 'app-sales-order',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-4">
      <div>
        <a routerLink="/pos" class="small text-decoration-none"><i class="ti ti-arrow-left me-1"></i>Nuevo pedido</a>
        <h1 class="fs-3 mb-1 mt-1">
          Pedido #{{ order()?.id }}
          @if (order(); as o) { <span class="badge ms-2" [class]="statusBadge(o.status)">{{ o.status }}</span> }
        </h1>
        @if (order(); as o) {
          <p class="text-secondary mb-0 small">
            {{ channelLabel(o.channel) }}
            @if (o.tableId) { · Mesa #{{ o.tableId }} }
            @if (o.tableSessionId) { · Sesión #{{ o.tableSessionId }} }
            · {{ o.orderTime | date: 'short' }}
          </p>
        }
      </div>
      <a routerLink="/sales/orders" class="btn btn-light btn-sm"><i class="ti ti-list me-1"></i>Historial</a>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    @if (order(); as o) {
      <div class="row g-4">
        <!-- Carta -->
        <div class="col-lg-7">
          <div class="card">
            <div class="card-body">
              <div class="d-flex flex-wrap gap-1 mb-3">
                <button type="button" class="btn btn-sm" [class]="catFilter() === null ? 'btn-primary' : 'btn-light'"
                  (click)="catFilter.set(null)">Todas</button>
                @for (c of categories(); track c.id) {
                  <button type="button" class="btn btn-sm" [class]="catFilter() === c.id ? 'btn-primary' : 'btn-light'"
                    (click)="catFilter.set(c.id)">{{ c.name }}</button>
                }
              </div>
              @if (!editable(o)) {
                <p class="text-secondary small mb-0">El pedido está {{ o.status }}: no se pueden cambiar los ítems.</p>
              } @else {
                <div class="row g-2">
                  @for (m of visibleMenu(); track m.id) {
                    <div class="col-6 col-xl-4">
                      <button type="button" class="card w-100 h-100 text-start border" (click)="addItem(m)">
                        <div class="card-body p-2">
                          <div class="fw-semibold small text-truncate">{{ m.name }}</div>
                          <div class="text-secondary small">{{ m.price | number: '1.2-2' }}</div>
                        </div>
                      </button>
                    </div>
                  } @empty {
                    <div class="col-12 text-secondary small">No hay platos en esta categoría.</div>
                  }
                </div>
              }
            </div>
          </div>
        </div>

        <!-- Cuenta -->
        <div class="col-lg-5">
          <div class="card mb-3">
            <div class="card-header fw-semibold">Cuenta</div>
            <ul class="list-group list-group-flush">
              @for (it of o.items; track it.id) {
                <li class="list-group-item">
                  <div class="d-flex justify-content-between align-items-center">
                    <div class="me-2">
                      <div class="small fw-semibold">{{ menuName(it.menuItemId) }}</div>
                      <div class="text-secondary" style="font-size: .78rem">{{ it.unitPrice | number: '1.2-2' }} c/u</div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                      @if (editable(o)) {
                        <div class="btn-group btn-group-sm">
                          <button type="button" class="btn btn-outline-secondary" (click)="changeQty(it.id, it.quantity - 1, it.notes)">−</button>
                          <span class="btn btn-outline-secondary disabled">{{ it.quantity }}</span>
                          <button type="button" class="btn btn-outline-secondary" (click)="changeQty(it.id, it.quantity + 1, it.notes)">+</button>
                        </div>
                      } @else {
                        <span class="small text-secondary">× {{ it.quantity }}</span>
                      }
                      <span class="small fw-semibold" style="min-width: 4rem; text-align: right">{{ it.lineTotal | number: '1.2-2' }}</span>
                      @if (editable(o)) {
                        <button type="button" class="btn btn-link btn-sm text-danger p-0" (click)="removeItem(it.id)"><i class="ti ti-x"></i></button>
                      }
                    </div>
                  </div>
                  @if (editable(o)) {
                    <input type="text" class="form-control form-control-sm mt-1" placeholder="Nota…" [value]="it.notes ?? ''"
                      (change)="changeNote(it.id, it.quantity, $any($event.target).value)" />
                  } @else if (it.notes) {
                    <div class="text-secondary" style="font-size: .78rem">{{ it.notes }}</div>
                  }
                </li>
              } @empty {
                <li class="list-group-item text-center text-secondary small py-4">Sin ítems todavía.</li>
              }
            </ul>
          </div>

          <!-- Descuentos -->
          <div class="card mb-3">
            <div class="card-header fw-semibold">Descuentos</div>
            <ul class="list-group list-group-flush">
              @for (d of o.discounts; track d.id) {
                <li class="list-group-item d-flex justify-content-between align-items-center py-2">
                  <span class="small">{{ discountName(d.discountId) }}</span>
                  <span class="d-flex align-items-center gap-2">
                    <span class="small text-danger">−{{ d.appliedAmount | number: '1.2-2' }}</span>
                    @if (editable(o)) {
                      <button type="button" class="btn btn-link btn-sm text-danger p-0" (click)="removeDiscount(d.discountId)"><i class="ti ti-x"></i></button>
                    }
                  </span>
                </li>
              }
              @if (editable(o)) {
                <li class="list-group-item py-2">
                  <div class="d-flex gap-2">
                    <select class="form-select form-select-sm" [value]="pickedDiscount()"
                      (change)="pickedDiscount.set($any($event.target).value)">
                      <option value="">Añadir descuento…</option>
                      @for (d of applicableDiscounts(); track d.id) {
                        <option [value]="d.id">{{ d.name }} ({{ d.type === 'PERCENTAGE' ? d.value + ' %' : d.value }})</option>
                      }
                    </select>
                    <button type="button" class="btn btn-light btn-sm" (click)="applyPickedDiscount()"
                      [disabled]="!pickedDiscount()">Aplicar</button>
                  </div>
                </li>
              }
            </ul>
          </div>

          <!-- Totales -->
          <div class="card mb-3">
            <ul class="list-group list-group-flush">
              <li class="list-group-item d-flex justify-content-between py-2 small">
                <span class="text-secondary">Subtotal</span><span>{{ o.itemsSubtotal | number: '1.2-2' }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between py-2 small">
                <span class="text-secondary">Descuentos</span><span>−{{ o.discountTotal | number: '1.2-2' }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between py-2 fw-semibold">
                <span>Total</span><span>{{ o.totalAmount | number: '1.2-2' }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between py-2 small">
                <span class="text-secondary">Pagado</span><span>{{ o.confirmedPaid | number: '1.2-2' }}</span>
              </li>
              <li class="list-group-item d-flex justify-content-between py-2 fw-semibold" [class.text-success]="o.balance <= 0">
                <span>Saldo</span><span>{{ o.balance | number: '1.2-2' }}</span>
              </li>
            </ul>
          </div>

          <!-- Pagos -->
          <div class="card mb-3">
            <div class="card-header fw-semibold">Pagos</div>
            <ul class="list-group list-group-flush">
              @for (p of o.payments; track p.id) {
                <li class="list-group-item d-flex justify-content-between align-items-center py-2 small">
                  <span>{{ methodLabel(p.paymentMethod) }} <span class="text-secondary">· {{ p.paymentTime | date: 'shortTime' }}</span></span>
                  <span class="d-flex align-items-center gap-2">
                    <span [class.text-decoration-line-through]="p.status === 'REFUNDED'">{{ p.amount | number: '1.2-2' }}</span>
                    <span class="badge" [class]="p.status === 'CONFIRMED' ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'">{{ p.status }}</span>
                  </span>
                </li>
              } @empty { <li class="list-group-item text-secondary small py-2">Sin pagos.</li> }

              @if (o.balance > 0 && o.status !== 'CANCELLED' && o.status !== 'CLOSED') {
                <li class="list-group-item py-2">
                  <form [formGroup]="payForm" (ngSubmit)="pay()" class="row g-2">
                    <div class="col-5">
                      <select class="form-select form-select-sm" formControlName="method">
                        @for (m of methods; track m) { <option [value]="m">{{ methodLabel(m) }}</option> }
                      </select>
                    </div>
                    <div class="col-4">
                      <input type="number" step="0.01" min="0.01" class="form-control form-control-sm" formControlName="amount" />
                    </div>
                    <div class="col-3">
                      <button type="submit" class="btn btn-primary btn-sm w-100" [disabled]="payForm.invalid">Cobrar</button>
                    </div>
                    @if (payForm.controls.method.value === 'GIFT_CARD') {
                      <div class="col-12">
                        <input type="number" min="1" class="form-control form-control-sm" placeholder="ID de tarjeta regalo"
                          formControlName="giftCardId" />
                      </div>
                    }
                    <div class="col-12">
                      <button type="button" class="btn btn-link btn-sm p-0" (click)="payForm.controls.amount.setValue(o.balance)">
                        Cobrar el saldo ({{ o.balance | number: '1.2-2' }})
                      </button>
                    </div>
                  </form>
                </li>
              }
            </ul>
          </div>

          <div class="d-flex gap-2">
            <button type="button" class="btn btn-success flex-fill" [disabled]="o.balance > 0 || o.status === 'CLOSED' || o.status === 'CANCELLED'"
              (click)="close()">Cerrar pedido</button>
            @if (o.status === 'OPEN' || o.status === 'PAID') {
              <button type="button" class="btn btn-outline-danger" (click)="cancel()">Cancelar</button>
            }
          </div>
        </div>
      </div>
    } @else if (!message()) {
      <p class="text-secondary">Cargando pedido…</p>
    }
  `,
})
export class OrderPage {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalesApiService);
  private readonly menuApi = inject(MenuApiService);
  protected readonly branch = inject(BranchContextService);

  private readonly orderId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly order = signal<Order | null>(null);
  protected readonly categories = signal<Category[]>([]);
  protected readonly menu = signal<MenuItem[]>([]);
  protected readonly discounts = signal<Discount[]>([]);
  protected readonly catFilter = signal<number | null>(null);
  protected readonly pickedDiscount = signal<string>('');
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected readonly methods = PAYMENT_METHODS;

  protected readonly payForm = this.fb.nonNullable.group({
    method: ['CASH' as PaymentMethod, Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    giftCardId: this.fb.control<number | null>(null),
  });

  private readonly menuById = computed(() => new Map(this.menu().map((m) => [m.id, m])));

  protected readonly visibleMenu = computed(() => {
    const cat = this.catFilter();
    return this.menu().filter((m) => cat === null || m.categoryId === cat);
  });

  protected readonly applicableDiscounts = computed(() => {
    const used = new Set((this.order()?.discounts ?? []).map((d) => d.discountId));
    return this.discounts().filter((d) => !used.has(d.id));
  });

  constructor() {
    forkJoin({
      cats: this.menuApi.listCategories(),
      items: this.menuApi.listMenuItems(),
      discs: this.api.listDiscounts(),
    })
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: ({ cats, items, discs }) => {
          this.categories.set(cats.items);
          this.menu.set(items.items);
          this.discounts.set(discs.items);
        },
        error: (err) => this.fail(err),
      });
    this.reload();
  }

  protected editable(o: Order): boolean {
    return o.status === 'OPEN';
  }

  protected menuName(id: number): string {
    return this.menuById().get(id)?.name ?? `Plato #${id}`;
  }
  protected discountName(id: number): string {
    return this.discounts().find((d) => d.id === id)?.name ?? `Descuento #${id}`;
  }
  protected channelLabel(c: string): string {
    return { MESA: 'Mesa', BARRA: 'Barra', TAKEAWAY: 'Para llevar', DELIVERY: 'Delivery' }[c] ?? c;
  }
  protected methodLabel(m: string): string {
    return { CASH: 'Efectivo', CARD: 'Tarjeta', TRANSFER: 'Transferencia', GIFT_CARD: 'Tarjeta regalo', OTHER: 'Otro' }[m] ?? m;
  }
  protected statusBadge(s: string): string {
    switch (s) {
      case 'OPEN': return 'bg-primary-subtle text-primary';
      case 'PAID': return 'bg-success-subtle text-success';
      case 'CLOSED': return 'bg-secondary-subtle text-secondary';
      default: return 'bg-danger-subtle text-danger';
    }
  }

  reload(): void {
    this.api.getOrder(this.orderId).subscribe({
      next: (o) => {
        this.order.set(o);
        if (this.payForm.controls.amount.value === 0 || this.payForm.pristine) {
          this.payForm.controls.amount.setValue(o.balance > 0 ? o.balance : 0);
        }
      },
      error: (err) => this.fail(err),
    });
  }

  // Los cambios de ítems no muestran alerta (el POS encadena muchos toques);
  // solo se refresca la cuenta. Descuentos, pagos y cierre sí avisan.
  protected addItem(m: MenuItem): void {
    this.api.addItem(this.orderId, { menuItemId: m.id, quantity: 1 }).subscribe({
      next: () => this.reload(),
      error: (err) => this.fail(err),
    });
  }
  protected changeQty(itemId: number, quantity: number, notes: string | null): void {
    if (quantity <= 0) {
      this.removeItem(itemId);
      return;
    }
    this.api.updateItem(this.orderId, itemId, { quantity, notes }).subscribe({
      next: () => this.reload(),
      error: (err) => this.fail(err),
    });
  }
  protected changeNote(itemId: number, quantity: number, notes: string): void {
    this.api.updateItem(this.orderId, itemId, { quantity, notes: notes || null }).subscribe({
      next: () => this.reload(),
      error: (err) => this.fail(err),
    });
  }
  protected removeItem(itemId: number): void {
    this.api.removeItem(this.orderId, itemId).subscribe({
      next: () => this.reload(),
      error: (err) => this.fail(err),
    });
  }

  protected applyPickedDiscount(): void {
    const id = this.pickedDiscount();
    if (!id) {
      return;
    }
    this.pickedDiscount.set('');
    this.api.applyDiscount(this.orderId, Number(id)).subscribe({
      next: () => this.done('Descuento aplicado.'),
      error: (err) => this.fail(err),
    });
  }
  protected removeDiscount(discountId: number): void {
    this.api.removeOrderDiscount(this.orderId, discountId).subscribe({
      next: () => this.done('Descuento quitado.'),
      error: (err) => this.fail(err),
    });
  }

  protected pay(): void {
    if (this.payForm.invalid) {
      return;
    }
    const v = this.payForm.getRawValue();
    this.api
      .registerPayment(this.orderId, {
        method: v.method,
        amount: Number(v.amount),
        giftCardId: v.method === 'GIFT_CARD' ? Number(v.giftCardId) : null,
      })
      .subscribe({
        next: () => {
          this.payForm.reset({ method: 'CASH', amount: 0, giftCardId: null });
          this.done('Pago registrado.');
        },
        error: (err) => this.fail(err),
      });
  }

  protected close(): void {
    this.api.closeOrder(this.orderId).subscribe({
      next: () => this.done('Pedido cerrado.'),
      error: (err) => this.fail(err),
    });
  }
  protected cancel(): void {
    this.api.cancelOrder(this.orderId).subscribe({
      next: () => this.done('Pedido cancelado.'),
      error: (err) => this.fail(err),
    });
  }

  private done(msg: string | null): void {
    this.ok.set(true);
    this.message.set(msg);
    this.reload();
  }
  private fail(err: unknown): void {
    this.ok.set(false);
    this.message.set(apiErrorMessage(err));
  }
}
