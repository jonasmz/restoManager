import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-inventory',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6">
      <h1 class="fs-3 mb-1">Inventario</h1>
      <p class="text-secondary mb-0">Saldo por sucursal y movimientos — se implementa en la Fase 3 del roadmap.</p>
    </div>
    <div class="card">
      <div class="card-body text-secondary">Próximamente.</div>
    </div>
  `,
})
export class Inventory {}
