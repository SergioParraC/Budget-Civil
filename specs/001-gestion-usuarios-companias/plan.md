# Implementation Plan: Sistema de Usuarios y Panel de Administracion de Usuarios/Companias

**Branch**: `001-gestion-usuarios-companias` | **Date**: 2026-09-22 | **Spec**: specs/001-gestion-usuarios-companias/spec.md

**Input**: Feature specification from specs/001-gestion-usuarios-companias/spec.md

## Summary

Actualizar el plan tecnico del feature 001 para mantener alcance backend-only, arquitectura en 4 capas productivas (Api, Application, Domain, Infrastructure), autorizacion data-driven por `permission_key`, y estrategia de pruebas unificada en un solo proyecto `BlackBoxTests`. La implementacion centraliza gobierno de permisos en BD (`Role`, `Permission`, `RolePermission`) y sincroniza reglas recientes del spec: creacion administrativa de usuarios exclusiva de Administrador, creacion de compania por Gerente de proyectos limitada a una unica compania propia, edicion de compania del gerente restringida a su propia compania y exclusiones explicitas de operaciones de usuario-proyecto en este feature.

## Technical Context

**Language/Version**: C# con .NET 10 (ASP.NET Core Web API)

**Primary Dependencies**:
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- EntityFrameworkCore 8.x
- FluentValidation.AspNetCore
- AutoMapper
- Serilog.AspNetCore
- Swashbuckle.AspNetCore
- Implementacion compatible de hash Argon2id para credenciales

**Storage**: SQL Server 2022 via EF Core; conexion centralizada en un unico punto de app config backend

**Testing**: Unico proyecto `backend/tests/BudgetCivil.Backend.BlackBoxTests` (xUnit + FluentAssertions) para pruebas black-box de contratos, autorizacion y flujos end-to-end del feature

**Target Platform**: Despliegue Linux Ubuntu 24.04; desarrollo local Windows

**Project Type**: Backend web service

**Performance Goals**:
- p95 <= 2 segundos para busquedas administrativas de usuarios por filtros
- 95% de operaciones administrativas completadas por usuario entrenado en menos de 3 minutos

**Constraints**:
- Alcance exclusivo backend-only
- 4 capas productivas obligatorias: Api, Application, Domain, Infrastructure
- JWT + validacion por `permission_key` activo + privacidad por `CompanyId` en endpoints privados
- Politica fail-closed: rol inactivo o permiso faltante/inactivo => `403` + auditoria
- Sin borrado fisico de usuarios ni companias
- Roles base sin CRUD en esta fase
- Creacion administrativa de usuarios solo por Administrador; para no administradores aplica self-registration
- Gerente de proyectos puede crear una sola compania propia y solo editar esa compania; no puede desactivar companias
- Administrador no puede consultar APUs ni insumos en este feature
- Asignacion de usuarios a proyectos queda fuera de alcance y debe rechazarse en endpoints del feature

**Scale/Scope**:
- 6 historias de usuario del spec
- 17 endpoints en contrato del feature
- 4 roles base iniciales (Administrador, Gerente de proyectos, Presupuestador, Auditor)
- Extensibilidad futura de roles habilitada via BD (catalogo Role + asignaciones RolePermission)
- Reglas de compania por rol: Admin (alta/edicion/desactivacion), Gerente (alta unica propia + edicion propia)

**NEEDS CLARIFICATION**: None

## Constitution Check

*GATE: MUST pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Result

- [x] C1 - Source-of-truth alignment: Plan alineado con spec activo y docs canonicos (arquitectura, stack, modelo de datos, constitucion).
- [x] C2 - Scope alignment: Trabajo restringido a backend del feature 001.
- [x] C3 - Architecture alignment: Reglas de negocio en Domain/Application; acceso a datos en Infrastructure; exposicion HTTP en Api.
- [x] C4 - Security alignment: JWT + RBAC por `permission_key` + privacidad por `CompanyId` + fail-closed.
- [x] C5 - Data schema governance: Cambios estructurales potenciales documentados con impacto y migracion; autorizacion explicita requerida en implementacion.
- [x] C6 - API contract readiness: Contratos incluyen metodo, ruta, auth, `permission_key`, request/response, validaciones y errores.
- [x] C7 - Test strategy alignment: Alineado a decision de equipo: un solo `BlackBoxTests` para el feature en esta fase.
- [x] C8 - Traceability completeness: Mapeo FR/BR/endpoint/pruebas definido en plan y artefactos de fase.
- [x] C9 - Documentation sync: `plan.md`, `research.md`, `data-model.md`, `quickstart.md` y `contracts/` sincronizados en el mismo cambio.

