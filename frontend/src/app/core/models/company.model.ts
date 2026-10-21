export type CompanyType = 'PUBLICA' | 'PRIVADA';
export type RepresentativeMode = 'USER_REFERENCE' | 'MANUAL_DATA';
export type RoleCode = 'GERENTE_PROYECTOS' | 'PRESUPUESTADOR' | 'AUDITOR';

export interface Company {
  company_id: string;
  business_name: string;
  tax_id: string;
  company_type: CompanyType;
  territorial_level: string | null;
  country_id: string;
  department_id: string;
  municipality_id: string;
  representative_mode: RepresentativeMode;
  representative_user_id: string | null;
  representative_full_name: string | null;
  representative_document_type_id: string | null;
  representative_document_number: string | null;
  is_active: boolean;
  created_by_user_id: string | null;
  created_at: string;
  last_modified_by_user_id: string | null;
  last_modified_at: string | null;
}

export interface CompanyRepresentativePayload {
  mode: RepresentativeMode;
  user_id?: string;
  full_name?: string;
  document_type_id?: string;
  document_number?: string;
}

export interface CompanyFormValue {
  business_name: string;
  tax_id: string;
  company_type: CompanyType;
  territorial_level?: string;
  country_id: string;
  department_id: string;
  municipality_id: string;
  representative: CompanyRepresentativePayload;
  is_active?: boolean;
}

export interface Membership {
  membership_id: string;
  company_id?: string;
  project_id?: string;
  user_id: string;
  role_code: RoleCode;
  is_active: boolean;
}
