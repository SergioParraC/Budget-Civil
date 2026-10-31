import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import { RoleCode } from '../models/company.model';
import { UserListItem } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  listAll(
    query: {
      email?: string;
      username?: string;
      fullName?: string;
      documentType?: string;
      documentNumber?: string;
      search?: string;
      roleCode?: string;
      unassignedOnly?: boolean;
      companyId?: string;
    } = {}
  ) {
    let params = new HttpParams();
    if (query.email) params = params.set('email', query.email);
    if (query.username) params = params.set('username', query.username);
    if (query.fullName) params = params.set('nombre_apellidos', query.fullName);
    if (query.documentType) params = params.set('tipo_documento', query.documentType);
    if (query.documentNumber) params = params.set('numero_documento', query.documentNumber);
    if (query.search) params = params.set('search', query.search);
    if (query.roleCode) params = params.set('roleCode', query.roleCode);
    if (query.unassignedOnly) params = params.set('unassignedOnly', true);
    if (query.companyId) params = params.set('companyId', query.companyId);

    return this.http.get<UserListItem[]>(this.baseUrl, { params });
  }

  /** Usuarios sin compañía con el rol indicado (candidatos a vincular). */
  listAssignable(
    roleCode: RoleCode,
    filters: {
      search?: string;
      email?: string;
      username?: string;
      fullName?: string;
      documentType?: string;
      documentNumber?: string;
    } = {}
  ) {
    return this.listAll({ roleCode, unassignedOnly: true, ...filters });
  }

  /** Candidatos a representante legal (`D-87`): tipo y número de documento son obligatorios. */
  searchRepresentativeCandidates(query: {
    documentType: string;
    documentNumber: string;
    username?: string;
    email?: string;
  }) {
    let params = new HttpParams()
      .set('documentType', query.documentType)
      .set('documentNumber', query.documentNumber);

    if (query.username) params = params.set('username', query.username);
    if (query.email) params = params.set('email', query.email);

    return this.http.get<UserListItem[]>(`${this.baseUrl}/representative-candidates`, { params });
  }

  getById(userId: string) {
    return this.http.get<UserListItem>(`${this.baseUrl}/${userId}`);
  }

  listByCompany(companyId: string, query: PageQuery & { roleCode?: string } = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.roleCode) params = params.set('roleCode', query.roleCode);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<UserListItem>>(`${environment.apiUrl}/companies/${companyId}/users`, { params });
  }

  listMyCompany(query: PageQuery & { roleCode?: string } = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.roleCode) params = params.set('roleCode', query.roleCode);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<UserListItem>>(`${this.baseUrl}/company/me`, { params });
  }

  deactivate(userId: string, reason: string) {
    return this.http.patch<{ userId: string; accountEnabled: boolean }>(`${this.baseUrl}/${userId}/deactivate`, { reason });
  }

  enable(userId: string) {
    return this.http.patch<{ userId: string; accountEnabled: boolean }>(`${this.baseUrl}/${userId}/enable`, {});
  }

  changeRole(userId: string, roleCode: RoleCode) {
    return this.http.patch<{ userId: string; assignedRole: string }>(`${this.baseUrl}/${userId}/role`, {
      assignedRoleCode: roleCode
    });
  }

  changeCompany(userId: string, companyId: string) {
    return this.http.patch<{ userId: string; companyId: string }>(`${this.baseUrl}/${userId}/company`, { companyId });
  }
}
