# Phase 0 Research: Sistema de Usuarios y Panel de Administracion de Usuarios/Companias

Date: 2026-09-22
Feature: specs/001-gestion-usuarios-companias/spec.md
Scope: Backend only

## Decision 1: Authorization data-driven for private endpoints

Decision:
- En todos los endpoints privados, la autorizacion se evalua por `permission_key` activo en BD a traves de `Role`, `Permission` y `RolePermission`.
- Las listas de roles quedan como referencia funcional/documental, no como control hardcodeado de decision.

Rationale:
- Permite escalar permisos y habilitar roles futuros sin redeploy.
- Reduce riesgo de drift entre codigo y configuracion operativa.
- Facilita auditoria de denegaciones y cobertura de seguridad.

Alternatives considered:
- Hardcode por rol en atributos/controladores: descartado por baja extensibilidad.
- Matriz solo por rol sin `permission_key`: descartado por granularidad insuficiente.

## Decision 2: Fail-closed as mandatory policy

Decision:
- Si el rol esta inactivo o falta/inactiva la asignacion del `permission_key`, la operacion privada retorna `403` y registra evento de seguridad/auditoria.

Rationale:
- El acceso se deniega por defecto ante configuracion incompleta.
- Evita permisos implicitos no auditables.
- Alineado con FR-036 y BR-024.

Alternatives considered:
- Fail-open ante ausencia de permiso: descartado por riesgo critico.
- Denegar sin auditoria: descartado por falta de trazabilidad.

## Decision 3: Security baseline seeded, idempotent and versioned

Decision:
- El baseline de `Role`, `Permission` y `RolePermission` se provisiona por seed idempotente y versionado.
- No se permite autocreacion de permisos/asignaciones en runtime.

Rationale:
- Garantiza consistencia entre ambientes.
- Soporta rollback controlado y rastreo de cambios de seguridad.
- Alineado con FR-037 y NFR-007.

Alternatives considered:
- Crear permisos dinamicamente al ejecutar endpoints: descartado por opacidad operativa.
- Mantener baseline solo en codigo sin control de version de seed: descartado por riesgo de deriva.

## Decision 4: Roles base immutable in this phase, but extensible from DB

Decision:
- En esta fase no hay CRUD de roles base mediante endpoints.
- Roles futuros se habilitan desde BD mediante catalogo `Role` y asignaciones `RolePermission`, bajo gobernanza de cambios.

Rationale:
- Controla riesgo durante el arranque del modulo de usuarios.
- Mantiene compatibilidad con crecimiento futuro sin depender de hardcode.

Alternatives considered:
- Exponer CRUD completo de roles desde el inicio: descartado por sobrealcance y riesgo de seguridad.
- Congelar roles para siempre: descartado por falta de flexibilidad evolutiva.

## Decision 5: Team test strategy for this feature

Decision:
- Se adopta un unico proyecto `BudgetCivil.Backend.BlackBoxTests` para validar contratos y comportamiento end-to-end del feature.
- No se separan `ContractTests` y `IntegrationTests` en esta fase.

Rationale:
- Decision explicita de equipo y aclaracion del spec.
- Reduce complejidad y mantenimiento de suites paralelas.
- Mejora trazabilidad por historia al validar flujo real entre capas.

Alternatives considered:
- Separar proyectos por tipo de prueba: descartado en esta fase por overhead y duplicidad.
- Solo unitarias: descartado por cobertura insuficiente de autorizacion/tenant.

## Decision 6: Four productive backend layers are mandatory

Decision:
- Mantener solo estas capas productivas: Api, Application, Domain, Infrastructure.

Rationale:
- Alinea implementacion con arquitectura general.
- Evita fuga de reglas de negocio a controladores o repositorios.

Alternatives considered:
- Incluir capa adicional para orquestacion de permisos: descartado, se resuelve en Application + Domain.
- Mezclar reglas de autorizacion en Api: descartado por alto acoplamiento.

## Decision 7: Password and lockout controls

Decision:
- Password siempre en hash Argon2id con politica minima.
- Lockout automatico al 5to intento fallido consecutivo.
- Recuperacion asistida en esta fase solo por Administrador.

