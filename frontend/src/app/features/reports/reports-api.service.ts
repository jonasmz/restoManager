import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  DashboardPayload, DiscountApplied, LowStockLine, PaymentMethodTotal, PurchasingCost,
  SalesBucket, SalesGroupBy, SalesSummary, TableTurnover, TopProduct,
} from './reports.models';

/** Rango de fechas para un reporte (medias-abiertas [from, to)). */
export interface DateRange {
  from?: string;
  to?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1/reports`;

  private range(r: DateRange): HttpParams {
    let p = new HttpParams();
    if (r.from) {
      p = p.set('from', r.from);
    }
    if (r.to) {
      p = p.set('to', r.to);
    }
    return p;
  }

  dashboard(r: DateRange = {}): Observable<DashboardPayload> {
    return this.http.get<DashboardPayload>(`${this.base}/dashboard`, { params: this.range(r) });
  }

  salesSummary(r: DateRange): Observable<SalesSummary> {
    return this.http.get<SalesSummary>(`${this.base}/sales/summary`, { params: this.range(r) });
  }

  salesBy(groupBy: SalesGroupBy, r: DateRange): Observable<SalesBucket[]> {
    return this.http.get<SalesBucket[]>(`${this.base}/sales`, {
      params: this.range(r).set('groupBy', groupBy),
    });
  }

  topProducts(r: DateRange, limit = 10): Observable<TopProduct[]> {
    return this.http.get<TopProduct[]>(`${this.base}/products/top`, {
      params: this.range(r).set('limit', limit),
    });
  }

  paymentsByMethod(r: DateRange): Observable<PaymentMethodTotal[]> {
    return this.http.get<PaymentMethodTotal[]>(`${this.base}/payments/by-method`, { params: this.range(r) });
  }

  discountsApplied(r: DateRange): Observable<DiscountApplied[]> {
    return this.http.get<DiscountApplied[]>(`${this.base}/discounts/applied`, { params: this.range(r) });
  }

  lowStock(): Observable<LowStockLine[]> {
    return this.http.get<LowStockLine[]>(`${this.base}/inventory/low-stock`);
  }

  purchasingCost(r: DateRange): Observable<PurchasingCost> {
    return this.http.get<PurchasingCost>(`${this.base}/purchasing/cost`, { params: this.range(r) });
  }

  tableTurnover(r: DateRange): Observable<TableTurnover[]> {
    return this.http.get<TableTurnover[]>(`${this.base}/tables/turnover`, { params: this.range(r) });
  }
}
