import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { SalesApiService } from './sales-api.service';
import { DISCOUNT_TYPES, Discount, DiscountType } from './sales.models';

@Component({
  selector: 'app-sales-discounts',
  imports: [ReactiveFormsModule, DecimalPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Descuentos</h1>
        <p class="text-secondary mb-0">
          Catálogo global. Porcentual sobre el subtotal de ítems o importe fijo; el
          importe aplicado se congela al añadirlo a un pedido.
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nuevo</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Tipo</th><th class="text-end">Valor</th><th>Vigencia</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (d of rows(); track d.id) {
              <tr>
                <td>{{ d.name }}</td>
                <td class="small">{{ d.type === 'PERCENTAGE' ? 'Porcentaje' : 'Importe fijo' }}</td>
                <td class="text-end">{{ d.value | number: '1.2-2' }}{{ d.type === 'PERCENTAGE' ? ' %' : '' }}</td>
                <td class="small text-secondary">{{ d.startDate | date: 'shortDate' }} – {{ d.endDate | date: 'shortDate' }}</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(d)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin descuentos.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar descuento' : 'Nuevo descuento' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" placeholder="Happy Hour" /></div>
            <div class="col-md-3"><label class="form-label" for="type">Tipo</label>
              <select id="type" class="form-select" formControlName="type">
                <option value="PERCENTAGE">Porcentaje</option>
                <option value="FIXED_AMOUNT">Importe fijo</option>
              </select></div>
            <div class="col-md-4"><label class="form-label" for="value">Valor</label>
              <input id="value" type="number" step="0.01" min="0.01" class="form-control" formControlName="value" /></div>
            <div class="col-md-4"><label class="form-label" for="start">Desde</label>
              <input id="start" type="date" class="form-control" formControlName="startDate" /></div>
            <div class="col-md-4"><label class="form-label" for="end">Hasta</label>
              <input id="end" type="date" class="form-control" formControlName="endDate" /></div>
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
export class DiscountsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalesApiService);

  protected readonly rows = signal<Discount[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly types = DISCOUNT_TYPES;
  protected form: ReturnType<DiscountsPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listDiscounts().subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  private buildForm() {
    const today = new Date().toISOString().slice(0, 10);
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      type: ['PERCENTAGE' as DiscountType, Validators.required],
      value: [0, [Validators.required, Validators.min(0.01)]],
      startDate: [today, Validators.required],
      endDate: [today, Validators.required],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(d: Discount): void {
    this.editingId = d.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({
      name: d.name,
      type: d.type,
      value: d.value,
      startDate: d.startDate.slice(0, 10),
      endDate: d.endDate.slice(0, 10),
    });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .saveDiscount(
        {
          name: v.name,
          type: v.type,
          value: Number(v.value),
          startDate: v.startDate,
          endDate: v.endDate,
        },
        this.editingId ?? undefined,
      )
      .subscribe({
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
