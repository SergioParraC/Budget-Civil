# API Contracts: Companies and Projects

Date: 2026-09-30
Feature: specs/002-gestion-companias-proyectos/spec.md
Scope: Backend only

## 1. Contract Conventions

- API base: `/api/v1`
- Auth: JWT Bearer obligatorio en endpoints privados.
- Authorization: validacion por `permission_key` activo + reglas por rol del feature.
- Security posture: fail-closed (rol/permiso inactivo o faltante => denegacion).
- Privacy: aislamiento por `CompanyId` en operaciones privadas.
- Soft delete: companias, proyectos y memberships se desactivan logicamente.
- State changes: transiciones de estado son explicitas; no hay cambios automaticos por editar proyecto.
- Pagination: `pageSize` valido solo en {10, 50, 100}.

## 2. Standard Payloads

## 2.1 Success envelope

```json
{
  "data": {},
  "meta": {
    "traceId": "string"
  }
}
```

## 2.2 Error envelope

```json
{
  "error": {
    "code": "STRING_CODE",
    "message": "Human readable message",
    "details": []
  },
  "meta": {
    "traceId": "string"
  }
}
```

Codigos de error esperados del feature:
- `VALIDATION_ERROR`
- `PAGE_SIZE_INVALID`
- `FORBIDDEN`
- `UNAUTHORIZED`
- `NOT_FOUND`
- `CONFLICT`
- `PROJECT_STATE_TRANSITION_INVALID`
- `PROJECT_STATE_PRECONDITION_FAILED`
- `COMPANY_INACTIVE`

## 3. Non-functional Contract Rules (FR-048/049/050)

- Persistencia segregada por esquema:
  - `param`: catalogos
  - `iam`: identidad y acceso
  - `dbo`: companias/proyectos/memberships
  - `audit`: cambios de estado/visibilidad
  - `log`: logs tecnicos
- FK permitidas:
  - `dbo` -> (`param`, `iam`)
  - `audit` -> (`dbo`, `iam`)
- FK prohibidas:
  - inversas desde `param` o `iam` hacia `dbo`, `audit`, `log`
  - cualquier FK hacia o desde `log` para datos de negocio
- Cadena de migraciones:
  - orden `param` -> `iam` -> `dbo` -> `audit` -> `log`
  - nombre `YYYYMMDDHHMM_<scope>_<change>`

## 4. Permission Keys Baseline

| Endpoint | Permission Key |
|----------|----------------|
| GET /api/v1/catalogs/countries | CATALOGS_READ |
| GET /api/v1/catalogs/departments | CATALOGS_READ |
| GET /api/v1/catalogs/municipalities | CATALOGS_READ |
| GET /api/v1/catalogs/currencies | CATALOGS_READ |
| GET /api/v1/catalogs/document-types | CATALOGS_READ |
| POST /api/v1/companies | COMPANIES_CREATE |
| PUT /api/v1/companies/{companyId} | COMPANIES_UPDATE |
| PATCH /api/v1/companies/{companyId}/disable | COMPANIES_DEACTIVATE |
| PATCH /api/v1/companies/{companyId}/enable | COMPANIES_REACTIVATE |
| GET /api/v1/companies | COMPANIES_READ |
| POST /api/v1/companies/{companyId}/memberships | COMPANY_MEMBERS_ASSIGN |
| PATCH /api/v1/companies/{companyId}/memberships/{membershipId}/deactivate | COMPANY_MEMBERS_DEACTIVATE |
| GET /api/v1/users | USERS_READ |
| GET /api/v1/companies/{companyId}/users | USERS_READ |
| GET /api/v1/users/company/me | USERS_READ |
| GET /api/v1/users/company/me/estimators-auditors | USERS_READ |
| POST /api/v1/projects | PROJECTS_CREATE |
| PUT /api/v1/projects/{projectId} | PROJECTS_UPDATE |
| PATCH /api/v1/projects/{projectId}/status | PROJECTS_STATUS_CHANGE |
| PATCH /api/v1/projects/{projectId}/status/reactivate | PROJECTS_STATUS_CHANGE |
| PATCH /api/v1/projects/{projectId}/visibility | PROJECTS_VISIBILITY_CHANGE |
| GET /api/v1/projects | PROJECTS_READ |
| GET /api/v1/projects/{projectId} | PROJECTS_READ |
| POST /api/v1/projects/{projectId}/memberships | PROJECT_MEMBERS_ASSIGN |
| PATCH /api/v1/projects/{projectId}/memberships/{membershipId}/deactivate | PROJECT_MEMBERS_DEACTIVATE |
| GET /api/v1/projects/{projectId}/users | PROJECTS_READ |

## 5. Shared Query Contracts

## 5.1 Pagination query

- `pageNumber`: int >= 1 (default 1)
- `pageSize`: int in {10, 50, 100} (default 10)

Error when invalid:
- HTTP 400, code `PAGE_SIZE_INVALID`

## 5.2 List response contract

