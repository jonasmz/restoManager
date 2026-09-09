import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { BranchContextService } from '../../../core/branch/branch-context.service';
import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';
import { Department, Employee, Role } from '../org.models';

@Component({
  selector: 'app-org-employees',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Empleados</h1>
        <p class="text-secondary mb-0">Personal de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()">
        <i class="ti ti-plus me-1"></i>Nuevo
      </button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Nombre</th><th>Correo</th><th>Departamento</th><th>Puesto</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (e of rows(); track e.id) {
              <tr>
                <td><a [routerLink]="['/org/employees', e.id]">{{ e.firstName }} {{ e.lastName }}</a></td>
                <td class="text-secondary">{{ e.email }}</td>
                <td class="text-secondary">{{ deptName(e.departmentId) }}</td>
                <td class="text-secondary">{{ roleName(e.roleId) }}</td>
                <td class="text-end">
                  <button type="button" class="btn btn-light btn-sm" (click)="openEdit(e)"><i class="ti ti-edit"></i></button>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="5" class="text-center text-secondary py-4">Sin empleados.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar empleado' : 'Nuevo empleado' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-6"><label class="form-label" for="firstName">Nombre</label>
              <input id="firstName" class="form-control" formControlName="firstName" /></div>
            <div class="col-md-6"><label class="form-label" for="lastName">Apellido</label>
              <input id="lastName" class="form-control" formControlName="lastName" /></div>
            <div class="col-md-6"><label class="form-label" for="email">Correo</label>
              <input id="email" class="form-control" formControlName="email" /></div>
            <div class="col-md-6"><label class="form-label" for="phone">Teléfono</label>
              <input id="phone" class="form-control" formControlName="phone" /></div>
            <div class="col-md-4"><label class="form-label" for="departmentId">Departamento</label>
              <select id="departmentId" class="form-select" formControlName="departmentId">
                @for (d of departments(); track d.id) { <option [value]="d.id">{{ d.name }}</option> }
              </select></div>
            <div class="col-md-4"><label class="form-label" for="roleId">Puesto</label>
              <select id="roleId" class="form-select" formControlName="roleId">
                @for (r of roles(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
              </select></div>
            <div class="col-md-4"><label class="form-label" for="hireDate">Ingreso</label>
              <input id="hireDate" type="date" class="form-control" formControlName="hireDate" /></div>
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
export class EmployeesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly rows = signal<Employee[]>([]);
  protected readonly departments = signal<Department[]>([]);
  protected readonly roles = signal<Role[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<EmployeesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  constructor() {
    this.reload();
    forkJoin({ d: this.api.listDepartments(), r: this.api.listRoles() }).subscribe({
      next: ({ d, r }) => {
        this.departments.set(d);
        this.roles.set(r.items);
      },
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  protected deptName(id: number): string {
    return this.departments().find((d) => d.id === id)?.name ?? `#${id}`;
  }
  protected roleName(id: number): string {
    return this.roles().find((r) => r.id === id)?.name ?? `#${id}`;
  }

  private reload(): void {
    this.api.listEmployees().subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: [''],
      phone: [''],
      departmentId: [this.departments()[0]?.id ?? 0, Validators.required],
      roleId: [this.roles()[0]?.id ?? 0, Validators.required],
      hireDate: [new Date().toISOString().slice(0, 10), Validators.required],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
  }

  protected openEdit(e: Employee): void {
    this.editingId = e.id;
    this.error.set(null);
    this.form = this.buildForm();
    this.form.patchValue({
      firstName: e.firstName,
      lastName: e.lastName,
      email: e.email,
      phone: e.phone,
      departmentId: e.departmentId,
      roleId: e.roleId,
      hireDate: e.hireDate,
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
      .saveEmployee(
        {
          firstName: v.firstName,
          lastName: v.lastName,
          email: v.email,
          phone: v.phone,
          departmentId: Number(v.departmentId),
          roleId: Number(v.roleId),
          hireDate: v.hireDate,
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
