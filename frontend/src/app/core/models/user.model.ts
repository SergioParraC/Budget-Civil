/** Campos por los que se puede filtrar la búsqueda de usuarios (`GET /api/v1/users`). */
export type UserSearchFilter = 'all' | 'email' | 'username' | 'fullName' | 'document';

export interface UserListItem {
  userId: string;
  companyId: string | null;
  email: string;
  username: string;
  roleCode: string;
  roleName?: string;
  accountEnabled: boolean;
  firstName: string;
  middleName: string | null;
  lastName: string;
  secondLastName: string | null;
  documentType: string;
  documentNumber: string;
  /** Fecha de vinculación con la compañía (`users_company.created_at`). */
  linkedAtUtc?: string | null;
  /** Última actualización del vínculo (reactivaciones incluidas). */
  membershipUpdatedAtUtc?: string | null;
}
