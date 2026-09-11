import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  Ingredient, Movement, Paged, PurchaseOrder, PurchaseOrderLine, StockLine, Supplier,
} from './inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Ingredientes ----
  listIngredients(search?: string): Observable<Paged<Ingredient>> {
    let params = new HttpParams().set('pageSize', 200);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Ingredient>>(`${this.base}/ingredients`, { params });
  }
  saveIngredient(body: Omit<Ingredient, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/ingredients/${id}`, body)
      : this.http.post(`${this.base}/ingredients`, body);
  }
  /** Baja lógica (issue #47): no afecta recetas/movimientos/compras que ya lo referencian. */
  deleteIngredient(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/ingredients/${id}`);
  }

  // ---- Existencias y movimientos (sucursal activa) ----
  stock(): Observable<StockLine[]> {
    return this.http.get<StockLine[]>(`${this.base}/inventory`);
  }
  movements(filter: { ingredientId?: number; type?: string; from?: string; to?: string }): Observable<Movement[]> {
    let params = new HttpParams();
    if (filter.ingredientId) params = params.set('ingredientId', filter.ingredientId);
    if (filter.type) params = params.set('type', filter.type);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    return this.http.get<Movement[]>(`${this.base}/inventory/movements`, { params });
  }

  initialLoad(body: { ingredientId: number; quantity: number }): Observable<unknown> {
    return this.http.post(`${this.base}/inventory/initial-load`, body);
  }
  adjust(body: { ingredientId: number; quantity: number; reason: string }): Observable<unknown> {
    return this.http.post(`${this.base}/inventory/adjustments`, body);
  }
  registerWaste(body: { ingredientId: number; quantity: number; reason: string }): Observable<unknown> {
    return this.http.post(`${this.base}/inventory/waste`, body);
  }

  // ---- Proveedores ----
  listSuppliers(): Observable<Paged<Supplier>> {
    return this.http.get<Paged<Supplier>>(`${this.base}/suppliers`, { params: new HttpParams().set('pageSize', 200) });
  }
  saveSupplier(body: Omit<Supplier, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/suppliers/${id}`, body)
      : this.http.post(`${this.base}/suppliers`, body);
  }

  // ---- Órdenes de compra (sucursal activa) ----
  listPurchaseOrders(status?: string): Observable<Paged<PurchaseOrder>> {
    let params = new HttpParams().set('pageSize', 100);
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Paged<PurchaseOrder>>(`${this.base}/purchase-orders`, { params });
  }
  createPurchaseOrder(body: { supplierId: number; orderDate: string; items: PurchaseOrderLine[] }): Observable<unknown> {
    return this.http.post(`${this.base}/purchase-orders`, body);
  }
  purchaseOrderAction(id: number, action: 'send' | 'receive' | 'cancel'): Observable<unknown> {
    return this.http.post(`${this.base}/purchase-orders/${id}/${action}`, {});
  }
}
