# Implementation Plan: Gestion de companias y proyectos

**Branch**: `002-gestion-companias-proyectos` | **Date**: 2026-09-30 | **Spec**: specs/002-gestion-companias-proyectos/spec.md

**Input**: Feature specification from specs/002-gestion-companias-proyectos/spec.md

## Summary

Traducir la especificacion del feature 002 a un plan tecnico ejecutable para backend, preservando arquitectura por capas (Api/Application/Domain/Infrastructure), seguridad fail-closed por `permission_key` + `CompanyId`, y cumplimiento de reglas funcionales de companias/proyectos. El plan incorpora explicitamente la segregacion por esquemas (`param`, `iam`, `dbo`, `audit`, `log`), reglas direccionales de FK y cadena unica de migraciones con convenciones obligatorias.

## Technical Context

**Language/Version**: C# con .NET 10 (`net10.0`)

**Primary Dependencies**:
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Design` (10.0.10)
- `FluentValidation.AspNetCore` y `FluentValidation`
- `Mapster`
- `Serilog.AspNetCore`
- `Swashbuckle.AspNetCore`

**Storage**: SQL Server 2022 via EF Core code-first

**Testing**: xUnit + FluentAssertions + Microsoft.AspNetCore.Mvc.Testing + EFCore.InMemory en `backend/tests/BudgetCivil.Backend.BlackBoxTests`

**Target Platform**: desarrollo local Windows y despliegue Linux Ubuntu 24.04 con Docker Compose

**Project Type**: backend web service

**Performance Goals**:
- p95 <= 1.5s en listados filtrables/paginados de usuarios, companias y proyectos
- p95 <= 1.5s en consultas por compania autenticada

**Constraints**:
- Alcance backend-only para este feature
- Arquitectura por capas obligatoria
- Reglas de negocio en Domain/Application
- Persistencia e integraciones tecnicas en Infrastructure
- JWT + RBAC por `permission_key` + aislamiento por `CompanyId`
- Politica fail-closed en endpoints privados
- Sin borrado fisico de companias/proyectos/vinculaciones
- Flujo de estados de proyecto con transiciones manuales explicitas
- Tamanos de pagina permitidos exclusivamente: 10, 50, 100
- Persistencia por esquemas y gobernanza FK/migraciones (FR-048/049/050)

**Scale/Scope**:
- 4 user stories (P1-P2)
- 50 requerimientos funcionales (FR-001..FR-050)
- 13 criterios medibles (SC-001..SC-013)
- 4 roles funcionales (Administrador, Director de proyectos, Presupuestador, Auditor)

**NEEDS CLARIFICATION**: None

## Constitution Check

*GATE: MUST pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Result: PASS

- [x] C1 - Fuentes de verdad: spec activo + docs 03/04/05 + constitucion revisadas.
- [x] C2 - Alcance activo: cambios limitados al feature 002, sin expansion fuera de alcance.
- [x] C3 - Arquitectura por capas: sin mover logica de negocio a controladores o infraestructura.
- [x] C4 - Seguridad: JWT, permisos y aislamiento por compania definidos como obligatorios.
- [x] C5 - Gobernanza de esquema: cambios estructurales sujetos a autorizacion y sincronizacion documental.
- [x] C6 - Contratos API: definidos con auth, permisos, request/response, validaciones y errores.
- [x] C7 - Estrategia de pruebas: black-box para feature, sin ampliar pruebas fuera de politica de fase.
- [x] C8 - Trazabilidad: mapeo FR -> contratos -> escenarios de validacion documentado.

## Project Structure

### Documentation (this feature)

```text
specs/002-gestion-companias-proyectos/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── companies-projects-api.md
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── BudgetCivil.Api/
│   │   ├── Controllers/
│   │   ├── Auth/
│   │   └── Middleware/
│   ├── BudgetCivil.Application/
│   │   ├── Companies/
│   │   ├── Users/
│   │   └── Common/
│   ├── BudgetCivil.Domain/
│   └── BudgetCivil.Infrastructure/
│       ├── Persistence/
│       └── Security/
└── tests/
    └── BudgetCivil.Backend.BlackBoxTests/
