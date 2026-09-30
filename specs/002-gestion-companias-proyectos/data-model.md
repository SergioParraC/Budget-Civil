# Data Model: Gestion de companias y proyectos

Date: 2026-09-30
Feature: specs/002-gestion-companias-proyectos/spec.md
Scope: Backend only

## 1. Model Overview

El modelo soporta:
- Ciclo de vida de companias (crear, editar, inhabilitar, rehabilitar).
- Ciclo de vida de proyectos con flujo de estados explicito.
- Vinculaciones usuario-compania y usuario-proyecto con historial de alta/baja.
- Catalogos reutilizables para formularios.
- Trazabilidad separada de estado y visibilidad.
- Gobernanza de persistencia por esquemas segun FR-048/049/050.

## 2. Persistencia por esquema

| Schema | Entidades | Objetivo |
|--------|-----------|----------|
| `param` | `DocumentType`, `Country`, `Department`, `Municipality`, `BaseCurrency` | Catalogos de referencia |
| `iam` | `UserAccount`, `Role`, `Permission`, `RolePermission`, `UserSignature` | Identidad, roles y permisos |
| `dbo` | `Company`, `CompanyUserMembership`, `Project`, `ProjectUserMembership` | Operacion de negocio |
| `audit` | `ProjectStatusTransition`, `ProjectVisibilityTransition` | Trazabilidad funcional |
| `log` | tablas de logs tecnicos y operacionales | Observabilidad tecnica sin FK de negocio |

## 3. Entidades y campos clave

## 3.1 `dbo.Company`

Purpose:
- Tenant organizacional del sistema.

Core fields:
- `id` (uuid, PK)
- `business_name` (nvarchar(255), required)
- `tax_id` (nvarchar(20), required, unique)
- `company_type` (enum: `PUBLICA`, `PRIVADA`, required)
- `territorial_level` (nvarchar(50), required si `company_type=PUBLICA`, opcional si `PRIVADA`)
- `country_id` (uuid, FK -> `param.Country.id`, required)
- `department_id` (uuid, FK -> `param.Department.id`, required)
- `municipality_id` (uuid, FK -> `param.Municipality.id`, required)
- `representative_mode` (enum: `USER_REFERENCE`, `MANUAL_DATA`, required)
- `representative_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `representative_full_name` (nvarchar(200), nullable)
- `representative_document_type_id` (uuid, FK -> `param.DocumentType.id`, nullable)
- `representative_document_number` (nvarchar(50), nullable)
- `is_active` (bit, required, default `1`)
- `created_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `created_at` (datetime2, required)
- `last_modified_by_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `last_modified_at` (datetime2, nullable)

Rules:
- Sin borrado fisico.
- Inhabilitar bloquea operacion para actores no autorizados.
- Rehabilitar no reactiva membresias manualmente desactivadas.

## 3.2 `dbo.CompanyUserMembership`

Purpose:
- Vinculacion historica entre usuario y compania.

Core fields:
- `id` (uuid, PK)
- `company_id` (uuid, FK -> `dbo.Company.id`, required)
- `user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `assigned_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `assigned_at` (datetime2, required)
- `deactivated_by_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `deactivated_at` (datetime2, nullable)
- `is_active` (bit, required)

Rules:
- Director de proyectos, Presupuestador y Auditor: maximo una membresia activa.
- Administrador no puede pertenecer a companias.

## 3.3 `dbo.Project`

Purpose:
- Unidad principal de operacion para cotizacion/aprobacion/cierre.

Core fields:
- `id` (uuid, PK)
- `company_id` (uuid, FK -> `dbo.Company.id`, required)
- `code` (nvarchar(50), nullable)
- `name` (nvarchar(255), required)
- `description` (nvarchar(1000), nullable)
- `objective` (nvarchar(1000), nullable)
- `country_id` (uuid, FK -> `param.Country.id`, required)
- `department_id` (uuid, FK -> `param.Department.id`, required)
- `municipality_id` (uuid, FK -> `param.Municipality.id`, required)
- `base_currency_id` (uuid, FK -> `param.BaseCurrency.id`, required)
- `aiu_admin_percent` (decimal(5,2), nullable)
- `aiu_unforeseen_percent` (decimal(5,2), nullable)
- `aiu_profit_percent` (decimal(5,2), nullable)
- `status` (enum: `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`, `INHABILITADO`, required)
- `is_public` (bit, required, default `0`)
- `signing_mode` (enum: `UNICA_POR_PRESUPUESTADOR_ASIGNADO`, `INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE`, required)
- `signer_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `director_owner_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `created_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `created_at` (datetime2, required)
- `last_modified_by_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `last_modified_at` (datetime2, nullable)

