import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgApexchartsModule } from 'ng-apexcharts';

import { HttpErrorResponse } from '@angular/common/http';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { barChart, CHART_COLORS } from '../reports/chart-theme';
import { ReportsApiService } from '../reports/reports-api.service';
import { CHANNEL_LABELS, DashboardPayload } from '../reports/reports.models';

const RANGES = [
  { label: '7 días', days: 7 },
  { label: '30 días', days: 30 },
  { label: '90 días', days: 90 },
] as const;

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, NgApexchartsModule, CurrencyPipe, DecimalPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
      <div>
        <h1 class="fs-3 mb-1">Panel</h1>
        <p class="text-secondary mb-0">
          {{ branch.activeBranch()?.name ?? 'Sucursal activa' }} · últimos {{ range().days }} días.
          Ventas efectivas (pagadas o cerradas).
        </p>
      </div>
      <div class="btn-group btn-group-sm">
        @for (r of ranges; track r.days) {
          <button type="button" class="btn" [class]="range().days === r.days ? 'btn-primary' : 'btn-outline-primary'"
            (click)="setRange(r)">{{ r.label }}</button>
        }
      </div>
    </div>

    @if (restricted()) {
      <div class="alert alert-info py-2 small">El panel de métricas está disponible para administración y encargados.</div>
    } @else if (error()) {
      <div class="alert alert-danger py-2 small">{{ error() }}</div>
    }

    @if (data(); as d) {
      <!-- KPIs -->
      <div class="row g-3 mb-4">
        <div class="col-lg-3 col-6">
          <div class="card p-4 bg-primary bg-opacity-10 border border-primary border-opacity-25 rounded-2 h-100">
            <div class="d-flex gap-3">
              <div class="icon-shape icon-md bg-primary text-white rounded-2"><i class="ti ti-report-money fs-4"></i></div>
              <div><h2 class="mb-2 fs-6">Ventas</h2><h3 class="fw-bold mb-0">{{ d.kpis.sales | currency }}</h3></div>
            </div>
          </div>
        </div>
        <div class="col-lg-3 col-6">
          <div class="card p-4 bg-success bg-opacity-10 border border-success border-opacity-25 rounded-2 h-100">
            <div class="d-flex gap-3">
              <div class="icon-shape icon-md bg-success text-white rounded-2"><i class="ti ti-truck-delivery fs-4"></i></div>
              <div><h2 class="mb-2 fs-6">Compras</h2><h3 class="fw-bold mb-0">{{ d.kpis.purchases | currency }}</h3></div>
            </div>
          </div>
        </div>
        <div class="col-lg-3 col-6">
          <div class="card p-4 bg-info bg-opacity-10 border border-info border-opacity-25 rounded-2 h-100">
            <div class="d-flex gap-3">
              <div class="icon-shape icon-md bg-info text-white rounded-2"><i class="ti ti-receipt fs-4"></i></div>
              <div><h2 class="mb-2 fs-6">Pedidos</h2><h3 class="fw-bold mb-0">{{ d.kpis.orders | number }}</h3></div>
            </div>
          </div>
        </div>
        <div class="col-lg-3 col-6">
          <div class="card p-4 bg-warning bg-opacity-10 border border-warning border-opacity-25 rounded-2 h-100">
            <div class="d-flex gap-3">
              <div class="icon-shape icon-md bg-warning text-white rounded-2"><i class="ti ti-calculator fs-4"></i></div>
              <div><h2 class="mb-2 fs-6">Ticket promedio</h2><h3 class="fw-bold mb-0">{{ d.kpis.averageTicket | currency }}</h3></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Gráficas -->
      <div class="row g-3 mb-4">
        <div class="col-lg-7">
          <div class="card h-100">
            <div class="card-header bg-transparent px-4 py-3"><h3 class="h5 mb-0">Ventas vs Compras</h3></div>
            <div class="card-body">
              @if (d.salesVsPurchase.length) {
                <apx-chart [series]="svp().series" [chart]="svp().chart" [colors]="svp().colors"
                  [xaxis]="svp().xaxis" [yaxis]="svp().yaxis" [dataLabels]="svp().dataLabels"
                  [legend]="svp().legend" [grid]="svp().grid" [stroke]="svp().stroke"
                  [plotOptions]="svp().plotOptions" [tooltip]="svp().tooltip" />
              } @else { <p class="text-secondary small mb-0">Sin datos en el período.</p> }
            </div>
          </div>
        </div>
        <div class="col-lg-5">
          <div class="card h-100">
            <div class="card-header bg-transparent px-4 py-3"><h3 class="h5 mb-0">Ventas por canal</h3></div>
            <div class="card-body d-flex align-items-center justify-content-center">
              @if (channelSeries().length) {
                <apx-chart [series]="channelSeries()" [labels]="channelLabels()"
                  [chart]="{ type: 'donut', height: 300, fontFamily: 'inherit' }"
                  [colors]="donutColors" [legend]="{ position: 'bottom' }"
                  [dataLabels]="{ enabled: true, formatter: pct }" />
              } @else { <p class="text-secondary small mb-0">Sin ventas en el período.</p> }
            </div>
          </div>
        </div>
      </div>

      <!-- Listas -->
      <div class="row g-3">
        <div class="col-lg-4">
          <div class="card h-100">
            <div class="card-header bg-white px-4 py-3"><h4 class="mb-0 h5">Más vendidos</h4></div>
            <ul class="list-group list-group-flush">
              @for (p of d.topSelling; track p.menuItemId) {
                <li class="list-group-item d-flex justify-content-between align-items-center">
                  <span class="small">{{ p.name }}</span>
                  <span class="text-end">
                    <span class="d-block small fw-semibold">{{ p.amount | currency }}</span>
                    <span class="d-block text-secondary" style="font-size:.75rem">{{ p.units }} u.</span>
                  </span>
                </li>
              } @empty { <li class="list-group-item text-secondary small py-4 text-center">Sin ventas.</li> }
            </ul>
          </div>
        </div>
        <div class="col-lg-4">
          <div class="card h-100">
            <div class="card-header bg-white px-4 py-3 d-flex justify-content-between align-items-center">
              <h4 class="mb-0 h5">Bajo stock</h4>
              @if (d.kpis.lowStockCount) { <span class="badge bg-danger">{{ d.kpis.lowStockCount }}</span> }
            </div>
            <ul class="list-group list-group-flush">
              @for (s of d.lowStock; track s.ingredientId) {
                <li class="list-group-item d-flex justify-content-between align-items-center">
                  <span class="small">{{ s.name }}</span>
                  <span class="small text-danger">{{ s.stockQuantity | number: '1.0-2' }} / {{ s.reorderPoint | number: '1.0-2' }} {{ s.unit }}</span>
                </li>
              } @empty { <li class="list-group-item text-secondary small py-4 text-center">Todo por encima del punto de reposición.</li> }
            </ul>
          </div>
        </div>
        <div class="col-lg-4">
          <div class="card h-100">
            <div class="card-header bg-white px-4 py-3"><h4 class="mb-0 h5">Ventas recientes</h4></div>
            <ul class="list-group list-group-flush">
              @for (o of d.recentSales; track o.orderId) {
                <li class="list-group-item d-flex justify-content-between align-items-center">
                  <span class="small">
                    <a [routerLink]="['/pos/order', o.orderId]">#{{ o.orderId }}</a>
                    <span class="text-secondary"> · {{ channelLabel(o.channel) }} · {{ o.orderTime | date: 'short' }}</span>
                  </span>
                  <span class="small fw-semibold">{{ o.totalAmount | currency }}</span>
                </li>
              } @empty { <li class="list-group-item text-secondary small py-4 text-center">Sin ventas.</li> }
            </ul>
          </div>
        </div>
      </div>
    } @else if (!error()) {
      <p class="text-secondary">Cargando panel…</p>
    }
  `,
})
export class Dashboard {
  private readonly api = inject(ReportsApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly ranges = RANGES;
  protected readonly range = signal<(typeof RANGES)[number]>(RANGES[1]);
  protected readonly data = signal<DashboardPayload | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly restricted = signal(false);

  protected readonly donutColors = [
    CHART_COLORS.sales, CHART_COLORS.green, '#3b82f6', CHART_COLORS.greenDark, '#f59e0b',
  ];
  protected readonly pct = (v: number) => `${v.toFixed(0)}%`;

  protected readonly channelSeries = computed(() => (this.data()?.salesByChannel ?? []).map((b) => b.totalSales));
  protected readonly channelLabels = computed(() => (this.data()?.salesByChannel ?? []).map((b) => b.label));

  protected readonly svp = computed(() => {
    const points = this.data()?.salesVsPurchase ?? [];
    return barChart(
      points.map((p) => this.dayLabel(p.date)),
      [
        { name: 'Ventas', data: points.map((p) => p.sales) },
        { name: 'Compras', data: points.map((p) => p.purchases) },
      ],
      [CHART_COLORS.salesLight, CHART_COLORS.sales],
    );
  });

  constructor() {
    this.load();
  }

  protected setRange(r: (typeof RANGES)[number]): void {
    this.range.set(r);
    this.load();
  }

  protected channelLabel(c: string): string {
    return CHANNEL_LABELS[c] ?? c;
  }

  private dayLabel(iso: string): string {
    const [, m, d] = iso.split('-');
    return `${d}/${m}`;
  }

  private load(): void {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - this.range().days);
    this.error.set(null);
    this.api.dashboard({ from: from.toISOString(), to: to.toISOString() }).subscribe({
      next: (d) => {
        this.restricted.set(false);
        this.data.set(d);
      },
      error: (err) => {
        if (err instanceof HttpErrorResponse && err.status === 403) {
          this.restricted.set(true);
        } else {
          this.error.set(apiErrorMessage(err));
        }
      },
    });
  }
}