```

**Structure Decision**: se mantiene la estructura existente de 4 capas productivas y una suite black-box, sin crear proyectos adicionales.

## Technical Decisions and Trade-offs

### Decision 1: Transiciones de estado por endpoint explicito

- Decision: cambios de estado posteriores a la creacion se realizan por endpoint dedicado, nunca por editar proyecto.
- Why: cumplimiento de FR-013 y FR-045, y mayor auditabilidad.
- Trade-off: mas validaciones de negocio por transicion.
- Mitigation: validadores de transicion en Application + invariantes en Domain.

### Decision 2: Matriz de autorizacion por estado

- Decision: `FALTA_INFORMACION` y `COTIZADO` por Director/Presupuestador; `FINALIZADO` e `INHABILITADO` por Director; salida de `INHABILITADO` por Director o Administrador con destino fijo `FALTA_INFORMACION`.
- Why: delegacion controlada y proteccion de estados criticos.
- Trade-off: complejidad de reglas de autorizacion.
- Mitigation: centralizar matriz en servicio de autorizacion de transiciones.

### Decision 3: Trazabilidad separada de estado y visibilidad

- Decision: usar tablas/auditorias distintas (`ProjectStatusTransition`, `ProjectVisibilityTransition`).
- Why: cumplimiento FR-019/FR-020 y consultas de auditoria mas precisas.
- Trade-off: mayor volumen de registros.
- Mitigation: indices por `project_id` y `changed_at`.

### Decision 4: Segregacion por esquemas

- Decision: aplicar distribucion de entidades por `param`, `iam`, `dbo`, `audit`, `log`.
- Why: claridad de dominio y mejor gobernanza de datos (FR-048).
- Trade-off: mapeo EF Core y migraciones mas cuidadosas.
- Mitigation: esquema explicito por entidad y orden de migraciones por dependencia.

### Decision 5: Direccionalidad de FK entre esquemas

- Decision: permitir solo `dbo` -> (`param`, `iam`) y `audit` -> (`dbo`, `iam`); prohibir resto (FR-049).
- Why: evita acoplamientos y dependencias inversas no deseadas.
- Trade-off: menor flexibilidad para relaciones ad-hoc.
- Mitigation: modelar referencias tecnicas en `log` por identificadores sin FK.

### Decision 6: Cadena unica de migraciones con convencion estricta

- Decision: una sola cadena EF Core en orden `param` -> `iam` -> `dbo` -> `audit` -> `log`, con nombres `YYYYMMDDHHMM_<scope>_<change>` y convenciones de constraints/indices (FR-050).
- Why: reproducibilidad y menor riesgo de drift entre ambientes.
- Trade-off: mayor disciplina de equipo para nombrado y secuencia.
- Mitigation: checklist de revision tecnica de migraciones.

### Decision 7: Paginacion estandarizada

- Decision: validar `pageSize` globalmente a {10, 50, 100}.
- Why: consistencia funcional y control de carga.
- Trade-off: rechazos tempranos para clientes no alineados.
- Mitigation: contrato de error uniforme y guia de quickstart.

## Phase 0 Output (Research)

Archivo generado:
- `specs/002-gestion-companias-proyectos/research.md`

Resultado:
- Todas las dudas tecnicas del contexto quedaron resueltas sin `NEEDS CLARIFICATION` abiertas.
- Se documentaron decisiones, racionales y alternativas para estado, permisos, esquemas, FK, migraciones y pruebas.

## Phase 1 Output (Design and Contracts)

Archivos generados:
- `specs/002-gestion-companias-proyectos/data-model.md`
- `specs/002-gestion-companias-proyectos/contracts/companies-projects-api.md`
- `specs/002-gestion-companias-proyectos/quickstart.md`

Resultado:
- Modelo de datos con entidades, relaciones, reglas de estado y gobierno de esquemas.
- Contratos API completos para consumo frontend/backend testing.
- Guia de validacion E2E y de cumplimiento de FR-048/049/050.

## Testing Strategy (Feature)

Cobertura black-box planificada:
- Autenticacion y autorizacion por `permission_key`.
- Aislamiento por `CompanyId`.
- Ciclo de vida de companias y proyectos.
- Matriz de transiciones de estado por rol.
- Trazabilidad separada estado/visibilidad.
- Listados y validacion de `pageSize`.
- Restricciones de visualizacion para `INHABILITADO`.
- Cumplimiento de contratos de error fail-closed.

## Data Schema Change Control

- **Driver Requirements**: FR-048, FR-049, FR-050 + FR funcionales de companias/proyectos asociados.
- **Planned structural impacts**:
  1. Asignacion explicita de esquemas por entidad.
  2. Incorporacion/ajuste de transiciones en `audit`.
  3. Restricciones y convenciones de FK/PK/IX/UQ/CK.
- **Migration strategy**:
  1. Generar migraciones en secuencia dependiente.
  2. Aplicar naming convention obligatoria.
  3. Verificar ausencia de FK prohibidas.
- **Rollback strategy**:
  1. Revertir ultima migracion del scope afectado.
  2. Reaplicar baseline previo.
  3. Revalidar contratos y escenarios criticos.
- **Authorization evidence required**: cambios estructurales solo con autorizacion explicita registrada del usuario responsable.

### Technical Appendix: Data Domain Segregation

Distribucion tecnica de entidades por dominio de datos:

- `param`: `DocumentType`, `Country`, `Department`, `Municipality`, `BaseCurrency`
- `iam`: `UserAccount`, `Role`, `Permission`, `RolePermission`, `UserSignature`
- `dbo`: `Company`, `CompanyUserMembership`, `Project`, `ProjectUserMembership`
- `audit`: `ProjectStatusTransition`, `ProjectVisibilityTransition`
- `log`: tablas de logs tecnicos y operacionales

### Technical Appendix: FK Direction Policy

Regla direccional de integridad entre dominios:

- Permitidas:
  - `dbo` -> (`param`, `iam`)
  - `audit` -> (`dbo`, `iam`)
- Prohibidas:
  - referencias inversas desde `param` o `iam` hacia `dbo`, `audit` o `log`
  - referencias hacia `log` desde otros dominios
  - FK de `log` hacia tablas de negocio

### Technical Appendix: Migration Sequence and Naming

Cadena unica de migraciones:

- Orden de ejecucion: `param` -> `iam` -> `dbo` -> `audit` -> `log`
- Convencion de constraints/indices:
  - `PK_<schema>_<table>`
  - `FK_<schema>_<table>_<ref_table>`
  - `IX_<schema>_<table>_<columns>`
  - `UQ_<schema>_<table>_<columns>`
  - `CK_<schema>_<table>_<rule>`
- Convencion de nombre de migracion:
  - `YYYYMMDDHHMM_<scope>_<change>`

## Traceability Matrix

| Requirement Group | Contracts/Endpoints | Validation Evidence |
|-------------------|---------------------|---------------------|
| FR-003..FR-011, FR-042 | `/api/v1/companies*` | `Features/Companies/CompanyLifecycleContractTests.cs`, `Features/Companies/CompanyInactiveAccessTests.cs` |
| FR-012..FR-022, FR-044..FR-047 | `/api/v1/projects*`, `/api/v1/projects/{id}/status*` | `Features/Projects/ProjectLifecycleContractTests.cs`, `Features/Projects/ProjectStatusAuthorizationTests.cs`, `Features/Projects/ProjectStatusReactivationTests.cs` |
| FR-018..FR-020 | `/api/v1/projects/{id}/status`, `/api/v1/projects/{id}/visibility` | `Features/Projects/ProjectLifecycleContractTests.cs` (transiciones) + `Features/Projects/ProjectStatusReactivationTests.cs` |
| FR-023..FR-030 | `/api/v1/companies/{companyId}/memberships*`, `/api/v1/projects/{projectId}/memberships*` | `Features/Memberships/CompanyMembershipAuthorizationTests.cs`, `Features/Memberships/ProjectMembershipAuthorizationTests.cs`, `Features/Memberships/UserCompanyConstraintTests.cs` |
| FR-034..FR-035 | `/api/v1/catalogs/*` | `Features/Listings/CatalogListingsContractTests.cs` |
| FR-036..FR-041, FR-043 | `/api/v1/users`, `/api/v1/companies/{companyId}/users`, `/api/v1/users/company/me*`, `/api/v1/projects` | `Features/Listings/RoleScopedListingsContractTests.cs`, `Features/Listings/PaginationValidationTests.cs` |
| FR-048..FR-050 | migraciones, mapeos EF Core y reglas de FK | `Features/SchemaGovernance/SchemaAllocationSmokeTests.cs`, `Features/SchemaGovernance/ForeignKeyDirectionRulesTests.cs`, `Features/SchemaGovernance/MigrationNamingConventionTests.cs` |
| SC-001..SC-013 | endpoints privados del feature | `backend/tests/BudgetCivil.Backend.BlackBoxTests/TestResults/feature-002-results.md` |

## Post-Phase 1 Constitution Check

Status: PASS

Evidence:
- `research.md` contiene decisiones y trade-offs con alternativas.
- `data-model.md` explicita esquema, FK y migraciones conforme FR-048/049/050.
- `contracts/companies-projects-api.md` documenta contratos, permisos, validaciones y errores.
- `quickstart.md` define escenarios de validacion funcional y de gobernanza de esquema.

## Complexity Tracking

Sin violaciones constitucionales en esta fase.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
