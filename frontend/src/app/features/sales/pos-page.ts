import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { SalesApiService } from './sales-api.service';
import { ORDER_CHANNELS, Order, OrderChannel } from './sales.models';

@Component({
  selector: 'app-sales-pos',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, DatePipe],
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
              <p class="text-secondary small">Para un pedido de mesa, ve al tablero y usa «Abrir cuenta».</p>
            }

            <button type="button" class="btn btn-primary w-100" (click)="create()" [disabled]="creating()">
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
                    <td class="text-end small">{{ o.totalAmount | number: '1.2-2' }}</td>
                    <td class="text-end small">{{ o.balance | number: '1.2-2' }}</td>
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
  private readonly api = inject(SalesApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly channels = ORDER_CHANNELS;
  protected readonly channel = signal<OrderChannel>('BARRA');
  protected readonly openOrders = signal<Order[]>([]);
  protected readonly message = signal<string | null>(null);
  protected readonly creating = signal(false);

  protected readonly ctxTableId = signal<number | null>(null);
  protected readonly ctxSessionId = signal<number | null>(null);
  private readonly ctxCustomerId = signal<number | null>(null);

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
    this.reload();
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
    this.creating.set(true);
    this.message.set(null);
    this.api
      .createOrder({
        channel: this.channel(),
        tableId: this.ctxTableId(),
        tableSessionId: this.ctxSessionId(),
        customerId: this.ctxCustomerId(),
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
