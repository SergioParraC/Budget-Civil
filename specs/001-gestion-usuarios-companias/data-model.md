# Data Model: Sistema de Usuarios y Panel de Administracion de Usuarios/Companias

Date: 2026-09-22
Feature: specs/001-gestion-usuarios-companias/spec.md
Scope: Backend only

## 1. Model Overview

El modelo define autenticacion, administracion de usuarios/companias, permisos data-driven y trazabilidad de seguridad para el feature 001. Se mantiene multitenancy estricto por `CompanyId`, sin borrado fisico y con control de autorizacion por `permission_key`.

## 2. Entities

### 2.1 Company

Purpose:
- Tenant organizacional del sistema.

Core fields:
- id: UUID, PK
- business_name: string(255), required
- tax_id: string(20), required, unique
- is_active: bool, required, default true
- created_at: datetime, required
- updated_at: datetime, required

Rules:
- No se elimina fisicamente.
- Desactivacion logica para cierre operativo.

### 2.2 Role

Purpose:
- Catalogo de roles del sistema que gobierna autorizacion junto con permisos.

Core fields:
- id: UUID, PK
- code: string(50), required, unique
- name: string(120), required
- is_active: bool, required, default true
- is_base_role: bool, required, default true
- created_at: datetime, required
- updated_at: datetime, required

Rules:
- Roles base no tienen CRUD de negocio en esta fase.
- Extensibilidad futura de roles se habilita desde BD mediante gobernanza de cambios.

### 2.3 Permission

Purpose:
- Permisos atomicos para control de acceso por `permission_key`.

Core fields:
- id: UUID, PK
- permission_key: string(100), required, unique
- description: string(255), required
- is_active: bool, required, default true
- created_at: datetime, required
- updated_at: datetime, required
- seed_version: string(20), required

Rules:
- El catalogo se mantiene por baseline seed versionado.

### 2.4 RolePermission

Purpose:
- Asignacion many-to-many entre Role y Permission.

Core fields:
- id: UUID, PK
- role_id: UUID, FK -> Role.id
- permission_id: UUID, FK -> Permission.id
- is_active: bool, required, default true
- assigned_at: datetime, required
- revoked_at: datetime, optional
- seed_version: string(20), optional

Rules:
- Unicidad compuesta (`role_id`, `permission_id`).
- Si `is_active = false`, el permiso no puede autorizar operaciones.

### 2.5 UserAccount

Purpose:
- Credenciales, estado de cuenta y metadatos de autenticacion.

Core fields:
- id: UUID, PK
- company_id: UUID, FK -> Company.id, optional
- role_id: UUID, FK -> Role.id
- email: string(150), required, unique global
- username: string(100), required, unique global
- password_hash: string(255), required
- account_enabled: bool, required, default true
- account_locked: bool, required, default false
- failed_login_attempts: int, required, default 0
- last_login_at: datetime, optional
- created_at: datetime, required
- updated_at: datetime, required

Rules:
- Password siempre en hash Argon2id.
- Lockout al quinto intento fallido consecutivo.
- Si la cuenta esta bloqueada o deshabilitada, login retorna mensaje funcional obligatorio.
- Self-registration crea cuenta con rol Presupuestador y sin compania asignada (`company_id = null`).
- Usuario administrativo (Administrador) puede existir sin asociacion de compania (`company_id = null`).
- Sin borrado fisico.

### 2.6 UserProfile

Purpose:
- Datos personales y profesionales asociados al usuario.

Core fields:
- user_id: UUID, PK/FK -> UserAccount.id
- primer_nombre: string(100), required
- segundo_nombre: string(100), optional
- primer_apellido: string(100), required
- segundo_apellido: string(100), optional
- tipo_documento: string(20), required
- numero_documento: string(50), required
- fecha_nacimiento: date, required
- municipality_id: UUID, FK -> `param.Municipality.id`, required for new/updated profiles
- department_id: UUID, FK -> `param.Department.id`, required for new/updated profiles
- country_id: UUID, FK -> `param.Country.id`, required for new/updated profiles
- tarjeta_profesional: string(80), optional
- fecha_expedicion_tarjeta_profesional: date, optional
- archivo_tarjeta_profesional: string(255), optional

Rules:
- `numero_documento` participa en busqueda administrativa.
- La ubicacion se persiste mediante claves de catalogo; municipio, departamento y pais deben pertenecer a la misma jerarquia y estar activos.
- En registros historicos, las tres FK pueden ser nulas si no se puede determinar la ubicacion anterior sin ambiguedad. Las nuevas altas y actualizaciones siempre las requieren.

### 2.7 UserCompanyMembership

Purpose:
- Historial de pertenencia de usuarios a companias.

Core fields:
- id: UUID, PK
- user_id: UUID, FK -> UserAccount.id
- company_id: UUID, FK -> Company.id
- is_active: bool, required
- assigned_by_user_id: UUID, FK -> UserAccount.id
- assigned_at: datetime, required
- ended_at: datetime, optional

Rules:
- Maximo una membresia activa por usuario.
- Cambio de compania solo si no hay membresia activa previa.

### 2.8 PasswordRecoveryGrant

Purpose:
- Habilitacion asistida por Administrador para recuperar acceso/cambio de password.

Core fields:
- id: UUID, PK
- user_id: UUID, FK -> UserAccount.id
- enabled_by_user_id: UUID, FK -> UserAccount.id
- reason: string(255), required
- enabled_at: datetime, required
- expires_at: datetime, optional
- is_active: bool, required
- consumed_at: datetime, optional

Rules:
- Solo Administrador puede habilitar.

