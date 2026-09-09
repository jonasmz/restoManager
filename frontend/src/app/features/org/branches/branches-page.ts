import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';
import { Branch, Restaurant } from '../org.models';

@Component({
  selector: 'app-org-branches',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Sucursales</h1>
        <p class="text-secondary mb-0">Puntos de venta de la empresa.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()">
        <i class="ti ti-plus me-1"></i>Nueva
      </button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead>
            <tr><th>Nombre</th><th>Dirección</th><th>Horario</th><th class="text-end">Acción</th></tr>
          </thead>
          <tbody>
            @for (b of rows(); track b.id) {
              <tr>
                <td>{{ b.name }}</td>
                <td class="text-secondary">{{ b.address }}</td>
                <td class="text-secondary">{{ b.openingTime }} – {{ b.closingTime }}</td>
                <td class="text-end">
                  <button type="button" class="btn btn-light btn-sm" (click)="openEdit(b)">
                    <i class="ti ti-edit"></i>
                  </button>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-center text-secondary py-4">Sin sucursales.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar sucursal' : 'Nueva sucursal' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-6">
              <label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" />
            </div>
            <div class="col-md-6">
              <label class="form-label" for="restaurantId">Empresa</label>
              <select id="restaurantId" class="form-select" formControlName="restaurantId">
                @for (r of restaurants(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
              </select>
            </div>
            <div class="col-12">
              <label class="form-label" for="address">Dirección</label>
              <input id="address" class="form-control" formControlName="address" />
            </div>
            <div class="col-md-6"><label class="form-label" for="phone">Teléfono</label>
              <input id="phone" class="form-control" formControlName="phone" /></div>
            <div class="col-md-6"><label class="form-label" for="email">Correo</label>
              <input id="email" class="form-control" formControlName="email" /></div>
            <div class="col-md-3"><label class="form-label" for="openingTime">Apertura</label>
              <input id="openingTime" type="time" class="form-control" formControlName="openingTime" /></div>
            <div class="col-md-3"><label class="form-label" for="closingTime">Cierre</label>
              <input id="closingTime" type="time" class="form-control" formControlName="closingTime" /></div>
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
export class BranchesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);

  protected readonly rows = signal<Branch[]>([]);
  protected readonly restaurants = signal<Restaurant[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<BranchesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
    this.api.listRestaurants().subscribe((p) => this.restaurants.set(p.items));
  }

  private reload(): void {
    this.api.listBranches().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      restaurantId: [this.restaurants()[0]?.id ?? 1, Validators.required],
      name: ['', Validators.required],
      address: ['', Validators.required],
      phone: [''],
      email: [''],
      openingTime: ['08:00', Validators.required],
      closingTime: ['23:00', Validators.required],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(b: Branch): void {
    this.editingId = b.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({
      restaurantId: b.restaurantId,
      name: b.name,
      address: b.address,
      phone: b.phone,
      email: b.email,
      openingTime: b.openingTime.slice(0, 5),
      closingTime: b.closingTime.slice(0, 5),
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
      .saveBranch(
        {
          restaurantId: Number(v.restaurantId),
          name: v.name,
          address: v.address,
          phone: v.phone,
          email: v.email,
          openingTime: `${v.openingTime}:00`,
          closingTime: `${v.closingTime}:00`,
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
