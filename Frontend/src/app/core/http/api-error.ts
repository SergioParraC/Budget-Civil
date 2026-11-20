import { HttpErrorResponse } from '@angular/common/http';

/**
 * Contrato de error del backend (`ProblemDetails`). Las extensiones `code` y `traceId`
 * se serializan en la raíz del documento porque `ProblemDetails.Extensions` usa
 * `JsonExtensionData`.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** Error normalizado que consumen los componentes del cliente. */
export interface ApiError {
  status: number;
  code: string;
  message: string;
  traceId: string | null;
  fieldErrors: Record<string, string[]>;
}

const GENERIC_MESSAGE = 'Ocurrió un error inesperado. Intenta nuevamente.';

const NETWORK_MESSAGE = 'No se pudo contactar al servidor. Verifica que el backend esté en ejecución.';

const CODE_MESSAGES: Record<string, string> = {
  VALIDATION_ERROR: 'Revisa los datos enviados: hay campos obligatorios o con formato inválido.',
  INVALID_CREDENTIALS: 'Usuario o contraseña incorrectos.',
  ACCOUNT_NOT_ENABLED: 'Esta cuenta no se encuentra habilitada. Solicita al administrador su habilitación.',
  EMAIL_ALREADY_EXISTS: 'Ya existe una cuenta con ese correo electrónico.',
  USERNAME_ALREADY_EXISTS: 'Ese nombre de usuario ya está en uso.',
  DEFAULT_ROLE_NOT_AVAILABLE: 'El registro no está disponible en este momento. Contacta al administrador.',
  INVALID_TOKEN: 'La sesión expiró. Inicia sesión nuevamente.',
  UNAUTHORIZED: 'Debes iniciar sesión para continuar.',
  FORBIDDEN: 'No tienes permisos para realizar esta acción.',
  ACTOR_NOT_ENABLED: 'Tu cuenta no está operativa. Contacta al administrador.',
  AUTH_USER_NOT_FOUND: 'El usuario autenticado no existe.',
  INVALID_AUTH_SUBJECT: 'La sesión no es válida. Inicia sesión nuevamente.',
  COMPANY_CONTEXT_REQUIRED: 'Tu usuario no tiene una compañía activa asignada. Asígnala antes de continuar.',
  COMPANY_CONTEXT_MISMATCH: 'El contexto de compañía del token no coincide con tu usuario.',
  COMPANY_CLAIM_REQUIRED: 'La sesión no incluye el contexto de compañía. Cierra sesión y vuelve a entrar.',
  COMPANY_INACTIVE: 'La compañía está inactiva.',
  COMPANY_NOT_FOUND: 'La compañía no existe.',
  COMPANY_ALREADY_INACTIVE: 'La compañía ya está inactiva.',
  COMPANY_TAX_ID_ALREADY_EXISTS: 'Ya existe una compañía registrada con ese NIT.',
  MANAGER_COMPANY_LIMIT_REACHED: 'El Director de proyectos solo puede tener una compañía propia.',
  COMPANY_MEMBER_ALREADY_ACTIVE: 'El usuario ya está activo en esta compañía.',
  COMPANY_MEMBERSHIP_LIMIT_REACHED: 'El usuario ya tiene una compañía activa asignada.',
  COMPANY_MEMBERSHIP_NOT_FOUND: 'La membresía de compañía no existe.',
  COMPANY_MEMBER_ASSIGN_FORBIDDEN: 'No puedes asignar miembros a esta compañía.',
  COMPANY_MEMBER_DEACTIVATE_FORBIDDEN: 'No puedes retirar miembros de esta compañía.',
  ADMIN_COMPANY_MEMBERSHIP_FORBIDDEN: 'Un administrador no puede pertenecer a una compañía.',
  USER_NOT_FOUND: 'El usuario no existe.',
  ASSIGNED_ROLE_NOT_AVAILABLE: 'El rol solicitado no está disponible.',
  USERS_SCOPE_FORBIDDEN: 'No puedes consultar los usuarios de esta compañía.',
  USERS_READ_FORBIDDEN: 'No puedes consultar el listado global de usuarios.',
  PROJECT_NOT_FOUND: 'El proyecto no existe.',
  PROJECT_CONFIGURATION_NOT_FOUND: 'El proyecto no tiene configuración asociada.',
  PROJECT_REFERENCE_INVALID: 'La ubicación o la divisa del proyecto no son válidas.',
  PROJECT_DIRECTOR_REQUIRED: 'La compañía necesita al menos un Director de proyectos activo.',
  PROJECT_CREATE_FORBIDDEN: 'No puedes crear proyectos.',
  PROJECT_UPDATE_FORBIDDEN: 'No puedes editar este proyecto.',
  PROJECT_READ_FORBIDDEN: 'No puedes consultar proyectos.',
  PROJECT_STATUS_FORBIDDEN: 'No puedes cambiar el estado de este proyecto.',
  PROJECT_VISIBILITY_FORBIDDEN: 'No puedes cambiar la visibilidad de este proyecto.',
  PROJECT_STATE_TRANSITION_INVALID: 'La transición de estado solicitada no es válida.',
  PROJECT_SCOPE_FORBIDDEN: 'No puedes acceder a proyectos de otra compañía.',
  PROJECT_STATUS_HIDDEN: 'El proyecto no está disponible para tu rol.',
  PROJECT_MEMBER_ALREADY_ACTIVE: 'El usuario ya está asignado a este proyecto.',
  PROJECT_MEMBERSHIP_COMPANY_AFFILIATION_REQUIRED: 'El usuario debe pertenecer a la compañía del proyecto.',
  PROJECT_MEMBER_ASSIGN_FORBIDDEN: 'No puedes asignar miembros a este proyecto.',
  PROJECT_MEMBER_DEACTIVATE_FORBIDDEN: 'No puedes retirar miembros de este proyecto.',
  PROJECT_MEMBERSHIP_NOT_FOUND: 'La membresía del proyecto no existe.',
  PAGE_SIZE_INVALID: 'El tamaño de página debe ser 10, 50 o 100.',
  WBS_NOT_FOUND: 'El nodo ya no existe en el proyecto. Actualizamos el árbol.',
  WBS_CODE_INVALID: 'El código debe tener entre 1 y 10 caracteres; se permiten letras, números y símbolos como . , - _.',
  WBS_CODE_DUPLICATED: 'Ese código ya está en uso en el proyecto (sin distinguir mayúsculas). Elige otro.',
  WBS_QUANTITY_INVALID: 'La cantidad de obra no puede ser negativa.',
  WBS_CONCURRENCY_CONFLICT:
    'Otro usuario modificó el EDT mientras trabajabas. Actualizamos el árbol; revisa los cambios y vuelve a intentarlo.',
  WBS_TARGET_NOT_FOUND:
    'El destino ya no existe en el proyecto. Actualizamos el árbol; elige un capítulo vigente.',
  WBS_MOVE_CYCLE: 'No puedes mover un capítulo dentro de sí mismo ni dentro de uno de sus subcapítulos.',
  WBS_CHAPTER_HAS_APU: 'El capítulo contiene ítems con APU. Elimina o mueve primero esos ítems.',
  WBS_NODE_HAS_CHILDREN: 'El capítulo tiene subcapítulos o ítems; solo se elimina un capítulo vacío.',
  WBS_READ_FORBIDDEN: 'No tienes permisos para consultar el EDT de este proyecto.',
  WBS_WRITE_FORBIDDEN: 'No tienes permisos para modificar el EDT de este proyecto.',
  APU_NOT_FOUND: 'El APU ya no existe en el proyecto.',
  APU_READ_FORBIDDEN: 'No tienes permisos para consultar los APUs de este proyecto.',
  APU_WRITE_FORBIDDEN: 'No tienes permisos para modificar los APUs de este proyecto.',
  APU_RESOURCE_NOT_FOUND: 'Uno de los insumos del APU ya no existe en el proyecto.',
  APU_RESOURCE_PROJECT_MISMATCH: 'El APU solo admite insumos de este proyecto.',
  APU_CONCURRENCY_CONFLICT:
    'Otro usuario modificó el APU mientras editabas. Actualizamos el APU; revisa los cambios y vuelve a intentarlo.',
  APU_COMPOSITION_RESOLUTION_REQUIRED: 'Falta resolver los insumos de la copia del APU.',
  MATRIX_NOT_FOUND: 'La Matriz ya no existe en el proyecto.',
  MATRIX_TYPE_INVALID: 'El insumo seleccionado no es un insumo simple del proyecto.',
  MATRIX_NESTING_FORBIDDEN: 'Una Matriz no puede contener otra Matriz ni a sí misma.',
  MATRIX_CONCURRENCY_CONFLICT:
    'Otro usuario modificó la Matriz. Actualizamos la composición; vuelve a intentarlo.',
  RESOURCE_NOT_FOUND: 'Este insumo ya no existe en el proyecto.',
  RESOURCE_IN_USE:
    'Este insumo está en uso en un APU, en una Matriz o como origen de una copia. No se puede eliminar.',
  RESOURCE_CONCURRENCY_CONFLICT:
    'Otro usuario modificó este insumo mientras editabas. Actualizamos los datos; vuelve a intentarlo.',
  RESOURCE_TYPE_INVALID: 'El tipo de insumo seleccionado no es válido.',
  MUNICIPALITY_INVALID: 'El municipio seleccionado no es válido.',
  RESOURCE_READ_FORBIDDEN: 'No tienes permisos para consultar los insumos de este proyecto.',
  RESOURCE_WRITE_FORBIDDEN: 'No tienes permisos para modificar los insumos de este proyecto.',
  RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION:
    'La copia de una Matriz requiere la operación de copia de Matriz.',
  RESOURCE_QUOTATION_INCOMPLETE:
    'El insumo está incompleto; completa descripción, unidad, precio, marca y proveedor antes de cotizarlo.',
  RESOURCE_QUOTATION_CHANGED: 'El estado de cotización del insumo cambió durante la operación.',
  UNIT_ALREADY_EXISTS: 'Ya existe una unidad con esa sigla. Elige otra o usa la existente.',
  UNIT_VALIDATION_ERROR: 'La sigla debe tener de 1 a 10 caracteres y el nombre de 1 a 100.',
  REGISTRATION_RATE_LIMIT_EXCEEDED: 'Demasiados intentos de registro. Intenta más tarde.',
  TOO_MANY_REQUESTS: 'Demasiadas solicitudes. Intenta nuevamente en unos minutos.'
};

