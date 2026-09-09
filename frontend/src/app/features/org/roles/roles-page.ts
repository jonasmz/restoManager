import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';
import { Role } from '../org.models';

@Component({
  selector: 'app-org-roles',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Puestos</h1>
        <p class="text-secondary mb-0">Puestos de trabajo y tarifa horaria.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()">
        <i class="ti ti-plus me-1"></i>Nuevo
      </button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Descripción</th><th>Tarifa/h</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (r of rows(); track r.id) {
              <tr>
                <td>{{ r.name }}</td>
                <td class="text-secondary">{{ r.description }}</td>
                <td>{{ r.hourlyRate }}</td>
                <td class="text-end">
                  <button type="button" class="btn btn-light btn-sm" (click)="openEdit(r)"><i class="ti ti-edit"></i></button>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-center text-secondary py-4">Sin puestos.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar puesto' : 'Nuevo puesto' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" /></div>
            <div class="col-md-4"><label class="form-label" for="hourlyRate">Tarifa/h</label>
              <input id="hourlyRate" type="number" step="0.01" min="0" class="form-control" formControlName="hourlyRate" /></div>
            <div class="col-12"><label class="form-label" for="description">Descripción</label>
              <input id="description" class="form-control" formControlName="description" /></div>
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
export class RolesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);

  protected readonly rows = signal<Role[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<RolesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listRoles().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      description: [''],
      hourlyRate: [0, [Validators.required, Validators.min(0)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(r: Role): void {
    this.editingId = r.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue(r);
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .saveRole({ name: v.name, description: v.description, hourlyRate: Number(v.hourlyRate) }, this.editingId ?? undefined)
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
