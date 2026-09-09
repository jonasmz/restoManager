import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { SalonApiService } from './salon-api.service';
import { FloorTable, TableDisplayStatus } from './salon.models';

@Component({
  selector: 'app-salon-floor',
  imports: [ReactiveFormsModule, DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Tablero de salón</h1>
        <p class="text-secondary mb-0">
          <strong>{{ branch.activeBranch()?.name ?? 'Sucursal activa' }}</strong> —
          estado de uso derivado (Anexo B). Se refresca solo cada 20 s.
        </p>
      </div>
      <button type="button" class="btn btn-light btn-sm" (click)="reload()"><i class="ti ti-refresh me-1"></i>Refrescar</button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="d-flex flex-wrap gap-2 mb-4 small">
      @for (s of legend; track s.status) {
        <span class="badge" [class]="badge(s.status)">{{ s.label }}</span>
      }
    </div>

    <div class="row g-3">
      @for (t of tables(); track t.tableId) {
        <div class="col-6 col-md-4 col-lg-3 col-xl-2">
          <button type="button" class="card w-100 h-100 border-2 text-start" [class]="cardClass(t.displayStatus)"
            (click)="select(t)" [class.shadow]="selected()?.tableId === t.tableId">
            <div class="card-body p-3">
              <div class="d-flex justify-content-between align-items-center mb-1">
                <span class="fs-5 fw-semibold">#{{ t.number }}</span>
                <span class="badge" [class]="badge(t.displayStatus)">{{ statusLabel(t.displayStatus) }}</span>
              </div>
              <div class="small text-secondary">{{ t.capacity }} pers.</div>
              @if (t.openedAt) { <div class="small text-secondary">Abierta {{ t.openedAt | date: 'shortTime' }}</div> }
              @if (!t.openSessionId && t.nextReservationTime) {
                <div class="small text-secondary">Reserva {{ t.nextReservationTime | date: 'shortTime' }}</div>
              }
            </div>
          </button>
        </div>
      } @empty {
        <div class="col-12 text-center text-secondary py-5">No hay mesas en esta sucursal.</div>
      }
    </div>

    @if (selected(); as t) {
      <div class="card mt-4">
        <div class="card-body">
          <h2 class="fs-6 mb-3">Mesa #{{ t.number }} — {{ statusLabel(t.displayStatus) }}</h2>

          @if (t.openSessionId) {
            <p class="small text-secondary">Sesión abierta #{{ t.openSessionId }} desde {{ t.openedAt | date: 'short' }}.</p>
            <div class="d-flex gap-2">
              <a class="btn btn-primary" [routerLink]="['/pos']"
                [queryParams]="{ tableId: t.tableId, sessionId: t.openSessionId }">
                <i class="ti ti-cash-register me-1"></i>Abrir cuenta
              </a>
              <button type="button" class="btn btn-outline-success" (click)="closeSession(t)">Cerrar sesión</button>
            </div>
          } @else if (t.operationalStatus === 'ACTIVE') {
            <form [formGroup]="openForm" (ngSubmit)="openSession(t)" class="row g-2 align-items-end" style="max-width: 22rem">
              <div class="col-7"><label class="form-label small" for="gc">Comensales</label>
                <input id="gc" type="number" min="1" class="form-control form-control-sm" formControlName="guestCount" /></div>
              <div class="col-5"><button type="submit" class="btn btn-primary btn-sm w-100" [disabled]="openForm.invalid">Abrir sesión</button></div>
            </form>
          } @else {
            <p class="small text-secondary">La mesa está {{ statusLabel(t.displayStatus) }}. Cambia el estado operativo para operarla.</p>
          }

          <hr />
          <div class="form-label small">Estado operativo</div>
          <div class="btn-group d-block">
            <button type="button" class="btn btn-outline-secondary btn-sm" [class.active]="t.operationalStatus === 'ACTIVE'"
              (click)="setStatus(t, 'ACTIVE')">Activa</button>
            <button type="button" class="btn btn-outline-secondary btn-sm" [class.active]="t.operationalStatus === 'CLEANING'"
              (click)="setStatus(t, 'CLEANING')">Limpieza</button>
            <button type="button" class="btn btn-outline-secondary btn-sm" [class.active]="t.operationalStatus === 'OUT_OF_SERVICE'"
              (click)="setStatus(t, 'OUT_OF_SERVICE')">Fuera de servicio</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class FloorPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalonApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly branch = inject(BranchContextService);

  protected readonly tables = signal<FloorTable[]>([]);
  protected readonly selected = signal<FloorTable | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected readonly openForm = this.fb.nonNullable.group({
    guestCount: [2, [Validators.required, Validators.min(1)]],
  });

  protected readonly legend: { status: TableDisplayStatus; label: string }[] = [
    { status: 'AVAILABLE', label: 'Disponible' },
    { status: 'RESERVED', label: 'Reservada' },
    { status: 'OCCUPIED', label: 'Ocupada' },
    { status: 'CLEANING', label: 'Limpieza' },
    { status: 'OUT_OF_SERVICE', label: 'Fuera de servicio' },
  ];

  constructor() {
    this.reload();
    interval(20_000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());
  }

  protected statusLabel(s: TableDisplayStatus): string {
    return this.legend.find((l) => l.status === s)?.label ?? s;
  }

  protected badge(s: TableDisplayStatus): string {
    switch (s) {
      case 'AVAILABLE': return 'bg-success-subtle text-success';
      case 'RESERVED': return 'bg-warning-subtle text-warning-emphasis';
      case 'OCCUPIED': return 'bg-primary-subtle text-primary';
      case 'CLEANING': return 'bg-info-subtle text-info-emphasis';
      default: return 'bg-secondary-subtle text-secondary';
    }
  }

  protected cardClass(s: TableDisplayStatus): string {
    switch (s) {
      case 'AVAILABLE': return 'border-success';
      case 'RESERVED': return 'border-warning';
      case 'OCCUPIED': return 'border-primary';
      case 'CLEANING': return 'border-info';
      default: return 'border-secondary opacity-75';
    }
  }

  protected select(t: FloorTable): void {
    this.message.set(null);
    this.selected.set(this.selected()?.tableId === t.tableId ? null : t);
    this.openForm.reset({ guestCount: Math.min(t.capacity, 2) || 2 });
  }

  reload(): void {
    this.api.floor().subscribe({
      next: (t) => {
        this.tables.set(t);
        const sel = this.selected();
        if (sel) {
          this.selected.set(t.find((x) => x.tableId === sel.tableId) ?? null);
        }
      },
      error: (err) => this.fail(err),
    });
  }

  protected openSession(t: FloorTable): void {
    if (this.openForm.invalid) {
      return;
    }
    this.api.openSession(t.tableId, Number(this.openForm.getRawValue().guestCount)).subscribe({
      next: () => this.done(`Sesión abierta en la mesa #${t.number}.`),
      error: (err) => this.fail(err),
    });
  }

  protected closeSession(t: FloorTable): void {
    if (!t.openSessionId) {
      return;
    }
    this.api.closeSession(t.tableId, t.openSessionId).subscribe({
      next: () => this.done(`Sesión de la mesa #${t.number} cerrada.`),
      error: (err) => this.fail(err),
    });
  }

  protected setStatus(t: FloorTable, status: 'ACTIVE' | 'CLEANING' | 'OUT_OF_SERVICE'): void {
    this.api.setTableStatus(t.tableId, status).subscribe({
      next: () => this.done(`Mesa #${t.number}: estado actualizado.`),
      error: (err) => this.fail(err),
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