/**
 * Códigos que dejan el estado del cliente desactualizado: además del mensaje, la pantalla debe refrescar
 * los datos del servidor (`D-50`).
 */
const REFRESH_CODES = new Set<string>([
  'WBS_TARGET_NOT_FOUND',
  'WBS_CONCURRENCY_CONFLICT',
  'WBS_NOT_FOUND',
  'APU_CONCURRENCY_CONFLICT',
  'MATRIX_CONCURRENCY_CONFLICT',
  'RESOURCE_CONCURRENCY_CONFLICT',
  'RESOURCE_NOT_FOUND',
  'MATRIX_NOT_FOUND',
  'APU_NOT_FOUND'
]);

const ALERT_TITLES: Record<string, string> = {
  WBS_MOVE_CYCLE: 'Movimiento no permitido',
  WBS_TARGET_NOT_FOUND: 'El destino cambió',
  WBS_CONCURRENCY_CONFLICT: 'El EDT cambió',
  WBS_NOT_FOUND: 'El nodo cambió',
  WBS_CHAPTER_HAS_APU: 'No se puede eliminar el capítulo',
  WBS_NODE_HAS_CHILDREN: 'No se puede eliminar el capítulo',
  WBS_CODE_DUPLICATED: 'Código en uso',
  WBS_CODE_INVALID: 'Código inválido',
  WBS_QUANTITY_INVALID: 'Cantidad de obra inválida',
  APU_CONCURRENCY_CONFLICT: 'El APU cambió',
  APU_RESOURCE_NOT_FOUND: 'Insumo no disponible',
  APU_RESOURCE_PROJECT_MISMATCH: 'Insumo de otro proyecto',
  MATRIX_CONCURRENCY_CONFLICT: 'La Matriz cambió',
  MATRIX_NESTING_FORBIDDEN: 'Composición no permitida',
  MATRIX_TYPE_INVALID: 'Insumo no permitido',
  RESOURCE_IN_USE: 'Insumo en uso',
  RESOURCE_NOT_FOUND: 'El insumo cambió',
  RESOURCE_CONCURRENCY_CONFLICT: 'El insumo cambió',
  UNIT_ALREADY_EXISTS: 'Unidad duplicada',
  UNIT_VALIDATION_ERROR: 'Unidad inválida'
};

