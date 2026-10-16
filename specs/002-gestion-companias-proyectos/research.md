# Phase 0 Research: Gestion de companias y proyectos

Date: 2026-09-30
Feature: specs/002-gestion-companias-proyectos/spec.md
Scope: Backend only

## Context

Este research consolida decisiones tecnicas para ejecutar el feature 002 en el backend, respetando Clean Architecture, seguridad por `permission_key` + `CompanyId`, y las aclaraciones finales de persistencia por esquemas (FR-048, FR-049, FR-050).

## Decision 1: Segregacion por esquemas en SQL Server

- Decision: adoptar asignacion fija de entidades por esquema: `param` (catalogos), `iam` (identidad/acceso), `dbo` (operacion), `audit` (trazabilidad de negocio) y `log` (logs tecnicos/operacionales).
- Rationale: separa responsabilidades de datos, facilita gobernanza de cambios y mejora trazabilidad de ownership tecnico.
- Alternatives considered: mantener todo en `dbo` con prefijos por tabla; descartado por baja claridad semantica y mayor riesgo de drift.

## Decision 2: Reglas direccionales de FK entre esquemas

- Decision: permitir solo `dbo` -> (`param`, `iam`) y `audit` -> (`dbo`, `iam`); prohibir referencias inversas desde `param` o `iam` hacia `dbo`, `audit` o `log`; `log` sin FK de negocio ni FK entrantes.
- Rationale: evita acoplamientos circulares, mantiene capas de datos limpias y protege tablas de logs de dependencias transaccionales.
- Alternatives considered: permitir FK bidireccionales por conveniencia; descartado por violar gobernanza y dificultar evolucion del modelo.

## Decision 3: Cadena unica de migraciones y convencion de nombres

- Decision: usar una sola cadena EF Core con orden `param` -> `iam` -> `dbo` -> `audit` -> `log`, esquema explicito por entidad y convencion obligatoria (`PK_`, `FK_`, `IX_`, `UQ_`, `CK_`) mas nombre de migracion `YYYYMMDDHHMM_<scope>_<change>`.
- Rationale: asegura reproducibilidad, orden de dependencias y lectura operativa uniforme en revisiones tecnicas.
- Alternatives considered: migraciones separadas por esquema o por modulo; descartado por complejidad de despliegue y rollback.

## Decision 4: Cambios de estado de proyecto solo por comando explicito

- Decision: cualquier transicion posterior a la creacion se realiza via endpoint explicito de cambio de estado; editar proyecto no cambia estado automaticamente.
- Rationale: elimina efectos colaterales, facilita auditoria y cumple FR-013/FR-045.
- Alternatives considered: transicion automatica por edicion de campos; descartado por ambiguedad operativa.

## Decision 5: Matriz de autorizacion por estado con excepciones cerradas

- Decision: `FALTA_INFORMACION` y `COTIZADO` por Director de proyectos o Presupuestador; `FINALIZADO` e `INHABILITADO` solo por Director de proyectos; salida de `INHABILITADO` por Director de proyectos o Administrador, siempre a `FALTA_INFORMACION`.
- Rationale: balancea delegacion operativa y control de acciones criticas.
- Alternatives considered: permitir a Administrador todas las transiciones; descartado por sobreprivilegio y menor segregacion de responsabilidades.

## Decision 6: Reapertura y reactivacion diferenciadas

- Decision: tratar reapertura (`FINALIZADO` -> `FALTA_INFORMACION`) y reactivacion (`INHABILITADO` -> `FALTA_INFORMACION`) como operaciones de negocio diferenciadas, con validaciones y evidencias de auditoria.
- Rationale: simplifica reglas, evita ambiguedad funcional y mejora trazabilidad de intencion del cambio.
- Alternatives considered: una sola operacion generica de cambio de estado para todos los casos; descartado por menor claridad de reglas criticas.

## Decision 7: Trazabilidad separada para estado y visibilidad

- Decision: registrar en tablas/eventos separados cambios de estado (`ProjectStatusTransition`) y cambios de visibilidad (`ProjectVisibilityTransition`).
- Rationale: cumple FR-019 y FR-020 con consultas de auditoria precisas y reportes independientes.
- Alternatives considered: bitacora unica sin tipificacion; descartado por perdida de granularidad y consultas mas costosas.

## Decision 8: Ciclo de vida de compania con desactivacion logica

- Decision: companias se crean/editan/inhabilitan/rehabilitan sin borrado fisico; rehabilitar no reactiva membresias previamente desactivadas manualmente.
- Rationale: preserva historial y consistencia referencial.
- Alternatives considered: borrado fisico o archivado duro; descartado por riesgo de perdida de trazabilidad.

## Decision 9: Precondicion de director activo para crear proyecto

- Decision: rechazar creacion de proyecto si la compania no tiene Director de proyectos activo; si crea Presupuestador, asignar automaticamente el Director activo mas antiguo como encargado.
- Rationale: asegura gobernanza minima del proyecto desde su inicio.
- Alternatives considered: permitir creacion y asignar director despues; descartado por crear proyectos huerfanos.

## Decision 10: Paginacion unificada por contrato

- Decision: todos los listados del feature usan `pageSize` restringido a {10, 50, 100}; cualquier otro valor retorna error de validacion.
- Rationale: uniformidad funcional y proteccion de rendimiento.
- Alternatives considered: limites variables por endpoint; descartado por inconsistencia y mayor costo de mantenimiento.

## Decision 11: Seguridad fail-closed y aislamiento por compania

- Decision: endpoint privado requiere JWT valido, `permission_key` activo y enforcement de `CompanyId` desde contexto autenticado; ante ausencia/inactividad de permiso la respuesta es denegacion.
- Rationale: mantiene postura de seguridad conservadora alineada a arquitectura existente.
- Alternatives considered: autorizacion hardcodeada solo por rol; descartado por baja granularidad y menor auditabilidad.

## Decision 12: Estrategia de pruebas para esta fase

- Decision: priorizar pruebas black-box en `backend/tests/BudgetCivil.Backend.BlackBoxTests` para contratos, autorizacion, transiciones y trazabilidad; unitarias adicionales solo si se toca calculo APU (fuera de alcance principal del feature).
- Rationale: alinea politica constitucional y valida comportamiento real de endpoints.
- Alternatives considered: abrir una segunda suite de integracion separada en esta fase; descartado por complejidad operativa innecesaria.

## Clarification Status

No quedan `NEEDS CLARIFICATION` abiertas para este feature. Las aclaraciones de sesiones 2026-09-29 y 2026-09-30 quedaron incorporadas en decisiones de diseno y contratos.
