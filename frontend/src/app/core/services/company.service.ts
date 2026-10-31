import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import { Company, CompanyFormValue, CompanyMemberAssignment, LeaveCompanyResponse, MyCompany, RoleCode } from '../models/company.model';

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
    return this.http.post<{ companyId: string; createdAtUtc: string }>(this.baseUrl, value);
  }

  /** Ficha de la compañía del usuario autenticado (`204` cuando no tiene vínculo vigente). */
  getMyCompany() {
    return this.http.get<MyCompany | null>(`${this.baseUrl}/me`);
  }

  /** Salida voluntaria de la compañía (Presupuestador y Auditor). */
  leaveMyCompany() {
    return this.http.post<LeaveCompanyResponse>(`${this.baseUrl}/me/leave`, {});
  }

  update(companyId: string, value: CompanyFormValue) {
    return this.http.put<{ companyId: string; updatedAtUtc: string }>(`${this.baseUrl}/${companyId}`, value);
  }

  disable(companyId: string, reason: string) {
    return this.http.patch<{ companyId: string; isActive: boolean }>(`${this.baseUrl}/${companyId}/disable`, { reason });
  }

  enable(companyId: string, reason?: string) {
    return this.http.patch<{ companyId: string; isActive: boolean }>(`${this.baseUrl}/${companyId}/enable`, { reason });
  }

  assignMembership(companyId: string, userId: string, roleCode: RoleCode) {
    return this.http.post<CompanyMemberAssignment>(`${this.baseUrl}/${companyId}/memberships`, { userId, roleCode });
  }

  /** La baja de compañía se identifica por `userId`, no por `membershipId` (`IF-07`). */
  deactivateMembership(companyId: string, userId: string) {
    return this.http.patch<{ companyId: string; userId: string; isActive: boolean; deactivatedAtUtc: string }>(
      `${this.baseUrl}/${companyId}/memberships/${userId}/deactivate`,
      {}
    );
  }
}
