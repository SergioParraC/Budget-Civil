import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/api-envelope.model';
import { Country, Currency, Department, DocumentType, Municipality } from '../models/catalog.model';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalogs`;

  countries(search = '') {
    return this.http.get<PagedResult<Country>>(`${this.baseUrl}/countries`, {
      params: new HttpParams({ fromObject: { search, pageSize: 100 } })
    });
  }

  departments(countryId: string, search = '') {
    return this.http.get<PagedResult<Department>>(`${this.baseUrl}/departments`, {
      params: new HttpParams({ fromObject: { countryId, search, pageSize: 100 } })
    });
  }

  municipalities(departmentId: string, search = '') {
    return this.http.get<PagedResult<Municipality>>(`${this.baseUrl}/municipalities`, {
      params: new HttpParams({ fromObject: { departmentId, search, pageSize: 100 } })
    });
  }

  currencies(search = '') {
    return this.http.get<PagedResult<Currency>>(`${this.baseUrl}/currencies`, {
      params: new HttpParams({ fromObject: { search, pageSize: 100 } })
    });
  }

  documentTypes(search = '') {
    return this.http.get<PagedResult<DocumentType>>(`${this.baseUrl}/document-types`, {
      params: new HttpParams({ fromObject: { search, pageSize: 100 } })
    });
  }
}
