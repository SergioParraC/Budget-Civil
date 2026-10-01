# Tasks: Gestion de companias y proyectos

**Input**: Design documents from specs/002-gestion-companias-proyectos/

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/companies-projects-api.md, quickstart.md

**Tests**: Se incluyen tareas de pruebas black-box antes de implementacion por historia, de acuerdo con spec y constitucion del proyecto.

**Organization**: Las tareas se agrupan por historia de usuario para permitir implementacion y validacion independiente.

## Format: [ID] [P?] [Story] Description

- [P]: tarea paralelizable (archivos distintos, sin dependencia directa)
- [Story]: etiqueta de historia de usuario (US1, US2, US3, US4)
- Cada tarea incluye rutas de archivo y trazabilidad a FR/SC/AC

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar estructura de modulos y baseline tecnico para el feature 002.

- [X] T001 Crear estructura de carpetas del feature en backend/src/BudgetCivil.Domain/Projects, backend/src/BudgetCivil.Application/Projects y backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/Projects (Trace: FR-012..FR-022)
- [X] T002 Crear estructura de membresias en backend/src/BudgetCivil.Domain/Memberships, backend/src/BudgetCivil.Application/Memberships y backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/Memberships (Trace: FR-023..FR-030)
- [X] T003 [P] Crear estructura de listados/catalogos en backend/src/BudgetCivil.Application/Catalogs y backend/src/BudgetCivil.Application/Listings (Trace: FR-034..FR-043)
- [X] T004 [P] Registrar baseline de permission keys del feature en backend/src/BudgetCivil.Api/Auth/AuthorizationPolicies.cs (Trace: FR-001, FR-002)
- [X] T005 Crear base de fixtures compartidas para black-box en backend/tests/BudgetCivil.Backend.BlackBoxTests/Infrastructure/Feature002TestData.cs (Trace: SC-004)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura bloqueante que debe quedar lista antes de cualquier historia.

**CRITICAL**: Ninguna tarea de US1-US4 debe iniciar antes de completar esta fase.

- [X] T006 Implementar enums de estado y tipos de transicion en backend/src/BudgetCivil.Domain/Projects/ProjectStatus.cs y backend/src/BudgetCivil.Domain/Projects/ProjectTransitionKind.cs (Trace: FR-018, FR-044..FR-047)
- [X] T007 [P] Implementar DTO y validador comun de paginacion en backend/src/BudgetCivil.Application/Common/Pagination/PaginationQuery.cs y backend/src/BudgetCivil.Application/Common/Pagination/PaginationValidator.cs (Trace: FR-043)
- [X] T008 [P] Extender politicas de autorizacion por permission key en backend/src/BudgetCivil.Api/Auth/AuthorizationPolicyExtensions.cs y backend/src/BudgetCivil.Api/Auth/PermissionRequirement.cs (Trace: FR-001, FR-002)
- [X] T009 Configurar mapeo explicito de esquemas param/iam/dbo/audit/log en backend/src/BudgetCivil.Infrastructure/Persistence/BudgetCivilDbContext.cs (Trace: FR-048)
- [X] T010 Implementar configuraciones de constraints con convencion PK/FK/IX/UQ/CK en backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/SchemaNamingConventions.cs (Trace: FR-050)
- [X] T011 Implementar guardas de direccion de FK entre esquemas en backend/src/BudgetCivil.Infrastructure/Persistence/SchemaRules/ForeignKeyDirectionValidator.cs (Trace: FR-049)
- [X] T012 Crear migracion baseline con formato requerido en backend/src/BudgetCivil.Infrastructure/Persistence/Migrations/202610010900_feature002_schema_baseline.cs (Trace: FR-050)
- [X] T013 [P] Implementar repositorios base de proyecto/companias/membresias/transiciones en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/BaseFeature002Repository.cs (Trace: FR-003..FR-030)
- [X] T014 [P] Implementar normalizacion de codigos de error del feature en backend/src/BudgetCivil.Api/Middleware/ExceptionHandlingMiddleware.cs (Trace: SC-004, SC-006)
- [X] T015 Actualizar harness de autenticacion por rol y permiso en backend/tests/BudgetCivil.Backend.BlackBoxTests/Infrastructure/Authentication/TestAuthHandler.cs (Trace: FR-001, FR-002)

