export type ProjectStatus = 'POR_COTIZAR' | 'FALTA_INFORMACION' | 'COTIZADO' | 'FINALIZADO' | 'INHABILITADO';
export type SigningMode = 'UNICA_POR_PRESUPUESTADOR_ASIGNADO' | 'INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE';

export interface ProjectListItem {
  project_id: string;
  company_id: string;
  name: string;
  status: ProjectStatus;
  is_public: boolean;
}

export interface Project {
  project_id: string;
  company_id: string;
  name: string;
  code: string | null;
  description: string | null;
  objective: string | null;
  country_id: string;
  department_id: string;
  municipality_id: string;
  base_currency_id: string;
  aiu_admin_percent: number | null;
  aiu_unforeseen_percent: number | null;
  aiu_profit_percent: number | null;
  status: ProjectStatus;
  is_public: boolean;
}

export interface ProjectFormValue {
  name: string;
  code?: string;
  description?: string;
  objective?: string;
  country_id: string;
  department_id: string;
  municipality_id: string;
  base_currency_id: string;
  aiu_admin_percent?: number;
  aiu_unforeseen_percent?: number;
  aiu_profit_percent?: number;
  signing_mode?: SigningMode;
  signer_user_id?: string;
  company_id?: string;
}

export interface ProjectStatusChangeRequest {
  target_status: Extract<ProjectStatus, 'FALTA_INFORMACION' | 'COTIZADO' | 'FINALIZADO' | 'INHABILITADO'>;
  reason?: string;
}
