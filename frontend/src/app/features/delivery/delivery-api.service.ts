import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import { Delivery, Driver, Paged, SaveDriverBody } from './delivery.models';

export type DeliveryStep = 'in-transit' | 'delivered' | 'failed';

@Injectable({ providedIn: 'root' })
export class DeliveryApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Repartidores ----
  listDrivers(search?: string): Observable<Paged<Driver>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Driver>>(`${this.base}/delivery-drivers`, { params });
  }
  getDriver(id: number): Observable<Driver> {
    return this.http.get<Driver>(`${this.base}/delivery-drivers/${id}`);
  }
  saveDriver(body: SaveDriverBody, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/delivery-drivers/${id}`, body)
      : this.http.post(`${this.base}/delivery-drivers`, body);
  }

  // ---- Entregas (sucursal activa, X-Branch-Id) ----
  listDeliveries(status?: string): Observable<Delivery[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Delivery[]>(`${this.base}/deliveries`, { params });
  }
  getDelivery(id: number): Observable<Delivery> {
    return this.http.get<Delivery>(`${this.base}/deliveries/${id}`);
  }
  assignDriver(deliveryId: number, driverId: number): Observable<unknown> {
    return this.http.post(`${this.base}/deliveries/${deliveryId}/assign`, { driverId });
  }
  advance(deliveryId: number, step: DeliveryStep): Observable<unknown> {
    return this.http.post(`${this.base}/deliveries/${deliveryId}/${step}`, {});
  }
  cancelDelivery(deliveryId: number): Observable<unknown> {
    return this.http.post(`${this.base}/deliveries/${deliveryId}/cancel`, {});
  }
}