Rationale:
- Cumple FR-017, FR-018, FR-019, FR-020 y FR-031.
- Equilibra seguridad y continuidad operativa.

Alternatives considered:
- Umbral de bloqueo menor o mayor configurable por tenant en esta fase: diferido para evitar complejidad temprana.
- Recuperacion publica por correo en esta fase: fuera de alcance.

## Decision 8: CompanyId privacy enforcement across all private operations

Decision:
- `CompanyId` se deriva del contexto autenticado y se valida en toda operacion privada de lectura/escritura.

Rationale:
- Evita acceso cross-tenant por manipulacion de payload.
- Alineado con NFR-002 y BR-007.

Alternatives considered:
- Confiar en `company_id` enviado por cliente: descartado por vulnerabilidad critica.

## Decision 9: Bootstrap governance

Decision:
- `POST /api/v1/admin/bootstrap/first-administrator` es idempotente y se deshabilita cuando existe al menos un Administrador.
- Toda ejecucion se audita.

Rationale:
- Evita creaciones duplicadas de administradores iniciales.
- Conserva trazabilidad de eventos de arranque.

Alternatives considered:
- Seed automatico silencioso de primer Admin: descartado por ausencia de control funcional.

## Decision 10: Single source for DB connection setting

Decision:
- La conexion a BD se mantiene en un unico punto de app config del backend, consumido por Infrastructure.

Rationale:
- Reduce drift de configuracion entre capas y ambientes.
- Simplifica operacion, despliegue y soporte.

Alternatives considered:
- Multiples puntos de configuracion por capa/servicio: descartado por inconsistencias operativas.

## Decision 11: Administrative user creation is admin-only

Decision:
- `POST /api/v1/users` se limita al rol Administrador.
- Gerente de proyectos, Presupuestador y Auditor no crean usuarios por endpoint administrativo.

Rationale:
- Alinea la aclaracion mas reciente del spec.
- Reduce riesgo de proliferacion de cuentas administrativas fuera de control central.

Alternatives considered:
- Permitir creacion administrativa a Gerente de proyectos: descartado por decision funcional del alcance actual.
- Permitir creacion administrativa a Presupuestador: descartado por principio de minimo privilegio.

## Decision 12: Company management split by role with one-company limit for manager

Decision:
- Administrador conserva creacion/edicion/desactivacion de companias.
- Gerente de proyectos puede crear exactamente una compania propia y editar solo esa compania.

Rationale:
- Habilita operacion de arranque para gerente sin abrir gestion global de tenants.
- Mantiene control de gobierno de companias en el rol Administrador.

Alternatives considered:
- Solo Administrador gestiona companias: descartado por necesidad operativa del gerente en alcance funcional aprobado.
- Gerente con capacidad de crear multiples companias: descartado por romper regla de pertenencia unica.

## Decision 13: Read-only APU/insumo access restricted to operational roles

Decision:
- En este feature, el consumo de APUs publicos/insumos existentes queda para roles operativos autorizados (Gerente de proyectos, Presupuestador, Auditor).
- El rol Administrador queda sin acceso de consulta a APUs/insumos.

Rationale:
- Alinea la matriz funcional aprobada y separa gobierno administrativo de operacion tecnica.
- Evita sobreexposicion de datos tecnicos a un rol de administracion de cuentas/companias.

Alternatives considered:
- Permitir consulta APU/insumos a Administrador por defecto: descartado por decision funcional explicita del spec.

## Risks and Mitigations

- Risk: Baseline de permisos incompleto provoca denegaciones masivas.
  - Mitigation: Validacion obligatoria de seed idempotente en quickstart y pipeline.
- Risk: Consultas de autorizacion lentas por falta de indices.
  - Mitigation: Indices en `RolePermission(role_id, permission_id, is_active)` y `Permission(permission_key, is_active)`.
- Risk: Desactivacion accidental del ultimo Administrador activo.
  - Mitigation: Regla de negocio que bloquea esa operacion con `409`.
- Risk: Drift entre contrato documentado y comportamiento real.
  - Mitigation: BlackBoxTests de contrato en el mismo proyecto unificado.

## Clarification Status

No quedan NEEDS CLARIFICATION abiertas para este feature.
