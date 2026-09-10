import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { DeliveryApiService, DeliveryStep } from './delivery-api.service';
import { Delivery, DeliveryStatus, Driver } from './delivery.models';

interface Column {
  title: string;
  statuses: DeliveryStatus[];
  accent: string;
}

@Component({
  selector: 'app-delivery-board',
  imports: [RouterLink, DatePipe, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-4">
      <div>
        <h1 class="fs-3 mb-1">Despacho de delivery</h1>
        <p class="text-secondary mb-0">
          Entregas de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
          Se refresca solo cada 20 s.
        </p>
      </div>
      <button type="button" class="btn btn-light btn-sm" (click)="reload()"><i class="ti ti-refresh me-1"></i>Refrescar</button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="row g-3">
      @for (col of columns; track col.title) {
        <div class="col-12 col-lg-3">
          <div class="card h-100">
            <div class="card-header fw-semibold d-flex justify-content-between" [class]="col.accent">
              <span>{{ col.title }}</span>
              <span class="badge bg-white text-dark">{{ inColumn(col).length }}</span>
            </div>
            <div class="card-body d-flex flex-column gap-2 p-2">
              @for (d of inColumn(col); track d.id) {
                <div class="border rounded p-2">
                  <div class="d-flex justify-content-between align-items-start">
                    <a [routerLink]="['/pos/order', d.orderId]" class="fw-semibold small text-decoration-none">
                      Pedido #{{ d.orderId }}
                    </a>
                    <span class="badge" [class]="orderBadge(d.orderStatus)">{{ d.orderStatus }}</span>
                  </div>
                  <div class="small text-secondary text-truncate">{{ d.deliveryAddress }}</div>
                  <div class="small text-secondary">
                    <i class="ti ti-clock me-1"></i>{{ d.estimatedTime | date: 'short' }}
                    @if (d.actualTime) { · real {{ d.actualTime | date: 'shortTime' }} }
                  </div>
                  <div class="small text-secondary">
                    <i class="ti ti-motorbike me-1"></i>{{ driverLabel(d.driverId) }} ·
                    {{ d.orderTotal | currency }}
                  </div>

                  @if (assigningId() === d.id) {
                    <div class="d-flex gap-1 mt-2">
                      <select class="form-select form-select-sm" [value]="pickedDriver()"
                        (change)="pickedDriver.set($any($event.target).value)">
                        <option value="">Repartidor…</option>
                        @for (dr of drivers(); track dr.id) {
                          <option [value]="dr.id">{{ dr.licensePlate }} ({{ vehicle(dr.vehicleType) }})</option>
                        }
                      </select>
                      <button type="button" class="btn btn-primary btn-sm" (click)="confirmAssign(d)"
                        [disabled]="!pickedDriver()">OK</button>
                      <button type="button" class="btn btn-link btn-sm p-0" (click)="assigningId.set(null)">✕</button>
                    </div>
                  } @else {
                    <div class="d-flex flex-wrap gap-1 mt-2">
                      @if (d.status === 'PENDING' || d.status === 'ASSIGNED' || d.status === 'FAILED') {
                        <button type="button" class="btn btn-light btn-sm" (click)="startAssign(d)">
                          {{ d.status === 'PENDING' ? 'Asignar' : 'Reasignar' }}
                        </button>
                      }
                      @if (d.status === 'ASSIGNED') {
                        <button type="button" class="btn btn-primary btn-sm" (click)="advance(d, 'in-transit')">En camino</button>
                      }
                      @if (d.status === 'IN_TRANSIT') {
                        <button type="button" class="btn btn-success btn-sm" (click)="advance(d, 'delivered')">Entregada</button>
                      }
                      @if (d.status === 'ASSIGNED' || d.status === 'IN_TRANSIT') {
                        <button type="button" class="btn btn-outline-warning btn-sm" (click)="advance(d, 'failed')">Fallida</button>
                      }
                      @if (d.status !== 'DELIVERED' && d.status !== 'CANCELLED') {
                        <button type="button" class="btn btn-outline-danger btn-sm" (click)="cancel(d)">Cancelar</button>
                      }
                    </div>
                  }
                </div>
              } @empty {
                <div class="text-center text-secondary small py-3">—</div>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class BoardPage {
  private readonly api = inject(DeliveryApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly branch = inject(BranchContextService);

  protected readonly deliveries = signal<Delivery[]>([]);
  protected readonly drivers = signal<Driver[]>([]);
  protected readonly assigningId = signal<number | null>(null);
  protected readonly pickedDriver = signal<string>('');
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected readonly columns: Column[] = [
    { title: 'Pendientes', statuses: ['PENDING'], accent: 'bg-secondary-subtle' },
    { title: 'Asignadas', statuses: ['ASSIGNED'], accent: 'bg-primary-subtle' },
    { title: 'En camino', statuses: ['IN_TRANSIT'], accent: 'bg-info-subtle' },
    { title: 'Cerradas', statuses: ['DELIVERED', 'FAILED', 'CANCELLED'], accent: 'bg-light' },
  ];

  private readonly driverById = computed(() => new Map(this.drivers().map((d) => [d.id, d])));

  constructor() {
    this.api.listDrivers().subscribe({ next: (p) => this.drivers.set(p.items) });
    this.reload();
    interval(20_000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());
  }

  protected inColumn(col: Column): Delivery[] {
    return this.deliveries().filter((d) => col.statuses.includes(d.status));
  }

  protected driverLabel(id: number): string {
    return this.driverById().get(id)?.licensePlate ?? `#${id}`;
  }
  protected vehicle(v: string): string {
    return { MOTORCYCLE: 'Moto', BICYCLE: 'Bici', CAR: 'Auto', ON_FOOT: 'A pie' }[v] ?? v;
  }
  protected orderBadge(s: string): string {
    switch (s) {
      case 'OPEN': return 'bg-primary-subtle text-primary';
      case 'PAID': return 'bg-success-subtle text-success';
      case 'CLOSED': return 'bg-secondary-subtle text-secondary';
      default: return 'bg-danger-subtle text-danger';
    }
  }

  reload(): void {
    this.api.listDeliveries().subscribe({
      next: (d) => this.deliveries.set(d),
      error: (err) => this.fail(err),
    });
  }

  protected startAssign(d: Delivery): void {
    this.message.set(null);
    this.pickedDriver.set(String(d.driverId));
    this.assigningId.set(d.id);
  }

  protected confirmAssign(d: Delivery): void {
    const driverId = Number(this.pickedDriver());
    if (!driverId) {
      return;
    }
    this.assigningId.set(null);
    this.api.assignDriver(d.id, driverId).subscribe({
      next: () => this.done('Repartidor asignado.'),
      error: (err) => this.fail(err),
    });
  }

  protected advance(d: Delivery, step: DeliveryStep): void {
    this.api.advance(d.id, step).subscribe({
      next: () => this.done('Entrega actualizada.'),
      error: (err) => this.fail(err),
    });
  }

  protected cancel(d: Delivery): void {
    this.api.cancelDelivery(d.id).subscribe({
      next: () => this.done(`Entrega #${d.id} cancelada (también el pedido #${d.orderId}).`),
      error: (err) => this.fail(err),
    });
  }

  private done(msg: string): void {
    this.ok.set(true);
    this.message.set(msg);
    this.reload();
  }
  private fail(err: unknown): void {
    this.ok.set(false);
    this.message.set(apiErrorMessage(err));
  }
}
