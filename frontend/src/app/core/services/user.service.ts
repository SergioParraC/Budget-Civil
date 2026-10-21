import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import { UserListItem } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  listAll(query: { email?: string; username?: string; fullName?: string; documentNumber?: string } = {}) {
    let params = new HttpParams();
    if (query.email) params = params.set('email', query.email);
    if (query.username) params = params.set('username', query.username);
    if (query.fullName) params = params.set('nombre_apellidos', query.fullName);
    if (query.documentNumber) params = params.set('numero_documento', query.documentNumber);

    return this.http.get<UserListItem[]>(this.baseUrl, { params });
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

  deactivate(userId: string, motivo: string) {
    return this.http.patch<{ user_id: string; estado_cuenta: boolean }>(`${this.baseUrl}/${userId}/deactivate`, { motivo });
  }

  changeRole(userId: string, rol_asignado: string) {
    return this.http.patch<{ user_id: string; rol_asignado: string }>(`${this.baseUrl}/${userId}/role`, { rol_asignado });
  }

  changeCompany(userId: string, company_id: string) {
    return this.http.patch<{ user_id: string; company_id: string }>(`${this.baseUrl}/${userId}/company`, { company_id });
  }
}
