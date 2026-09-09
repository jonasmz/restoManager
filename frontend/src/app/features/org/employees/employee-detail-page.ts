import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';
import { Employee, Leave, LEAVE_TYPES, Shift } from '../org.models';

@Component({
  selector: 'app-org-employee-detail',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/org/employees" class="small text-secondary"><i class="ti ti-arrow-left me-1"></i>Empleados</a>

    @if (error()) { <div class="alert alert-danger py-2 small mt-3">{{ error() }}</div> }

    @if (employee(); as e) {
      <h1 class="fs-3 mt-2 mb-4">{{ e.firstName }} {{ e.lastName }}</h1>

      <div class="row g-4">
        <!-- Turnos -->
        <div class="col-lg-6">
          <div class="card h-100">
            <div class="card-body">
              <h2 class="fs-6 mb-3">Turnos</h2>
              <ul class="list-group list-group-flush mb-3">
                @for (s of shifts(); track s.id) {
                  <li class="list-group-item px-0 d-flex justify-content-between align-items-center">
                    <span class="small">{{ s.startTime }} → {{ s.endTime }} ({{ s.scheduledHours }} h)</span>
                    <button type="button" class="btn btn-light btn-sm" (click)="removeShift(s.id)"><i class="ti ti-trash"></i></button>
                  </li>
                } @empty { <li class="list-group-item px-0 text-secondary small">Sin turnos.</li> }
              </ul>
              <form [formGroup]="shiftForm" (ngSubmit)="addShift()" class="row g-2">
                <div class="col-5"><input type="datetime-local" class="form-control form-control-sm" formControlName="startTime" /></div>
                <div class="col-5"><input type="datetime-local" class="form-control form-control-sm" formControlName="endTime" /></div>
                <div class="col-2"><input type="number" min="0" step="0.5" class="form-control form-control-sm" formControlName="scheduledHours" placeholder="h" /></div>
                <div class="col-12"><button type="submit" class="btn btn-primary btn-sm" [disabled]="shiftForm.invalid">Añadir turno</button></div>
              </form>
            </div>
          </div>
        </div>

        <!-- Ausencias -->
        <div class="col-lg-6">
          <div class="card h-100">
            <div class="card-body">
              <h2 class="fs-6 mb-3">Ausencias</h2>
              <ul class="list-group list-group-flush mb-3">
                @for (l of leaves(); track l.id) {
                  <li class="list-group-item px-0">
                    <div class="d-flex justify-content-between align-items-center">
                      <span class="small">{{ l.startDate }} → {{ l.endDate }} · {{ l.leaveType }}</span>
                      <span class="badge" [class]="badge(l.status)">{{ l.status }}</span>
                    </div>
                    @if (l.status === 'REQUESTED') {
                      <div class="mt-1 d-flex gap-1">
                        <button type="button" class="btn btn-success btn-sm py-0" (click)="leaveAction(l.id, 'approve')">Aprobar</button>
                        <button type="button" class="btn btn-outline-danger btn-sm py-0" (click)="leaveAction(l.id, 'reject')">Rechazar</button>
                      </div>
                    }
                    @if (l.status === 'APPROVED') {
                      <button type="button" class="btn btn-light btn-sm py-0 mt-1" (click)="leaveAction(l.id, 'cancel')">Cancelar</button>
                    }
                  </li>
                } @empty { <li class="list-group-item px-0 text-secondary small">Sin ausencias.</li> }
              </ul>
              <form [formGroup]="leaveForm" (ngSubmit)="requestLeave()" class="row g-2">
                <div class="col-4"><input type="date" class="form-control form-control-sm" formControlName="startDate" /></div>
                <div class="col-4"><input type="date" class="form-control form-control-sm" formControlName="endDate" /></div>
                <div class="col-4">
                  <select class="form-select form-select-sm" formControlName="leaveType">
                    @for (t of leaveTypes; track t) { <option [value]="t">{{ t }}</option> }
                  </select>
                </div>
                <div class="col-12"><button type="submit" class="btn btn-primary btn-sm" [disabled]="leaveForm.invalid">Solicitar ausencia</button></div>
              </form>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class EmployeeDetailPage implements OnInit {
  readonly id = input.required<string>();

  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);

  protected readonly leaveTypes = LEAVE_TYPES;
  protected readonly employee = signal<Employee | null>(null);
  protected readonly shifts = signal<Shift[]>([]);
  protected readonly leaves = signal<Leave[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly shiftForm = this.fb.nonNullable.group({
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    scheduledHours: [8, [Validators.required, Validators.min(0.01)]],
  });
  protected readonly leaveForm = this.fb.nonNullable.group({
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    leaveType: ['VACATION', Validators.required],
  });

  private get employeeId(): number {
    return Number(this.id());
  }

  ngOnInit(): void {
    this.api.getEmployee(this.employeeId).subscribe({
      next: (e) => this.employee.set(e),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
    this.reloadShifts();
    this.reloadLeaves();
  }

  protected badge(status: string): string {
    switch (status) {
      case 'APPROVED': return 'bg-success-subtle text-success';
      case 'REJECTED': return 'bg-danger-subtle text-danger';
      case 'CANCELLED': return 'bg-secondary-subtle text-secondary';
      default: return 'bg-warning-subtle text-warning';
    }
  }

  private reloadShifts(): void {
    this.api.listShifts(this.employeeId).subscribe((s) => this.shifts.set(s));
  }
  private reloadLeaves(): void {
    this.api.listLeaves(this.employeeId).subscribe((l) => this.leaves.set(l));
  }

  protected addShift(): void {
    if (this.shiftForm.invalid) {
      return;
    }
    const v = this.shiftForm.getRawValue();
    this.error.set(null);
    this.api
      .addShift(this.employeeId, { startTime: v.startTime, endTime: v.endTime, scheduledHours: Number(v.scheduledHours) })
      .subscribe({ next: () => this.reloadShifts(), error: (err) => this.error.set(apiErrorMessage(err)) });
  }

  protected removeShift(shiftId: number): void {
    this.api.removeShift(this.employeeId, shiftId).subscribe({
      next: () => this.reloadShifts(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  protected requestLeave(): void {
    if (this.leaveForm.invalid) {
      return;
    }
    const v = this.leaveForm.getRawValue();
    this.error.set(null);
    this.api
      .requestLeave(this.employeeId, { startDate: v.startDate, endDate: v.endDate, leaveType: v.leaveType })
      .subscribe({ next: () => this.reloadLeaves(), error: (err) => this.error.set(apiErrorMessage(err)) });
  }

  protected leaveAction(leaveId: number, action: 'approve' | 'reject' | 'cancel'): void {
    this.api.leaveAction(this.employeeId, leaveId, action).subscribe({
      next: () => this.reloadLeaves(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