**Checkpoint**: Fundacion lista; US1-US4 pueden iniciar segun dependencias.

---

## Phase 3: User Story 1 - Gestion del ciclo de vida de companias (Priority: P1)

**Goal**: Permitir crear, editar, inhabilitar y rehabilitar companias con trazabilidad y restricciones de acceso.

**Independent Test**: Crear una compania, editarla, inhabilitarla y rehabilitarla validando bloqueo de acceso y conservacion historica.

### Tests for User Story 1

- [X] T016 [P] [US1] Crear pruebas de contrato para POST/PUT/PATCH/GET de companias en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/CompanyLifecycleContractTests.cs (Trace: FR-003..FR-011, FR-042; AC-US1-1..5)
- [X] T017 [P] [US1] Crear prueba black-box de observacion por compania inhabilitada en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/CompanyInactiveAccessTests.cs (Trace: FR-006, FR-007; AC-US1-4)

### Implementation for User Story 1

- [X] T018 [US1] Implementar reglas de dominio de compania y representante legal en backend/src/BudgetCivil.Domain/Companies/Company.cs (Trace: FR-003, FR-010, FR-011)
- [X] T019 [P] [US1] Implementar CreateCompanyCommand y validador en backend/src/BudgetCivil.Application/Companies/CreateCompany/CreateCompanyCommandHandler.cs y backend/src/BudgetCivil.Application/Companies/CreateCompany/CreateCompanyValidator.cs (Trace: FR-003)
- [X] T020 [P] [US1] Implementar UpdateCompanyCommand y validador en backend/src/BudgetCivil.Application/Companies/UpdateCompany/UpdateCompanyCommandHandler.cs y backend/src/BudgetCivil.Application/Companies/UpdateCompany/UpdateCompanyValidator.cs (Trace: FR-004)
- [X] T021 [US1] Implementar Disable/Enable Company handlers en backend/src/BudgetCivil.Application/Companies/ChangeCompanyStatus/DisableCompanyCommandHandler.cs y backend/src/BudgetCivil.Application/Companies/ChangeCompanyStatus/EnableCompanyCommandHandler.cs (Trace: FR-005..FR-009)
- [X] T022 [US1] Implementar persistencia de companias en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/CompanyRepository.cs (Trace: FR-003..FR-009)
- [X] T023 [US1] Exponer endpoints de ciclo de vida de companias en backend/src/BudgetCivil.Api/Controllers/CompaniesController.cs (Trace: FR-003..FR-009, FR-042)
- [X] T024 [US1] Aplicar bloqueo y mensaje funcional de compania inhabilitada en backend/src/BudgetCivil.Api/Middleware/CompanyContextMiddleware.cs (Trace: FR-006, FR-007)
- [X] T025 [US1] Ajustar DTOs y hacer pasar pruebas US1 en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/CompanyLifecycleContractTests.cs (Trace: SC-002, SC-008)

**Checkpoint**: US1 funcional y validable de forma independiente.

---

## Phase 4: User Story 2 - Gestion del ciclo de vida de proyectos y estados (Priority: P1)

**Goal**: Administrar proyectos con flujo estricto de estados, reglas de firma y visibilidad controlada.

**Independent Test**: Crear y editar proyecto, ejecutar transiciones validas/invalidas y comprobar restricciones de visualizacion para INHABILITADO.

### Tests for User Story 2

- [X] T026 [P] [US2] Crear pruebas de contrato para endpoints de proyectos y estado en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Projects/ProjectLifecycleContractTests.cs (Trace: FR-012..FR-022, FR-044..FR-047; AC-US2-1..8)
- [X] T027 [P] [US2] Crear pruebas de autorizacion por rol para transiciones en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Projects/ProjectStatusAuthorizationTests.cs (Trace: FR-013..FR-018, FR-044..FR-047)
- [X] T028 [P] [US2] Crear pruebas de reactivacion/reapertura y precondiciones de firma en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Projects/ProjectStatusReactivationTests.cs (Trace: FR-015..FR-017, FR-044, FR-046)

