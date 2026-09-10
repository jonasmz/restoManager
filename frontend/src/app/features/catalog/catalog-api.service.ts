import { HttpBackend, HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../core/config/environment';
import { PublicCatalog } from './catalog.models';

/**
 * Cliente de la carta pública. Usa `HttpBackend` para saltarse los interceptores de
 * token y de sucursal: la carta es anónima y un empleado con sesión abierta no debe
 * filtrar su `Authorization` / `X-Branch-Id` al abrirla.
 */
@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly base = `${environment.businessApiUrl}/api/v1/public`;

  getCatalog(slug: string): Observable<PublicCatalog> {
    return this.http.get<PublicCatalog>(`${this.base}/catalog/${encodeURIComponent(slug)}`);
  }

  /** URL absoluta de una imagen de plato a partir de la ruta relativa del DTO. */
  imageUrl(relative: string | null): string | null {
    return relative ? `${environment.businessApiUrl}${relative}` : null;
  }
}
