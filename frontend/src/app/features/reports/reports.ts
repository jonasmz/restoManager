import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { NgApexchartsModule } from 'ng-apexcharts';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { ExportColumn } from '../../core/export/table-export';

/** Tabla de reporte lista para mostrar y exportar (columnas y filas ya resueltas). */
interface ReportTable {
  title: string;
  file: string;
  columns: ExportColumn<unknown>[];
  rows: readonly unknown[];
}

const erase = <T>(columns: ExportColumn<T>[]): ExportColumn<unknown>[] =>
  columns as unknown as ExportColumn<unknown>[];
import { apiErrorMessage } from '../../core/http/api-error';
import { areaChart, CHART_COLORS } from './chart-theme';
import { ExportButtons } from './export-buttons';
import { DateRange, ReportsApiService } from './reports-api.service';
import {
  DiscountApplied, METHOD_LABELS, PaymentMethodTotal, PurchasingCost,
  SalesBucket, SalesSummary, TableTurnover, TopProduct,
} from './reports.models';

@Component({
  selector: 'app-reports',
  imports: [FormsModule, NgApexchartsModule, ExportButtons, CurrencyPipe, DecimalPipe],
  providers: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-end gap-3 mb-4">
      <div>
        <h1 class="fs-3 mb-1">Reportes</h1>
        <p class="text-secondary mb-0">
          {{ branch.activeBranch()?.name ?? 'Sucursal activa' }} · ventas efectivas (pagadas o cerradas).
        </p>
      </div>
      <div class="d-flex flex-wrap align-items-end gap-2">
        <div>
          <label class="form-label small mb-1" for="from">Desde</label>
          <input id="from" type="date" class="form-control form-control-sm" [(ngModel)]="fromDate" />
        </div>
        <div>
          <label class="form-label small mb-1" for="to">Hasta</label>
          <input id="to" type="date" class="form-control form-control-sm" [(ngModel)]="toDate" />
        </div>
        <button type="button" class="btn btn-primary btn-sm" (click)="reload()">Aplicar</button>
        <div class="btn-group btn-group-sm">
          <button type="button" class="btn btn-outline-secondary" (click)="preset(7)">7d</button>
          <button type="button" class="btn btn-outline-secondary" (click)="preset(30)">30d</button>
          <button type="button" class="btn btn-outline-secondary" (click)="preset(90)">90d</button>
        </div>
      </div>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    @if (summary(); as s) {
      <div class="row g-3 mb-4">
        <div class="col-lg-3 col-6"><div class="card h-100"><div class="card-body">
          <p class="text-secondary small mb-1">Ventas</p><h3 class="fw-bold h4 mb-0">{{ s.totalSales | currency }}</h3>
        </div></div></div>
        <div class="col-lg-3 col-6"><div class="card h-100"><div class="card-body">
          <p class="text-secondary small mb-1">Pedidos</p><h3 class="fw-bold h4 mb-0">{{ s.orderCount | number }}</h3>
          <span class="small text-secondary">{{ s.paidCount }} pagados · {{ s.closedCount }} cerrados</span>
        </div></div></div>
        <div class="col-lg-3 col-6"><div class="card h-100"><div class="card-body">
          <p class="text-secondary small mb-1">Ticket promedio</p><h3 class="fw-bold h4 mb-0">{{ s.averageTicket | currency }}</h3>
        </div></div></div>
        <div class="col-lg-3 col-6"><div class="card h-100"><div class="card-body">
          <p class="text-secondary small mb-1">Descuentos</p><h3 class="fw-bold h4 mb-0">{{ s.discountTotal | currency }}</h3>
          <span class="small text-secondary">Compras: {{ purchasing()?.totalCost ?? 0 | currency }}</span>
        </div></div></div>
      </div>

      <div class="card mb-4">
        <div class="card-header bg-transparent px-4 py-3"><h3 class="h5 mb-0">Ventas por día</h3></div>
        <div class="card-body">
          @if (byDay().length) {
            <apx-chart [series]="dayChart().series" [chart]="dayChart().chart" [colors]="dayChart().colors"
              [xaxis]="dayChart().xaxis" [yaxis]="dayChart().yaxis" [dataLabels]="dayChart().dataLabels"
              [legend]="dayChart().legend" [grid]="dayChart().grid" [stroke]="dayChart().stroke"
              [fill]="dayChart().fill" [tooltip]="dayChart().tooltip" />
          } @else { <p class="text-secondary small mb-0">Sin ventas en el período.</p> }
        </div>
      </div>

      <div class="row g-3">
        @for (t of tables(); track t.title) {
          <div class="col-lg-6">
            <div class="card h-100">
              <div class="card-header bg-white px-4 py-3 d-flex justify-content-between align-items-center">
                <h4 class="mb-0 h6">{{ t.title }}</h4>
                <app-export-buttons [name]="t.file" [sheet]="t.title" [columns]="t.columns" [rows]="t.rows" />
              </div>
              <div class="table-responsive">
                <table class="table table-sm align-middle mb-0">
                  <thead><tr>
                    @for (c of t.columns; track c.header) { <th [class.text-end]="!$first">{{ c.header }}</th> }
                  </tr></thead>
                  <tbody>
                    @for (row of t.rows; track $index) {
                      <tr>
                        @for (c of t.columns; track c.header) {
                          <td class="small" [class.text-end]="!$first">{{ c.value(row) }}</td>
                        }
                      </tr>
                    } @empty {
                      <tr><td [attr.colspan]="t.columns.length" class="text-center text-secondary py-4">Sin datos.</td></tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        }
      </div>
    } @else if (!error()) {
      <p class="text-secondary">Cargando reportes…</p>
    }
  `,
})
export class Reports {
  private readonly api = inject(ReportsApiService);
  protected readonly branch = inject(BranchContextService);
  private readonly money = inject(CurrencyPipe);

  protected fromDate = isoDate(daysAgo(30));
  protected toDate = isoDate(new Date());

  protected readonly error = signal<string | null>(null);
  protected readonly summary = signal<SalesSummary | null>(null);
  protected readonly purchasing = signal<PurchasingCost | null>(null);
  protected readonly byChannel = signal<SalesBucket[]>([]);
  protected readonly byEmployee = signal<SalesBucket[]>([]);
  protected readonly byCategory = signal<SalesBucket[]>([]);
  protected readonly byDay = signal<SalesBucket[]>([]);
  protected readonly top = signal<TopProduct[]>([]);
  protected readonly payments = signal<PaymentMethodTotal[]>([]);
  protected readonly discounts = signal<DiscountApplied[]>([]);
  protected readonly turnover = signal<TableTurnover[]>([]);

  private m(v: number): string {
    return this.money.transform(v, 'ARS', 'symbol', '1.2-2', 'es-AR') ?? String(v);
  }

  protected readonly dayChart = computed(() => {
    const rows = this.byDay();
    return areaChart(
      rows.map((r) => r.label),
      [{ name: 'Ventas', data: rows.map((r) => r.totalSales) }],
      [CHART_COLORS.sales],
    );
  });

  /** Definición declarativa de cada tabla + sus columnas de exportación. */
  protected readonly tables = computed<ReportTable[]>(() => {
    const bucketCols = (labelHeader: string): ExportColumn<SalesBucket>[] => [
      { header: labelHeader, value: (r) => r.label },
      { header: 'Pedidos', value: (r) => r.orderCount },
      { header: 'Ventas', value: (r) => this.m(r.totalSales) },
    ];
    return [
      {
        title: 'Ventas por canal', file: 'ventas-por-canal',
        columns: erase(bucketCols('Canal')), rows: this.byChannel(),
      },
      {
        title: 'Ventas por empleado', file: 'ventas-por-empleado',
        columns: erase(bucketCols('Empleado')), rows: this.byEmployee(),
      },
      {
        title: 'Ventas por categoría', file: 'ventas-por-categoria',
        columns: erase(bucketCols('Categoría')), rows: this.byCategory(),
      },
      {
        title: 'Productos más vendidos', file: 'productos-top',
        columns: erase<TopProduct>([
          { header: 'Producto', value: (r) => r.name },
          { header: 'Unidades', value: (r) => r.units },
          { header: 'Monto', value: (r) => this.m(r.amount) },
        ]),
        rows: this.top(),
      },
      {
        title: 'Pagos por método', file: 'pagos-por-metodo',
        columns: erase<PaymentMethodTotal>([
          { header: 'Método', value: (r) => METHOD_LABELS[r.method] ?? r.method },
          { header: 'Cantidad', value: (r) => r.count },
          { header: 'Monto', value: (r) => this.m(r.amount) },
        ]),
        rows: this.payments(),
      },
      {
        title: 'Descuentos aplicados', file: 'descuentos-aplicados',
        columns: erase<DiscountApplied>([
          { header: 'Descuento', value: (r) => r.name },
          { header: 'Veces', value: (r) => r.timesApplied },
          { header: 'Total', value: (r) => this.m(r.totalAmount) },
        ]),
        rows: this.discounts(),
      },
      {
        title: 'Rotación de mesas', file: 'rotacion-mesas',
        columns: erase<TableTurnover>([
          { header: 'Mesa', value: (r) => r.tableNumber },
          { header: 'Sesiones', value: (r) => r.sessions },
          { header: 'Duración media (min)', value: (r) => r.avgDurationMinutes },
          { header: 'Comensales medios', value: (r) => r.avgGuests },
        ]),
        rows: this.turnover(),
      },
    ];
  });

  constructor() {
    this.reload();
  }

  protected preset(days: number): void {
    this.fromDate = isoDate(daysAgo(days));
    this.toDate = isoDate(new Date());
    this.reload();
  }

  protected reload(): void {
    const r: DateRange = {
      from: new Date(this.fromDate + 'T00:00:00').toISOString(),
      to: new Date(this.toDate + 'T23:59:59').toISOString(),
    };
    this.error.set(null);
    forkJoin({
      summary: this.api.salesSummary(r),
      purchasing: this.api.purchasingCost(r),
      byChannel: this.api.salesBy('channel', r),
      byEmployee: this.api.salesBy('employee', r),
      byCategory: this.api.salesBy('category', r),
      byDay: this.api.salesBy('day', r),
      top: this.api.topProducts(r, 10),
      payments: this.api.paymentsByMethod(r),
      discounts: this.api.discountsApplied(r),
      turnover: this.api.tableTurnover(r),
    }).subscribe({
      next: (x) => {
        this.summary.set(x.summary);
        this.purchasing.set(x.purchasing);
        this.byChannel.set(x.byChannel);
        this.byEmployee.set(x.byEmployee);
        this.byCategory.set(x.byCategory);
        this.byDay.set(x.byDay);
        this.top.set(x.top);
        this.payments.set(x.payments);
        this.discounts.set(x.discounts);
        this.turnover.set(x.turnover);
      },
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}

function daysAgo(n: number): Date {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d;
}

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}