### Implementation for User Story 2

- [X] T029 [US2] Implementar politica de transiciones de estado en backend/src/BudgetCivil.Domain/Projects/ProjectStatusTransitionPolicy.cs (Trace: FR-045..FR-047)
- [X] T030 [US2] Implementar invariantes del agregado Project en backend/src/BudgetCivil.Domain/Projects/Project.cs (Trace: FR-012..FR-017, FR-021, FR-022)
- [X] T031 [P] [US2] Implementar create/update project commands en backend/src/BudgetCivil.Application/Projects/Commands/CreateProject/CreateProjectCommandHandler.cs y backend/src/BudgetCivil.Application/Projects/Commands/UpdateProject/UpdateProjectCommandHandler.cs (Trace: FR-012, FR-021, FR-022)
- [X] T032 [US2] Implementar cambio de estado y reactivacion en backend/src/BudgetCivil.Application/Projects/Commands/ChangeProjectStatus/ChangeProjectStatusCommandHandler.cs (Trace: FR-013..FR-020, FR-044..FR-047)
- [X] T033 [P] [US2] Implementar cambio de visibilidad de proyecto en backend/src/BudgetCivil.Application/Projects/Commands/ChangeProjectVisibility/ChangeProjectVisibilityCommandHandler.cs (Trace: FR-020)
- [X] T034 [US2] Implementar persistencia de transiciones en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/ProjectTransitionRepository.cs y backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/Audit/ProjectStatusTransitionConfiguration.cs (Trace: FR-019, FR-020)
- [X] T035 [US2] Implementar reglas de director activo y firma en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/ProjectRepository.cs (Trace: FR-015, FR-021, FR-022)
- [X] T036 [US2] Exponer endpoints de ciclo de vida de proyectos en backend/src/BudgetCivil.Api/Controllers/ProjectsController.cs (Trace: FR-012..FR-022, FR-044..FR-047)
- [X] T037 [US2] Hacer pasar pruebas US2 en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Projects/ProjectLifecycleContractTests.cs (Trace: SC-003, SC-005, SC-009..SC-013)

**Checkpoint**: US2 funcional y validable de forma independiente.

---

## Phase 5: User Story 3 - Asignaciones y permisos por rol en companias y proyectos (Priority: P1)

**Goal**: Aplicar matriz estricta de permisos para vinculaciones y administracion de acceso.

**Independent Test**: Ejecutar matriz de autorizacion por rol sobre asignaciones en compania/proyecto y verificar denegaciones esperadas.

### Tests for User Story 3

- [X] T038 [P] [US3] Crear pruebas de autorizacion para memberships de compania en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Memberships/CompanyMembershipAuthorizationTests.cs (Trace: FR-023..FR-026; AC-US3-1..4)
- [X] T039 [P] [US3] Crear pruebas de autorizacion para memberships de proyecto en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Memberships/ProjectMembershipAuthorizationTests.cs (Trace: FR-027..FR-030; AC-US3-5..6)
- [X] T040 [P] [US3] Crear pruebas de una sola compania activa y exclusion de administrador en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Memberships/UserCompanyConstraintTests.cs (Trace: FR-023, FR-024)

### Implementation for User Story 3

