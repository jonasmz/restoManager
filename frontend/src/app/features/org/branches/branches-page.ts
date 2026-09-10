import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import qrcode from 'qrcode-generator';

import { OrgApiService } from '../org-api.service';
import { apiErrorMessage } from '../org-util';
import { Branch, Restaurant } from '../org.models';

function qrDataUrl(text: string): string {
  const qr = qrcode(0, 'M');
  qr.addData(text);
  qr.make();
  return qr.createDataURL(6, 8);
}

@Component({
  selector: 'app-org-branches',
  imports: [ReactiveFormsModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Sucursales</h1>
        <p class="text-secondary mb-0">Puntos de venta de la empresa.</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="openNew()">
        <i class="ti ti-plus me-1"></i>Nueva
      </button>
    </div>

    @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }

    <div class="card mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead>
            <tr><th>Nombre</th><th>Dirección</th><th>Horario</th><th class="text-end">Acción</th></tr>
          </thead>
          <tbody>
            @for (b of rows(); track b.id) {
              <tr>
                <td>{{ b.name }}</td>
                <td class="text-secondary">{{ b.address }}</td>
                <td class="text-secondary">{{ b.openingTime }} – {{ b.closingTime }}</td>
                <td class="text-end">
                  <button type="button" class="btn btn-light btn-sm" (click)="openEdit(b)">
                    <i class="ti ti-edit"></i>
                  </button>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-center text-secondary py-4">Sin sucursales.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    @if (form) {
      <div class="card">
        <div class="card-body">
          <h2 class="fs-6 mb-3">{{ editingId ? 'Editar sucursal' : 'Nueva sucursal' }}</h2>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3">
            <div class="col-md-6">
              <label class="form-label" for="name">Nombre</label>
              <input id="name" class="form-control" formControlName="name" />
            </div>
            <div class="col-md-6">
              <label class="form-label" for="restaurantId">Empresa</label>
              <select id="restaurantId" class="form-select" formControlName="restaurantId">
                @for (r of restaurants(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
              </select>
            </div>
            <div class="col-12">
              <label class="form-label" for="address">Dirección</label>
              <input id="address" class="form-control" formControlName="address" />
            </div>
            <div class="col-md-6"><label class="form-label" for="phone">Teléfono</label>
              <input id="phone" class="form-control" formControlName="phone" /></div>
            <div class="col-md-6"><label class="form-label" for="email">Correo</label>
              <input id="email" class="form-control" formControlName="email" /></div>
            <div class="col-md-3"><label class="form-label" for="openingTime">Apertura</label>
              <input id="openingTime" type="time" class="form-control" formControlName="openingTime" /></div>
            <div class="col-md-3"><label class="form-label" for="closingTime">Cierre</label>
              <input id="closingTime" type="time" class="form-control" formControlName="closingTime" /></div>
            <div class="col-12 d-flex gap-2">
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar</button>
              <button type="button" class="btn btn-light" (click)="form = null">Cancelar</button>
            </div>
          </form>
        </div>
      </div>

      @if (editingId) {
        <div class="card mt-4">
          <div class="card-body">
            <h2 class="fs-6 mb-1">Carta pública (QR)</h2>
            <p class="text-secondary small">
              Slug con el que los clientes acceden a la carta de esta sucursal. Solo
              minúsculas, números y guiones (3–60).
            </p>

            @if (slugError()) { <div class="alert alert-danger py-2 small">{{ slugError() }}</div> }

            <div class="row g-3 align-items-end">
              <div class="col-sm-6">
                <label class="form-label" for="publicSlug">Slug</label>
                <div class="input-group">
                  <input id="publicSlug" class="form-control" [(ngModel)]="slugValue"
                    placeholder="p. ej. centro" [disabled]="slugSaving()" />
                  <button type="button" class="btn btn-primary" (click)="saveSlug()"
                    [disabled]="slugSaving()">Guardar</button>
                  @if (currentSlug()) {
                    <button type="button" class="btn btn-outline-secondary" (click)="clearSlug()"
                      [disabled]="slugSaving()">Quitar</button>
                  }
                </div>
              </div>

              @if (currentSlug()) {
                <div class="col-sm-6">
                  <label class="form-label" for="publicUrl">Enlace</label>
                  <div class="input-group">
                    <input id="publicUrl" class="form-control" [value]="publicUrl()" readonly />
                    <button type="button" class="btn btn-outline-secondary" (click)="copyUrl()">
                      <i class="ti ti-copy"></i>{{ copied() ? ' Copiado' : '' }}
                    </button>
                  </div>
                </div>
              }
            </div>

            @if (currentSlug() && qrSrc(); as src) {
              <div class="mt-3">
                <img [src]="src" alt="QR de la carta" width="180" height="180"
                  style="image-rendering: pixelated; border: 1px solid var(--bs-border-color)" />
                <div>
                  <button type="button" class="btn btn-light btn-sm mt-2" (click)="downloadQr()">
                    <i class="ti ti-download me-1"></i>Descargar PNG
                  </button>
                </div>
              </div>
            }
          </div>
        </div>
      }
    }
  `,
})
export class BranchesPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OrgApiService);

  protected readonly rows = signal<Branch[]>([]);
  protected readonly restaurants = signal<Restaurant[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected form: ReturnType<BranchesPage['buildForm']> | null = null;
  protected editingId: number | null = null;

  // Carta pública / QR (Fase 11)
  protected slugValue = '';
  protected readonly currentSlug = signal<string | null>(null);
  protected readonly slugSaving = signal(false);
  protected readonly slugError = signal<string | null>(null);
  protected readonly copied = signal(false);
  protected readonly publicUrl = computed(() =>
    this.currentSlug() ? `${location.origin}/carta/${this.currentSlug()}` : '',
  );
  protected readonly qrSrc = computed(() =>
    this.currentSlug() ? qrDataUrl(this.publicUrl()) : null,
  );

  constructor() {
    this.reload();
    this.api.listRestaurants().subscribe((p) => this.restaurants.set(p.items));
  }

  private reload(): void {
    this.api.listBranches().subscribe((p) => this.rows.set(p.items));
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      restaurantId: [this.restaurants()[0]?.id ?? 1, Validators.required],
      name: ['', Validators.required],
      address: ['', Validators.required],
      phone: [''],
      email: [''],
      openingTime: ['08:00', Validators.required],
      closingTime: ['23:00', Validators.required],
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.error.set(null);
    this.form = this.buildForm();
    this.currentSlug.set(null);
    this.slugValue = '';
    this.slugError.set(null);
  }

  protected openEdit(b: Branch): void {
    this.editingId = b.id;
    this.error.set(null);
    this.currentSlug.set(b.publicSlug);
    this.slugValue = b.publicSlug ?? '';
    this.slugError.set(null);
    this.copied.set(false);
    this.form = this.buildForm();
    this.form.patchValue({
      restaurantId: b.restaurantId,
      name: b.name,
      address: b.address,
      phone: b.phone,
      email: b.email,
      openingTime: b.openingTime.slice(0, 5),
      closingTime: b.closingTime.slice(0, 5),
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
      .saveBranch(
        {
          restaurantId: Number(v.restaurantId),
          name: v.name,
          address: v.address,
          phone: v.phone,
          email: v.email,
          openingTime: `${v.openingTime}:00`,
          closingTime: `${v.closingTime}:00`,
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

  protected saveSlug(): void {
    if (this.editingId === null) {
      return;
    }
    const value = this.slugValue.trim();
    this.slugSaving.set(true);
    this.slugError.set(null);
    this.api.setBranchPublicSlug(this.editingId, value || null).subscribe({
      next: () => {
        this.slugSaving.set(false);
        this.currentSlug.set(value || null);
        this.reload();
      },
      error: (err) => {
        this.slugSaving.set(false);
        this.slugError.set(apiErrorMessage(err));
      },
    });
  }

  protected clearSlug(): void {
    this.slugValue = '';
    this.saveSlug();
  }

  protected copyUrl(): void {
    navigator.clipboard?.writeText(this.publicUrl()).then(
      () => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 1500);
      },
      () => undefined,
    );
  }

  protected downloadQr(): void {
    const src = this.qrSrc();
    if (!src) {
      return;
    }
    const img = new Image();
    img.onload = () => {
      const size = 512;
      const canvas = document.createElement('canvas');
      canvas.width = size;
      canvas.height = size;
      const ctx = canvas.getContext('2d');
      if (!ctx) {
        return;
      }
      ctx.imageSmoothingEnabled = false;
      ctx.drawImage(img, 0, 0, size, size);
      const a = document.createElement('a');
      a.href = canvas.toDataURL('image/png');
      a.download = `carta-${this.currentSlug()}.png`;
      a.click();
    };
    img.src = src;
  }
}
