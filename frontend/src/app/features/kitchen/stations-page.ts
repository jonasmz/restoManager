import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { MenuApiService } from '../menu/menu-api.service';
import { KitchenStation, MenuItem } from '../menu/menu.models';

@Component({
  selector: 'app-kitchen-stations',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Estaciones de cocina</h1>
        <p class="text-secondary mb-0">
          Estaciones de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong> y los platos que preparan.
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Descripción</th><th>Platos</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (s of rows(); track s.id) {
              <tr>
                <td>{{ s.name }}</td>
                <td class="text-secondary">{{ s.description }}</td>
                <td class="text-secondary small">{{ s.menuItemIds.length }} asignado(s)</td>
                <td class="text-end">
                  <button type="button" class="btn btn-light btn-sm me-1" (click)="openEdit(s)"><i class="ti ti-edit"></i></button>
                  <button type="button" class="btn btn-light btn-sm" (click)="openItems(s)"><i class="ti ti-tools-kitchen-2"></i> Platos</button>
                </td>
              </tr>
            } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin estaciones.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card mb-4">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar estación' : 'Nueva estación' }}</h2>
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

    @if (itemsFor()) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">Platos de «{{ itemsFor()!.name }}»</h2>
          <div class="row">
            @for (m of menuItems(); track m.id) {
              <div class="col-md-6">
                <div class="form-check">
                  <input type="checkbox" class="form-check-input" [id]="'mi' + m.id"
                    [checked]="picked().has(m.id)" (change)="togglePick(m.id)" />
                  <label class="form-check-label" [for]="'mi' + m.id">{{ m.name }}</label>
                </div>
              </div>
            } @empty { <p class="text-secondary small">No hay platos en la carta.</p> }
          </div>
          <div class="d-flex gap-2 mt-3">
            <button type="button" class="btn btn-primary" [disabled]="saving()" (click)="saveItems()">Guardar platos</button>
            <button type="button" class="btn btn-light" (click)="itemsFor.set(null)">Cerrar</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class StationsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MenuApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly rows = signal<KitchenStation[]>([]);
  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<StationsPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  protected readonly itemsFor = signal<KitchenStation | null>(null);
  protected readonly picked = signal<Set<number>>(new Set());

  constructor() {
    this.api.listMenuItems().subscribe((p) => this.menuItems.set(p.items));
    this.reload();
  }

  private reload(): void {
    this.api.listStations().subscribe({
      next: (s) => this.rows.set(s),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
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

  protected openEdit(s: KitchenStation): void {
    this.editingId = s.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({ name: s.name, description: s.description });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api.saveStation({ name: v.name, description: v.description }, this.editingId ?? undefined).subscribe({
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

  protected openItems(s: KitchenStation): void {
    this.error.set(null);
    this.itemsFor.set(s);
    this.picked.set(new Set(s.menuItemIds));
  }

  protected togglePick(id: number): void {
    const next = new Set(this.picked());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.picked.set(next);
  }

  protected saveItems(): void {
    const s = this.itemsFor();
    if (!s) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api.setStationMenuItems(s.id, [...this.picked()]).subscribe({
      next: () => {
        this.saving.set(false);
        this.itemsFor.set(null);
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err));
      },
    });
  }
}