- [X] T041 [US3] Implementar politicas de dominio de memberships en backend/src/BudgetCivil.Domain/Memberships/CompanyMembershipPolicy.cs y backend/src/BudgetCivil.Domain/Memberships/ProjectMembershipPolicy.cs (Trace: FR-023..FR-030)
- [X] T042 [US3] Implementar assign/deactivate de compania en backend/src/BudgetCivil.Application/Memberships/Companies/AssignCompanyMemberCommandHandler.cs y backend/src/BudgetCivil.Application/Memberships/Companies/DeactivateCompanyMemberCommandHandler.cs (Trace: FR-023..FR-026)
- [X] T043 [US3] Implementar assign/deactivate de proyecto en backend/src/BudgetCivil.Application/Memberships/Projects/AssignProjectMemberCommandHandler.cs y backend/src/BudgetCivil.Application/Memberships/Projects/DeactivateProjectMemberCommandHandler.cs (Trace: FR-027..FR-030)
- [X] T044 [US3] Implementar repositorios y constraints de memberships en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/CompanyMembershipRepository.cs y backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/ProjectMembershipRepository.cs (Trace: FR-023..FR-030)
- [X] T045 [US3] Exponer endpoints de memberships de compania en backend/src/BudgetCivil.Api/Controllers/CompaniesController.cs (Trace: FR-025, FR-026)
- [X] T046 [US3] Exponer endpoints de memberships de proyecto en backend/src/BudgetCivil.Api/Controllers/ProjectsController.cs (Trace: FR-027..FR-030)
- [X] T047 [US3] Hacer pasar pruebas US3 en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Memberships/ProjectMembershipAuthorizationTests.cs (Trace: SC-004, SC-007)

**Checkpoint**: US3 funcional y validable de forma independiente.

---

## Phase 6: User Story 4 - Listados reutilizables para operacion y administracion (Priority: P2)

**Goal**: Exponer listados filtrables/paginados de catalogos, usuarios, companias y proyectos con visibilidad por rol.

**Independent Test**: Validar listados por rol/alcance y rechazo de tamanos de pagina fuera de 10, 50 o 100.

### Tests for User Story 4

- [X] T048 [P] [US4] Crear pruebas de contrato para listados de catalogos en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Listings/CatalogListingsContractTests.cs (Trace: FR-034, FR-035; AC-US4-1)
- [X] T049 [P] [US4] Crear pruebas de listados por alcance de rol en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Listings/RoleScopedListingsContractTests.cs (Trace: FR-036..FR-042; AC-US4-2..7)
- [X] T050 [P] [US4] Crear pruebas de validacion de pageSize en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Listings/PaginationValidationTests.cs (Trace: FR-043; AC-US4-8)

### Implementation for User Story 4

- [X] T051 [US4] Aplicar paginacion comun a queries de listados en backend/src/BudgetCivil.Application/Listings/Queries/ListingQueryPipeline.cs (Trace: FR-043)
- [X] T052 [US4] Implementar queries de catalogos en backend/src/BudgetCivil.Application/Catalogs/Queries/GetCatalogItemsQueryHandler.cs (Trace: FR-034, FR-035)
- [X] T053 [US4] Implementar queries de usuarios/companias/proyectos por alcance en backend/src/BudgetCivil.Application/Listings/Queries/GetScopedUsersQueryHandler.cs y backend/src/BudgetCivil.Application/Listings/Queries/GetScopedProjectsQueryHandler.cs (Trace: FR-036..FR-042)
- [X] T054 [US4] Implementar repositorio de listados e indices de filtro en backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/ListingRepository.cs y backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/Indexes/ListingIndexesConfiguration.cs (Trace: FR-036..FR-043)
- [X] T055 [US4] Exponer endpoints de listados en backend/src/BudgetCivil.Api/Controllers/CatalogsController.cs, backend/src/BudgetCivil.Api/Controllers/UsersController.cs, backend/src/BudgetCivil.Api/Controllers/CompaniesController.cs y backend/src/BudgetCivil.Api/Controllers/ProjectsController.cs (Trace: FR-034..FR-043)
- [X] T056 [US4] Hacer pasar pruebas US4 en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Listings/RoleScopedListingsContractTests.cs (Trace: SC-006)

**Checkpoint**: US4 funcional y validable de forma independiente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cierre transversal, evidencia de gobernanza y validacion final de contrato/arquitectura.

