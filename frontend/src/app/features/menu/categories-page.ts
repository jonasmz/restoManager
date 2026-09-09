import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { MenuApiService } from './menu-api.service';
import { Category } from './menu.models';

@Component({
  selector: 'app-menu-categories',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Categorías</h1>
        <p class="text-secondary mb-0">Agrupan los platos de la carta (catálogo global).</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Descripción</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (c of rows(); track c.id) {
              <tr>
                <td>{{ c.name }}</td>
                <td class="text-secondary">{{ c.description }}</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(c)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="3" class="text-center text-secondary py-4">Sin categorías.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar categoría' : 'Nueva categoría' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5"><label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" /></div>
            <div class="col-md-7"><label class="form-label" for="description">Descripción</label>
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
export class CategoriesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MenuApiService);

  protected readonly rows = signal<Category[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<CategoriesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.api.listCategories().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      description: [''],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(c: Category): void {
    this.editingId = c.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue(c);
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api.saveCategory({ name: v.name, description: v.description }, this.editingId ?? undefined).subscribe({
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
