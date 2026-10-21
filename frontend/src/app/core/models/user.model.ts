export interface UserListItem {
  user_id: string;
  company_id: string | null;
  email: string;
  username: string;
  role_code: string;
  estado_cuenta: boolean;
  primer_nombre: string;
  segundo_nombre?: string | null;
  primer_apellido: string;
  segundo_apellido?: string | null;
  numero_documento: string;
}