Rules:
- Estado inicial obligatorio: `POR_COTIZAR`.
- Cambio de moneda base no recalcula automaticamente montos existentes.
- `signer_user_id` puede ser null segun modo de firma.

## 3.4 `dbo.ProjectUserMembership`

Purpose:
- Vinculacion historica entre usuario y proyecto.

Core fields:
- `id` (uuid, PK)
- `project_id` (uuid, FK -> `dbo.Project.id`, required)
- `user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `assigned_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `assigned_at` (datetime2, required)
- `deactivated_by_user_id` (uuid, FK -> `iam.UserAccount.id`, nullable)
- `deactivated_at` (datetime2, nullable)
- `is_active` (bit, required)

Rules:
- El usuario debe estar afiliado previamente a la compania del proyecto.
- Administrador solo asigna Directores de proyectos en proyecto.

## 3.5 `audit.ProjectStatusTransition`

Purpose:
- Historial de cambios de estado del proyecto.

Core fields:
- `id` (uuid, PK)
- `project_id` (uuid, FK -> `dbo.Project.id`, required)
- `previous_status` (enum, required)
- `new_status` (enum, required)
- `changed_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `changed_at` (datetime2, required)
- `reason` (nvarchar(500), nullable)
- `transition_kind` (enum: `ORDINARY`, `REOPEN`, `DISABLE`, `REACTIVATE`, required)

Rules:
- Registro obligatorio por cada cambio de estado.
- Tabla append-only.

## 3.6 `audit.ProjectVisibilityTransition`

Purpose:
- Historial de cambios de visibilidad publica/privada.

Core fields:
- `id` (uuid, PK)
- `project_id` (uuid, FK -> `dbo.Project.id`, required)
- `previous_is_public` (bit, required)
- `new_is_public` (bit, required)
- `changed_by_user_id` (uuid, FK -> `iam.UserAccount.id`, required)
- `changed_at` (datetime2, required)
- `reason` (nvarchar(500), nullable)

Rules:
- Registro obligatorio por cada cambio de visibilidad.
- Tabla append-only.

## 3.7 `iam.UserSignature`

Purpose:
- Evidencia de firma de usuario para habilitar cierre de proyecto.

Core fields:
- `id` (uuid, PK)
- `user_id` (uuid, FK -> `iam.UserAccount.id`, required, unique)
- `signature_name` (nvarchar(150), required)
- `signature_image_base64` (nvarchar(max), required)
- `is_active` (bit, required, default `1`)
- `updated_at` (datetime2, required)

Rules:
- Para pasar a `FINALIZADO`, el actor debe tener firma activa cargada.

## 3.8 Catalogos en `param`

Entidades:
- `DocumentType(id, code, name, is_active)`
- `Country(id, code, name, is_active)`
- `Department(id, country_id, code, name, is_active)`
- `Municipality(id, department_id, code, name, is_active)`
- `BaseCurrency(id, iso_code, name, is_active)`

Rules:
- Son read-only para este feature.
- Se consumen por endpoints filtrables de catalogos.

## 3.9 Logs tecnicos en `log`

- El feature no define columnas cerradas para logs operacionales.
- Restriccion obligatoria: las tablas de `log` almacenan identificadores/snapshots y no crean FK a tablas de negocio.

## 4. Relaciones

- `Company (dbo)` 1:N `Project (dbo)`
- `Company (dbo)` 1:N `CompanyUserMembership (dbo)`
- `UserAccount (iam)` 1:N `CompanyUserMembership (dbo)`
- `Project (dbo)` 1:N `ProjectUserMembership (dbo)`
- `UserAccount (iam)` 1:N `ProjectUserMembership (dbo)`
- `Project (dbo)` 1:N `ProjectStatusTransition (audit)`
- `Project (dbo)` 1:N `ProjectVisibilityTransition (audit)`
- `UserAccount (iam)` 1:N `ProjectStatusTransition (audit)`
- `UserAccount (iam)` 1:N `ProjectVisibilityTransition (audit)`
- `UserAccount (iam)` 1:1 `UserSignature (iam)` (opcional por usuario)
- `Country (param)` 1:N `Department (param)`
- `Department (param)` 1:N `Municipality (param)`
- `Country/Department/Municipality (param)` 1:N `Company (dbo)`
- `Country/Department/Municipality (param)` 1:N `Project (dbo)`
- `BaseCurrency (param)` 1:N `Project (dbo)`

## 5. Reglas de FK entre esquemas (FR-049)

Permitidas:
- `dbo` -> `param`
- `dbo` -> `iam`
- `audit` -> `dbo`
- `audit` -> `iam`

Prohibidas:
- `param` -> `dbo`, `audit`, `log`
- `iam` -> `dbo`, `audit`, `log`
- cualquier esquema -> `log`
- `log` -> cualquier tabla de negocio

## 6. Maquina de estados de proyecto

## 6.1 Transiciones permitidas

| Desde | Hacia | Actor permitido | Precondiciones |
|------|-------|-----------------|----------------|
| `POR_COTIZAR` | `FALTA_INFORMACION` | Director de proyectos, Presupuestador | accion explicita |
| `FALTA_INFORMACION` | `COTIZADO` | Director de proyectos, Presupuestador | datos completos + insumos cotizados |
| `COTIZADO` | `FINALIZADO` | Director de proyectos | reglas de firma cumplidas + firma activa |
| `FINALIZADO` | `FALTA_INFORMACION` | Director de proyectos | reapertura explicita |
| `ANY` | `INHABILITADO` | Director de proyectos | accion explicita |
| `INHABILITADO` | `FALTA_INFORMACION` | Director de proyectos, Administrador | reactivacion explicita |

## 6.2 Reglas prohibidas

- Cambiar estado por editar campos del proyecto.
- Salir de `INHABILITADO` hacia un estado distinto a `FALTA_INFORMACION`.
- Finalizar sin firma activa del actor autorizado.

## 7. Invariantes funcionales

- Todos los endpoints privados exigen JWT valido.
- Todas las operaciones sensibles exigen `permission_key` activo.
- Aislamiento por `CompanyId` se aplica en lecturas y escrituras privadas.
- Tamanos de pagina permitidos: 10, 50, 100.
- Usuarios no administradores: maximo una compania activa.
- Administrador no pertenece a companias.
- Creacion de proyecto se rechaza si no hay Director de proyectos activo.
- Si crea Presupuestador, se asigna automaticamente el Director activo mas antiguo.
- En estado `INHABILITADO`, el proyecto solo es visible para Administrador y Director de proyectos.

## 8. Estrategia de migraciones y convenciones (FR-050)

Cadena unica EF Core:
- Orden obligatorio por dependencia: `param` -> `iam` -> `dbo` -> `audit` -> `log`.
- Cada entidad declara esquema explicito en mapeo y migracion.

Convencion de nombres:
- PK: `PK_<schema>_<table>`
- FK: `FK_<schema>_<table>_<ref_table>`
- IX: `IX_<schema>_<table>_<columns>`
- UQ: `UQ_<schema>_<table>_<columns>`
- CK: `CK_<schema>_<table>_<rule>`

Nombre de migracion:
- `YYYYMMDDHHMM_<scope>_<change>`

Ejemplo:
- `202609301130_param_initial_catalogs`
- `202609301145_iam_initial_access`
- `202609301200_dbo_companies_projects`
- `202609301215_audit_project_transitions`
- `202609301230_log_operational_logs`

## 9. Indices sugeridos

- `IX_dbo_Project_CompanyId_Status` en `dbo.Project(company_id, status, id)`
- `IX_dbo_Project_CompanyId_IsPublic` en `dbo.Project(company_id, is_public, id)`
- `IX_dbo_Company_IsActive` en `dbo.Company(is_active, id)`
- `UQ_dbo_CompanyUserMembership_UserActive` filtrado por `is_active=1` para no administradores
- `IX_dbo_ProjectUserMembership_ProjectActive` en `dbo.ProjectUserMembership(project_id, is_active)`
- `IX_audit_ProjectStatusTransition_ProjectChangedAt` en `audit.ProjectStatusTransition(project_id, changed_at desc)`
- `IX_audit_ProjectVisibilityTransition_ProjectChangedAt` en `audit.ProjectVisibilityTransition(project_id, changed_at desc)`

## 10. Data Change Governance

Los cambios estructurales derivados de este modelo requieren:
- trazabilidad a FR/UC aprobados,
- autorizacion explicita del usuario responsable,
- propuesta de migracion/impacto,
- sincronizacion documental en el mismo ciclo.
