import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import {
  Project,
  ProjectFormValue,
  ProjectListItem,
  ProjectStatus,
  ProjectStatusChangeRequest
} from '../models/project.model';
import { Membership, RoleCode } from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/projects`;

  list(query: PageQuery & { companyId?: string; status?: ProjectStatus } = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.companyId) params = params.set('companyId', query.companyId);
    if (query.status) params = params.set('status', query.status);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<ProjectListItem>>(this.baseUrl, { params });
  }

  getById(projectId: string) {
    return this.http.get<Project>(`${this.baseUrl}/${projectId}`);
  }

  create(value: ProjectFormValue) {
    return this.http.post<{ project_id: string; status: ProjectStatus; created_at_utc: string }>(this.baseUrl, value);
  }

  update(projectId: string, value: ProjectFormValue) {
    return this.http.put<{ project_id: string; updated_at_utc: string }>(`${this.baseUrl}/${projectId}`, value);
  }

  changeStatus(projectId: string, request: ProjectStatusChangeRequest) {
    return this.http.patch<{ project_id: string; previous_status: ProjectStatus; new_status: ProjectStatus }>(
      `${this.baseUrl}/${projectId}/status`,
      request
    );
  }

  reactivate(projectId: string, reason?: string) {
    return this.http.patch<{ project_id: string; previous_status: ProjectStatus; new_status: ProjectStatus }>(
      `${this.baseUrl}/${projectId}/status/reactivate`,
      { reason }
    );
  }

  changeVisibility(projectId: string, isPublic: boolean, reason?: string) {
    return this.http.patch<{ project_id: string; new_is_public: boolean }>(`${this.baseUrl}/${projectId}/visibility`, {
      is_public: isPublic,
      reason
    });
  }

  assignMembership(projectId: string, userId: string, roleCode: RoleCode) {
    return this.http.post<Membership>(`${this.baseUrl}/${projectId}/memberships`, { user_id: userId, role_code: roleCode });
  }

  deactivateMembership(projectId: string, membershipId: string) {
    return this.http.patch<{ membership_id: string; is_active: boolean }>(
      `${this.baseUrl}/${projectId}/memberships/${membershipId}/deactivate`,
      {}
    );
  }

  listMembers(projectId: string, query: PageQuery = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    params = params.set('pageNumber', query.pageNumber ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<PagedResult<unknown>>(`${this.baseUrl}/${projectId}/users`, { params });
  }
}
