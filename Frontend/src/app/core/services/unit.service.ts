import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CreateUnitRequest, Unit } from '../models/unit.model';

/** Catálogo global de unidades (`REQ-UNIDADES-03`): sugerencias para APU e insumos. */
@Injectable({ providedIn: 'root' })
export class UnitService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalogs/units`;

  private readonly cache = signal<Unit[] | null>(null);

  /** Unidades ya cargadas, disponibles para los selectores sin otra llamada. */
  readonly units = this.cache.asReadonly();

  list() {
    return this.http
      .get<Unit[]>(this.baseUrl)
      .pipe(tap((units) => this.cache.set(units)));
  }

  create(request: CreateUnitRequest) {
    return this.http
      .post<Unit>(this.baseUrl, request)
      .pipe(tap((unit) => this.cache.update((units) => (units ? [...units, unit] : [unit]))));
  }
}
