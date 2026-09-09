import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { SalesApiService } from './sales-api.service';
import { ORDER_CHANNELS, ORDER_STATUSES, Order } from './sales.models';

@Component({
  selector: 'app-sales-orders',
  imports: [RouterLink, DecimalPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-4">
      <div>
        <h1 class="fs-3 mb-1">Pedidos</h1>
        <p class="text-secondary mb-0">Historial de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.</p>
      </div>
      <a routerLink="/pos" class="btn btn-primary btn-sm"><i class="ti ti-plus me-1"></i>Nuevo pedido</a>
    </div>

    @if (message()) { <div class="alert alert-danger py-2 small">{{ message() }}</div> }

    <div class="card">
      <div class="card-body pb-0 d-flex flex-wrap gap-3 align-items-end">
        <div>
          <label class="form-label small" for="date">Día</label>
          <input id="date" type="date" class="form-control form-control-sm" [value]="date()"
            (change)="date.set($any($event.target).value); reload()" />
        </div>
        <div>
          <label class="form-label small" for="channel">Canal</label>
          <select id="channel" class="form-select form-select-sm" [value]="channel()"
            (change)="channel.set($any($event.target).value); reload()">
            <option value="">Todos</option>
            @for (c of channels; track c) { <option [value]="c">{{ c }}</option> }
          </select>
        </div>
        <div>
          <label class="form-label small" for="status">Estado</label>
          <select id="status" class="form-select form-select-sm" [value]="status()"
            (change)="status.set($any($event.target).value); reload()">
            <option value="">Todos</option>
            @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
          </select>
        </div>
      </div>
      <div class="table-responsive mt-2">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>#</th><th>Hora</th><th>Canal</th><th>Estado</th><th class="text-end">Total</th><th class="text-end">Pagado</th><th></th></tr></thead>
          <tbody>
            @for (o of rows(); track o.id) {
              <tr>
                <td>{{ o.id }}</td>
                <td class="small">{{ o.orderTime | date: 'short' }}</td>
                <td class="small">{{ o.channel }}@if (o.tableId) { <span class="text-secondary"> · #{{ o.tableId }}</span> }</td>
                <td><span class="badge" [class]="badge(o.status)">{{ o.status }}</span></td>
                <td class="text-end small">{{ o.totalAmount | number: '1.2-2' }}</td>
                <td class="text-end small">{{ o.confirmedPaid | number: '1.2-2' }}</td>
                <td class="text-end"><a class="btn btn-light btn-sm" [routerLink]="['/pos/order', o.id]">Ver</a></td>
              </tr>
            } @empty { <tr><td colspan="7" class="text-center text-secondary py-4">Sin pedidos.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class OrdersPage {
  private readonly api = inject(SalesApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly channels = ORDER_CHANNELS;
  protected readonly statuses = ORDER_STATUSES;

  protected readonly rows = signal<Order[]>([]);
  protected readonly message = signal<string | null>(null);
  protected readonly date = signal(new Date().toISOString().slice(0, 10));
  protected readonly channel = signal('');
  protected readonly status = signal('');

  constructor() {
    this.reload();
  }

  protected badge(s: string): string {
    switch (s) {
      case 'OPEN': return 'bg-primary-subtle text-primary';
      case 'PAID': return 'bg-success-subtle text-success';
      case 'CLOSED': return 'bg-secondary-subtle text-secondary';
      default: return 'bg-danger-subtle text-danger';
    }
  }

  reload(): void {
    this.api
      .listOrders({ date: this.date(), channel: this.channel(), status: this.status() })
      .subscribe({
        next: (p) => this.rows.set(p.items),
        error: (err) => this.message.set(apiErrorMessage(err)),
      });
  }
}
