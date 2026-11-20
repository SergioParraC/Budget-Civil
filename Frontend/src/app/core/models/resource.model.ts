/** Estados de cotización del insumo, calculados por el servidor (`BR-009`, `D-89`). */
export type QuotationStatus = 'FALTA_INFORMACION' | 'POR_COTIZAR' | 'COTIZADO';

/** Código del tipo Matriz (`ResourceTypeCatalog.Matrix`). */
export const MATRIX_RESOURCE_TYPE_CODE = 'MATRIX';

/** Opción del selector de proveedor (`SupplierOptionDto`). */
export interface SupplierOption {
  supplierId: string;
  businessName: string;
}

export interface SupplierOptionsResponse {
  items: SupplierOption[];
}

/** `POST /suppliers`: solo la razón social es obligatoria (`REQ-PROVEEDORES-01`). */
export interface CreateSupplierRequest {
  businessName: string;
  contactName?: string | null;
  contactPhone?: string | null;
  email?: string | null;
  address?: string | null;
  observations?: string | null;
}

export interface SupplierSummary {
  supplierId: string;
  businessName: string;
  contactName: string | null;
  contactPhone: string | null;
  email: string | null;
  address: string | null;
  countryId: string | null;
  countryName: string | null;
  departmentId: string | null;
  departmentName: string | null;
  municipalityId: string | null;
  municipalityName: string | null;
  isActive: boolean;
}

/** Insumo del proyecto (`ResourceDto`). */
export interface Resource {
  resourceId: string;
  projectId: string;
  resourceTypeId: string;
  resourceTypeCode: string;
  resourceTypeName: string;
  municipalityId: string;
  description: string;
  unit: string | null;
  unitPrice: number | null;
  brand: string | null;
  distributor: string | null;
  userKey: string | null;
  details: string | null;
  contactNumber: string | null;
  supplierId: string | null;
  supplier: SupplierSummary | null;
  quotationStatus: QuotationStatus | string;
  originResourceId: string | null;
  lastModifiedBy: string;
  lastModifiedAt: string;
  rowVersion: string;
  isActive: boolean;
}

/** Tipos activos del catálogo, en su orden (`ResourceTypeOptionDto`). */
export interface ResourceTypeOption {
  id: string;
  code: string;
  name: string;
}

export interface ResourceSuggestion {
  resourceId: string;
  description: string;
  userKey: string | null;
  unitPrice: number | null;
  quotationStatus: QuotationStatus | string;
}

export interface ResourceSuggestionsResponse {
  items: ResourceSuggestion[];
}

export type ResourceSortField = 'description' | 'unitPrice' | 'quotationStatus' | 'lastModifiedAt';

export type SortDirection = 'asc' | 'desc';

/** Criterios de `GET /resources`. */
export interface ResourceListQuery {
  search?: string;
  resourceTypeId?: string;
  quotationStatus?: string;
  brand?: string;
  municipalityId?: string;
  minUnitPrice?: number;
  maxUnitPrice?: number;
  page?: number;
  pageSize?: 10 | 50 | 100;
  sortBy?: ResourceSortField;
  sortDirection?: SortDirection;
}

export interface CreateResourceRequest {
  resourceTypeId: string;
  municipalityId: string;
  description: string;
  unit?: string | null;
  unitPrice?: number | null;
  brand?: string | null;
  distributor?: string | null;
  userKey?: string | null;
  details?: string | null;
  contactNumber?: string | null;
  supplierId?: string | null;
}

export interface UpdateResourceRequest extends CreateResourceRequest {
  rowVersion: string;
}

export interface CopyResourceRequest {
  sourceResourceId: string;
}

/** Filtro reutilizado por la cotización masiva (`scope = FILTER`). */
export interface ResourceQuotationFilter {
  description?: string;
  resourceTypeId?: string;
  quotationStatus?: string;
  brand?: string;
  municipalityId?: string;
  minPrice?: number;
  maxPrice?: number;
}

export type ResourceQuotationScope = 'SELECTION' | 'FILTER' | 'ALL';

export interface ChangeResourceQuotationStatusRequest {
  scope: ResourceQuotationScope;
  resourceIds?: string[];
  filter?: ResourceQuotationFilter;
}

export interface ResourceQuotationSkipped {
  resourceId: string;
  code: string;
}

export interface ChangeResourceQuotationStatusResponse {
  projectId: string;
  quotationStatus: string;
  requested: number;
  updated: number;
  alreadyQuoted: number;
  skipped: ResourceQuotationSkipped[];
  pendingResources: number;
  allQuoted: boolean;
}
