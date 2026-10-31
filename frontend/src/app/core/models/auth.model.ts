export interface LoginRequest {
  identifier: string;
  password: string;
}

/** `POST /auth/register`: autorregistro público con identidad y residencia (`D-76`). */
export interface RegisterRequest {
  documentType: string;
  documentNumber: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  birthDate: string;
  email: string;
  username: string;
  password: string;
  countryId: string;
  departmentId: string;
  municipalityId: string;
}

export interface RegisterResponse {
  userId: string;
  assignedRole: string;
  companyId: string | null;
  accountEnabled: boolean;
  createdAtUtc: string;
}

export interface AuthenticatedUser {
  userId: string;
  email: string;
  username: string;
  /** Nombre visible del rol (el código viaja en el claim `role_code` del token). */
  role: string;
  companyId: string | null;
  accountEnabled: boolean;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}
