# API Contracts: Users and Companies (Backend)

Date: 2026-09-22
Feature: specs/001-gestion-usuarios-companias/spec.md
Scope: Backend only

## Contract Conventions

- Auth: JWT Bearer en endpoints privados.
- Authorization: validacion por `permission_key` activo (data-driven en BD).
- Role lists: se mantienen como referencia funcional, no como control hardcodeado.
- Role extensibility: roles futuros se habilitan desde BD (`Role` + `RolePermission`) sin modificar contratos.
- Fail-closed: rol inactivo o permiso faltante/inactivo => `403` + evento de auditoria.
- Privacy: `CompanyId` obligatorio en operaciones privadas segun contexto autenticado.
- Soft delete: usuarios y companias solo se desactivan.
- Password: politica minima y hash Argon2id.
- Read-only APU/insumo access en este feature: solo roles operativos autorizados; Administrador no tiene acceso de consulta.

## Permission Keys Baseline (private endpoints)

| Endpoint | Permission Key |
|----------|----------------|
| POST /api/v1/users | USERS_CREATE |
| PUT /api/v1/users/{userId} | USERS_UPDATE |
| PATCH /api/v1/users/{userId}/deactivate | USERS_DEACTIVATE |
| PATCH /api/v1/users/{userId}/role | USERS_ROLE_CHANGE |
| PATCH /api/v1/users/{userId}/company | USERS_COMPANY_CHANGE |
| GET /api/v1/users | USERS_READ |
| POST /api/v1/companies | COMPANIES_CREATE |
| PUT /api/v1/companies/{companyId} | COMPANIES_UPDATE |
| PATCH /api/v1/companies/{companyId}/deactivate | COMPANIES_DEACTIVATE |
| GET /api/v1/profile/me | PROFILE_READ_SELF |
| PUT /api/v1/profile/me | PROFILE_UPDATE_SELF |
| POST /api/v1/profile/me/change-password | PROFILE_CHANGE_PASSWORD_SELF |
| POST /api/v1/users/{userId}/password-recovery/enable | USERS_PASSWORD_RECOVERY_ENABLE |
| GET /api/v1/audit/bootstrap | AUDIT_BOOTSTRAP_READ |

Public endpoints:
- POST /api/v1/auth/login
- POST /api/v1/auth/register
- POST /api/v1/admin/bootstrap/first-administrator (public only while bootstrap is enabled)

## Endpoints

### 1) POST /api/v1/auth/login

Auth required: No
Allowed roles reference: Public endpoint

Request:
- identifier: string, required (email o username)
- password: string, required

Response 200:
- access_token: string
- token_type: string
- expires_at: datetime
- user: { user_id, email, username, role, company_id, estado_cuenta }

Validations:
- Cuenta activa y no bloqueada.
- Lockout al 5to intento fallido consecutivo.

Errors:
- 401 credenciales invalidas
- 423 cuenta bloqueada o deshabilitada con mensaje funcional obligatorio

### 2) POST /api/v1/auth/register

Auth required: No
Allowed roles reference: Public endpoint

Request:
- primer_nombre: string, required
- primer_apellido: string, required
- email: string, required
- username: string, required
- password: string, required
- tipo_documento: string, required
- numero_documento: string, required

Response 201:
- user_id: uuid
- rol_asignado: string (fixed = Presupuestador)
- company_id: null
- estado_cuenta: bool
- fecha_creacion: datetime

Validations:
- Self-registration siempre fija rol Presupuestador.
- Debe rechazar payload con `rol_asignado`, `company_id` o `project_id`.

Errors:
- 400 payload invalido o campos no permitidos
- 409 conflicto de unicidad

### 3) POST /api/v1/admin/bootstrap/first-administrator

Auth required: No (solo si no existe Administrador)
Allowed roles reference: Public bootstrap controlado por estado

Request:
- admin_profile: object, required
- company: object, required

Response 200/201:
- bootstrap_enabled: bool
- administrator_created: bool
- administrator_id: uuid
- audit_event_id: uuid

Validations:
- Solo habilitado cuando no existe ningun Administrador.
- Idempotente ante reintentos.

Errors:
- 409 bootstrap deshabilitado por existencia previa de Administrador

### 4) POST /api/v1/users

Auth required: Yes
Permission key: USERS_CREATE
Allowed roles reference: Administrador

Company ownership:
- Solo Administrador puede crear usuarios por endpoint administrativo.
- Gerente de proyectos, Presupuestador y Auditor no pueden crear usuarios por este endpoint.

Request:
- primer_nombre: string, required
- segundo_nombre: string, optional
- primer_apellido: string, required
- segundo_apellido: string, optional
- tipo_documento: string, required
- numero_documento: string, required
- fecha_nacimiento: date, required
- residencia: object, required (municipio, departamento, pais)
- email: string, required
- username: string, required
- password: string, required
- rol_asignado: string, optional (solo Admin)
- tarjeta_profesional: string, optional
- fecha_expedicion_tarjeta_profesional: date, optional
- archivo_tarjeta_profesional: string, optional
- company_id: uuid, optional (solo Admin)

Response 201:
- user_id: uuid
- rol_asignado: string
- company_id: uuid/null
- estado_cuenta: bool
- fecha_creacion: datetime

Validations:
- email unico global
- username unico global
- politica minima de password
- Si actor != Administrador, rechazar creacion administrativa.

Errors:
- 400 validacion
- 403 sin permisos por permiso/rol/tenant
- 409 conflicto de unicidad

### 5) PUT /api/v1/users/{userId}

Auth required: Yes
Permission key: USERS_UPDATE
Allowed roles reference: Administrador, Gerente de proyectos

Request:
- Campos administrativos editables del perfil (sin cambio directo de password)