```json
{
  "data": {
    "items": [],
    "pagination": {
      "pageNumber": 1,
      "pageSize": 10,
      "totalItems": 0,
      "totalPages": 0
    }
  },
  "meta": {
    "traceId": "string"
  }
}
```

## 6. Catalog Endpoints

### GET /api/v1/catalogs/countries

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `search`, `pageNumber`, `pageSize`
- Response 200: `{ id, code, name }[]` + pagination
- Errors: 400 (`PAGE_SIZE_INVALID`), 403 (`FORBIDDEN`)

### GET /api/v1/catalogs/departments

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `countryId`, `search`, `pageNumber`, `pageSize`
- Response 200: `{ id, countryId, code, name }[]` + pagination
- Errors: 400, 403

### GET /api/v1/catalogs/municipalities

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `departmentId`, `search`, `pageNumber`, `pageSize`
- Response 200: `{ id, departmentId, code, name }[]` + pagination
- Errors: 400, 403

### GET /api/v1/catalogs/currencies

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `search`, `pageNumber`, `pageSize`
- Response 200: `{ id, isoCode, name }[]` + pagination
- Errors: 400, 403

### GET /api/v1/catalogs/document-types

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `search`, `pageNumber`, `pageSize`
- Response 200: `{ id, code, name }[]` + pagination
- Errors: 400, 403

## 7. Company Lifecycle Endpoints

### POST /api/v1/companies

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request:
  - `businessName` (required)
  - `taxId` (required)
  - `companyType` (`PUBLICA|PRIVADA`, required)
  - `territorialLevel` (required when `companyType=PUBLICA`)
  - `countryId`, `departmentId`, `municipalityId` (required)
  - `representative`:
    - `mode` (`USER_REFERENCE|MANUAL_DATA`, required)
    - `userId` (optional)
    - `fullName` (optional)
    - `documentTypeId` (optional)
    - `documentNumber` (optional)
- Response 201: `companyId`, `isActive`, `createdAt`
- Errors:
  - 400 `VALIDATION_ERROR`
  - 403 `FORBIDDEN`
  - 409 `CONFLICT` (taxId duplicado)

### PUT /api/v1/companies/{companyId}

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request: campos editables de compania y representante
- Response 200: `companyId`, `updatedAt`
- Errors: 400, 403, 404

### PATCH /api/v1/companies/{companyId}/disable

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request: `reason` (required)
- Response 200: `companyId`, `isActive=false`
- Behavior: bloquea acceso operativo para usuarios no autorizados de esa compania
- Errors: 403, 404

### PATCH /api/v1/companies/{companyId}/enable

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request: `reason` (optional)
- Response 200: `companyId`, `isActive=true`
- Behavior: restaura acceso de memberships activas; no reactiva memberships inactivas manualmente
- Errors: 403, 404

### GET /api/v1/companies

- Auth required: Yes
- Allowed roles: Administrador
- Query: `search`, `isActive`, `pageNumber`, `pageSize`
- Response 200: listado de companias + pagination
- Errors: 400, 403

## 8. Company Memberships and Users

### POST /api/v1/companies/{companyId}/memberships

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request:
  - `userId` (required)
  - `roleCode` (`DIRECTOR_PROYECTOS|PRESUPUESTADOR|AUDITOR`, required)
- Validations:
  - Administrador no puede vincularse a compania
  - no administradores maximo una compania activa
- Response 201: `membershipId`, `companyId`, `userId`, `isActive`
- Errors: 400, 403, 409

### PATCH /api/v1/companies/{companyId}/memberships/{membershipId}/deactivate

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Response 200: `membershipId`, `isActive=false`
- Errors: 403, 404

### GET /api/v1/users

- Auth required: Yes
- Allowed roles: Administrador
- Query: `search`, `roleCode`, `isActive`, `pageNumber`, `pageSize`
- Response 200: listado global de usuarios + pagination
- Errors: 400, 403

### GET /api/v1/companies/{companyId}/users

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Query: `search`, `roleCode`, `pageNumber`, `pageSize`
- Response 200: usuarios de compania especifica + pagination
- Errors: 400, 403, 404

### GET /api/v1/users/company/me

- Auth required: Yes
- Allowed roles: Director de proyectos, Presupuestador
- Query: `search`, `roleCode`, `pageNumber`, `pageSize`
- Response 200: usuarios de la compania del actor + pagination
- Errors: 400, 403

### GET /api/v1/users/company/me/estimators-auditors

- Auth required: Yes
- Allowed roles: Presupuestador, Auditor
- Query: `search`, `pageNumber`, `pageSize`
- Response 200: usuarios de rol Presupuestador/Auditor de la compania del actor + pagination
- Errors: 400, 403

## 9. Project Lifecycle and Visibility

### POST /api/v1/projects

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador
- Request:
  - `name` (required)
  - `description` (optional)
  - `objective` (optional, max 1000)
  - `countryId`, `departmentId`, `municipalityId` (required)
  - `baseCurrencyId` (required)
  - `aiu`: `adminPercent`, `unforeseenPercent`, `profitPercent` (optional)
  - `signingMode` (`UNICA_POR_PRESUPUESTADOR_ASIGNADO|INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE`, required)
  - `signerUserId` (optional)
