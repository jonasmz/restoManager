import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  CreateOrderBody, Discount, Order, Paged, PaymentMethod, SaveDiscountBody,
} from './sales.models';

@Injectable({ providedIn: 'root' })
export class SalesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Pedidos (sucursal activa, X-Branch-Id) ----
  listOrders(
    filter: { channel?: string; status?: string; sessionId?: number; date?: string; page?: number } = {},
  ): Observable<Paged<Order>> {
    let params = new HttpParams().set('pageSize', 50);
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, value as string | number);
      }
    }
    return this.http.get<Paged<Order>>(`${this.base}/orders`, { params });
  }
  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.base}/orders/${id}`);
  }
  createOrder(body: CreateOrderBody): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/orders`, body);
  }

  // ---- Ítems ----
  addItem(orderId: number, body: { menuItemId: number; quantity: number; notes?: string | null }): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/orders/${orderId}/items`, body);
  }
  updateItem(orderId: number, itemId: number, body: { quantity: number; notes?: string | null }): Observable<unknown> {
    return this.http.put(`${this.base}/orders/${orderId}/items/${itemId}`, body);
  }
  removeItem(orderId: number, itemId: number): Observable<unknown> {
    return this.http.delete(`${this.base}/orders/${orderId}/items/${itemId}`);
  }

  // ---- Descuentos aplicados ----
  applyDiscount(orderId: number, discountId: number): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/orders/${orderId}/discounts`, { discountId });
  }
  removeOrderDiscount(orderId: number, discountId: number): Observable<unknown> {
    return this.http.delete(`${this.base}/orders/${orderId}/discounts/${discountId}`);
  }

  // ---- Pagos ----
  registerPayment(
    orderId: number, body: { method: PaymentMethod; amount: number; giftCardId?: number | null },
  ): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/orders/${orderId}/payments`, body);
  }

  // ---- Cierre / cancelación ----
  closeOrder(orderId: number): Observable<unknown> {
    return this.http.post(`${this.base}/orders/${orderId}/close`, {});
  }
  cancelOrder(orderId: number): Observable<unknown> {
    return this.http.post(`${this.base}/orders/${orderId}/cancel`, {});
  }

  // ---- Catálogo de descuentos ----
  listDiscounts(search?: string): Observable<Paged<Discount>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Discount>>(`${this.base}/discounts`, { params });
  }
  getDiscount(id: number): Observable<Discount> {
    return this.http.get<Discount>(`${this.base}/discounts/${id}`);
  }
  saveDiscount(body: SaveDiscountBody, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/discounts/${id}`, body)
      : this.http.post(`${this.base}/discounts`, body);
  }
}
