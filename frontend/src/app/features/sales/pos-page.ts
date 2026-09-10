import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { map } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { CustomersApiService } from '../customers/customers-api.service';
import { Customer } from '../customers/customers.models';
import { DeliveryApiService } from '../delivery/delivery-api.service';
import { Driver } from '../delivery/delivery.models';
import { SalesApiService } from './sales-api.service';
import { ORDER_CHANNELS, Order, OrderChannel } from './sales.models';

@Component({
  selector: 'app-sales-pos',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-4">
      <h1 class="fs-3 mb-1">Punto de venta</h1>
      <p class="text-secondary mb-0">
        Abre un pedido en <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
        Las mesas se abren desde el <a routerLink="/salon/floor">tablero de salón</a>.
      </p>
    </div>

    @if (message()) {
      <div class="alert alert-danger py-2 small">{{ message() }}</div>
    }

    <div class="row g-4">
      <div class="col-lg-5">
        <div class="card">
          <div class="card-body">
            <h2 class="fs-6 mb-3">Nuevo pedido</h2>

            @if (ctxTableId()) {
              <div class="alert alert-info py-2 small mb-3">
                Canal <strong>MESA</strong> · Mesa #{{ ctxTableId() }}
                @if (ctxSessionId()) { · Sesión #{{ ctxSessionId() }} }
              </div>
            } @else {
              <div class="form-label small">Canal</div>
              <div class="btn-group d-flex mb-3" role="group">
                @for (c of channels; track c) {
                  <button type="button" class="btn btn-outline-primary" [class.active]="channel() === c"
                    (click)="channel.set(c)" [disabled]="c === 'MESA'">{{ label(c) }}</button>
                }
              </div>

              @if (channel() === 'DELIVERY') {
                <form [formGroup]="deliveryForm" class="row g-2 mb-3">
                  <div class="col-12">
                    <label class="form-label small" for="addr">Dirección de entrega</label>
                    <input id="addr" class="form-control form-control-sm" formControlName="deliveryAddress" />
                  </div>
                  <div class="col-7">
                    <label class="form-label small" for="eta">Hora estimada</label>
                    <input id="eta" type="datetime-local" class="form-control form-control-sm" formControlName="estimatedTime" />
                  </div>
                  <div class="col-5">
                    <label class="form-label small" for="drv">Repartidor</label>
                    <select id="drv" class="form-select form-select-sm" formControlName="driverId">
                      <option [value]="0" disabled>—</option>
                      @for (d of drivers(); track d.id) { <option [value]="d.id">{{ d.licensePlate }}</option> }
                    </select>
                  </div>
                  @if (drivers().length === 0) {
                    <div class="col-12"><span class="small text-danger">No hay repartidores. Crea uno en Delivery → Repartidores.</span></div>
                  }
                </form>
              } @else {
                <p class="text-secondary small">Para un pedido de mesa, ve al tablero y usa «Abrir cuenta».</p>
              }

              <label class="form-label small" for="cust">Cliente <span class="text-secondary">(opcional)</span></label>
              <select id="cust" class="form-select form-select-sm mb-3" [value]="selectedCustomerId()"
                (change)="selectedCustomerId.set(+$any($event.target).value)">
                <option [value]="0">Sin identificar</option>
                @for (c of customers(); track c.id) {
                  <option [value]="c.id">{{ c.lastName }}, {{ c.firstName }}</option>
                }
              </select>
            }

            <button type="button" class="btn btn-primary w-100" (click)="create()" [disabled]="creating() || !canCreate()">
              {{ creating() ? 'Creando…' : 'Abrir pedido' }}
            </button>
          </div>
        </div>
      </div>

      <div class="col-lg-7">
        <div class="card">
          <div class="card-header fw-semibold d-flex justify-content-between align-items-center">
            <span>Pedidos abiertos hoy</span>
            <button type="button" class="btn btn-light btn-sm" (click)="reload()"><i class="ti ti-refresh"></i></button>
          </div>
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead><tr><th>#</th><th>Canal</th><th>Estado</th><th class="text-end">Total</th><th class="text-end">Saldo</th><th></th></tr></thead>
              <tbody>
                @for (o of openOrders(); track o.id) {
                  <tr>
                    <td>{{ o.id }}</td>
                    <td class="small">{{ label(o.channel) }}@if (o.tableId) { <span class="text-secondary"> · Mesa #{{ o.tableId }}</span> }</td>
                    <td><span class="badge" [class]="o.status === 'OPEN' ? 'bg-primary-subtle text-primary' : 'bg-success-subtle text-success'">{{ o.status }}</span></td>
                    <td class="text-end small">{{ o.totalAmount | currency }}</td>
                    <td class="text-end small">{{ o.balance | currency }}</td>
                    <td class="text-end"><a class="btn btn-light btn-sm" [routerLink]="['/pos/order', o.id]">Abrir</a></td>
                  </tr>
                } @empty {
                  <tr><td colspan="6" class="text-center text-secondary py-4">Sin pedidos abiertos.</td></tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class PosPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalesApiService);
  private readonly deliveryApi = inject(DeliveryApiService);
  private readonly customersApi = inject(CustomersApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly channels = ORDER_CHANNELS;
  protected readonly channel = signal<OrderChannel>('BARRA');
  protected readonly openOrders = signal<Order[]>([]);
  protected readonly drivers = signal<Driver[]>([]);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly selectedCustomerId = signal(0);
  protected readonly message = signal<string | null>(null);
  protected readonly creating = signal(false);

  protected readonly ctxTableId = signal<number | null>(null);
  protected readonly ctxSessionId = signal<number | null>(null);
  private readonly ctxCustomerId = signal<number | null>(null);

  protected readonly deliveryForm = this.fb.nonNullable.group({
    deliveryAddress: ['', [Validators.required, Validators.maxLength(255)]],
    estimatedTime: [this.defaultEta(), Validators.required],
    driverId: [0, [Validators.required, Validators.min(1)]],
  });

  // La app es zoneless: se refleja la validez del form reactivo en una señal.
  private readonly deliveryValid = toSignal(
    this.deliveryForm.statusChanges.pipe(map((s) => s === 'VALID')),
    { initialValue: this.deliveryForm.valid },
  );

  protected canCreate(): boolean {
    return this.channel() !== 'DELIVERY' || !!this.ctxTableId() || this.deliveryValid();
  }

  constructor() {
    const q = this.route.snapshot.queryParamMap;
    const tableId = q.get('tableId');
    if (tableId) {
      this.ctxTableId.set(Number(tableId));
      this.channel.set('MESA');
      if (q.get('sessionId')) {
        this.ctxSessionId.set(Number(q.get('sessionId')));
      }
      if (q.get('customerId')) {
        this.ctxCustomerId.set(Number(q.get('customerId')));
      }
    }
    this.deliveryApi.listDrivers().subscribe({ next: (p) => this.drivers.set(p.items) });
    this.customersApi.listCustomers().subscribe({ next: (p) => this.customers.set(p.items) });
    this.reload();
  }

  private defaultEta(): string {
    const t = new Date(Date.now() + 45 * 60_000);
    t.setSeconds(0, 0);
    return `${t.getFullYear()}-${pad(t.getMonth() + 1)}-${pad(t.getDate())}T${pad(t.getHours())}:${pad(t.getMinutes())}`;
  }

  protected label(c: string): string {
    return { MESA: 'Mesa', BARRA: 'Barra', TAKEAWAY: 'Para llevar', DELIVERY: 'Delivery' }[c] ?? c;
  }

  reload(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.api.listOrders({ date: today }).subscribe({
      next: (p) => this.openOrders.set(p.items.filter((o) => o.status === 'OPEN' || o.status === 'PAID')),
      error: (err) => this.message.set(apiErrorMessage(err)),
    });
  }

  protected create(): void {
    if (!this.canCreate()) {
      return;
    }
    this.creating.set(true);
    this.message.set(null);

    const isDelivery = this.channel() === 'DELIVERY' && !this.ctxTableId();
    const dv = this.deliveryForm.getRawValue();

    this.api
      .createOrder({
        channel: this.channel(),
        tableId: this.ctxTableId(),
        tableSessionId: this.ctxSessionId(),
        customerId: this.ctxCustomerId() ?? (this.selectedCustomerId() || null),
        deliveryAddress: isDelivery ? dv.deliveryAddress : null,
        estimatedTime: isDelivery ? dv.estimatedTime : null,
        driverId: isDelivery ? Number(dv.driverId) : null,
      })
      .subscribe({
        next: ({ id }) => this.router.navigate(['/pos/order', id]),
        error: (err) => {
          this.creating.set(false);
          this.message.set(apiErrorMessage(err));
        },
      });
  }
}

function pad(n: number): string {
  return String(n).padStart(2, '0');
}