- Response 201: `projectId`, `status=POR_COTIZAR`, `directorOwnerUserId`, `createdAt`
- Validations:
  - compania debe tener al menos un Director de proyectos activo
  - si crea Presupuestador, se autoasigna Director activo mas antiguo como encargado
- Errors: 400, 403, 409

### PUT /api/v1/projects/{projectId}

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador
- Request: campos editables del proyecto (sin transicion de estado implicita)
- Response 200: `projectId`, `updatedAt`
- Errors: 400, 403, 404

### PATCH /api/v1/projects/{projectId}/status

- Auth required: Yes
- Allowed roles: segun matriz por estado
- Request:
  - `targetStatus` (`FALTA_INFORMACION|COTIZADO|FINALIZADO|INHABILITADO`)
  - `reason` (optional)
- Response 200:
  - `projectId`
  - `previousStatus`
  - `newStatus`
  - `changedAt`
  - `changedByUserId`
- Validations:
  - flujo ordinario: `POR_COTIZAR -> FALTA_INFORMACION -> COTIZADO -> FINALIZADO`
  - reapertura `FINALIZADO -> FALTA_INFORMACION` solo Director de proyectos
  - `ANY -> INHABILITADO` solo Director de proyectos
  - salida de `INHABILITADO` no se hace por este endpoint, sino por `/status/reactivate`
- Errors:
  - 400 `PROJECT_STATE_TRANSITION_INVALID`
  - 403 `FORBIDDEN`
  - 404 `NOT_FOUND`
  - 409 `PROJECT_STATE_PRECONDITION_FAILED`

### PATCH /api/v1/projects/{projectId}/status/reactivate

- Auth required: Yes
- Allowed roles: Director de proyectos, Administrador
- Request: `reason` (optional)
- Response 200:
  - `projectId`
  - `previousStatus=INHABILITADO`
  - `newStatus=FALTA_INFORMACION`
- Validations:
  - estado actual debe ser `INHABILITADO`
  - estado destino siempre `FALTA_INFORMACION`
- Errors: 400, 403, 404

### PATCH /api/v1/projects/{projectId}/visibility

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos
- Request:
  - `isPublic` (required)
  - `reason` (optional)
- Response 200:
  - `projectId`
  - `previousIsPublic`
  - `newIsPublic`
  - `changedAt`
- Errors: 403, 404

### GET /api/v1/projects

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor
- Query: `companyId`, `status`, `search`, `pageNumber`, `pageSize`
- Response 200: listado de proyectos + pagination
- Visibility rules:
  - Administrador: lectura global
  - Director de proyectos: lectura por compania de contexto autenticado
  - Presupuestador/Auditor: lectura restringida por compania y reglas de estado
  - si estado es `INHABILITADO`, solo Administrador y Director de proyectos pueden verlo
- Errors: 400, 403

### GET /api/v1/projects/{projectId}

- Auth required: Yes
- Allowed roles: segun alcance de compania y regla de estado
- Response 200: detalle de proyecto
- Errors: 403, 404

## 10. Project Memberships

### POST /api/v1/projects/{projectId}/memberships

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador (con limites)
- Request:
  - `userId` (required)
  - `roleCode` (`DIRECTOR_PROYECTOS|PRESUPUESTADOR|AUDITOR`, required)
- Validations:
  - usuario debe estar afiliado previamente a la compania del proyecto
  - Administrador solo asigna Directores de proyectos
  - Director de proyectos asigna usuarios permitidos de su compania
  - Presupuestador solo asigna Presupuestador/Auditor de su compania
  - Auditor no asigna usuarios
- Response 201: `membershipId`, `projectId`, `userId`, `isActive`
- Errors: 400, 403, 409

### PATCH /api/v1/projects/{projectId}/memberships/{membershipId}/deactivate

- Auth required: Yes
- Allowed roles: Director de proyectos, Presupuestador (con limites)
- Response 200: `membershipId`, `isActive=false`
- Errors: 403, 404

### GET /api/v1/projects/{projectId}/users

- Auth required: Yes
- Allowed roles: Administrador, Director de proyectos, Presupuestador, Auditor (segun alcance)
- Query: `search`, `roleCode`, `pageNumber`, `pageSize`
- Response 200: usuarios vinculados al proyecto + pagination
- Errors: 400, 403, 404

## 11. Out-of-Scope Contract Notes

- No se define aqui CRUD de roles/permisos.
- No se redefine contrato de autenticacion/login existente.
- No se incluyen endpoints de calculo APU/WBS fuera del alcance del feature 002.

## 12. Traceability Snapshot

- FR-003..FR-011, FR-042: contratos de companias
- FR-012..FR-022, FR-044..FR-047: contratos de proyectos y estado
- FR-018..FR-020: contratos de visibilidad y trazabilidad
- FR-023..FR-030: contratos de memberships
- FR-034..FR-035: contratos de catalogos
- FR-036..FR-041, FR-043: contratos de listados
- FR-048..FR-050: reglas no funcionales de esquema/FK/migraciones
