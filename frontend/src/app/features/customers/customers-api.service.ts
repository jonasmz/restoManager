import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  CreateReviewBody, Customer, CustomerOrder, GiftCard, IssueGiftCardBody,
  LoyaltyBalance, LoyaltyRedemptionResult, Paged, Review, SaveCustomerBody,
} from './customers.models';

@Injectable({ providedIn: 'root' })
export class CustomersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Clientes (catálogo global) ----
  listCustomers(search?: string): Observable<Paged<Customer>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Customer>>(`${this.base}/customers`, { params });
  }
  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.base}/customers/${id}`);
  }
  saveCustomer(body: SaveCustomerBody, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/customers/${id}`, body)
      : this.http.post(`${this.base}/customers`, body);
  }

  // ---- Ficha: historial de pedidos y fidelización ----
  listCustomerOrders(id: number, page = 1): Observable<Paged<CustomerOrder>> {
    return this.http.get<Paged<CustomerOrder>>(`${this.base}/customers/${id}/orders`, {
      params: new HttpParams().set('page', page).set('pageSize', 20),
    });
  }
  getLoyalty(id: number): Observable<LoyaltyBalance> {
    return this.http.get<LoyaltyBalance>(`${this.base}/customers/${id}/loyalty`);
  }
  redeemLoyalty(id: number, body: { orderId: number; points: number }): Observable<LoyaltyRedemptionResult> {
    return this.http.post<LoyaltyRedemptionResult>(`${this.base}/customers/${id}/loyalty/redeem`, body);
  }

  // ---- Gift cards ----
  listCustomerGiftCards(id: number): Observable<GiftCard[]> {
    return this.http.get<GiftCard[]>(`${this.base}/customers/${id}/gift-cards`);
  }
  issueGiftCard(body: IssueGiftCardBody): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/gift-cards`, body);
  }
  getGiftCardBalance(cardNumber: string): Observable<GiftCard> {
    return this.http.get<GiftCard>(`${this.base}/gift-cards/${encodeURIComponent(cardNumber)}/balance`);
  }

  // ---- Reseñas (sucursal activa, X-Branch-Id) ----
  listReviews(filter: { minRating?: number; page?: number } = {}): Observable<Paged<Review>> {
    let params = new HttpParams().set('pageSize', 50);
    if (filter.minRating) {
      params = params.set('minRating', filter.minRating);
    }
    if (filter.page) {
      params = params.set('page', filter.page);
    }
    return this.http.get<Paged<Review>>(`${this.base}/reviews`, { params });
  }
  createReview(body: CreateReviewBody): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/reviews`, body);
  }
}
