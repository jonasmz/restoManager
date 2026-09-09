import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';

@Component({
  selector: 'app-org-restaurant',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6">
      <h1 class="fs-3 mb-1">Empresa</h1>
      <p class="text-secondary mb-0">Datos del restaurante.</p>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <div class="card">
      <div class="card-body">
        <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
          <div class="col-md-6">
            <label class="form-label" for="name">Nombre</label>
            <input id="name" class="form-control" formControlName="name" />
          </div>
          <div class="col-md-6">
            <label class="form-label" for="taxNumber">CUIT / N.º fiscal</label>
            <input id="taxNumber" class="form-control" formControlName="taxNumber" />
          </div>
          <div class="col-12">
            <label class="form-label" for="address">Dirección</label>
            <input id="address" class="form-control" formControlName="address" />
          </div>
          <div class="col-md-6">
            <label class="form-label" for="phone">Teléfono</label>
            <input id="phone" class="form-control" formControlName="phone" />
          </div>
          <div class="col-md-6">
            <label class="form-label" for="email">Correo</label>
            <input id="email" class="form-control" formControlName="email" />
          </div>
          <div class="col-12">
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar</button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class RestaurantPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    address: ['', Validators.required],
    phone: [''],
    email: [''],
    taxNumber: [''],
  });
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  private id: number | null = null;

  constructor() {
    this.api.listRestaurants().subscribe((page) => {
      const r = page.items[0];
      if (r) {
        this.id = r.id;
        this.form.patchValue(r);
      }
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this.message.set(null);
    this.api.saveRestaurant(this.form.getRawValue(), this.id ?? undefined).subscribe({
      next: () => {
        this.saving.set(false);
        this.ok.set(true);
        this.message.set('Guardado.');
      },
      error: (err) => {
        this.saving.set(false);
        this.ok.set(false);
        this.message.set(apiErrorMessage(err));
      },
    });
  }
}