### Post-Phase 1 Gate Result

Status: PASS

Evidence:
- `research.md` resuelve decisiones y trade-offs sin pendientes
- `data-model.md` define entidades, relaciones, validaciones y transiciones
- `contracts/users-companies-api.md` documenta autorizacion por `permission_key` y convenciones fail-closed
- `quickstart.md` valida seguridad por `CompanyId`, denegaciones `403`, lockout y seed idempotente

## Project Structure

### Documentation (this feature)

```text
specs/001-gestion-usuarios-companias/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── users-companies-api.md
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── BudgetCivil.Api/
│   ├── BudgetCivil.Application/
│   ├── BudgetCivil.Domain/
│   └── BudgetCivil.Infrastructure/
└── tests/
    └── BudgetCivil.Backend.BlackBoxTests/
```

**Structure Decision**:
Se mantiene arquitectura por capas con 4 capas productivas obligatorias. La suite de validacion del feature se concentra en un unico proyecto `BlackBoxTests` para reducir complejidad y maximizar trazabilidad de comportamiento real de API en esta fase.

## Technical Decisions and Trade-offs

### Decision A: Authorization data-driven by permission_key

- Decision: Endpoints privados autorizan por `permission_key` activo obtenido desde BD (`Role` + `Permission` + `RolePermission`), no por listas hardcodeadas de rol.
- Why: Permite habilitar roles/permisos futuros sin redeploy ni cambios de codigo en controladores.
- Trade-off: Requiere baseline seed confiable y consultas de permisos eficientes.
- Mitigation: Seed idempotente versionado + indices en `RolePermission`.

### Decision B: Base role catalog immutable in this phase

- Decision: No hay CRUD de roles base en endpoints del feature.
- Why: Evita drift de seguridad durante fase inicial.
- Trade-off: Menor flexibilidad operativa inmediata.
- Mitigation: Extensibilidad controlada via BD y gestion de permisos por `RolePermission` con gobierno de migraciones.

### Decision C: Fail-closed policy

- Decision: Rol inactivo o permiso faltante/inactivo produce `403` y evento de seguridad obligatorio.
- Why: Reduce riesgo de privilegios implicitos o configuraciones incompletas.
- Trade-off: Puede aumentar rechazos en ambientes mal configurados.
- Mitigation: Quickstart incluye validacion de baseline seed y pruebas negativas de autorizacion.

### Decision D: Single BlackBoxTests project

- Decision: Consolidar contrato + integracion en `BudgetCivil.Backend.BlackBoxTests` para este feature.
- Why: Decision explicita del equipo y de la aclaracion del spec.
- Trade-off: Menor separacion fisica por tipo de prueba.
- Mitigation: Organizacion por carpetas/traits por historia y por tipo de escenario (auth, users, companies, security).

### Decision E: Administrative user creation restricted to Administrador

- Decision: `POST /api/v1/users` se habilita solo para rol Administrador.
- Why: Alinea el spec actualizado y evita escalamiento lateral de altas administrativas desde roles operativos.
- Trade-off: Menor descentralizacion operativa para alta de usuarios internos.
- Mitigation: Mantener self-registration publico para altas no administrativas y flujos de asignacion/rol separados.

### Decision F: Company management split by role

- Decision: Administrador mantiene gestion completa de companias; Gerente de proyectos puede crear una unica compania propia y editar solo su compania.
- Why: Alinea necesidad operativa de arranque por gerente sin perder gobierno central de tenant.
- Trade-off: Requiere validaciones adicionales de unicidad por gerente y ownership de compania.
- Mitigation: Regla de negocio explicita + validacion por membresia activa/company ownership + pruebas negativas de segunda compania.