### 2.9 SecurityAuditEvent

Purpose:
- Auditoria de operaciones sensibles y denegaciones de seguridad.

Core fields:
- id: UUID, PK
- actor_user_id: UUID, optional
- company_id: UUID, optional
- event_type: enum {AUTH_LOGIN, AUTH_LOCKOUT, AUTHORIZATION_DENIED, BOOTSTRAP, USER_ADMIN, COMPANY_ADMIN, PASSWORD_RECOVERY}
- permission_key: string(100), optional
- target_type: string(50), optional
- target_id: UUID, optional
- reason_code: string(80), optional
- result: enum {SUCCESS, DENIED, FAILED}
- occurred_at: datetime, required
- metadata_json: json/text, optional

Rules:
- Append-only.
- Obligatorio para denegaciones fail-closed.

## 3. Relationships

- Company 1:N UserAccount (relacion opcional en usuario por `company_id` nullable)
- Role 1:N UserAccount
- Role N:M Permission via RolePermission
- UserAccount 1:1 UserProfile
- UserAccount 1:N UserCompanyMembership
- Company 1:N UserCompanyMembership
- UserAccount 1:N PasswordRecoveryGrant (target)
- UserAccount 1:N PasswordRecoveryGrant (enabled_by)
- UserAccount 1:N SecurityAuditEvent (actor)

## 4. Permission Keys Baseline (Reference)

- USERS_CREATE
- USERS_UPDATE
- USERS_DEACTIVATE
- USERS_ROLE_CHANGE
- USERS_COMPANY_CHANGE
- USERS_READ
- COMPANIES_CREATE
- COMPANIES_UPDATE
- COMPANIES_DEACTIVATE
- PROFILE_READ_SELF
- PROFILE_UPDATE_SELF
- PROFILE_CHANGE_PASSWORD_SELF
- USERS_PASSWORD_RECOVERY_ENABLE
- AUDIT_BOOTSTRAP_READ

Notas:
- Los nombres son claves estables para autorizacion.
- Las asignaciones por rol viven en `RolePermission` y pueden evolucionar por migracion/seed versionado.

## 5. Validation Rules and Invariants

- email unico global.
- username unico global.
- password_hash obligatorio con algoritmo seguro (Argon2id).
- borrado fisico prohibido para UserAccount y Company.
- privacidad por CompanyId obligatoria en operaciones privadas.
- fail-closed obligatorio: rol inactivo o permiso faltante/inactivo => `403` + auditoria.
- Auditor no crea usuarios.
- Creacion administrativa de usuarios exclusiva de Administrador.
- Para actores no Administrador, el alta de usuarios se canaliza por self-registration con defaults:
  - rol_asignado = Presupuestador
  - company_id = null
- Gerente de proyectos puede crear una unica compania propia y editar solo esa compania.
- Gerente de proyectos opera dentro de su compania y no eleva a Administrador.
- Administrador no tiene permisos de consulta de APUs/insumos en este feature.
- Desactivar al ultimo Administrador activo debe rechazarse (continuidad de gobierno).

## 6. State Transitions

### 6.1 Account State

States:
- ENABLED
- LOCKED
- DISABLED

Transitions:
- ENABLED -> LOCKED: al llegar a 5 intentos fallidos consecutivos
- LOCKED -> ENABLED: recuperacion asistida o habilitacion administrativa
- ENABLED -> DISABLED: desactivacion administrativa
- DISABLED -> ENABLED: habilitacion administrativa

### 6.2 Bootstrap State

States:
- BOOTSTRAP_ENABLED
- BOOTSTRAP_DISABLED

Transitions:
- BOOTSTRAP_ENABLED -> BOOTSTRAP_DISABLED: al crear primer Administrador
- BOOTSTRAP_DISABLED -> BOOTSTRAP_DISABLED: reintentos idempotentes rechazados

### 6.3 Permission Assignment State

States:
- ACTIVE
- INACTIVE

Transitions:
- ACTIVE -> INACTIVE: revocacion administrativa controlada por datos
- INACTIVE -> ACTIVE: reactivacion controlada por datos

Behavior:
- INACTIVE nunca autoriza solicitudes privadas.

### 6.4 Password Recovery State

States:
- NOT_GRANTED
- GRANTED
- CONSUMED
- EXPIRED

Transitions:
- NOT_GRANTED -> GRANTED: habilitacion por Administrador
- GRANTED -> CONSUMED: password cambiada exitosamente
- GRANTED -> EXPIRED: supera ventana de vigencia

## 7. Index and Performance Considerations

- uq_user_email_global on UserAccount(email)
- uq_user_username_global on UserAccount(username)
- idx_user_company_role_state on UserAccount(company_id, role_id, account_enabled, account_locked)
- uq_role_code on Role(code)
- uq_permission_key on Permission(permission_key)
- uq_role_permission on RolePermission(role_id, permission_id)
- idx_role_permission_active on RolePermission(role_id, permission_id, is_active)
- uq_user_active_membership filtered on UserCompanyMembership(user_id) where is_active = true
- idx_security_audit_time on SecurityAuditEvent(occurred_at DESC)
- idx_security_audit_actor on SecurityAuditEvent(actor_user_id, occurred_at DESC)

## 8. Data Change Impact

Potential structural updates for implementation:
- Incorporar catalogo Permission y tabla RolePermission (si aun no existen).
- Extender UserAccount con campos de lockout/auditoria de acceso (si faltan).
- Incorporar SecurityAuditEvent y PasswordRecoveryGrant (si faltan).

Governance:
- Toda migracion estructural requiere autorizacion explicita en PR/issue y sincronizacion documental.
