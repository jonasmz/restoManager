import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { MenuApiService } from '../menu/menu-api.service';
import { TaxRate } from '../menu/menu.models';

@Component({
  selector: 'app-fiscal-tax-rates',
  imports: [ReactiveFormsModule, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Tasas de impuesto</h1>
        <p class="text-secondary mb-0">
          Porcentaje aplicado a los platos. Los impuestos son <strong>inclusivos</strong>:
          el precio del plato ya los contiene (§7.5).
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th class="text-end">Tasa</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (t of rows(); track t.id) {
              <tr>
                <td>{{ t.name }}</td>
                <td class="text-end">{{ t.rate | number: '1.2-4' }} %</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(t)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="3" class="text-center text-secondary py-4">Sin tasas.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar tasa' : 'Nueva tasa' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-6"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" placeholder="IVA 21 %" /></div>
            <div class="col-md-3"><label class="form-label" for="rate">Tasa (%)</label>
              <input id="rate" type="number" step="0.01" min="0" max="100" class="form-control" formControlName="rate" /></div>
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
export class TaxRatesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MenuApiService);

  protected readonly rows = signal<TaxRate[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<TaxRatesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listTaxRates().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      rate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(t: TaxRate): void {
    this.editingId = t.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue(t);
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api.saveTaxRate({ name: v.name, rate: Number(v.rate) }, this.editingId ?? undefined).subscribe({
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
