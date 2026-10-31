export type ProjectStatus = 'POR_COTIZAR' | 'FALTA_INFORMACION' | 'COTIZADO' | 'FINALIZADO' | 'INHABILITADO';

export const SIGNING_MODES = [
  'SIN_FIRMA',
  'UNICA_POR_PRESUPUESTADOR_ASIGNADO',
  'INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE'
] as const;

export type SigningMode = (typeof SIGNING_MODES)[number];

export interface ProjectListItem {
  projectId: string;
  companyId: string;
  name: string;
  status: ProjectStatus;
  isPublic: boolean;
}

export interface Project {
  projectId: string;
  companyId: string;
  name: string;
  code: string | null;
  description: string | null;
  objective: string | null;
  countryId: string;
  departmentId: string;
  municipalityId: string;
  baseCurrencyId: string;
  budgetLimit: number | null;
  aiuAdminPercent: number | null;
  aiuUnforeseenPercent: number | null;
  aiuProfitPercent: number | null;
  signatureMode: SigningMode;
  signerUserId: string | null;
  requiresAuditor: boolean;
  status: ProjectStatus;
  isPublic: boolean;
}

export interface ProjectFormValue {
  name: string;
  code?: string;
  description?: string;
  objective?: string;
  countryId: string;
  departmentId: string;
  municipalityId: string;
  baseCurrencyId: string;
  budgetLimit?: number;
  aiuAdminPercent?: number;
  aiuUnforeseenPercent?: number;
  aiuProfitPercent?: number;
  signingMode?: SigningMode;
  signerUserId?: string;
  requiresAuditor?: boolean;
  companyId?: string;
}

export interface ProjectStatusChangeRequest {
  targetStatus: ProjectStatus;
  reason?: string;
}

/** Respuesta de `POST /projects/{projectId}/memberships` (`IF-07`). */
export interface ProjectMemberAssignment {
  membershipId: string;
  projectId: string;
  userId: string;
  isActive: boolean;
}

/** Miembro del proyecto (`GET /projects/{projectId}/members`). */
export interface ProjectMemberItem {
  membershipId: string;
  userId: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  roleCode: string;
  roleName: string;
  accountEnabled: boolean;
  assignedAtUtc: string;
}

export interface ProjectMembersResponse {
  items: ProjectMemberItem[];
}
