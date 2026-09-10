import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { apiErrorMessage } from '../../core/http/api-error';
import { CustomersApiService } from './customers-api.service';
import { Customer, CustomerOrder, GiftCard } from './customers.models';

type Tab = 'data' | 'orders' | 'loyalty' | 'giftcards';

@Component({
  selector: 'app-customer-detail',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/customers" class="small text-decoration-none"><i class="ti ti-arrow-left me-1"></i>Clientes</a>

    @if (customer(); as c) {
      <h1 class="fs-3 mb-1 mt-1">{{ c.lastName }}, {{ c.firstName }}</h1>
      <p class="text-secondary small mb-4">
        {{ c.phone }}@if (c.email) { · {{ c.email }} } ·
        <span class="badge bg-primary-subtle text-primary">{{ c.loyaltyPoints }} puntos</span>
      </p>
    } @else {
      <h1 class="fs-3 mb-4 mt-1">Cliente #{{ customerId }}</h1>
    }

    @if (message()) {
      <div class="alert py-2 small" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ message() }}</div>
    }

    <ul class="nav nav-tabs mb-3">
      <li class="nav-item"><button type="button" class="nav-link" [class.active]="tab() === 'data'" (click)="tab.set('data')">Datos</button></li>
      <li class="nav-item"><button type="button" class="nav-link" [class.active]="tab() === 'orders'" (click)="tab.set('orders')">Pedidos</button></li>
      <li class="nav-item"><button type="button" class="nav-link" [class.active]="tab() === 'loyalty'" (click)="tab.set('loyalty')">Puntos</button></li>
      <li class="nav-item"><button type="button" class="nav-link" [class.active]="tab() === 'giftcards'" (click)="tab.set('giftcards')">Gift cards</button></li>
    </ul>

    @switch (tab()) {
      @case ('data') {
        <div class="card"><div class="card-body">
          <form [formGroup]="form" (ngSubmit)="saveData()" class="row g-3" style="max-width: 640px">
            <div class="col-md-6"><label class="form-label" for="fn">Nombre</label>
              <input id="fn" class="form-control" formControlName="firstName" /></div>
            <div class="col-md-6"><label class="form-label" for="ln">Apellido</label>
              <input id="ln" class="form-control" formControlName="lastName" /></div>
            <div class="col-md-6"><label class="form-label" for="ph">Teléfono</label>
              <input id="ph" class="form-control" formControlName="phone" /></div>
            <div class="col-md-6"><label class="form-label" for="em">Email</label>
              <input id="em" type="email" class="form-control" formControlName="email" /></div>
            <div class="col-12"><button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">Guardar</button></div>
          </form>
        </div></div>
      }

      @case ('orders') {
        <div class="card"><div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead><tr><th>#</th><th>Canal</th><th>Estado</th><th class="text-end">Total</th><th>Fecha</th></tr></thead>
            <tbody>
              @for (o of orders(); track o.id) {
                <tr>
                  <td>{{ o.id }}</td>
                  <td class="small">{{ channelLabel(o.channel) }}</td>
                  <td><span class="badge" [class]="statusBadge(o.status)">{{ o.status }}</span></td>
                  <td class="text-end small">{{ o.totalAmount | currency }}</td>
                  <td class="small text-secondary">{{ o.orderTime | date: 'short' }}</td>
                </tr>
              } @empty { <tr><td colspan="5" class="text-center text-secondary py-4">Sin pedidos.</td></tr> }
            </tbody>
          </table>
        </div></div>
      }

      @case ('loyalty') {
        <div class="card"><div class="card-body">
          <p class="mb-1 text-secondary small">Saldo de puntos de fidelidad</p>
          <p class="display-6 mb-0">{{ customer()?.loyaltyPoints ?? 0 }}</p>
          <p class="text-secondary small mt-3 mb-0">
            Se acumulan al cerrar un pedido identificado con este cliente y se canjean como descuento
            desde el pedido abierto en el punto de venta.
          </p>
        </div></div>
      }

      @case ('giftcards') {
        <div class="card mb-3"><div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead><tr><th>Número</th><th class="text-end">Saldo</th><th>Caduca</th><th></th></tr></thead>
            <tbody>
              @for (g of giftCards(); track g.id) {
                <tr>
                  <td class="small">{{ g.cardNumber }}</td>
                  <td class="text-end small">{{ g.balance | currency }}</td>
                  <td class="small text-secondary">{{ g.expiryDate }}</td>
                  <td>@if (g.expired) { <span class="badge bg-danger-subtle text-danger">Caducada</span> }</td>
                </tr>
              } @empty { <tr><td colspan="4" class="text-center text-secondary py-4">Sin gift cards.</td></tr> }
            </tbody>
          </table>
        </div></div>

        <div class="card"><div class="card-body">
          <h2 class="fs-6 mb-3">Emitir gift card</h2>
          <form [formGroup]="giftForm" (ngSubmit)="issueGift()" class="row g-3" style="max-width: 640px">
            <div class="col-md-5"><label class="form-label" for="cn">Número de tarjeta</label>
              <input id="cn" class="form-control" formControlName="cardNumber" placeholder="GC-0001" /></div>
            <div class="col-md-3"><label class="form-label" for="ib">Saldo inicial</label>
              <input id="ib" type="number" step="0.01" min="0.01" class="form-control" formControlName="initialBalance" /></div>
            <div class="col-md-4"><label class="form-label" for="ed">Caducidad</label>
              <input id="ed" type="date" class="form-control" formControlName="expiryDate" /></div>
            <div class="col-12"><button type="submit" class="btn btn-primary" [disabled]="giftForm.invalid || saving()">Emitir</button></div>
          </form>
        </div></div>
      }
    }
  `,
})
export class CustomerDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CustomersApiService);

  protected readonly customerId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly tab = signal<Tab>('data');
  protected readonly customer = signal<Customer | null>(null);
  protected readonly orders = signal<CustomerOrder[]>([]);
  protected readonly giftCards = signal<GiftCard[]>([]);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly ok = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.maxLength(20)]],
    email: ['', [Validators.email, Validators.maxLength(100)]],
  });

  protected readonly giftForm = this.fb.nonNullable.group({
    cardNumber: ['', [Validators.required, Validators.maxLength(50)]],
    initialBalance: [0, [Validators.required, Validators.min(0.01)]],
    expiryDate: [this.defaultExpiry(), Validators.required],
  });

  protected readonly channelLabel = (c: string) =>
    ({ MESA: 'Mesa', BARRA: 'Barra', TAKEAWAY: 'Para llevar', DELIVERY: 'Delivery' } as Record<string, string>)[c] ?? c;

  protected statusBadge(s: string): string {
    switch (s) {
      case 'OPEN': return 'bg-primary-subtle text-primary';
      case 'PAID': return 'bg-success-subtle text-success';
      case 'CLOSED': return 'bg-secondary-subtle text-secondary';
      default: return 'bg-danger-subtle text-danger';
    }
  }

  constructor() {
    this.reload();
  }

  private reload(): void {
    forkJoin({
      customer: this.api.getCustomer(this.customerId),
      orders: this.api.listCustomerOrders(this.customerId),
      cards: this.api.listCustomerGiftCards(this.customerId),
    }).subscribe({
      next: ({ customer, orders, cards }) => {
        this.customer.set(customer);
        this.orders.set(orders.items);
        this.giftCards.set(cards);
        this.form.patchValue({
          firstName: customer.firstName, lastName: customer.lastName,
          phone: customer.phone, email: customer.email,
        });
      },
      error: (err) => this.fail(err),
    });
  }

  private defaultExpiry(): string {
    const d = new Date();
    d.setFullYear(d.getFullYear() + 1);
    return d.toISOString().slice(0, 10);
  }

  protected saveData(): void {
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this.message.set(null);
    this.api.saveCustomer(this.form.getRawValue(), this.customerId).subscribe({
      next: () => {
        this.saving.set(false);
        this.done('Datos guardados.');
      },
      error: (err) => {
        this.saving.set(false);
        this.fail(err);
      },
    });
  }

  protected issueGift(): void {
    if (this.giftForm.invalid) {
      return;
    }
    const v = this.giftForm.getRawValue();
    this.saving.set(true);
    this.message.set(null);
    this.api.issueGiftCard({
      customerId: this.customerId,
      cardNumber: v.cardNumber,
      initialBalance: Number(v.initialBalance),
      expiryDate: v.expiryDate,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.giftForm.reset({ cardNumber: '', initialBalance: 0, expiryDate: this.defaultExpiry() });
        this.done('Gift card emitida.');
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
