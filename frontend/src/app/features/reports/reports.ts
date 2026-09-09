import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-reports',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6">
      <h1 class="fs-3 mb-1">Reportes</h1>
      <p class="text-secondary mb-0">Analítica de ventas y operación — se implementa en la Fase 9 del roadmap.</p>
    </div>
    <div class="card">
      <div class="card-body text-secondary">Próximamente.</div>
    </div>
  `,
})
export class Reports {}
