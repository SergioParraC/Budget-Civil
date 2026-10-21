export interface PagedResult<T> {
  items: T[];
  page_number: number;
  page_size: number;
  total_items: number;
}

export interface PageQuery {
  pageNumber?: number;
  pageSize?: 10 | 50 | 100;
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
