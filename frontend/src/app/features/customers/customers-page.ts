import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { apiErrorMessage } from '../../core/http/api-error';
import { CustomersApiService } from './customers-api.service';
import { Customer } from './customers.models';

@Component({
  selector: 'app-customers',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Clientes</h1>
        <p class="text-secondary mb-0">Identidad de negocio: puntos de fidelidad, gift cards e historial. Catálogo global.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nuevo</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="card-body">
        <input class="form-control" placeholder="Buscar por nombre, apellido o teléfono…"
          [value]="search()" (input)="onSearch($any($event.target).value)" />
      </div>
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Cliente</th><th>Contacto</th><th class="text-end">Puntos</th><th class="text-end">Ficha</th></tr></thead>
          <tbody>
            @for (c of rows(); track c.id) {
              <tr>
                <td>{{ c.lastName }}, {{ c.firstName }}</td>
                <td class="small text-secondary">{{ c.phone }}@if (c.email) { · {{ c.email }} }</td>
                <td class="text-end"><span class="badge bg-primary-subtle text-primary">{{ c.loyaltyPoints }}</span></td>
                <td class="text-end">
                  <a class="btn btn-light btn-sm" [routerLink]="['/customers', c.id]">Abrir</a>
                  <button type="button" class="btn btn-light btn-sm ms-1" (click)="openEdit(c)"><i class="ti ti-edit"></i></button>
                </td>
              </tr>
            } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin clientes.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar cliente' : 'Nuevo cliente' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-3"><label class="form-label" for="fn">Nombre</label>
              <input id="fn" class="form-control" formControlName="firstName" /></div>
            <div class="col-md-3"><label class="form-label" for="ln">Apellido</label>
              <input id="ln" class="form-control" formControlName="lastName" /></div>
            <div class="col-md-3"><label class="form-label" for="ph">Teléfono</label>
              <input id="ph" class="form-control" formControlName="phone" /></div>
            <div class="col-md-3"><label class="form-label" for="em">Email</label>
              <input id="em" type="email" class="form-control" formControlName="email" /></div>
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
export class CustomersPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CustomersApiService);

  protected readonly rows = signal<Customer[]>([]);
  protected readonly search = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<CustomersPage['buildForm']> | null = null;
  protected editingId: number | null = null;
  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listCustomers(this.search() || undefined).subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.reload(), 250);
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      phone: ['', [Validators.required, Validators.maxLength(20)]],
      email: ['', [Validators.email, Validators.maxLength(100)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(c: Customer): void {
    this.editingId = c.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({ firstName: c.firstName, lastName: c.lastName, phone: c.phone, email: c.email });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api.saveCustomer(this.form.getRawValue(), this.editingId ?? undefined).subscribe({
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
