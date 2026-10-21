export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface AuthenticatedUser {
  user_id: string;
  email: string;
  username: string;
  role: string;
  company_id: string | null;
  estado_cuenta: boolean;
}

export interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_at: string;
  user: AuthenticatedUser;
}
