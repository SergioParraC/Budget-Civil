import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import { Company, CompanyFormValue, Membership, RoleCode } from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/companies`;

  list(query: PageQuery & { isActive?: boolean } = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<Company>>(this.baseUrl, { params });
  }

  create(value: CompanyFormValue) {
    return this.http.post<{ company_id: string; is_active: boolean; created_at: string }>(this.baseUrl, value);
  }

  update(companyId: string, value: CompanyFormValue) {
    return this.http.put<{ company_id: string; updated_at: string }>(`${this.baseUrl}/${companyId}`, value);
  }

  disable(companyId: string, reason: string) {
    return this.http.patch<{ company_id: string; is_active: boolean }>(`${this.baseUrl}/${companyId}/disable`, { reason });
  }

  enable(companyId: string, reason?: string) {
    return this.http.patch<{ company_id: string; is_active: boolean }>(`${this.baseUrl}/${companyId}/enable`, { reason });
  }

  assignMembership(companyId: string, userId: string, roleCode: RoleCode) {
    return this.http.post<Membership>(`${this.baseUrl}/${companyId}/memberships`, { user_id: userId, role_code: roleCode });
  }

  deactivateMembership(companyId: string, membershipId: string) {
    return this.http.patch<{ membership_id: string; is_active: boolean }>(
      `${this.baseUrl}/${companyId}/memberships/${membershipId}/deactivate`,
      {}
    );
  }

  listUsers(companyId: string, query: PageQuery = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<unknown>>(`${this.baseUrl}/${companyId}/users`, { params });
  }
}
