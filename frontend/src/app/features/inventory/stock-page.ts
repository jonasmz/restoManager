import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from './inventory-api.service';
import { Ingredient, StockLine } from './inventory.models';

type MoveKind = 'initial' | 'adjust' | 'waste';

@Component({
  selector: 'app-inv-stock',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Existencias</h1>
        <p class="text-secondary mb-0">
          Stock de <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
          <a routerLink="/inventory/ingredients" class="ms-2 small">Ingredientes</a> ·
          <a routerLink="/inventory/movements" class="small">Movimientos</a>
        </p>
      </div>
      <div class="d-flex gap-2">
        <button type="button" class="btn btn-light btn-sm" (click)="open('initial')">Carga inicial</button>
        <button type="button" class="btn btn-light btn-sm" (click)="open('adjust')">Ajuste</button>
        <button type="button" class="btn btn-primary btn-sm" (click)="open('waste')"><i class="ti ti-trash me-1"></i>Merma</button>
      </div>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="card mb-4">
      <div class="card-body pb-0">
        <input class="form-control form-control-sm mb-3" style="max-width: 20rem" placeholder="Buscar ingrediente…"
          [value]="search()" (input)="search.set($any($event.target).value)" />
      </div>
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead><tr><th>Ingrediente</th><th>Unidad</th><th class="text-end">Stock</th><th class="text-end">Precio</th><th class="text-end">Valor</th></tr></thead>
          <tbody>
            @for (s of filtered(); track s.ingredientId) {
              <tr>
                <td>{{ s.ingredientName }}</td>
                <td class="text-secondary">{{ s.unit }}</td>
                <td class="text-end">{{ s.stockQuantity }}</td>
                <td class="text-end text-secondary">{{ s.unitPrice }}</td>
                <td class="text-end">{{ s.stockValue | number: '1.2-2' }}</td>
              </tr>
            } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin existencias.</td></tr> }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ title() }}</h2>
          <form [formGroup]="form" (ngSubmit)="submit()" class="row g-3">
            <div class="col-md-5">
              <label class="form-label" for="ingredientId">Ingrediente</label>
              <select id="ingredientId" class="form-select" formControlName="ingredientId">
                @for (i of ingredients(); track i.id) { <option [value]="i.id">{{ i.name }} ({{ i.unit }})</option> }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label" for="quantity">Cantidad {{ kind === 'adjust' ? '(±)' : '' }}</label>
              <input id="quantity" type="number" step="0.01" class="form-control" formControlName="quantity" />
            </div>
            @if (kind !== 'initial') {
              <div class="col-md-4">
                <label class="form-label" for="reason">Motivo</label>
                <input id="reason" class="form-control" formControlName="reason" />
              </div>
            }
            <div class="col-12 d-flex gap-2">
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Confirmar</button>
              <button type="button" class="btn btn-light" (click)="form = null">Cancelar</button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class StockPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly stock = signal<StockLine[]>([]);
  protected readonly ingredients = signal<Ingredient[]>([]);
  protected readonly search = signal('');
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    return q ? this.stock().filter((s) => s.ingredientName.toLowerCase().includes(q)) : this.stock();
  });

  protected form: ReturnType<StockPage['buildForm']> | null = null;
  protected kind: MoveKind = 'waste';

  constructor() {
    this.reload();
    this.api.listIngredients().subscribe((p) => this.ingredients.set(p.items));
  }

  protected title(): string {
    return this.kind === 'initial' ? 'Carga inicial' : this.kind === 'adjust' ? 'Ajuste de stock' : 'Registrar merma';
  }

  private reload(): void {
    this.api.stock().subscribe({
      next: (s) => this.stock.set(s),
      error: (err) => this.message.set(apiErrorMessage(err)),
    });
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      ingredientId: [this.ingredients()[0]?.id ?? 0, Validators.required],
      quantity: [0, [Validators.required, Validators.min(this.kind === 'adjust' ? -1e9 : 0.01)]],
      reason: [''],
    });
  }

  protected open(kind: MoveKind): void {
    this.kind = kind;
    this.message.set(null);
    this.form = this.buildForm();
    if (kind !== 'initial') {
      this.form.controls.reason.addValidators(Validators.required);
    }
  }

  protected submit(): void {
    if (!this.form || this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    const ingredientId = Number(v.ingredientId);
    const quantity = Number(v.quantity);
    this.saving.set(true);
    this.message.set(null);

    const req =
      this.kind === 'initial'
        ? this.api.initialLoad({ ingredientId, quantity })
        : this.kind === 'adjust'
          ? this.api.adjust({ ingredientId, quantity, reason: v.reason })
          : this.api.registerWaste({ ingredientId, quantity, reason: v.reason });

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.form = null;
        this.ok.set(true);
        this.message.set('Movimiento registrado.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.ok.set(false);
        this.message.set(apiErrorMessage(err));
      },
    });
  }
}
