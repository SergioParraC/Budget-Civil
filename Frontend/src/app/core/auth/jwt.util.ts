export interface TokenClaims {
  roleCode: string | null;
  companyId: string | null;
  permissions: string[];
}

const EMPTY_CLAIMS: TokenClaims = { roleCode: null, companyId: null, permissions: [] };

/**
 * El `LoginResponse` solo expone el nombre visible del rol, así que el código de rol y los
 * permisos se leen de los claims del token (`role_code`, `company_id`, `permission_key`).
 */
export function decodeTokenClaims(token: string | null | undefined): TokenClaims {
  if (!token) {
    return EMPTY_CLAIMS;
  }

  const payload = token.split('.')[1];
  if (!payload) {
    return EMPTY_CLAIMS;
  }

  try {
    const json = JSON.parse(decodeBase64Url(payload)) as Record<string, unknown>;

    return {
      roleCode: typeof json['role_code'] === 'string' ? json['role_code'] : null,
      companyId: typeof json['company_id'] === 'string' ? json['company_id'] : null,
      permissions: toPermissionList(json['permission_key'])
    };
  } catch {
    return EMPTY_CLAIMS;
  }
}

function toPermissionList(value: unknown): string[] {
  if (typeof value === 'string') {
    return [value];
  }

  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === 'string');
  }

  return [];
}

function decodeBase64Url(value: string): string {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const remainder = normalized.length % 4;
  const padding = remainder === 0 ? '' : '='.repeat(4 - remainder);
  const binary = atob(normalized + padding);

  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
