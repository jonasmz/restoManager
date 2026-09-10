import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import {
  Branch, Department, Employee, Leave, Paged, Restaurant, Role, Shift,
} from './org.models';

interface CreatedId {
  id: number;
}

@Injectable({ providedIn: 'root' })
export class OrgApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.businessApiUrl}/api/v1`;

  // ---- Restaurantes ----
  listRestaurants(): Observable<Paged<Restaurant>> {
    return this.http.get<Paged<Restaurant>>(`${this.base}/restaurants`);
  }
  saveRestaurant(body: Omit<Restaurant, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/restaurants/${id}`, body)
      : this.http.post<CreatedId>(`${this.base}/restaurants`, body);
  }

  // ---- Sucursales ----
  listBranches(): Observable<Paged<Branch>> {
    return this.http.get<Paged<Branch>>(`${this.base}/branches`, { params: new HttpParams().set('pageSize', 100) });
  }
  saveBranch(body: Omit<Branch, 'id' | 'publicSlug'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/branches/${id}`, body)
      : this.http.post<CreatedId>(`${this.base}/branches`, body);
  }
  /** Fija (string) o quita (null) el slug público de la carta. Fase 11. */
  setBranchPublicSlug(id: number, slug: string | null): Observable<unknown> {
    return this.http.put(`${this.base}/branches/${id}/public-slug`, { slug });
  }

  // ---- Puestos / roles ----
  listRoles(): Observable<Paged<Role>> {
    return this.http.get<Paged<Role>>(`${this.base}/roles`, { params: new HttpParams().set('pageSize', 100) });
  }
  saveRole(body: Omit<Role, 'id'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/roles/${id}`, body)
      : this.http.post<CreatedId>(`${this.base}/roles`, body);
  }

  // ---- Departamentos (sucursal activa) ----
  listDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.base}/departments`);
  }
  saveDepartment(body: { name: string; description: string }, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/departments/${id}`, body)
      : this.http.post<CreatedId>(`${this.base}/departments`, body);
  }

  // ---- Empleados (sucursal activa) ----
  listEmployees(search?: string): Observable<Paged<Employee>> {
    let params = new HttpParams().set('pageSize', 100);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<Paged<Employee>>(`${this.base}/employees`, { params });
  }
  getEmployee(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.base}/employees/${id}`);
  }
  saveEmployee(body: Omit<Employee, 'id' | 'branchId'>, id?: number): Observable<unknown> {
    return id
      ? this.http.put(`${this.base}/employees/${id}`, body)
      : this.http.post<CreatedId>(`${this.base}/employees`, body);
  }

  listShifts(employeeId: number): Observable<Shift[]> {
    return this.http.get<Shift[]>(`${this.base}/employees/${employeeId}/shifts`);
  }
  addShift(employeeId: number, body: { startTime: string; endTime: string; scheduledHours: number }): Observable<unknown> {
    return this.http.post(`${this.base}/employees/${employeeId}/shifts`, body);
  }
  removeShift(employeeId: number, shiftId: number): Observable<unknown> {
    return this.http.delete(`${this.base}/employees/${employeeId}/shifts/${shiftId}`);
  }

  listLeaves(employeeId: number): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.base}/employees/${employeeId}/leaves`);
  }
  requestLeave(employeeId: number, body: { startDate: string; endDate: string; leaveType: string }): Observable<unknown> {
    return this.http.post(`${this.base}/employees/${employeeId}/leaves`, body);
  }
  leaveAction(employeeId: number, leaveId: number, action: 'approve' | 'reject' | 'cancel'): Observable<unknown> {
    return this.http.post(`${this.base}/employees/${employeeId}/leaves/${leaveId}/${action}`, {});
  }
}
