export const ROLE_CODES = ['ADMINISTRADOR', 'DIRECTOR', 'PRESUPUESTADOR', 'AUDITOR'] as const;

export type RoleCode = (typeof ROLE_CODES)[number];
export type CompanyType = 'PUBLICA' | 'PRIVADA';
export type RepresentativeMode = 'USER_REFERENCE' | 'MANUAL_DATA';

export interface Company {
  companyId: string;
  businessName: string;
  taxId: string;
  companyType: CompanyType | null;
  territorialLevel: string | null;
  countryId: string | null;
  departmentId: string | null;
  municipalityId: string | null;
  representativeMode: RepresentativeMode | null;
  representativeUserId: string | null;
  representativeFullName: string | null;
  representativeDocumentTypeId: string | null;
  representativeDocumentNumber: string | null;
  isActive: boolean;
  createdByUserId: string | null;
  createdAtUtc: string;
  lastModifiedByUserId: string | null;
  lastModifiedAtUtc: string | null;
}

export interface CompanyRepresentativePayload {
  mode: RepresentativeMode;
  userId?: string;
  fullName?: string;
  documentNumber?: string;
  documentTypeId?: string;
}

export interface CompanyFormValue {
  businessName: string;
  taxId: string;
  companyType: CompanyType;
  territorialLevel?: string;
  countryId: string;
  departmentId: string;
  municipalityId: string;
  representative: CompanyRepresentativePayload;
  isActive?: boolean;
}

/** Respuesta de `GET /companies/me`: ficha de la compañía del usuario autenticado. */
export interface MyCompany {
  companyId: string;
  businessName: string;
  taxId: string;
  companyType: CompanyType | null;
  territorialLevel: string | null;
  countryId: string | null;
  countryName: string | null;
  departmentId: string | null;
  departmentName: string | null;
  municipalityId: string | null;
  municipalityName: string | null;
  representativeMode: RepresentativeMode | null;
  representativeUserId: string | null;
  representativeFullName: string | null;
  representativeDocumentTypeId: string | null;
  representativeDocumentNumber: string | null;
  isActive: boolean;
  /** Fecha de vinculación del usuario con la compañía (`users_company.created_at`). */
  linkedAtUtc: string | null;
  roleCode: string;
  roleName: string;
}

/** Respuesta de `POST /companies/me/leave`. */
export interface LeaveCompanyResponse {
  companyId: string;
  userId: string;
  leftAtUtc: string;
  projectsDetached: number;
  requiresRelogin: boolean;
}

/** Respuesta de `POST /companies/{companyId}/memberships` (`IF-07`). */
export interface CompanyMemberAssignment {
  companyId: string;
  userId: string;
  isActive: boolean;
}
