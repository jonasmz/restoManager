import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { apiErrorMessage } from '../../core/http/api-error';
import { OrgApiService } from '../org/org-api.service';
import { Employee } from '../org/org.models';
import { DeliveryApiService } from './delivery-api.service';
import { Driver, VEHICLE_TYPES, VehicleType } from './delivery.models';

@Component({
  selector: 'app-delivery-drivers',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Repartidores</h1>
        <p class="text-secondary mb-0">Vinculados a un empleado. Vehículo y patente para el canal DELIVERY.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nuevo</button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Empleado</th><th>Vehículo</th><th>Patente</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (d of rows(); track d.id) {
              <tr>
                <td>{{ employeeName(d.employeeId) }}</td>
                <td class="small">{{ vehicle(d.vehicleType) }}</td>
                <td class="small text-secondary">{{ d.licensePlate }}</td>
                <td class="text-end"><button type="button" class="btn btn-light btn-sm" (click)="openEdit(d)"><i class="ti ti-edit"></i></button></td>
              </tr>
            } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin repartidores.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar repartidor' : 'Nuevo repartidor' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5">
              <label class="form-label" for="emp">Empleado</label>
              <select id="emp" class="form-select" formControlName="employeeId">
                <option [value]="0" disabled>Selecciona…</option>
                @for (e of employees(); track e.id) { <option [value]="e.id">{{ e.lastName }}, {{ e.firstName }}</option> }
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label" for="vt">Vehículo</label>
              <select id="vt" class="form-select" formControlName="vehicleType">
                @for (v of vehicleTypes; track v) { <option [value]="v">{{ vehicle(v) }}</option> }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label" for="plate">Patente</label>
              <input id="plate" class="form-control" formControlName="licensePlate" placeholder="ABC 123" />
            </div>
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
export class DriversPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(DeliveryApiService);
  private readonly org = inject(OrgApiService);

  protected readonly rows = signal<Driver[]>([]);
  protected readonly employees = signal<Employee[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly vehicleTypes = VEHICLE_TYPES;
  protected form: ReturnType<DriversPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  private readonly employeeById = computed(() => new Map(this.employees().map((e) => [e.id, e])));

  constructor() {
    this.org.listEmployees().subscribe({ next: (p) => this.employees.set(p.items) });
    this.reload();
  }

  private reload(): void {
    this.api.listDrivers().subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  protected employeeName(id: number): string {
    const e = this.employeeById().get(id);
    return e ? `${e.lastName}, ${e.firstName}` : `#${id}`;
  }
  protected vehicle(v: string): string {
    return { MOTORCYCLE: 'Motocicleta', BICYCLE: 'Bicicleta', CAR: 'Automóvil', ON_FOOT: 'A pie' }[v] ?? v;
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      employeeId: [0, [Validators.required, Validators.min(1)]],
      vehicleType: ['MOTORCYCLE' as VehicleType, Validators.required],
      licensePlate: ['', [Validators.required, Validators.maxLength(20)]],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(d: Driver): void {
    this.editingId = d.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({ employeeId: d.employeeId, vehicleType: d.vehicleType, licensePlate: d.licensePlate });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .saveDriver(
        { employeeId: Number(v.employeeId), vehicleType: v.vehicleType, licensePlate: v.licensePlate },
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