/** Indica si la causa del error exige volver a consultar el servidor. */
export function errorNeedsRefresh(code: string): boolean {
  return REFRESH_CODES.has(code);
}

/** Título en español para el modal de alerta; cae en un título genérico si el código no está mapeado. */
export function alertTitleFor(code: string): string {
  return ALERT_TITLES[code] ?? 'No se pudo completar la operación';
}

const STATUS_CODES: Record<number, string> = {
  0: 'NETWORK_ERROR',
  400: 'VALIDATION_ERROR',
  401: 'UNAUTHORIZED',
  403: 'FORBIDDEN',
  404: 'NOT_FOUND',
  409: 'CONFLICT',
  423: 'LOCKED',
  429: 'TOO_MANY_REQUESTS',
  500: 'UNHANDLED_ERROR'
};

/** Convierte una respuesta HTTP fallida en el error normalizado del cliente. */
export function toApiError(error: HttpErrorResponse): ApiError {
  const problem = extractProblemDetails(error);
  const status = error.status;
  const code = resolveCode(status, problem?.code);
  const fieldErrors = normalizeFieldErrors(problem?.errors);
  const detail = problem?.detail?.trim();

  return {
    status,
    code,
    message: resolveMessage(status, code, detail),
    traceId: problem?.traceId ?? null,
    fieldErrors
  };
}

