import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/api-envelope.model';
import {
  ChangeResourceQuotationStatusRequest,
  ChangeResourceQuotationStatusResponse,
  CopyResourceRequest,
  CreateResourceRequest,
  Resource,
  ResourceListQuery,
  ResourceSuggestionsResponse,
  ResourceTypeOption,
  UpdateResourceRequest
} from '../models/resource.model';

/** Insumos del proyecto (`REQ-INSUMOS-06`…`08`, `REQ-INSUMOS-18`). */
@Injectable({ providedIn: 'root' })
export class ResourceService {
  private readonly http = inject(HttpClient);

  private base(projectId: string): string {
    return `${environment.apiUrl}/projects/${projectId}/resources`;
  }

  list(projectId: string, query: ResourceListQuery = {}) {
    let params = new HttpParams();

    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.resourceTypeId) params = params.set('resourceTypeId', query.resourceTypeId);
    if (query.quotationStatus) params = params.set('quotationStatus', query.quotationStatus);
    if (query.brand?.trim()) params = params.set('brand', query.brand.trim());
    if (query.municipalityId) params = params.set('municipalityId', query.municipalityId);
    if (query.minUnitPrice !== undefined && query.minUnitPrice !== null) {
      params = params.set('minUnitPrice', query.minUnitPrice);
    }
    if (query.maxUnitPrice !== undefined && query.maxUnitPrice !== null) {
      params = params.set('maxUnitPrice', query.maxUnitPrice);
    }

    params = params.set('page', query.page ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

    return this.http.get<PagedResult<Resource>>(this.base(projectId), { params });
  }

  getById(projectId: string, resourceId: string) {
    return this.http.get<Resource>(`${this.base(projectId)}/${resourceId}`);
  }

  byUnit(projectId: string, shortUnit: string) {
    return this.http.get<Resource[]>(
      `${this.base(projectId)}/by-unit/${encodeURIComponent(shortUnit)}`
    );
  }

  types(projectId: string) {
    return this.http.get<ResourceTypeOption[]>(
      `${environment.apiUrl}/projects/${projectId}/resource-types`
    );
  }

  suggestions(projectId: string, description?: string, userKey?: string) {
    let params = new HttpParams();
    if (description?.trim()) params = params.set('description', description.trim());
    if (userKey?.trim()) params = params.set('userKey', userKey.trim());

    return this.http.get<ResourceSuggestionsResponse>(`${this.base(projectId)}/suggestions`, {
      params
    });
  }

  create(projectId: string, request: CreateResourceRequest) {
    return this.http.post<Resource>(this.base(projectId), request);
  }

  update(projectId: string, resourceId: string, request: UpdateResourceRequest) {
    return this.http.put<Resource>(`${this.base(projectId)}/${resourceId}`, request);
  }

  delete(projectId: string, resourceId: string) {
    return this.http.delete<void>(`${this.base(projectId)}/${resourceId}`);
  }

  copy(projectId: string, request: CopyResourceRequest) {
    return this.http.post<Resource>(`${this.base(projectId)}/copy`, request);
  }

  changeQuotationStatus(projectId: string, request: ChangeResourceQuotationStatusRequest) {
    return this.http.patch<ChangeResourceQuotationStatusResponse>(
      `${this.base(projectId)}/quotation-status`,
      request
    );
  }
}
