import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6">
      <h1 class="fs-3 mb-1">Panel</h1>
      <p class="text-secondary mb-0">Indicadores y gráficas — se implementa en la Fase 9 del roadmap.</p>
    </div>

    <div class="row g-3">
      @for (kpi of placeholders; track kpi.label) {
        <div class="col-lg-3 col-12">
          <div
            class="card p-4 rounded-2 border"
            [class]="'bg-' + kpi.tone + ' bg-opacity-10 border-' + kpi.tone + ' border-opacity-25'"
          >
            <div class="d-flex gap-3">
              <div class="icon-shape icon-md rounded-2 text-white" [class]="'bg-' + kpi.tone">
                <i class="ti fs-4" [class]="kpi.icon"></i>
              </div>
              <div>
                <h2 class="mb-2 fs-6">{{ kpi.label }}</h2>
                <h3 class="fw-bold mb-0">—</h3>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class Dashboard {
  protected readonly placeholders = [
    { label: 'Ventas', tone: 'primary', icon: 'ti-report-analytics' },
    { label: 'Compras', tone: 'success', icon: 'ti-repeat' },
    { label: 'Inventario', tone: 'info', icon: 'ti-box-seam' },
    { label: 'Pedidos', tone: 'warning', icon: 'ti-receipt' },
  ];
}