/** Resuelve cualquier valor lanzado por `HttpClient` a un `ApiError`. */
export function resolveApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    return toApiError(error);
  }

  return {
    status: 0,
    code: 'UNHANDLED_ERROR',
    message: GENERIC_MESSAGE,
    traceId: null,
    fieldErrors: {}
  };
}

function resolveCode(status: number, rawCode: string | undefined): string {
  if (rawCode && rawCode.trim().length > 0) {
    return rawCode.trim().toUpperCase();
  }

  return STATUS_CODES[status] ?? 'UNHANDLED_ERROR';
}

function resolveMessage(status: number, code: string, detail: string | undefined): string {
  if (status === 0) {
    return NETWORK_MESSAGE;
  }

  // El detalle de validación enumera los campos inválidos, por eso tiene prioridad.
  if (code === 'VALIDATION_ERROR' && detail) {
    return detail;
  }

  return CODE_MESSAGES[code] ?? detail ?? GENERIC_MESSAGE;
}

function extractProblemDetails(error: HttpErrorResponse): ProblemDetails | null {
  const body: unknown = error.error;

  if (body === null || body === undefined) {
    return null;
  }

  if (typeof body === 'string') {
    const detail = body.trim();
    return detail.length > 0 ? { detail } : null;
  }

  if (typeof body === 'object') {
    return body as ProblemDetails;
  }

  return null;
}

function normalizeFieldErrors(errors: Record<string, string[]> | undefined): Record<string, string[]> {
  if (!errors) {
    return {};
  }

  return Object.entries(errors).reduce<Record<string, string[]>>((accumulator, [field, messages]) => {
    accumulator[field] = Array.isArray(messages) ? messages : [String(messages)];
    return accumulator;
  }, {});
}

function isApiError(value: unknown): value is ApiError {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  return 'code' in value && 'status' in value && 'message' in value;
}
