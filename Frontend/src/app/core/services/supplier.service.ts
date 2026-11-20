import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { CreateSupplierRequest, SupplierOptionsResponse, SupplierSummary } from '../models/resource.model';

/** Directorio de proveedores de la compañía (`GET /suppliers/options`, `POST /suppliers`). */
@Injectable({ providedIn: 'root' })
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/suppliers`;

  options(query: { businessName?: string; companyId?: string; limit?: number } = {}) {
    let params = new HttpParams();
    if (query.businessName?.trim()) params = params.set('businessName', query.businessName.trim());
    if (query.companyId) params = params.set('companyId', query.companyId);
    params = params.set('limit', query.limit ?? 50);

    return this.http.get<SupplierOptionsResponse>(`${this.baseUrl}/options`, { params });
  }

  /** Alta de proveedor en la compañía del actor (`REQ-PROVEEDORES-01`). */
  create(request: CreateSupplierRequest) {
    return this.http.post<SupplierSummary>(this.baseUrl, request);
  }
}
