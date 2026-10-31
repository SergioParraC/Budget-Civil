import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult, PageQuery } from '../models/api-envelope.model';
import {
  Project,
  ProjectFormValue,
  ProjectListItem,
  ProjectMemberAssignment,
  ProjectMembersResponse,
  ProjectStatus,
  ProjectStatusChangeRequest
} from '../models/project.model';
import { RoleCode } from '../models/company.model';

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
    return this.http.post<{
      projectId: string;
      companyId: string;
      status: ProjectStatus;
      countryId: string;
      departmentId: string;
      municipalityId: string;
      baseCurrencyId: string;
      createdAtUtc: string;
    }>(this.baseUrl, value);
  }

  update(projectId: string, value: ProjectFormValue) {
    return this.http.put<{ projectId: string; updatedAtUtc: string }>(`${this.baseUrl}/${projectId}`, value);
  }

  changeStatus(projectId: string, request: ProjectStatusChangeRequest) {
    return this.http.patch<{
      projectId: string;
      previousStatus: ProjectStatus;
      newStatus: ProjectStatus;
      changedAtUtc: string;
      changedByUserId: string;
    }>(`${this.baseUrl}/${projectId}/status`, request);
  }

  reactivate(projectId: string, reason?: string) {
    return this.http.patch<{
      projectId: string;
      previousStatus: ProjectStatus;
      newStatus: ProjectStatus;
      changedAtUtc: string;
      changedByUserId: string;
    }>(`${this.baseUrl}/${projectId}/status/reactivate`, { reason });
  }

  changeVisibility(projectId: string, isPublic: boolean, reason?: string) {
    return this.http.patch<{
      projectId: string;
      previousIsPublic: boolean;
      newIsPublic: boolean;
      changedAtUtc: string;
    }>(`${this.baseUrl}/${projectId}/visibility`, { isPublic, reason });
  }

  assignMembership(projectId: string, userId: string, roleCode: RoleCode) {
    return this.http.post<ProjectMemberAssignment>(`${this.baseUrl}/${projectId}/memberships`, { userId, roleCode });
  }

  /** Miembros del proyecto con su rol y fecha de asignación. */
  listMembers(projectId: string) {
    return this.http.get<ProjectMembersResponse>(`${this.baseUrl}/${projectId}/members`);
  }

  deactivateMembership(projectId: string, membershipId: string) {
    return this.http.patch<{ membershipId: string; isActive: boolean; deactivatedAtUtc: string }>(
      `${this.baseUrl}/${projectId}/memberships/${membershipId}/deactivate`,
      {}
    );
  }
}
