import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { environment } from '../../core/config/environment';
import { apiErrorMessage } from '../../core/http/api-error';
import { InventoryApiService } from '../inventory/inventory-api.service';
import { Ingredient } from '../inventory/inventory.models';
import { MenuApiService } from './menu-api.service';
import { Category, MenuItemAvailability, MenuItemCost, TaxRate } from './menu.models';

@Component({
  selector: 'app-menu-item-detail',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/menu/items" class="small text-secondary"><i class="ti ti-arrow-left me-1"></i>Platos</a>

    @if (error()) { <div class="alert alert-danger py-2 small mt-3">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success py-2 small mt-3">{{ message() }}</div> }

    <h1 class="fs-3 mt-2 mb-4">{{ isNew() ? 'Nuevo plato' : form.getRawValue().name || 'Plato' }}</h1>

    <form [formGroup]="form" (ngSubmit)="save()">
      <div class="row g-4">
        <!-- Datos + receta -->
        <div class="col-lg-7">
          <div class="card mb-4">
            <div class="card-body">
              <h2 class="fs-6 mb-3">Datos</h2>
              <div class="row g-3">
                <div class="col-md-7"><label class="form-label" for="name">Nombre</label>
                  <input id="name" class="form-control" formControlName="name" /></div>
                <div class="col-md-5"><label class="form-label" for="categoryId">Categoría</label>
                  <select id="categoryId" class="form-select" formControlName="categoryId">
                    <option [value]="0" disabled>Selecciona…</option>
                    @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
                  </select></div>
                <div class="col-12"><label class="form-label" for="description">Descripción</label>
                  <input id="description" class="form-control" formControlName="description" /></div>
                <div class="col-md-4"><label class="form-label" for="price">Precio (impuestos incl.)</label>
                  <input id="price" type="number" step="0.01" min="0" class="form-control" formControlName="price" /></div>
                <div class="col-md-8 d-flex align-items-end">
                  <div class="form-check">
                    <input id="isAvailable" type="checkbox" class="form-check-input" formControlName="isAvailable" />
                    <label class="form-check-label" for="isAvailable">Disponible (por defecto en todas las sucursales)</label>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="card mb-4">
            <div class="card-body">
              <div class="d-flex justify-content-between align-items-center mb-2">
                <h2 class="fs-6 mb-0">Receta</h2>
                <button type="button" class="btn btn-light btn-sm" (click)="addLine()"><i class="ti ti-plus"></i> Ingrediente</button>
              </div>
              <table class="table table-sm align-middle mb-0">
                <thead><tr><th style="width: 60%">Ingrediente</th><th>Cantidad</th><th></th></tr></thead>
                <tbody formArrayName="recipe">
                  @for (line of recipe.controls; track $index) {
                    <tr [formGroupName]="$index">
                      <td>
                        <select class="form-select form-select-sm" formControlName="ingredientId">
                          <option [value]="0" disabled>Selecciona…</option>
                          @for (i of ingredients(); track i.id) { <option [value]="i.id">{{ i.name }} ({{ i.unit }})</option> }
                        </select>
                      </td>
                      <td><input type="number" step="0.0001" min="0.0001" class="form-control form-control-sm" formControlName="quantityRequired" /></td>
                      <td><button type="button" class="btn btn-link btn-sm text-danger p-0" (click)="removeLine($index)"><i class="ti ti-trash"></i></button></td>
                    </tr>
                  } @empty { <tr><td colspan="3" class="text-secondary small py-3">Sin ingredientes en la receta.</td></tr> }
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Impuestos + coste + disponibilidad -->
        <div class="col-lg-5">
          <div class="card mb-4">
            <div class="card-body">
              <h2 class="fs-6 mb-3">Impuestos</h2>
              @for (t of taxRates(); track t.id) {
                <div class="form-check">
                  <input type="checkbox" class="form-check-input" [id]="'tax' + t.id"
                    [checked]="selectedTaxes().has(t.id)" (change)="toggleTax(t.id)" />
                  <label class="form-check-label" [for]="'tax' + t.id">{{ t.name }} ({{ t.rate | number: '1.0-2' }} %)</label>
                </div>
              } @empty { <p class="text-secondary small mb-0">No hay tasas definidas.</p> }
            </div>
          </div>

          @if (!isNew()) {
            <div class="card mb-4">
              <div class="card-body">
                <h2 class="fs-6 mb-3">Imagen</h2>
                @if (imageUrl(); as url) {
                  <img [src]="url" alt="Imagen del plato" class="img-fluid rounded mb-2" style="max-height: 12rem" />
                } @else {
                  <p class="text-secondary small mb-2">Sin imagen. Se muestra en la carta pública.</p>
                }
                <div class="d-flex gap-2 align-items-center">
                  <input type="file" accept="image/jpeg,image/png,image/webp" class="form-control form-control-sm"
                    (change)="onImageSelected($event)" [disabled]="uploadingImage()" />
                  @if (imageUrl()) {
                    <button type="button" class="btn btn-light btn-sm text-danger" (click)="removeImage()"
                      [disabled]="uploadingImage()"><i class="ti ti-trash"></i></button>
                  }
                </div>
                <p class="text-secondary mt-1 mb-0" style="font-size: .75rem">JPG, PNG o WebP · máx. 2 MB.</p>
              </div>
            </div>

            <div class="card mb-4">
              <div class="card-body">
                <h2 class="fs-6 mb-3">Coste teórico</h2>
                @if (cost(); as c) {
                  <p class="fs-4 mb-2">{{ c.cost | currency }}</p>
                  <table class="table table-sm mb-0">
                    <tbody>
                      @for (l of c.lines; track l.ingredientId) {
                        <tr>
                          <td class="small">{{ l.ingredientName }}</td>
                          <td class="small text-end">{{ l.quantityRequired | number: '1.0-4' }} × {{ l.unitPrice | currency }}</td>
                          <td class="small text-end">{{ l.lineCost | currency }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                } @else { <p class="text-secondary small mb-0">Guarda la receta para calcular el coste.</p> }
              </div>
            </div>

            <div class="card mb-4">
              <div class="card-body">
                <h2 class="fs-6 mb-1">Disponibilidad en {{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</h2>
                @if (availability(); as a) {
                  <p class="text-secondary small mb-2">
                    {{ a.isOverride ? 'Ajuste propio de esta sucursal.' : 'Hereda del valor por defecto del plato.' }}
                  </p>
                  <div class="form-check form-switch">
                    <input type="checkbox" class="form-check-input" id="branchAvail"
                      [checked]="a.isAvailable" (change)="setAvailability($any($event.target).checked)" />
                    <label class="form-check-label" for="branchAvail">
                      {{ a.isAvailable ? 'Disponible aquí' : 'Oculto aquí' }}
                    </label>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      </div>

      <div class="d-flex gap-2">
        <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar plato</button>
        <a routerLink="/menu/items" class="btn btn-light">Cancelar</a>
      </div>
    </form>
  `,
})
export class MenuItemDetailPage implements OnInit {
  readonly id = input.required<string>();

  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MenuApiService);
  private readonly inventory = inject(InventoryApiService);
  private readonly router = inject(Router);
  protected readonly branch = inject(BranchContextService);

  protected readonly categories = signal<Category[]>([]);
  protected readonly ingredients = signal<Ingredient[]>([]);
  protected readonly taxRates = signal<TaxRate[]>([]);
  protected readonly selectedTaxes = signal<Set<number>>(new Set());
  protected readonly cost = signal<MenuItemCost | null>(null);
  protected readonly availability = signal<MenuItemAvailability | null>(null);
  protected readonly imageUrl = signal<string | null>(null);
  protected readonly uploadingImage = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);

  protected readonly isNew = computed(() => this.id() === 'new');

  protected readonly form = this.fb.nonNullable.group({
    categoryId: [0, [Validators.required, Validators.min(1)]],
    name: ['', Validators.required],
    description: [''],
    price: [0, [Validators.required, Validators.min(0)]],
    isAvailable: [true],
    recipe: this.fb.array<ReturnType<MenuItemDetailPage['lineGroup']>>([]),
  });

  protected get recipe(): FormArray {
    return this.form.controls.recipe as FormArray;
  }

  ngOnInit(): void {
    this.api.listCategories().subscribe((p) => this.categories.set(p.items));
    this.inventory.listIngredients().subscribe((p) => this.ingredients.set(p.items));
    this.api.listTaxRates().subscribe((p) => this.taxRates.set(p.items));

    if (this.isNew()) {
      return;
    }
    const itemId = Number(this.id());
    this.api.getMenuItem(itemId).subscribe({
      next: (m) => {
        this.form.patchValue({
          categoryId: m.categoryId,
          name: m.name,
          description: m.description,
          price: m.price,
          isAvailable: m.isAvailable,
        });
        this.recipe.clear();
        for (const r of m.recipe) {
          this.recipe.push(this.lineGroup(r.ingredientId, r.quantityRequired));
        }
        this.selectedTaxes.set(new Set(m.taxRateIds));
        this.imageUrl.set(m.imageUrl ? `${environment.businessApiUrl}${m.imageUrl}` : null);
      },
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
    this.api.menuItemCost(itemId).subscribe({ next: (c) => this.cost.set(c), error: () => this.cost.set(null) });
    this.api.getAvailability(itemId).subscribe({ next: (a) => this.availability.set(a), error: () => undefined });
  }

  private lineGroup(ingredientId = 0, quantityRequired = 1) {
    return this.fb.nonNullable.group({
      ingredientId: [ingredientId, [Validators.required, Validators.min(1)]],
      quantityRequired: [quantityRequired, [Validators.required, Validators.min(0.0001)]],
    });
  }

  protected onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    this.uploadingImage.set(true);
    this.error.set(null);
    this.api.uploadMenuItemImage(Number(this.id()), file).subscribe({
      next: (r) => {
        this.uploadingImage.set(false);
        this.imageUrl.set(`${environment.businessApiUrl}${r.imageUrl}`);
        this.message.set('Imagen actualizada.');
        input.value = '';
      },
      error: (err) => {
        this.uploadingImage.set(false);
        this.error.set(apiErrorMessage(err));
        input.value = '';
      },
    });
  }

  protected removeImage(): void {
    this.uploadingImage.set(true);
    this.api.deleteMenuItemImage(Number(this.id())).subscribe({
      next: () => {
        this.uploadingImage.set(false);
        this.imageUrl.set(null);
      },
      error: (err) => {
        this.uploadingImage.set(false);
        this.error.set(apiErrorMessage(err));
      },
    });
  }

  protected addLine(): void {
    this.recipe.push(this.lineGroup());
  }

  protected removeLine(i: number): void {
    this.recipe.removeAt(i);
  }

  protected toggleTax(id: number): void {
    const next = new Set(this.selectedTaxes());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectedTaxes.set(next);
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    const body = {
      categoryId: Number(v.categoryId),
      name: v.name,
      description: v.description,
      price: Number(v.price),
      isAvailable: v.isAvailable,
      recipe: v.recipe.map((r) => ({
        ingredientId: Number(r.ingredientId),
        quantityRequired: Number(r.quantityRequired),
      })),
      taxRateIds: [...this.selectedTaxes()],
    };
    this.saving.set(true);
    this.error.set(null);
    this.message.set(null);
    const req = this.isNew()
      ? this.api.saveMenuItem(body)
      : this.api.saveMenuItem(body, Number(this.id()));
    req.subscribe({
      next: () => {
        this.saving.set(false);
        if (this.isNew()) {
          this.router.navigate(['/menu/items']);
        } else {
          this.message.set('Plato guardado.');
          this.api.menuItemCost(Number(this.id())).subscribe({ next: (c) => this.cost.set(c) });
        }
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err));
      },
    });
  }

  protected setAvailability(isAvailable: boolean): void {
    const itemId = Number(this.id());
    this.api.setAvailability(itemId, isAvailable).subscribe({
      next: () => this.api.getAvailability(itemId).subscribe((a) => this.availability.set(a)),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
