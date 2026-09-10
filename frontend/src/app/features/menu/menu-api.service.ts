import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  Category, KitchenStation, MenuItem, MenuItemAvailability, MenuItemCost, Paged,
  SaveMenuItemBody, TaxRate,
} from './menu.models';

@Injectable({ providedIn: 'root' })
export class MenuApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Categorías (catálogo global) ----
  listCategories(search?: string): Observable<Paged<Category>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Category>>(`${this.base}/categories`, { params });
  }
  saveCategory(body: Omit<Category, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/categories/${id}`, body)
      : this.http.post(`${this.base}/categories`, body);
  }

  // ---- Platos (catálogo global; receta e impuestos en el mismo cuerpo) ----
  listMenuItems(filter: { categoryId?: number; search?: string } = {}): Observable<Paged<MenuItem>> {
    let params = new HttpParams().set('pageSize', 200);
    if (filter.categoryId) {
      params = params.set('categoryId', filter.categoryId);
    }
    if (filter.search) {
      params = params.set('search', filter.search);
    }
    return this.http.get<Paged<MenuItem>>(`${this.base}/menu-items`, { params });
  }
  getMenuItem(id: number): Observable<MenuItem> {
    return this.http.get<MenuItem>(`${this.base}/menu-items/${id}`);
  }
  saveMenuItem(body: SaveMenuItemBody, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/menu-items/${id}`, body)
      : this.http.post(`${this.base}/menu-items`, body);
  }
  menuItemCost(id: number): Observable<MenuItemCost> {
    return this.http.get<MenuItemCost>(`${this.base}/menu-items/${id}/cost`);
  }

  // ---- Imagen ilustrativa del plato (Fase 11) ----
  uploadMenuItemImage(id: number, file: File): Observable<{ imageUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.put<{ imageUrl: string }>(`${this.base}/menu-items/${id}/image`, form);
  }
  deleteMenuItemImage(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/menu-items/${id}/image`);
  }

  // ---- Disponibilidad por sucursal activa (X-Branch-Id) ----
  getAvailability(id: number): Observable<MenuItemAvailability> {
    return this.http.get<MenuItemAvailability>(`${this.base}/menu-items/${id}/availability`);
  }
  setAvailability(id: number, isAvailable: boolean): Observable<unknown> {
    return this.http.put(`${this.base}/menu-items/${id}/availability`, { isAvailable });
  }

  // ---- Estaciones de cocina (sucursal activa) ----
  listStations(): Observable<KitchenStation[]> {
    return this.http.get<KitchenStation[]>(`${this.base}/kitchen-stations`);
  }
  saveStation(body: { name: string; description: string }, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/kitchen-stations/${id}`, body)
      : this.http.post(`${this.base}/kitchen-stations`, body);
  }
  setStationMenuItems(id: number, menuItemIds: number[]): Observable<unknown> {
    return this.http.put(`${this.base}/kitchen-stations/${id}/menu-items`, { menuItemIds });
  }

  // ---- Tasas de impuesto (catálogo global) ----
  listTaxRates(search?: string): Observable<Paged<TaxRate>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<TaxRate>>(`${this.base}/tax-rates`, { params });
  }
  saveTaxRate(body: Omit<TaxRate, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/tax-rates/${id}`, body)
      : this.http.post(`${this.base}/tax-rates`, body);
  }
}
