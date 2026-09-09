import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from '../inventory/inventory-api.service';
import { Supplier } from '../inventory/inventory.models';

@Component({
  selector: 'app-buy-suppliers',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Proveedores</h1>
        <p class="text-secondary mb-0">Abastecimiento de materias primas.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nuevo</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Contacto</th><th>Teléfono</th><th>Correo</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (s of rows(); track s.id) {
              <tr>
                <td>{{ s.name }}</td>
                <td class="text-secondary">{{ s.contactName }}</td>
                <td class="text-secondary">{{ s.phone }}</td>
                <td class="text-secondary">{{ s.email }}</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(s)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin proveedores.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar proveedor' : 'Nuevo proveedor' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-6"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" /></div>
            <div class="col-md-6"><label class="form-label" for="contactName">Contacto</label>
              <input id="contactName" class="form-control" formControlName="contactName" /></div>
            <div class="col-md-4"><label class="form-label" for="phone">Teléfono</label>
              <input id="phone" class="form-control" formControlName="phone" /></div>
            <div class="col-md-4"><label class="form-label" for="email">Correo</label>
              <input id="email" class="form-control" formControlName="email" /></div>
            <div class="col-md-4"><label class="form-label" for="address">Dirección</label>
              <input id="address" class="form-control" formControlName="address" /></div>
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
export class SuppliersPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);

  protected readonly rows = signal<Supplier[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<SuppliersPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listSuppliers().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      contactName: [''],
      phone: [''],
      email: [''],
      address: [''],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(s: Supplier): void {
    this.editingId = s.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue(s);
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api.saveSupplier(this.form.getRawValue(), this.editingId ?? undefined).subscribe({
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
