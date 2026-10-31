export type PageSize = 10 | 50 | 100;

/** Envelope de los listados paginados del backend (`D-101`). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface PageQuery {
  pageNumber?: number;
  pageSize?: PageSize;
  search?: string;
}

export interface ApiErrorBody {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
  code: string;
  trace_id: string;
}
