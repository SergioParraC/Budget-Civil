# Tasks: Sistema de Usuarios y Panel de Administracion de Usuarios/Companias

**Input**: Design documents from /specs/001-gestion-usuarios-companias/

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Black-box tests are required for this feature. Backend unit tests for APU calculations are not included because APU calculation logic is out of scope in this feature.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently.

## Format: [ID] [P?] [Story] Description

- [P] means parallelizable (different files, no blocking dependency)
- [Story] appears only in user story phases (US1..US6)
- Every task includes an explicit file path and requirement mapping (FR/BR/AC)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize repository structure for backend implementation and establish traceability artifacts.

- [X] T001 Create backend solution skeleton in backend/BudgetCivil.Backend.sln and backend/src/ plus backend/tests/ (FR-008, FR-014)
- [X] T002 Create layer projects in backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj, backend/src/BudgetCivil.Application/BudgetCivil.Application.csproj, backend/src/BudgetCivil.Domain/BudgetCivil.Domain.csproj, backend/src/BudgetCivil.Infrastructure/BudgetCivil.Infrastructure.csproj, and backend/tests/BudgetCivil.Backend.BlackBoxTests/BudgetCivil.Backend.BlackBoxTests.csproj (FR-015)
- [X] T003 [P] Add package references for auth, EF Core, validation, mapping, logging, swagger, and test stack in backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj, backend/src/BudgetCivil.Application/BudgetCivil.Application.csproj, backend/src/BudgetCivil.Infrastructure/BudgetCivil.Infrastructure.csproj, and backend/tests/BudgetCivil.Backend.BlackBoxTests/BudgetCivil.Backend.BlackBoxTests.csproj (NFR-001, NFR-003)
- [X] T004 [P] Configure single-point database connection settings in backend/src/BudgetCivil.Api/appsettings.json and backend/src/BudgetCivil.Api/appsettings.Development.json (NFR-006)
- [X] T005 [P] Prepare black-box suite folder structure in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Authentication/, backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/, backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/, backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Profile/, and backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/ (C7)
- [X] T006 Capture initial task-to-requirement traceability section in specs/001-gestion-usuarios-companias/plan.md for implementation kickoff (C6)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build core security, persistence, and cross-cutting plumbing required by all stories.

**Critical**: No user story implementation starts before this phase is complete.

