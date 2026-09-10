import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { SalonApiService } from './salon-api.service';
import { Customer, RESERVATION_STATUSES, Reservation, Table } from './salon.models';

@Component({
  selector: 'app-salon-reservations',
  imports: [ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Reservas</h1>
        <p class="text-secondary mb-0">
          Agenda de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
          <code>RESERVED</code> es derivado: la reserva debe estar <code>CONFIRMED</code> y dentro de la ventana.
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva reserva</button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="card mb-4">
      <div class="card-body pb-0 d-flex flex-wrap gap-3 align-items-end">
        <div>
          <label class="form-label small" for="date">Día</label>
          <input id="date" type="date" class="form-control form-control-sm" [value]="date()"
            (change)="date.set($any($event.target).value); reload()" />
        </div>
        <div>
          <label class="form-label small" for="status">Estado</label>
          <select id="status" class="form-select form-select-sm" [value]="statusFilter()"
            (change)="statusFilter.set($any($event.target).value); reload()">
            <option value="">Todos</option>
            @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
          </select>
        </div>
      </div>
      <div class="table-responsive mt-2">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Hora</th><th>Cliente</th><th>Mesa</th><th>Pers.</th><th>Estado</th><th class="text-end">Acciones</th></tr></thead>
          <tbody>
            @for (r of rows(); track r.id) {
              <tr>
                <td class="small">{{ r.reservationTime | date: 'short' }}</td>
                <td>{{ customerName(r.customerId) }}</td>
                <td class="text-secondary">#{{ tableNumber(r.tableId) }}</td>
                <td class="text-secondary">{{ r.partySize }}</td>
                <td><span class="badge" [class]="badge(r.status)">{{ r.status }}</span></td>
                <td class="text-end">
                  @if (r.status === 'PENDING') {
                    <button type="button" class="btn btn-light btn-sm me-1" (click)="act(r, 'confirm')">Confirmar</button>
                  }
                  @if (r.status === 'CONFIRMED') {
                    @if (seatingId() === r.id) {
                      <span class="d-inline-flex gap-1 align-items-center">
                        <input type="number" min="1" [attr.max]="tableCapacity(r.tableId)"
                          class="form-control form-control-sm" style="width: 5rem"
                          [value]="r.partySize" #gc />
                        <button type="button" class="btn btn-success btn-sm" (click)="seat(r, gc.value)">OK</button>
                        <button type="button" class="btn btn-link btn-sm p-0" (click)="seatingId.set(null)">✕</button>
                      </span>
                    } @else {
                      <button type="button" class="btn btn-success btn-sm me-1" (click)="seatingId.set(r.id)">Sentar</button>
                      <button type="button" class="btn btn-light btn-sm me-1" (click)="act(r, 'no-show')">No-show</button>
                    }
                  }
                  @if (r.status === 'PENDING' || r.status === 'CONFIRMED') {
                    <button type="button" class="btn btn-outline-danger btn-sm" (click)="act(r, 'cancel')">Cancelar</button>
                  }
                </td>
              </tr>
            } @empty { <tr><td colspan="6" class="text-center text-secondary py-4">Sin reservas ese día.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">Nueva reserva</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-5">
              <label class="form-label" for="customerId">Cliente</label>
              <div class="input-group">
                <select id="customerId" class="form-select" formControlName="customerId">
                  <option [value]="0" disabled>Selecciona…</option>
                  @for (c of customers(); track c.id) { <option [value]="c.id">{{ c.lastName }}, {{ c.firstName }}</option> }
                </select>
                <button type="button" class="btn btn-outline-secondary" (click)="newCustomer.set(!newCustomer())">
                  <i class="ti ti-user-plus"></i>
                </button>
              </div>
            </div>
            <div class="col-md-4">
              <label class="form-label" for="tableId">Mesa</label>
              <select id="tableId" class="form-select" formControlName="tableId">
                <option [value]="0" disabled>Selecciona…</option>
                @for (t of tables(); track t.id) { <option [value]="t.id">#{{ t.number }} ({{ t.capacity }} pers.)</option> }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label" for="partySize">Comensales</label>
              <input id="partySize" type="number" min="1" class="form-control" formControlName="partySize" />
            </div>
            <div class="col-md-5">
              <label class="form-label" for="reservationTime">Fecha y hora</label>
              <input id="reservationTime" type="datetime-local" class="form-control" formControlName="reservationTime" />
            </div>

            @if (newCustomer()) {
              <div class="col-12"><div class="border rounded p-3 bg-light">
                <div class="fw-semibold small mb-2">Alta rápida de cliente</div>
                <form [formGroup]="customerForm" (ngSubmit)="createCustomer()" class="row g-2">
                  <div class="col-md-3"><input class="form-control form-control-sm" placeholder="Nombre" formControlName="firstName" /></div>
                  <div class="col-md-3"><input class="form-control form-control-sm" placeholder="Apellido" formControlName="lastName" /></div>
                  <div class="col-md-3"><input class="form-control form-control-sm" placeholder="Teléfono" formControlName="phone" /></div>
                  <div class="col-md-3"><input class="form-control form-control-sm" placeholder="Email (opcional)" formControlName="email" /></div>
                  <div class="col-12">
                    <button type="submit" class="btn btn-secondary btn-sm" [disabled]="customerForm.invalid || saving()">Crear y seleccionar</button>
                  </div>
                </form>
              </div></div>
            }

            <div class="col-12 d-flex gap-2">
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Crear reserva</button>
              <button type="button" class="btn btn-light" (click)="form = null">Cancelar</button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class ReservationsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalonApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly statuses = RESERVATION_STATUSES;
  protected readonly rows = signal<Reservation[]>([]);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly tables = signal<Table[]>([]);
  protected readonly date = signal(new Date().toISOString().slice(0, 10));
  protected readonly statusFilter = signal('');
  protected readonly seatingId = signal<number | null>(null);
  protected readonly newCustomer = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected form: ReturnType<ReservationsPage['buildForm']> | null = null;

  protected readonly customerForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: ['', Validators.required],
    email: [''],
  });

  protected readonly customerById = computed(() => new Map(this.customers().map((c) => [c.id, c])));

  constructor() {
    this.api.listCustomers().subscribe((p) => this.customers.set(p.items));
    this.api.listTables().subscribe((t) => this.tables.set(t));
    this.reload();
  }

  protected customerName(id: number): string {
    const c = this.customerById().get(id);
    return c ? `${c.lastName}, ${c.firstName}` : `#${id}`;
  }

  protected tableNumber(id: number): number | string {
    return this.tables().find((t) => t.id === id)?.number ?? `#${id}`;
  }

  protected tableCapacity(id: number): number | null {
    return this.tables().find((t) => t.id === id)?.capacity ?? null;
  }

  protected badge(status: string): string {
    switch (status) {
      case 'CONFIRMED': return 'bg-primary-subtle text-primary';
      case 'SEATED': return 'bg-success-subtle text-success';
      case 'CANCELLED': return 'bg-danger-subtle text-danger';
      case 'NO_SHOW': return 'bg-warning-subtle text-warning-emphasis';
      default: return 'bg-secondary-subtle text-secondary';
    }
  }

  reload(): void {
    this.api.listReservations({ date: this.date(), status: this.statusFilter() || undefined }).subscribe({
      next: (r) => this.rows.set(r),
      error: (err) => this.fail(err),
    });
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      customerId: [0, [Validators.required, Validators.min(1)]],
      tableId: [0, [Validators.required, Validators.min(1)]],
      partySize: [2, [Validators.required, Validators.min(1)]],
      reservationTime: [`${this.date()}T20:00`, Validators.required],
    });
  }

  protected openNew(): void {
    this.message.set(null);
    this.newCustomer.set(false);
    this.form = this.buildForm();
  }

  protected act(r: Reservation, action: 'confirm' | 'cancel' | 'no-show'): void {
    this.message.set(null);
    this.api.reservationAction(r.id, action).subscribe({
      next: () => this.done(`Reserva #${r.id} actualizada.`),
      error: (err) => this.fail(err),
    });
  }

  protected seat(r: Reservation, guestCount: string): void {
    const gc = Number(guestCount);
    if (!gc || gc < 1) {
      return;
    }
    this.api.seatReservation(r.id, gc).subscribe({
      next: () => {
        this.seatingId.set(null);
        this.done(`Reserva #${r.id} sentada: sesión abierta en la mesa.`);
      },
      error: (err) => this.fail(err),
    });
  }

  protected createCustomer(): void {
    if (this.customerForm.invalid) {
      return;
    }
    const v = this.customerForm.getRawValue();
    this.saving.set(true);
    this.api.createCustomer(v).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.api.listCustomers().subscribe((p) => {
          this.customers.set(p.items);
          this.form?.patchValue({ customerId: res.id });
          this.newCustomer.set(false);
          this.customerForm.reset({ firstName: '', lastName: '', phone: '', email: '' });
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.fail(err);
      },
    });
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.message.set(null);
    this.api
      .createReservation({
        customerId: Number(v.customerId),
        tableId: Number(v.tableId),
        partySize: Number(v.partySize),
        reservationTime: v.reservationTime,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.form = null;
          this.done('Reserva creada.');
        },
        error: (err) => {
          this.saving.set(false);
          this.fail(err);
        },
      });
  }

  private done(msg: string): void {
    this.ok.set(true);
    this.message.set(msg);
    this.reload();
  }

  private fail(err: unknown): void {
    this.ok.set(false);
    this.message.set(apiErrorMessage(err));
  }
}
