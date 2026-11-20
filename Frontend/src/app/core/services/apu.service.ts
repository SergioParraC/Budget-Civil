import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  AddMatrixComponentRequest,
  ApuDetail,
  ApuListResponse,
  MatrixDetail,
  UpdateMatrixComponentRequest
} from '../models/apu.model';

/** APUs del proyecto y composición de Matrices (`REQ-APUS-19`, `REQ-APUS-06`). */
@Injectable({ providedIn: 'root' })
export class ApuService {
  private readonly http = inject(HttpClient);

  private base(projectId: string): string {
    return `${environment.apiUrl}/projects/${projectId}`;
  }

  list(
    projectId: string,
    query: {
      search?: string;
      departmentId?: string;
      municipalityId?: string;
      page?: number;
      pageSize?: 10 | 50 | 100;
    } = {}
  ) {
    let params = new HttpParams();
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.departmentId) params = params.set('departmentId', query.departmentId);
    if (query.municipalityId) params = params.set('municipalityId', query.municipalityId);
    params = params.set('page', query.page ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<ApuListResponse>(`${this.base(projectId)}/apus`, { params });
  }

  getById(projectId: string, apuId: string) {
    return this.http.get<ApuDetail>(`${this.base(projectId)}/apus/${apuId}`);
  }

  getMatrixComponents(projectId: string, matrixResourceId: string) {
    return this.http.get<MatrixDetail>(
      `${this.base(projectId)}/matrices/${matrixResourceId}/components`
    );
  }

  addMatrixComponent(
    projectId: string,
    matrixResourceId: string,
    request: AddMatrixComponentRequest
  ) {
    return this.http.post<MatrixDetail>(
      `${this.base(projectId)}/matrices/${matrixResourceId}/components`,
      request
    );
  }

  updateMatrixComponent(
    projectId: string,
    matrixResourceId: string,
    componentId: string,
    request: UpdateMatrixComponentRequest
  ) {
    return this.http.put<MatrixDetail>(
      `${this.base(projectId)}/matrices/${matrixResourceId}/components/${componentId}`,
      request
    );
  }

  removeMatrixComponent(
    projectId: string,
    matrixResourceId: string,
    componentId: string,
    rowVersion: string
  ) {
    return this.http.delete<void>(
      `${this.base(projectId)}/matrices/${matrixResourceId}/components/${componentId}`,
      { params: new HttpParams().set('rowVersion', rowVersion) }
    );
  }
}