- [X] T007 Implement core security and tenant entities in backend/src/BudgetCivil.Domain/Users/Entities/Role.cs, backend/src/BudgetCivil.Domain/Users/Entities/Permission.cs, backend/src/BudgetCivil.Domain/Users/Entities/RolePermission.cs, backend/src/BudgetCivil.Domain/Users/Entities/Company.cs, and backend/src/BudgetCivil.Domain/Users/Entities/UserAccount.cs (FR-015, FR-034)
- [X] T008 [P] Implement profile, membership, recovery, and audit entities in backend/src/BudgetCivil.Domain/Users/Entities/UserProfile.cs, backend/src/BudgetCivil.Domain/Users/Entities/UserCompanyMembership.cs, backend/src/BudgetCivil.Domain/Users/Entities/PasswordRecoveryGrant.cs, and backend/src/BudgetCivil.Domain/Users/Entities/SecurityAuditEvent.cs (FR-019, FR-020, FR-028)
- [X] T009 Implement EF Core DbContext and entity configurations in backend/src/BudgetCivil.Infrastructure/Persistence/BudgetCivilDbContext.cs and backend/src/BudgetCivil.Infrastructure/Persistence/Configurations/*.cs (FR-008, FR-014)
- [X] T010 Create initial migrations for users, companies, permissions, and audit schema in backend/src/BudgetCivil.Infrastructure/Persistence/Migrations/ (FR-037)
- [X] T011 Implement idempotent security baseline seeding in backend/src/BudgetCivil.Infrastructure/Security/SecurityBaselineSeeder.cs for Role, Permission, and RolePermission (FR-035, FR-037, BR-025)
- [X] T012 [P] Implement JWT token service and Argon2id password hasher adapter in backend/src/BudgetCivil.Infrastructure/Security/JwtTokenService.cs and backend/src/BudgetCivil.Infrastructure/Security/Argon2PasswordHasher.cs (FR-017, NFR-001)
- [X] T013 Implement permission_key fail-closed authorization handler in backend/src/BudgetCivil.Api/Auth/PermissionRequirement.cs and backend/src/BudgetCivil.Api/Auth/PermissionAuthorizationHandler.cs (FR-034, FR-036, BR-024)
- [X] T014 [P] Implement CompanyId request-context resolver in backend/src/BudgetCivil.Api/Middleware/CompanyContextMiddleware.cs (FR-022, BR-007)
- [X] T015 [P] Implement exception-to-problem-details mapping for 400/401/403/409/423 in backend/src/BudgetCivil.Api/Middleware/ExceptionHandlingMiddleware.cs (FR-002, FR-031)
- [X] T016 Implement security audit write service in backend/src/BudgetCivil.Application/Common/Auditing/SecurityAuditService.cs and backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/SecurityAuditRepository.cs (NFR-003)
- [X] T017 [P] Wire dependency injection and middleware pipeline in backend/src/BudgetCivil.Api/Program.cs, backend/src/BudgetCivil.Application/DependencyInjection.cs, and backend/src/BudgetCivil.Infrastructure/DependencyInjection.cs (NFR-001, NFR-002)
- [X] T018 Build reusable black-box test host and auth helpers in backend/tests/BudgetCivil.Backend.BlackBoxTests/Infrastructure/TestWebApplicationFactory.cs and backend/tests/BudgetCivil.Backend.BlackBoxTests/Infrastructure/AuthenticatedClientBuilder.cs (C7)

**Checkpoint**: Foundation complete. User stories can proceed.

---

## Phase 3: User Story 1 - Ingreso Seguro y Estado de Cuenta (Priority: P1)

**Goal**: Deliver self-registration, login, lockout behavior, and role-scoped read-only access to existing public APU capabilities.

**Independent Test**: Validate self-registration defaults, login by email/username, lockout after 5 failures, required disabled-account message, and operational-role-only public APU read access.

### Tests for User Story 1 (write first)

- [X] T019 [P] [US1] Add login contract tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Authentication/LoginContractTests.cs (FR-001, FR-002, AC-US1-1, AC-US1-2, AC-US1-3)
- [X] T020 [P] [US1] Add register contract tests for defaults and forbidden fields in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Authentication/RegisterContractTests.cs (FR-038, FR-039, AC-US1-5)
- [X] T021 [P] [US1] Add lockout flow tests for 5 failed attempts in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Authentication/LockoutFlowTests.cs (FR-031, AC-US1-4)
- [X] T022 [P] [US1] Add role matrix tests for read-only public APU access and admin denial in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/PublicApuAccessTests.cs (FR-025, BR-006, AC-US1-6)

### Implementation for User Story 1

- [X] T023 [P] [US1] Create auth DTOs in backend/src/BudgetCivil.Application/Users/Auth/Dtos/RegisterUserRequest.cs, backend/src/BudgetCivil.Application/Users/Auth/Dtos/RegisterUserResponse.cs, backend/src/BudgetCivil.Application/Users/Auth/Dtos/LoginRequest.cs, and backend/src/BudgetCivil.Application/Users/Auth/Dtos/LoginResponse.cs (FR-001, FR-008)
- [X] T024 [P] [US1] Implement auth validators in backend/src/BudgetCivil.Application/Users/Auth/Validators/RegisterUserRequestValidator.cs and backend/src/BudgetCivil.Application/Users/Auth/Validators/LoginRequestValidator.cs (FR-018, FR-039)
- [X] T025 [US1] Implement self-registration use case in backend/src/BudgetCivil.Application/Users/Auth/UseCases/RegisterUserUseCase.cs forcing Presupuestador and company_id null defaults (FR-008, FR-038, BR-021)
- [X] T026 [US1] Implement login use case with failed-attempt counter and lockout message in backend/src/BudgetCivil.Application/Users/Auth/UseCases/LoginUseCase.cs (FR-002, FR-031, BR-010)
- [X] T027 [US1] Implement auth repository persistence in backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/AuthRepository.cs for credential lookup and lockout state updates (FR-001, FR-031)
- [X] T028 [US1] Expose POST /api/v1/auth/register and POST /api/v1/auth/login in backend/src/BudgetCivil.Api/Controllers/AuthController.cs (FR-008, FR-040)
- [X] T029 [US1] Emit AUTH_LOGIN and AUTH_LOCKOUT security events in backend/src/BudgetCivil.Application/Users/Auth/UseCases/LoginUseCase.cs via backend/src/BudgetCivil.Application/Common/Auditing/SecurityAuditService.cs (NFR-003)

---

## Phase 4: User Story 2 - Bootstrap del Primer Administrador (Priority: P1)

**Goal**: Deliver one-time first-administrator bootstrap with idempotency and admin-only audit visibility.

**Independent Test**: Execute bootstrap before and after first admin exists, then verify audit endpoint visibility is restricted to Administrador.

### Tests for User Story 2 (write first)

- [X] T030 [P] [US2] Add bootstrap idempotency tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/BootstrapFirstAdministratorTests.cs (FR-003, FR-004, FR-005, AC-US2-1, AC-US2-2)
- [X] T031 [P] [US2] Add bootstrap audit visibility tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/BootstrapAuditVisibilityTests.cs (FR-006, FR-007, AC-US2-3)

### Implementation for User Story 2

- [X] T032 [P] [US2] Create bootstrap DTOs and validators in backend/src/BudgetCivil.Application/Users/Bootstrap/Dtos/BootstrapRequest.cs, backend/src/BudgetCivil.Application/Users/Bootstrap/Dtos/BootstrapResponse.cs, and backend/src/BudgetCivil.Application/Users/Bootstrap/Validators/BootstrapRequestValidator.cs (FR-004, FR-005)
- [X] T033 [US2] Implement first-admin bootstrap use case in backend/src/BudgetCivil.Application/Users/Bootstrap/UseCases/BootstrapFirstAdministratorUseCase.cs with no-admin precondition and idempotent conflict behavior (FR-003, FR-004, BR-011, BR-012)
- [X] T034 [US2] Implement bootstrap audit query use case in backend/src/BudgetCivil.Application/Users/Bootstrap/UseCases/GetBootstrapAuditUseCase.cs (FR-006, FR-007)
- [X] T035 [US2] Implement bootstrap repository in backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/BootstrapRepository.cs for first-admin detection and transaction-safe creation (FR-003, FR-005)
- [X] T036 [US2] Expose bootstrap and audit endpoints in backend/src/BudgetCivil.Api/Controllers/AdminBootstrapController.cs and backend/src/BudgetCivil.Api/Controllers/AuditController.cs (FR-004, FR-007)

---

## Phase 5: User Story 3 - Administracion de Usuarios por Rol y Compania (Priority: P1)

**Goal**: Deliver administrative user management with role restrictions, soft deactivation, and company membership constraints.

**Independent Test**: Verify create, edit, deactivate, role change, and company assignment flows, including admin-only creation and one-active-company constraints.

### Tests for User Story 3 (write first)

- [X] T037 [P] [US3] Add admin-only create user authorization tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/AdminCreateUserAuthorizationTests.cs (FR-008, FR-033, BR-020, AC-US3-5)
- [X] T038 [P] [US3] Add user administration contract tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/UserAdministrationContractTests.cs for update, deactivate, role, and company changes (FR-009, FR-010, FR-011, FR-012, AC-US3-1, AC-US3-2, AC-US3-3)
- [X] T039 [P] [US3] Add active-membership conflict tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/UserCompanyMembershipRulesTests.cs (FR-013, FR-028, BR-008, BR-009, AC-US3-4)

### Implementation for User Story 3

- [X] T040 [P] [US3] Create user administration DTOs in backend/src/BudgetCivil.Application/Users/Administration/Dtos/CreateUserRequest.cs, backend/src/BudgetCivil.Application/Users/Administration/Dtos/UpdateUserRequest.cs, backend/src/BudgetCivil.Application/Users/Administration/Dtos/ChangeUserRoleRequest.cs, backend/src/BudgetCivil.Application/Users/Administration/Dtos/ChangeUserCompanyRequest.cs, and backend/src/BudgetCivil.Application/Users/Administration/Dtos/DeactivateUserRequest.cs (FR-009, FR-012)
- [X] T041 [P] [US3] Implement user administration validators in backend/src/BudgetCivil.Application/Users/Administration/Validators/*.cs (FR-018, FR-028)
- [X] T042 [US3] Implement create, update, and deactivate use cases in backend/src/BudgetCivil.Application/Users/Administration/UseCases/CreateUserUseCase.cs, backend/src/BudgetCivil.Application/Users/Administration/UseCases/UpdateUserUseCase.cs, and backend/src/BudgetCivil.Application/Users/Administration/UseCases/DeactivateUserUseCase.cs (FR-008, FR-009, FR-010, BR-001)
- [X] T043 [US3] Implement role and company assignment use cases in backend/src/BudgetCivil.Application/Users/Administration/UseCases/ChangeUserRoleUseCase.cs and backend/src/BudgetCivil.Application/Users/Administration/UseCases/ChangeUserCompanyUseCase.cs with manager elevation constraints (FR-011, FR-012, FR-013, BR-004)
- [X] T044 [US3] Implement user administration persistence and membership history in backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/UserAdministrationRepository.cs (FR-028, NFR-003)
- [X] T045 [US3] Expose administrative users endpoints in backend/src/BudgetCivil.Api/Controllers/UsersController.cs for POST /users, PUT /users/{userId}, PATCH /users/{userId}/deactivate, PATCH /users/{userId}/role, and PATCH /users/{userId}/company (FR-008, FR-013)
- [X] T046 [US3] Reject user-project assignment payloads in backend/src/BudgetCivil.Api/Controllers/UsersController.cs and backend/src/BudgetCivil.Application/Users/Administration/Guards/RejectProjectAssignmentGuard.cs (FR-024, BR-005)

---

## Phase 6: User Story 4 - Administracion de Companias por Rol (Priority: P2)

**Goal**: Deliver company create/edit/deactivate with role scope, including manager one-company-own limit.

**Independent Test**: Verify admin full company management, manager one-own-company create/edit behavior, and rejection of second company creation by manager.

### Tests for User Story 4 (write first)

- [X] T047 [P] [US4] Add company administration contract tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/CompanyAdministrationContractTests.cs (FR-014, AC-US4-1, AC-US4-4, AC-US4-5)
- [X] T048 [P] [US4] Add manager one-company limit tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Companies/ManagerOwnCompanyLimitTests.cs (FR-014, BR-003, AC-US4-2, AC-US4-3)

### Implementation for User Story 4

- [X] T049 [P] [US4] Create company DTOs and validators in backend/src/BudgetCivil.Application/Companies/Dtos/CreateCompanyRequest.cs, backend/src/BudgetCivil.Application/Companies/Dtos/UpdateCompanyRequest.cs, backend/src/BudgetCivil.Application/Companies/Dtos/DeactivateCompanyRequest.cs, and backend/src/BudgetCivil.Application/Companies/Validators/*.cs (FR-014)
- [X] T050 [US4] Implement company management use cases in backend/src/BudgetCivil.Application/Companies/UseCases/CreateCompanyUseCase.cs, backend/src/BudgetCivil.Application/Companies/UseCases/UpdateCompanyUseCase.cs, and backend/src/BudgetCivil.Application/Companies/UseCases/DeactivateCompanyUseCase.cs with manager own-company guard (FR-014, FR-023, BR-003)
- [X] T051 [US4] Implement company repository operations in backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/CompanyRepository.cs with soft deactivation semantics (BR-001)
- [X] T052 [US4] Expose companies endpoints in backend/src/BudgetCivil.Api/Controllers/CompaniesController.cs for POST /companies, PUT /companies/{companyId}, and PATCH /companies/{companyId}/deactivate (FR-014)
- [X] T053 [US4] Register company administration audit events in backend/src/BudgetCivil.Application/Companies/UseCases/*.cs through backend/src/BudgetCivil.Application/Common/Auditing/SecurityAuditService.cs (NFR-003)

---

## Phase 7: User Story 5 - Autoservicio de Cuenta y Contrasena (Priority: P2)

**Goal**: Deliver profile self-service and password operations for all roles, with admin-assisted recovery enablement.

**Independent Test**: Validate self-profile read/update limits, password policy enforcement, and admin-only password-recovery grant flow.

### Tests for User Story 5 (write first)

- [X] T054 [P] [US5] Add profile self-service tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Profile/ProfileSelfServiceTests.cs (FR-016, FR-029, AC-US5-1)
- [X] T055 [P] [US5] Add change-password policy tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Profile/ChangePasswordPolicyTests.cs (FR-017, FR-018, AC-US5-2)
- [X] T056 [P] [US5] Add admin-assisted recovery tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Profile/AdminPasswordRecoveryTests.cs (FR-019, FR-020, AC-US5-3)

### Implementation for User Story 5

- [X] T057 [P] [US5] Create profile DTOs and validators in backend/src/BudgetCivil.Application/Profile/Dtos/GetMyProfileResponse.cs, backend/src/BudgetCivil.Application/Profile/Dtos/UpdateMyProfileRequest.cs, backend/src/BudgetCivil.Application/Profile/Dtos/ChangeMyPasswordRequest.cs, and backend/src/BudgetCivil.Application/Profile/Validators/*.cs (FR-016, FR-018)
- [X] T058 [US5] Implement self profile read/update use cases in backend/src/BudgetCivil.Application/Profile/UseCases/GetMyProfileUseCase.cs and backend/src/BudgetCivil.Application/Profile/UseCases/UpdateMyProfileUseCase.cs (FR-016, FR-029)
- [X] T059 [US5] Implement change-my-password use case in backend/src/BudgetCivil.Application/Profile/UseCases/ChangeMyPasswordUseCase.cs with current-password verification and Argon2id hashing (FR-017, FR-018)
- [X] T060 [US5] Implement admin recovery enable use case in backend/src/BudgetCivil.Application/Profile/UseCases/EnableUserPasswordRecoveryUseCase.cs (FR-019, FR-020, BR-016)
- [X] T061 [US5] Expose profile and recovery endpoints in backend/src/BudgetCivil.Api/Controllers/ProfileController.cs and backend/src/BudgetCivil.Api/Controllers/UsersController.cs (FR-016, FR-020)

---

## Phase 8: User Story 6 - Busqueda de Usuarios y Privacidad por CompanyId (Priority: P3)

**Goal**: Deliver filtered user search with strict tenant isolation, auditor read scope, and fail-closed authorization behavior.

**Independent Test**: Validate search filters, cross-tenant access denial, auditor read-only scope, and fail-closed behavior when role or permission assignment is inactive.

### Tests for User Story 6 (write first)

- [X] T062 [P] [US6] Add users filter search tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/UserSearchFiltersTests.cs for email, username, name, and document (FR-027, AC-US6-1, AC-US6-2)
- [X] T063 [P] [US6] Add cross-tenant isolation tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/CompanyIsolationTests.cs (FR-022, NFR-002, AC-US6-3)
- [X] T064 [P] [US6] Add fail-closed permission tests for inactive role and inactive role-permission in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Security/FailClosedAuthorizationTests.cs (FR-034, FR-036, BR-024)
- [X] T065 [P] [US6] Add auditor read-only scope tests in backend/tests/BudgetCivil.Backend.BlackBoxTests/Features/Users/AuditorReadScopeTests.cs (FR-032, BR-019)

### Implementation for User Story 6

- [X] T066 [P] [US6] Implement user search query contract in backend/src/BudgetCivil.Application/Users/Search/UserSearchQuery.cs and backend/src/BudgetCivil.Application/Users/Search/UserSearchResult.cs (FR-027)
- [X] T067 [US6] Implement filtered search and CompanyId scoping in backend/src/BudgetCivil.Infrastructure/Persistence/Repositories/UserSearchRepository.cs (FR-022, FR-027)
- [X] T068 [US6] Implement users search endpoint behavior in backend/src/BudgetCivil.Api/Controllers/UsersController.cs for GET /api/v1/users filters and scope rules (FR-027, FR-032)
- [X] T069 [US6] Add denial-audit metadata enrichment for fail-closed decisions in backend/src/BudgetCivil.Api/Auth/PermissionAuthorizationHandler.cs and backend/src/BudgetCivil.Application/Common/Auditing/SecurityAuditService.cs (FR-036, NFR-003)

---

## Phase 9: Polish and Cross-Cutting

**Purpose**: Finalize documentation, regression validation, and compliance evidence across stories.

- [X] T070 [P] Refresh contract examples and final role notes in specs/001-gestion-usuarios-companias/contracts/users-companies-api.md (C4, C6)
- [X] T071 [P] Refresh quickstart scenarios and expected results in specs/001-gestion-usuarios-companias/quickstart.md (C7)
- [X] T072 [P] Refresh traceability matrix with final implemented task IDs and evidence links in specs/001-gestion-usuarios-companias/plan.md (C6)
- [X] T073 Execute full black-box suite and save report at backend/tests/BudgetCivil.Backend.BlackBoxTests/TestResults/feature-001-results.trx (NFR-003, NFR-005)
- [X] T074 Validate feature checklist completion in specs/001-gestion-usuarios-companias/checklists/requirements.md against constitution gates C1..C9 (C1, C2, C3, C5)
- [X] T075 Execute manual quickstart A-J and capture observed outcomes in specs/001-gestion-usuarios-companias/quickstart.md (SC-001..SC-009)
- [X] T076 Apply final security hardening pass in backend/src/BudgetCivil.Api/Program.cs and backend/src/BudgetCivil.Api/Middleware/CompanyContextMiddleware.cs for JWT/RBAC/CompanyId guardrails (NFR-001, NFR-002)

---

## Dependencies and Execution Order

### Phase Dependencies

- Phase 1 -> no dependencies.
- Phase 2 -> depends on Phase 1 and blocks all user story phases.
- Phase 3, 4, 5, 6, 7, 8 -> depend on Phase 2.
- Phase 9 -> depends on completion of all selected story phases.

### User Story Completion Order

- Preferred delivery order: US1 -> US2 -> US3 -> US4 -> US5 -> US6.
- P1 stories (US1, US2, US3) define MVP backbone for auth, bootstrap, and user admin.
- P2 stories (US4, US5) extend tenant management and self-service.
- P3 story (US6) finalizes search and privacy hardening.

### Within Each Story

- Tests first (must fail before implementation).
- DTOs and validators before use cases.
- Use cases before controllers.
- Repositories before endpoint final wiring when persistence is required.

---

## Parallel Execution Examples by Story

### US1 Parallel Example

- Run T019, T020, T021, and T022 in parallel.
- Run T023 and T024 in parallel.

### US2 Parallel Example

- Run T030 and T031 in parallel.
- Run T032 while test fixes are progressing.

### US3 Parallel Example

- Run T037, T038, and T039 in parallel.
- Run T040 and T041 in parallel.

### US4 Parallel Example

- Run T047 and T048 in parallel.
- Run T049 while contract tests are stabilizing.

### US5 Parallel Example

- Run T054, T055, and T056 in parallel.
- Run T057 while black-box test baselines are prepared.

### US6 Parallel Example

- Run T062, T063, T064, and T065 in parallel.
- Run T066 in parallel with early test authoring.

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete US1 (Phase 3).
3. Complete US2 (Phase 4).
4. Complete US3 (Phase 5).
5. Validate end-to-end before expanding to P2/P3.

### Incremental Delivery

1. Ship auth and registration baseline (US1).
2. Add bootstrap governance (US2).
3. Add user administration controls (US3).
4. Add company administration scope rules (US4).
5. Add self-service and recovery (US5).
6. Add search and privacy hardening (US6).
7. Execute polish and compliance closure (Phase 9).

### Notes

- Tasks marked [P] are safe to parallelize when team capacity allows.
- Each user story phase is independently testable using its own test tasks and independent test criteria.
- Out-of-scope items (full APU module implementation and user-project assignment features) are intentionally excluded and only validated as rejection behaviors where required.