- [X] T057 [P] Agregar smoke tests de asignacion por esquema en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/SchemaGovernance/SchemaAllocationSmokeTests.cs (Trace: FR-048)
- [X] T058 [P] Agregar tests de direccion de FK entre esquemas en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/SchemaGovernance/ForeignKeyDirectionRulesTests.cs (Trace: FR-049)
- [X] T059 [P] Agregar test de nomenclatura y orden de migraciones en backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/SchemaGovernance/MigrationNamingConventionTests.cs (Trace: FR-050)
- [X] T060 Actualizar guia de validacion final en specs/002-gestion-companias-proyectos/quickstart.md con rutas reales de pruebas (Trace: SC-004..SC-013)
- [X] T061 Ejecutar suite black-box completa y guardar evidencia en backend/tests/BudgetCivil.Backend.BlackBoxTests/TestResults/feature-002-results.md (Trace: SC-001..SC-013)
- [X] T062 Actualizar matriz final de trazabilidad en specs/002-gestion-companias-proyectos/plan.md y checklist de cierre en specs/002-gestion-companias-proyectos/tasks.md (Trace: Articulo 10 constitucion)

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup): inicia de inmediato.
- Phase 2 (Foundational): depende de Phase 1 y bloquea US1-US4.
- Phase 3 (US1): depende de Phase 2.
- Phase 4 (US2): depende de Phase 2.
- Phase 5 (US3): depende de Phase 2 y de endpoints base de US1/US2.
- Phase 6 (US4): depende de Phase 2 y de datos/alcances definidos en US1-US3.
- Phase 7 (Polish): depende de US1-US4.

### User Story Dependency Graph

- US1 -> US3
- US2 -> US3
- US1 -> US4
- US2 -> US4
- US3 -> US4

### Within Each User Story

- Pruebas primero (deben fallar antes de implementar).
- Reglas de dominio antes de handlers de aplicacion.
- Aplicacion antes de persistencia.
- Persistencia antes de controladores.
- Cierre de historia solo con pruebas de historia en verde.

---

## Parallel Opportunities

- Setup paralelo: T003 y T004.
- Foundational paralelo: T007, T008, T013 y T014.
- US1 paralelo: T016, T017, T019 y T020.
- US2 paralelo: T026, T027, T028, T031 y T033.
- US3 paralelo: T038, T039 y T040.
- US4 paralelo: T048, T049 y T050.
- Polish paralelo: T057, T058 y T059.

## Parallel Example: User Story 1

- Ejecutar en paralelo T016 y T017 (pruebas).
- Ejecutar en paralelo T019 y T020 (comandos create/update).
- Continuar secuencial con T021 -> T022 -> T023 -> T024 -> T025.

## Parallel Example: User Story 2

- Ejecutar en paralelo T026, T027 y T028 (pruebas).
- Ejecutar en paralelo T031 y T033.
- Continuar secuencial con T029 -> T030 -> T032 -> T034 -> T035 -> T036 -> T037.

## Parallel Example: User Story 3

- Ejecutar en paralelo T038, T039 y T040.
- Continuar secuencial con T041 -> T042 -> T043 -> T044 -> T045 -> T046 -> T047.

## Parallel Example: User Story 4

- Ejecutar en paralelo T048, T049 y T050.
- Continuar secuencial con T051 -> T052 -> T053 -> T054 -> T055 -> T056.

---

## Implementation Strategy

### MVP First (US1 only)

1. Completar Phase 1 y Phase 2.
2. Completar Phase 3 (US1).
3. Validar checkpoint de US1 y escenarios de companias.
4. Demostrar valor temprano con ciclo de vida de companias.

### Incremental Delivery

1. Foundation completa (Phase 1-2).
2. Entregar US1 (companias).
3. Entregar US2 (proyectos/estados).
4. Entregar US3 (asignaciones/permiso).
5. Entregar US4 (listados reutilizables).
6. Cerrar con Phase 7 (gobernanza de esquema y evidencia final).

### Team Parallel Strategy

1. Equipo completo en Phase 1-2.
2. Luego paralelizar por frente:
   - Dev A: US1
   - Dev B: US2
   - Dev C: US3 (al habilitarse dependencias)
   - Dev D: US4 (catalogos primero, resto tras US1-US3)
3. Consolidar en Phase 7.

---

## Notes

- Tareas [P] se disenan para no pisar el mismo archivo en paralelo.
- Cada tarea referencia FR/SC/AC para trazabilidad bidireccional.
- Si aparece cambio estructural adicional no planificado, registrar autorizacion explicita antes de implementar.
