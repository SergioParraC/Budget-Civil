import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CreateWbsChapterRequest,
  CreateWbsItemRequest,
  MoveWbsNodeRequest,
  ReorderWbsNodesRequest,
  ReorderWbsNodesResponse,
  UpdateWbsChapterRequest,
  UpdateWbsItemRequest,
  WbsActivityResponse,
  WbsCodeValidation,
  WbsItemApuResponse,
  WbsNode,
  WbsSearchResponse,
  WbsTreeResponse
} from '../models/wbs.model';

/** Superficie del EDT por proyecto (`D-55`, `D-118`). */
@Injectable({ providedIn: 'root' })
export class WbsService {
  private readonly http = inject(HttpClient);

  private base(projectId: string): string {
    return `${environment.apiUrl}/projects/${projectId}/wbs`;
  }

  getTree(projectId: string) {
    return this.http.get<WbsTreeResponse>(this.base(projectId));
  }

  search(
    projectId: string,
    query: { search?: string; page?: number; pageSize?: 10 | 50 | 100 } = {}
  ) {
    let params = new HttpParams();
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    params = params.set('page', query.page ?? 1);
    params = params.set('pageSize', query.pageSize ?? 10);

    return this.http.get<WbsSearchResponse>(`${this.base(projectId)}/search`, { params });
  }

  getActivity(projectId: string) {
    return this.http.get<WbsActivityResponse>(`${this.base(projectId)}/activity`);
  }

  validateCode(projectId: string, code: string, excludeNodeId?: string) {
    let params = new HttpParams().set('code', code);
    if (excludeNodeId) params = params.set('excludeNodeId', excludeNodeId);

    return this.http.get<WbsCodeValidation>(`${this.base(projectId)}/codes/validate`, { params });
  }

  createChapter(projectId: string, request: CreateWbsChapterRequest) {
    return this.http.post<WbsNode>(`${this.base(projectId)}/chapters`, request);
  }

  updateChapter(projectId: string, chapterId: string, request: UpdateWbsChapterRequest) {
    return this.http.put<WbsNode>(`${this.base(projectId)}/chapters/${chapterId}`, request);
  }

  deleteChapter(projectId: string, chapterId: string, rowVersion: string) {
    return this.http.delete<void>(`${this.base(projectId)}/chapters/${chapterId}`, {
      params: new HttpParams().set('rowVersion', rowVersion)
    });
  }

  createItem(projectId: string, request: CreateWbsItemRequest) {
    return this.http.post<WbsItemApuResponse>(`${this.base(projectId)}/items`, request);
  }

  updateItem(projectId: string, itemId: string, request: UpdateWbsItemRequest) {
    return this.http.put<WbsItemApuResponse>(`${this.base(projectId)}/items/${itemId}`, request);
  }

  deleteItem(projectId: string, itemId: string, rowVersion: string) {
    return this.http.delete<void>(`${this.base(projectId)}/items/${itemId}`, {
      params: new HttpParams().set('rowVersion', rowVersion)
    });
  }

  reorder(projectId: string, request: ReorderWbsNodesRequest) {
    return this.http.post<ReorderWbsNodesResponse>(`${this.base(projectId)}/reorder`, request);
  }

  move(projectId: string, request: MoveWbsNodeRequest) {
    return this.http.post<WbsNode>(`${this.base(projectId)}/move`, request);
  }
}
