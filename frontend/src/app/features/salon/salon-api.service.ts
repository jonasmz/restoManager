import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  Customer, FloorTable, Paged, Reservation, Table, TableOperationalStatus,
} from './salon.models';

@Injectable({ providedIn: 'root' })
export class SalonApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Mesas (sucursal activa) ----
  listTables(): Observable<Table[]> {
    return this.http.get<Table[]>(`${this.base}/tables`);
  }
  saveTable(body: { number: number; capacity: number }, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/tables/${id}`, body)
      : this.http.post(`${this.base}/tables`, body);
  }
  setTableStatus(id: number, operationalStatus: TableOperationalStatus): Observable<unknown> {
    return this.http.put(`${this.base}/tables/${id}/status`, { operationalStatus });
  }

  // ---- Sesiones ----
  openSession(tableId: number, guestCount: number): Observable<unknown> {
    return this.http.post(`${this.base}/tables/${tableId}/sessions`, { guestCount });
  }
  closeSession(tableId: number, sessionId: number): Observable<unknown> {
    return this.http.post(`${this.base}/tables/${tableId}/sessions/${sessionId}/close`, {});
  }

  // ---- Tablero ----
  floor(): Observable<FloorTable[]> {
    return this.http.get<FloorTable[]>(`${this.base}/floor`);
  }

  // ---- Reservas ----
  listReservations(filter: { date?: string; status?: string } = {}): Observable<Reservation[]> {
    let params = new HttpParams();
    if (filter.date) {
      params = params.set('date', filter.date);
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }
    return this.http.get<Reservation[]>(`${this.base}/reservations`, { params });
  }
  createReservation(body: {
    customerId: number; tableId: number; reservationTime: string; partySize: number;
  }): Observable<unknown> {
    return this.http.post(`${this.base}/reservations`, body);
  }
  reservationAction(id: number, action: 'confirm' | 'cancel' | 'no-show'): Observable<unknown> {
    return this.http.post(`${this.base}/reservations/${id}/${action}`, {});
  }
  seatReservation(id: number, guestCount: number): Observable<unknown> {
    return this.http.post(`${this.base}/reservations/${id}/seat`, { guestCount });
  }

  // ---- Clientes (alta rápida) ----
  listCustomers(search?: string): Observable<Paged<Customer>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Customer>>(`${this.base}/customers`, { params });
  }
  createCustomer(body: {
    firstName: string; lastName: string; phone: string; email: string;
  }): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/customers`, body);
  }
}