Response 200:
- user_id: uuid
- fecha_edicion: datetime

Errors:
- 403 sin permiso
- 404 no encontrado en alcance

### 6) PATCH /api/v1/users/{userId}/deactivate

Auth required: Yes
Permission key: USERS_DEACTIVATE
Allowed roles reference: Administrador, Gerente de proyectos

Request:
- motivo: string, required

Response 200:
- user_id: uuid
- estado_cuenta: bool (false)

Errors:
- 403 sin permiso
- 409 conflicto de continuidad administrativa (incluye regla ultimo Administrador)

### 7) PATCH /api/v1/users/{userId}/role

Auth required: Yes
Permission key: USERS_ROLE_CHANGE
Allowed roles reference: Administrador, Gerente de proyectos

Request:
- rol_asignado: string, required

Response 200:
- user_id: uuid
- rol_asignado: string

Validations:
- Gerente no eleva a Administrador.

Errors:
- 403 sin permiso

### 8) PATCH /api/v1/users/{userId}/company

Auth required: Yes
Permission key: USERS_COMPANY_CHANGE
Allowed roles reference: Administrador, Gerente de proyectos

Request:
- company_id: uuid, required

Response 200:
- user_id: uuid
- company_id: uuid

Validations:
- Cambio permitido solo si no existe membresia activa previa.

Errors:
- 403 sin permiso
- 409 usuario ya pertenece a compania activa

### 9) GET /api/v1/users

Auth required: Yes
Permission key: USERS_READ
Allowed roles reference: Administrador, Gerente de proyectos, Auditor (solo lectura)

Query params:
- email, username, nombre_apellidos, numero_documento, estado_cuenta

Response 200:
- Lista de usuarios filtrada por alcance autorizado

Errors:
- 403 sin permiso

### 10) POST /api/v1/companies

Auth required: Yes
Permission key: COMPANIES_CREATE
Allowed roles reference: Administrador, Gerente de proyectos

Company ownership:
- Administrador puede crear companias segun alcance autorizado.
- Gerente de proyectos solo puede crear una compania propia si no tiene una asociada.

Request:
- business_name: string, required
- tax_id: string, required
- is_active: bool, optional

Response 201:
- company_id: uuid
- fecha_creacion: datetime

Errors:
- 403 sin permiso
- 409 gerente ya tiene compania asociada
- 409 tax_id duplicado

### 11) PUT /api/v1/companies/{companyId}

Auth required: Yes
Permission key: COMPANIES_UPDATE
Allowed roles reference: Administrador, Gerente de proyectos

Company ownership:
- Administrador puede editar companias segun alcance autorizado.
- Gerente de proyectos solo puede editar su propia compania.

Request:
- Campos editables de compania

Response 200:
- company_id: uuid
- fecha_edicion: datetime

Errors:
- 403 sin permiso
- 404 no encontrada
- 409 conflicto de datos

### 12) PATCH /api/v1/companies/{companyId}/deactivate

Auth required: Yes
Permission key: COMPANIES_DEACTIVATE
Allowed roles reference: Administrador

Request:
- motivo: string, required

Response 200:
- company_id: uuid
- is_active: bool (false)

Errors:
- 403 sin permiso
- 409 conflicto de continuidad

### 13) GET /api/v1/profile/me

Auth required: Yes
Permission key: PROFILE_READ_SELF
Allowed roles reference: Administrador, Gerente de proyectos, Presupuestador, Auditor

Response 200:
- Perfil del usuario autenticado

Errors:
- 401 no autenticado

### 14) PUT /api/v1/profile/me

Auth required: Yes
Permission key: PROFILE_UPDATE_SELF
Allowed roles reference: Administrador, Gerente de proyectos, Presupuestador, Auditor

Request:
- Campos permitidos de perfil propio

Response 200:
- user_id: uuid
- fecha_edicion: datetime

Errors:
- 400 validacion
- 403 fuera de perfil propio

### 15) POST /api/v1/profile/me/change-password

Auth required: Yes
Permission key: PROFILE_CHANGE_PASSWORD_SELF
Allowed roles reference: Administrador, Gerente de proyectos, Presupuestador, Auditor

Request:
- current_password: string, required
- new_password: string, required

Response 200:
- password_updated: bool
- updated_at: datetime

Errors:
- 400 nueva password invalida
- 401 password actual incorrecta

### 16) POST /api/v1/users/{userId}/password-recovery/enable

Auth required: Yes
Permission key: USERS_PASSWORD_RECOVERY_ENABLE
Allowed roles reference: Administrador

Request:
- reason: string, required

Response 200:
- user_id: uuid
- recovery_enabled: bool

Errors:
- 403 sin permiso

### 17) GET /api/v1/audit/bootstrap

Auth required: Yes
Permission key: AUDIT_BOOTSTRAP_READ
Allowed roles reference: Administrador

Request:
- filtros opcionales por fecha/resultado

Response 200:
- lista de eventos { actor, accion, objetivo, fecha_hora, resultado }

Errors:
- 403 acceso no autorizado

## Out-of-Scope Contracts

- No se exponen endpoints de CRUD para roles base.
- No se exponen endpoints para crear permisos en runtime.
- No se exponen endpoints de asignacion usuario-proyecto en este feature.
- El consumo de APUs publicos/insumos se apoya en capacidades existentes fuera de este contrato; este artefacto no redefine esos endpoints.
- Frontend UI contracts no se implementan en esta fase; solo contratos HTTP backend.

## Traceability Snapshot

- FR-001..FR-040 y BR-001..BR-026 quedan cubiertos en este contrato.
- El mapeo detallado FR/endpoint/evidencia se mantiene en `plan.md` y `quickstart.md`.
