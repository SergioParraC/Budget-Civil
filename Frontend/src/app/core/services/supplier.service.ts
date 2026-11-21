import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CreateSupplierRequest,
  SupplierDetail,
  SupplierOptionsResponse,
  SupplierSummary,
  UpdateSupplierRequest
} from '../models/resource.model';

/** Directorio de proveedores de la compañía (`GET /suppliers/options`, `POST /suppliers`, `PUT /suppliers/{id}`). */
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

  /** Detalle completo del proveedor (`REQ-PROVEEDORES-02`, `REQ-PROVEEDORES-03`). */
  getById(supplierId: string) {
    return this.http.get<SupplierDetail>(`${this.baseUrl}/${supplierId}`);
  }

  /** Alta de proveedor en la compañía del actor (`REQ-PROVEEDORES-01`). */
  create(request: CreateSupplierRequest) {
    return this.http.post<SupplierSummary>(this.baseUrl, request);
  }

  /** Edición del proveedor con control de concurrencia por `rowVersion` (`REQ-PROVEEDORES-03`). */
  update(supplierId: string, request: UpdateSupplierRequest) {
    return this.http.put<SupplierDetail>(`${this.baseUrl}/${supplierId}`, request);
  }
}
