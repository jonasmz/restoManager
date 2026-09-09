import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from './inventory-api.service';
import { Ingredient } from './inventory.models';

@Component({
  selector: 'app-inv-ingredients',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Ingredientes</h1>
        <p class="text-secondary mb-0">Catálogo de insumos (sin stock; el stock es por sucursal).</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nuevo</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Unidad</th><th>Precio unit.</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (i of rows(); track i.id) {
              <tr>
                <td>{{ i.name }}</td>
                <td class="text-secondary">{{ i.unit }}</td>
                <td>{{ i.unitPrice }}</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(i)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin ingredientes.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar ingrediente' : 'Nuevo ingrediente' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" /></div>
            <div class="col-md-3"><label class="form-label" for="unit">Unidad</label>
              <input id="unit" class="form-control" formControlName="unit" placeholder="kg, l, u…" /></div>
            <div class="col-md-4"><label class="form-label" for="unitPrice">Precio unitario</label>
              <input id="unitPrice" type="number" step="0.01" min="0" class="form-control" formControlName="unitPrice" /></div>
            <div class="col-12 d-flex gap-2">
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar</button>
              <button type="button" class="btn btn-light" (click)="form = null">Cancelar</button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class IngredientsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);

  protected readonly rows = signal<Ingredient[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<IngredientsPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listIngredients().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      unit: ['', Validators.required],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(i: Ingredient): void {
    this.editingId = i.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue(i);
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api.saveIngredient({ name: v.name, unit: v.unit, unitPrice: Number(v.unitPrice) }, this.editingId ?? undefined).subscribe({
      next: () => {
        this.saving.set(false);
        this.form = null;
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err));
      },
    });
  }
}
