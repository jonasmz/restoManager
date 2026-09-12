import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthAdminApiService } from '../auth-admin-api.service';
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

      <div class="card mb-4">
        <div class="card-body">
          <h2 class="fs-6 mb-3">Acceso al sistema</h2>
          @if (accessError()) { <div class="alert alert-danger py-2 small">{{ accessError() }}</div> }

          @if (e.userId) {
            <p class="small text-secondary mb-2"><i class="ti ti-lock-check me-1 text-success"></i>Tiene acceso.</p>
            <button type="button" class="btn btn-outline-danger btn-sm" (click)="unlinkAccess()">Quitar acceso</button>
          } @else if (pendingUserId(); as uid) {
            <p class="small text-warning mb-2">
              El login se creó, pero no se pudo vincular al empleado.
            </p>
            <button type="button" class="btn btn-primary btn-sm" (click)="retryLink(uid)">Reintentar vínculo</button>
          } @else {
            <form [formGroup]="accessForm" (ngSubmit)="createAccess()" class="row g-2">
              <div class="col-md-4">
                <input type="email" class="form-control form-control-sm" formControlName="email" placeholder="Email de login" />
              </div>
              <div class="col-md-3">
                <input type="password" class="form-control form-control-sm" formControlName="password" placeholder="Contraseña" />
              </div>
              <div class="col-md-3">
                <select class="form-select form-select-sm" formControlName="role">
                  <option value="" disabled>Rol de acceso</option>
                  @for (r of authRoles(); track r) { <option [value]="r">{{ r }}</option> }
                </select>
              </div>
              <div class="col-md-2">
                <button type="submit" class="btn btn-primary btn-sm w-100" [disabled]="accessForm.invalid || creatingAccess()">
                  Crear acceso
                </button>
              </div>
            </form>
          }
        </div>
      </div>

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
  private readonly authApi = inject(AuthAdminApiService);

  protected readonly leaveTypes = LEAVE_TYPES;
  protected readonly employee = signal<Employee | null>(null);
  protected readonly shifts = signal<Shift[]>([]);
  protected readonly leaves = signal<Leave[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly authRoles = signal<string[]>([]);
  protected readonly accessError = signal<string | null>(null);
  protected readonly creatingAccess = signal(false);
  /** Id del AppUser creado en Auth cuando el link a Business falló, para poder reintentarlo. */
  protected readonly pendingUserId = signal<number | null>(null);

  protected readonly accessForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['', Validators.required],
  });

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
    this.authApi.listRoles().subscribe({ next: (roles) => this.authRoles.set(roles) });
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

  protected createAccess(): void {
    const e = this.employee();
    if (this.accessForm.invalid || !e) {
      return;
    }
    const v = this.accessForm.getRawValue();
    this.accessError.set(null);
    this.creatingAccess.set(true);
    this.authApi
      .createUser({ email: v.email, password: v.password, role: v.role, employeeId: e.id, branchIds: [e.branchId] })
      .subscribe({
        next: (created) => {
          this.creatingAccess.set(false);
          this.retryLink(created.id);
        },
        error: (err) => {
          this.creatingAccess.set(false);
          this.accessError.set(apiErrorMessage(err));
        },
      });
  }

  protected retryLink(userId: number): void {
    this.accessError.set(null);
    this.api.linkEmployeeUser(this.employeeId, userId).subscribe({
      next: () => {
        this.pendingUserId.set(null);
        this.api.getEmployee(this.employeeId).subscribe((e) => this.employee.set(e));
      },
      error: (err) => {
        this.pendingUserId.set(userId);
        this.accessError.set(apiErrorMessage(err));
      },
    });
  }

  protected unlinkAccess(): void {
    this.accessError.set(null);
    this.api.unlinkEmployeeUser(this.employeeId).subscribe({
      next: () => this.api.getEmployee(this.employeeId).subscribe((e) => this.employee.set(e)),
      error: (err) => this.accessError.set(apiErrorMessage(err)),
    });
  }
}
