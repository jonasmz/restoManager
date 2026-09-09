import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from '../inventory/inventory-api.service';
import { Ingredient, PurchaseOrder, Supplier } from '../inventory/inventory.models';

const PO_STATUSES = ['DRAFT', 'SENT', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CANCELLED'] as const;

@Component({
  selector: 'app-buy-orders',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Órdenes de compra</h1>
        <p class="text-secondary mb-0">
          Compras de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
          Al recibir se postea el movimiento <code>PURCHASE</code> (§11.4, INV-07).
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()"><i class="ti ti-plus me-1"></i>Nueva orden</button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="card mb-4">
      <div class="card-body pb-0">
        <select class="form-select form-select-sm mb-3" style="max-width: 16rem"
          [value]="statusFilter()" (change)="statusFilter.set($any($event.target).value); reload()">
          <option value="">Todos los estados</option>
          @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
        </select>
      </div>
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>#</th><th>Proveedor</th><th>Fecha</th><th class="text-end">Total</th><th>Estado</th><th class="text-end">Acciones</th></tr></thead>
          <tbody>
            @for (o of rows(); track o.id) {
              <tr>
                <td>#{{ o.id }}</td>
                <td>{{ supplierName(o.supplierId) }}</td>
                <td class="text-secondary small">{{ o.orderDate | date: 'shortDate' }}</td>
                <td class="text-end">{{ o.totalAmount | number: '1.2-2' }}</td>
                <td><span class="badge" [class]="badge(o.status)">{{ o.status }}</span></td>
                <td class="text-end">
                  @if (o.status === 'DRAFT') {
                    <button type="button" class="btn btn-light btn-sm me-1" (click)="act(o, 'send')">Enviar</button>
                  }
                  @if (o.status === 'SENT' || o.status === 'PARTIALLY_RECEIVED') {
                    <button type="button" class="btn btn-success btn-sm me-1" (click)="act(o, 'receive')">Recibir</button>
                  }
                  @if (o.status === 'DRAFT' || o.status === 'SENT') {
                    <button type="button" class="btn btn-outline-danger btn-sm me-1" (click)="act(o, 'cancel')">Cancelar</button>
                  }
                  <button type="button" class="btn btn-link btn-sm p-0" (click)="toggle(o.id)">
                    {{ expanded() === o.id ? 'Ocultar' : 'Ítems' }}
                  </button>
                </td>
              </tr>
              @if (expanded() === o.id) {
                <tr>
                  <td colspan="6" class="bg-light">
                    <table class="table table-sm mb-0">
                      <thead><tr><th>Ingrediente</th><th class="text-end">Cantidad</th><th class="text-end">Precio</th><th class="text-end">Subtotal</th></tr></thead>
                      <tbody>
                        @for (it of o.items; track it.ingredientId) {
                          <tr>
                            <td>{{ ingredientName(it.ingredientId) }}</td>
                            <td class="text-end">{{ it.quantity }}</td>
                            <td class="text-end">{{ it.unitPrice | number: '1.2-2' }}</td>
                            <td class="text-end">{{ it.quantity * it.unitPrice | number: '1.2-2' }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </td>
                </tr>
              }
            } @empty { <tr><td colspan="6" class="text-center text-secondary py-4">Sin órdenes de compra.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">Nueva orden de compra</h2>
          <form [formGroup]="form" (ngSubmit)="save()">
            <div class="row g-3 mb-3">
              <div class="col-md-6">
                <label class="form-label" for="supplierId">Proveedor</label>
                <select id="supplierId" class="form-select" formControlName="supplierId">
                  <option [value]="0" disabled>Selecciona…</option>
                  @for (s of suppliers(); track s.id) { <option [value]="s.id">{{ s.name }}</option> }
                </select>
              </div>
              <div class="col-md-4">
                <label class="form-label" for="orderDate">Fecha</label>
                <input id="orderDate" type="date" class="form-control" formControlName="orderDate" />
              </div>
            </div>

            <div class="d-flex justify-content-between align-items-center mb-2">
              <span class="fw-semibold small">Ítems</span>
              <button type="button" class="btn btn-light btn-sm" (click)="addLine()"><i class="ti ti-plus"></i> Ítem</button>
            </div>
            <table class="table table-sm align-middle">
              <thead><tr><th style="width: 45%">Ingrediente</th><th>Cantidad</th><th>Precio unit.</th><th></th></tr></thead>
              <tbody formArrayName="items">
                @for (line of items.controls; track $index) {
                  <tr [formGroupName]="$index">
                    <td>
                      <select class="form-select form-select-sm" formControlName="ingredientId">
                        <option [value]="0" disabled>Selecciona…</option>
                        @for (i of ingredients(); track i.id) { <option [value]="i.id">{{ i.name }} ({{ i.unit }})</option> }
                      </select>
                    </td>
                    <td><input type="number" step="0.01" min="0.01" class="form-control form-control-sm" formControlName="quantity" /></td>
                    <td><input type="number" step="0.01" min="0" class="form-control form-control-sm" formControlName="unitPrice" /></td>
                    <td><button type="button" class="btn btn-link btn-sm text-danger p-0" (click)="removeLine($index)" [disabled]="items.length === 1"><i class="ti ti-trash"></i></button></td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr><th colspan="3" class="text-end">Total</th><th class="text-end">{{ total() | number: '1.2-2' }}</th></tr>
              </tfoot>
            </table>

            <div class="d-flex gap-2">
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Crear borrador</button>
              <button type="button" class="btn btn-light" (click)="form = null">Cancelar</button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class PurchaseOrdersPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly statuses = PO_STATUSES;
  protected readonly rows = signal<PurchaseOrder[]>([]);
  protected readonly suppliers = signal<Supplier[]>([]);
  protected readonly ingredients = signal<Ingredient[]>([]);
  protected readonly statusFilter = signal('');
  protected readonly expanded = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected form: ReturnType<PurchaseOrdersPage['buildForm']> | null = null;

  protected total(): number {
    if (!this.form) {
      return 0;
    }
    return this.items.controls.reduce((sum, c) => {
      const v = c.getRawValue();
      return sum + Number(v.quantity || 0) * Number(v.unitPrice || 0);
    }, 0);
  }

  constructor() {
    this.api.listSuppliers().subscribe((p) => this.suppliers.set(p.items));
    this.api.listIngredients().subscribe((p) => this.ingredients.set(p.items));
    this.reload();
  }

  protected reload(): void {
    this.api.listPurchaseOrders(this.statusFilter() || undefined).subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.fail(err),
    });
  }

  protected supplierName(id: number): string {
    return this.suppliers().find((s) => s.id === id)?.name ?? `#${id}`;
  }

  protected ingredientName(id: number): string {
    return this.ingredients().find((i) => i.id === id)?.name ?? `#${id}`;
  }

  protected badge(status: string): string {
    switch (status) {
      case 'RECEIVED': return 'bg-success-subtle text-success';
      case 'SENT': return 'bg-primary-subtle text-primary';
      case 'PARTIALLY_RECEIVED': return 'bg-info-subtle text-info';
      case 'CANCELLED': return 'bg-danger-subtle text-danger';
      default: return 'bg-secondary-subtle text-secondary';
    }
  }

  protected toggle(id: number): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  protected act(o: PurchaseOrder, action: 'send' | 'receive' | 'cancel'): void {
    this.message.set(null);
    this.api.purchaseOrderAction(o.id, action).subscribe({
      next: () => {
        this.ok.set(true);
        this.message.set(`Orden #${o.id}: ${action === 'send' ? 'enviada' : action === 'receive' ? 'recibida' : 'cancelada'}.`);
        this.reload();
      },
      error: (err) => this.fail(err),
    });
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      supplierId: [0, [Validators.required, Validators.min(1)]],
      orderDate: [new Date().toISOString().slice(0, 10), Validators.required],
      items: this.fb.array([this.lineGroup()]),
    });
  }

  private lineGroup() {
    return this.fb.nonNullable.group({
      ingredientId: [0, [Validators.required, Validators.min(1)]],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
    });
  }

  protected get items(): FormArray {
    return this.form!.controls.items as FormArray;
  }

  protected openNew(): void {
    this.message.set(null);
    this.form = this.buildForm();
  }

  protected addLine(): void {
    this.items.push(this.lineGroup());
  }

  protected removeLine(i: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(i);
    }
  }

  protected save(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.message.set(null);
    this.api
      .createPurchaseOrder({
        supplierId: Number(v.supplierId),
        orderDate: v.orderDate,
        items: v.items.map((l) => ({
          ingredientId: Number(l.ingredientId),
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
        })),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.form = null;
          this.ok.set(true);
          this.message.set('Orden de compra creada en borrador.');
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          this.fail(err);
        },
      });
  }

  private fail(err: unknown): void {
    this.ok.set(false);
    this.message.set(apiErrorMessage(err));
  }
}