## Phase 0 Output (Research)

Archivo: `specs/001-gestion-usuarios-companias/research.md`

Resultados clave:
- Modelo de autorizacion data-driven por `permission_key`
- Politica fail-closed con denegacion `403` auditable
- Baseline seed idempotente/versionado para `Role`, `Permission`, `RolePermission`
- Convencion para habilitar roles futuros desde BD sin hardcode por endpoint
- Justificacion de pruebas en un unico proyecto `BlackBoxTests`

## Phase 1 Output (Design and Contracts)

Archivos:
- `specs/001-gestion-usuarios-companias/data-model.md`
- `specs/001-gestion-usuarios-companias/contracts/users-companies-api.md`
- `specs/001-gestion-usuarios-companias/quickstart.md`

Resultados clave:
- Modelo de datos funcional para autenticacion, administracion, permisos y auditoria
- Contratos API backend listos para consumo frontend con `permission_key` por endpoint privado
- Guia quickstart de validacion end-to-end con escenarios positivos y negativos de seguridad

## Testing Strategy (Feature Decision)

- El feature usa un solo proyecto `BlackBoxTests` por decision de equipo.
- Se validan contratos y flujos completos atravesando Api -> Application -> Domain -> Infrastructure.
- Se evitan proyectos separados `ContractTests` y `IntegrationTests` en esta fase para reducir overhead y divergencia entre suites.
- No se agregan unit tests de dominio para este feature salvo impacto accidental en calculos APU (fuera de alcance previsto).

## Data Schema Change Control

- **Driver Requirement (FR/BR)**: FR-034..FR-037, BR-022..BR-025 y reglas de seguridad/tenant del feature.
- **Change Type**: Add/Alter (segun estado actual del esquema).
- **Candidate Structural Changes**:
  1. Entidades/catalogos de permisos (`Permission`, `RolePermission`) y metadatos de activacion.
  2. Campos de lockout/login en cuenta de usuario.
  3. Trazabilidad de denegaciones y auditoria de bootstrap.
- **Migration Strategy**:
  1. Migraciones EF Core versionadas.
  2. Seed baseline idempotente para seguridad.
  3. Verificacion de integridad e indices antes de habilitar endpoints.
- **Rollback Strategy**:
  1. Revertir migracion afectada.
  2. Restaurar baseline de permisos previo.
  3. Validar que endpoints privados no queden en estado fail-open.
- **Explicit User Authorization Evidence**: Se registra en PR/issue de implementacion cuando se concrete cambio estructural.

## Traceability Matrix

| Requirement Group | Endpoints/Contracts | Planned BlackBox Evidence |
|-------------------|---------------------|---------------------------|
| FR-001, FR-002, FR-031 | `POST /api/v1/auth/login` | Login exitoso/fallido + lockout al 5to intento |
| FR-003..FR-007 | `POST /api/v1/admin/bootstrap/first-administrator`, `GET /api/v1/audit/bootstrap` | Bootstrap idempotente + visibilidad de auditoria solo Admin |
| FR-008..FR-013, FR-028, FR-030, FR-033 | Endpoints `/api/v1/auth/register` y `/api/v1/users*` | Self-registration + creacion administrativa solo Admin + cambios de rol/compania por alcance |
| FR-014 | Endpoints `/api/v1/companies*` | Gestion por rol: Admin full, Gerente alta unica propia + edicion propia |
| FR-015, FR-022..FR-024, FR-026, FR-032 | Endpoints privados del feature | RBAC por `permission_key` + aislamiento por `CompanyId` |
| FR-025, FR-038..FR-040 | `POST /api/v1/auth/register`, `POST /api/v1/auth/login` y consumo de capacidades existentes de lectura APU publica | Usuario de self-registration autentica y consume lectura APU publica; Administrador sin acceso a APU/insumos |
| FR-016..FR-020 | Endpoints `/api/v1/profile/me*` y recovery admin | Autoservicio + politica password + recuperacion asistida |
| FR-034..FR-037 | Todos los endpoints privados | Fail-closed, baseline seed y roles/permisos desde BD |

## Complexity Tracking

Sin violaciones de constitucion en esta fase.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
