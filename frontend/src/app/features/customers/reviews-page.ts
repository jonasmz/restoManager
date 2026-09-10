import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { BranchContextService } from '../../core/branch/branch-context.service';
import { apiErrorMessage } from '../../core/http/api-error';
import { CustomersApiService } from './customers-api.service';
import { Customer, Review } from './customers.models';

@Component({
  selector: 'app-reviews',
  imports: [ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex justify-content-between align-items-start mb-6">
      <div>
        <h1 class="fs-3 mb-1">Reseñas</h1>
        <p class="text-secondary mb-0">
          Valoraciones de clientes sobre <strong>{{ branch.activeBranch()?.name ?? 'la sucursal activa' }}</strong>.
        </p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" (click)="showForm.set(!showForm())">
        <i class="ti ti-plus me-1"></i>Nueva reseña
      </button>
    </div>

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    @if (showForm()) {
      <div class="card mb-4"><div class="card-body">
        <form [formGroup]="form" (ngSubmit)="create()" class="row g-3" style="max-width: 720px">
          <div class="col-md-5">
            <label class="form-label" for="cust">Cliente</label>
            <select id="cust" class="form-select" formControlName="customerId">
              <option [value]="0" disabled>Selecciona…</option>
              @for (c of customers(); track c.id) { <option [value]="c.id">{{ c.lastName }}, {{ c.firstName }}</option> }
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label" for="rt">Valoración</label>
            <select id="rt" class="form-select" formControlName="rating">
              @for (n of [1, 2, 3, 4, 5]; track n) { <option [value]="n">{{ n }} ★</option> }
            </select>
          </div>
          <div class="col-12">
            <label class="form-label" for="cm">Comentario (opcional)</label>
            <textarea id="cm" class="form-control" rows="2" maxlength="500" formControlName="comment"></textarea>
          </div>
          <div class="col-12 d-flex gap-2">
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar</button>
            <button type="button" class="btn btn-light" (click)="showForm.set(false)">Cancelar</button>
          </div>
        </form>
      </div></div>
    }

    <div class="card mb-3"><div class="card-body py-2 d-flex align-items-center gap-2">
      <span class="small text-secondary">Filtrar:</span>
      <select class="form-select form-select-sm w-auto" [value]="minRating()" (change)="onFilter($any($event.target).value)">
        <option value="0">Todas</option>
        @for (n of [1, 2, 3, 4, 5]; track n) { <option [value]="n">{{ n }} ★ o más</option> }
      </select>
    </div></div>

    <div class="list-group">
      @for (r of rows(); track r.id) {
        <div class="list-group-item">
          <div class="d-flex justify-content-between">
            <span class="text-warning">{{ stars(r.rating) }}</span>
            <span class="small text-secondary">{{ customerName(r.customerId) }} · {{ r.reviewDate | date: 'mediumDate' }}</span>
          </div>
          @if (r.comment) { <p class="mb-0 mt-1 small">{{ r.comment }}</p> }
        </div>
      } @empty {
        <div class="list-group-item text-center text-secondary py-4">Sin reseñas para esta sucursal.</div>
      }
    </div>
  `,
})
export class ReviewsPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CustomersApiService);
  protected readonly branch = inject(BranchContextService);

  protected readonly rows = signal<Review[]>([]);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly minRating = signal(0);
  protected readonly showForm = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  private readonly customerById = computed(() => new Map(this.customers().map((c) => [c.id, c])));

  protected readonly form = this.fb.nonNullable.group({
    customerId: [0, [Validators.required, Validators.min(1)]],
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['', Validators.maxLength(500)],
  });

  constructor() {
    forkJoin({ reviews: this.api.listReviews(), customers: this.api.listCustomers() }).subscribe({
      next: ({ reviews, customers }) => {
        this.rows.set(reviews.items);
        this.customers.set(customers.items);
      },
      error: (err) => this.fail(err),
    });
  }

  protected stars(n: number): string {
    return '★★★★★'.slice(0, n) + '☆☆☆☆☆'.slice(0, 5 - n);
  }
  protected customerName(id: number): string {
    const c = this.customerById().get(id);
    return c ? `${c.lastName}, ${c.firstName}` : `Cliente #${id}`;
  }

  protected onFilter(value: string): void {
    this.minRating.set(Number(value));
    this.reload();
  }

  private reload(): void {
    const min = this.minRating();
    this.api.listReviews(min ? { minRating: min } : {}).subscribe({
      next: (p) => this.rows.set(p.items),
      error: (err) => this.fail(err),
    });
  }

  protected create(): void {
    if (this.form.invalid) {
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.message.set(null);
    this.api.createReview({
      customerId: Number(v.customerId),
      rating: Number(v.rating),
      comment: v.comment.trim() || null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.form.reset({ customerId: 0, rating: 5, comment: '' });
        this.ok.set(true);
        this.message.set('Reseña registrada.');
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
