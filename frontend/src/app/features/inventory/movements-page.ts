import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from './inventory-api.service';
import { Ingredient, Movement, MOVEMENT_TYPES } from './inventory.models';

@Component({
  selector: 'app-inv-movements',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6">
      <h1 class="fs-3 mb-1">Movimientos de inventario</h1>
      <p class="text-secondary mb-0">
        <a routerLink="/inventory" class="small">Existencias</a> ·
        Libro de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong> (§12.3).
      </p>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="card-body">
        <form [formGroup]="filter" (ngSubmit)="apply()" class="row g-2 align-items-end">
          <div class="col-md-3">
            <label class="form-label small" for="ingredientId">Ingrediente</label>
            <select id="ingredientId" class="form-select form-select-sm" formControlName="ingredientId">
              <option value="">Todos</option>
              @for (i of ingredients(); track i.id) { <option [value]="i.id">{{ i.name }}</option> }
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label small" for="type">Tipo</label>
            <select id="type" class="form-select form-select-sm" formControlName="type">
              <option value="">Todos</option>
              @for (t of types; track t) { <option [value]="t">{{ t }}</option> }
            </select>
          </div>
          <div class="col-md-2"><label class="form-label small" for="from">Desde</label>
            <input id="from" type="date" class="form-control form-control-sm" formControlName="from" /></div>
          <div class="col-md-2"><label class="form-label small" for="to">Hasta</label>
            <input id="to" type="date" class="form-control form-control-sm" formControlName="to" /></div>
          <div class="col-md-2"><button type="submit" class="btn btn-primary btn-sm">Filtrar</button></div>
        </form>
      </div>
    </div>

    <div class="card">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Fecha</th><th>Ingrediente</th><th>Tipo</th><th class="text-end">Cantidad</th><th>Referencia</th></tr></thead>
          <tbody>
            @for (m of rows(); track m.id) {
              <tr>
                <td class="text-secondary small">{{ m.movementTime | date: 'short' }}</td>
                <td>{{ ingredientName(m.ingredientId) }}</td>
                <td><span class="badge" [class]="badge(m.movementType)">{{ m.movementType }}</span></td>
                <td class="text-end" [class.text-danger]="m.quantity < 0" [class.text-success]="m.quantity > 0">{{ m.quantity }}</td>
                <td class="text-secondary small">{{ m.referenceType }}{{ m.referenceId ? ' #' + m.referenceId : '' }}</td>
              </tr>
            } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin movimientos.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class MovementsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly types = MOVEMENT_TYPES;
  protected readonly rows = signal<Movement[]>([]);
  protected readonly ingredients = signal<Ingredient[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly filter = this.fb.nonNullable.group({
    ingredientId: [''],
    type: [''],
    from: [''],
    to: [''],
  });

  constructor() {
    this.api.listIngredients().subscribe((p) => this.ingredients.set(p.items));
    this.apply();
  }

  protected ingredientName(id: number): string {
    return this.ingredients().find((i) => i.id === id)?.name ?? `#${id}`;
  }

  protected badge(type: string): string {
    switch (type) {
      case 'PURCHASE': return 'bg-success-subtle text-success';
      case 'SALE': return 'bg-primary-subtle text-primary';
      case 'WASTE': return 'bg-danger-subtle text-danger';
      default: return 'bg-secondary-subtle text-secondary';
    }
  }

  protected apply(): void {
    const v = this.filter.getRawValue();
    this.error.set(null);
    this.api
      .movements({
        ingredientId: v.ingredientId ? Number(v.ingredientId) : undefined,
        type: v.type || undefined,
        from: v.from || undefined,
        to: v.to || undefined,
      })
      .subscribe({
        next: (m) => this.rows.set(m),
        error: (err) => this.error.set(apiErrorMessage(err)),
      });
  }
}
