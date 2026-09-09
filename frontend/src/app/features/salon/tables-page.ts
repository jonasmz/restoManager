import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { SalonApiService } from './salon-api.service';
import { Table, TABLE_OPERATIONAL_STATUSES, TableOperationalStatus } from './salon.models';

@Component({
  selector: 'app-salon-tables',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Mesas</h1>
        <p class="text-secondary mb-0">
          Mesas de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
          El estado operativo es lo único que se guarda (TBL-01).
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva</button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Número</th><th>Capacidad</th><th style="width: 14rem">Estado operativo</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (t of rows(); track t.id) {
              <tr>
                <td>#{{ t.number }}</td>
                <td class="text-secondary">{{ t.capacity }} pers.</td>
                <td>
                  <select class="form-select form-select-sm" [value]="t.operationalStatus"
                    (change)="changeStatus(t, $any($event.target).value)">
                    @for (s of statuses; track s) { <option [value]="s">{{ label(s) }}</option> }
                  </select>
                </td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(t)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin mesas.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar mesa' : 'Nueva mesa' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-3"><label class="form-label" for="number">Número</label>
              <input id="number" type="number" min="1" class="form-control" formControlName="number" /></div>
            <div class="col-md-3"><label class="form-label" for="capacity">Capacidad</label>
              <input id="capacity" type="number" min="1" class="form-control" formControlName="capacity" /></div>
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
export class TablesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalonApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly statuses = TABLE_OPERATIONAL_STATUSES;
  protected readonly rows = signal<Table[]>([]);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected form: ReturnType<TablesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
  }

  protected label(s: TableOperationalStatus): string {
    return s === 'ACTIVE' ? 'Activa' : s === 'CLEANING' ? 'Limpieza' : 'Fuera de servicio';
  }

  private reload(): void {
    this.api.listTables().subscribe({
      next: (t) => this.rows.set(t),
      error: (err) => this.fail(err),
    });
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      number: [1, [Validators.required, Validators.min(1)]],
      capacity: [2, [Validators.required, Validators.min(1)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.message.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(t: Table): void {
    this.editingId = t.id;
    this.message.set(null);
    this.form = this.buildForm();
    this.form.patchValue({ number: t.number, capacity: t.capacity });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.message.set(null);
    this.api.saveTable({ number: Number(v.number), capacity: Number(v.capacity) }, this.editingId ?? undefined).subscribe({
      next: () => {
        this.saving.set(false);
        this.form = null;
        this.ok.set(true);
        this.message.set('Mesa guardada.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.fail(err);
      },
    });
  }

  protected changeStatus(t: Table, status: TableOperationalStatus): void {
    this.message.set(null);
    this.api.setTableStatus(t.id, status).subscribe({
      next: () => {
        this.ok.set(true);
        this.message.set(`Mesa #${t.number}: estado ${this.label(status)}.`);
        this.reload();
      },
      error: (err) => {
        this.fail(err);
        this.reload();
      },
    });
  }

  private fail(err: unknown): void {
    this.ok.set(false);
    this.message.set(apiErrorMessage(err));
  }
}
