# Tareas: i-2b-rf04-edt

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores se
registran en `PLAN §5.6.7` y amplían su alcance en este cambio:

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-18` | Código manual obligatorio, formato 1–10, mayúsculas/minúsculas, unicidad case-insensitive, endpoint de validación, inmutabilidad y compactación de posiciones | Unitaria |
| `T-U-19` | Caracteres, longitud, unicidad, cantidad de obra, validación de ciclo, `targetIndex` y orden de borrado en cascada | Unitaria |
| `T-I-39` | Persistencia: `CHECK`, FKs, índices, intercalación, nulabilidad de `wbs_items.wbs_chapter_id`, `wbs_project_activity` y `rowversion` | Integración manual |
| `T-I-40` | Jerarquía, orden, búsqueda, movimiento, cascada, actividad y participantes, y aislamiento entre proyectos de la misma compañía | Integración manual |
| `T-S-16` | Aislamiento entre compañías | Seguridad manual |
| `T-S-17` | Fail-closed y matriz `WBS_READ`/`WBS_WRITE` | Seguridad manual |
| `T-C-04` | Contrato v1: `camelCase`, `ProblemDetails`, `page`/`pageSize`, `move`, `activity`, `codes/validate` y `cascade` | Contrato en CI |
| `T-A-03` | Capas y módulo `M-03` sin dependencia de `M-02` | Arquitectura en CI |

## 0. Decisiones y documentación

- [x] 0.1 Registrar `D-117` (código manual, único y validado), `D-118` (reorganización libre con ítems
  raíz), `D-119` (baja en cascada, revisa `D-114`) y `D-120` (actividad y participantes) en `PLAN §0.5`, y
  ajustar `§2.4.2`, `§3.3.2`, `§3.3.3`, `§3.3.6`, `§4.2`, `§5.6.7` y el historial de versiones; verificación:
  `git diff --stat PLAN_INGENIERIA.md` muestra las secciones y `openspec validate --strict` sigue en verde.
- [x] 0.2 Actualizar `.github/docs/02` (fila `EDT` de la matriz, rutas de `wbs` con `move`, `activity`,
  `codes/validate` y `cascade`, y `RF-04` ampliado) y `.github/docs/05` (ítem raíz, cascada, tabla de
  actividad y corrección de la migración inexistente); verificación: `git diff` de ambos documentos y
  `Select-String` en `.github/docs` sin referencias a `WBS_ITEM_HAS_APU` ni a `20261113080043_wbs_add_i2b`.

## 1. Preparación y dominio

- [x] 1.1 Verificar la cadena de migraciones y la ausencia real de tablas `wbs_*`; verificación:
  `dotnet ef migrations list` lista la cadena vigente, `BudgetCivilDbContextModelSnapshot.cs` no contiene
  `wbs` y `backend/src` no tiene clases `Wbs*`.
- [x] 1.2 Crear `WbsNodeType`, `WbsNodeTypeCodes`, `WbsCode`, `WbsChapter`, `WbsItem` y `WbsProjectActivity`
  en `Domain/Wbs` con fábricas, autoría (`D-116`), cantidad validada, `WbsChapterId` nullable (`D-118`) y
  `ApuId` nullable; verificación: pruebas unitarias de alta válida, ítem raíz, nombre vacío, cantidad no
  positiva y APU ignorado (`SC-EDT-06`, `SC-EDT-25`…`29`, `SC-EDT-40`).
- [x] 1.3 Implementar `WbsCodePolicy` con `IsValidCode` y `NormalizeCode` (1–10, `R-19`, mayúsculas y
  minúsculas, sin composición ni respaldo); eliminar `RecomputeDescendantCodes`, `ComposeEffectiveCode`,
  `NextNaturalSegment`, `NextFallbackCode`, `ExceedsLimit` y la noción de código personalizado; verificación:
  pruebas de formato, longitud y normalización (`SC-EDT-20`…`24`, `SC-EDT-58`…`61`).
- [x] 1.4 Implementar la creación con código manual obligatorio, la edición explícita del código y la
  inmutabilidad ante baja, reordenamiento, movimiento y renombrado; verificación: `T-U-18` en verde con
  `SC-EDT-08`…`19`.
- [x] 1.5 Implementar en dominio la validación de movimiento (destino capítulo, mismo proyecto, ciclo,
  `targetIndex` después de retirar el nodo) y el orden de borrado de un subárbol de hijos a padres;
  verificación: `T-U-19` en verde con `SC-EDT-62`…`70`, `SC-EDT-75` y `SC-EDT-51`…`55`.

## 2. Persistencia

- [x] 2.1 Crear `WbsCodeConfiguration`, `WbsChapterConfiguration`, `WbsItemConfiguration` y
  `WbsProjectActivityConfiguration` con longitudes, intercalación `Latin1_General_100_CI_AS`, `CHECK`, FKs
  `RESTRICT`, `row_version`, índices, `wbs_chapter_id` nullable y el índice único filtrado de `apu_id`;
  verificación: `dotnet ef migrations script` muestra las cuatro tablas, la nulabilidad y
  `UQ_wbs_items_apu_id` con `WHERE apu_id IS NOT NULL`.
- [x] 2.2 Registrar `WbsCodes`, `WbsChapters`, `WbsItems` y `WbsProjectActivities` en `BudgetCivilDbContext`
  y aplicar las configuraciones; verificación: `dotnet build` sin errores y el snapshot contiene las cuatro
  tablas.
- [x] 2.3 Implementar `WbsRepository` (árbol, búsqueda, validación de código, posiciones, movimiento, cascada
  y actividad/participantes) y `WbsProjectAccessReader`; verificación: compila y `T-I-39` cubre lectura del
  árbol, unicidad y *upsert* de `wbs_project_activity`.
- [x] 2.4 Generar y revisar la migración `wbs_add_i2b`, y documentar respaldo, aplicación y reversión en
  `docs/runbook-migraciones.md`; verificación: `dotnet ef migrations script` revisado, `Down` elimina tablas,
  índices y FKs en orden inverso, y `dotnet ef migrations list` incluye la migración.
- [x] 2.5 Escribir `WbsPersistenceTests` contra SQL Server local; verificación: `rowversion`, `CHECK` de
  cantidad, unicidad `cim`/`CIM`, ítem raíz, `UQ_wbs_items_apu_id` y una fila por proyecto/usuario en
  `wbs_project_activity` en verde (`T-I-39`).

## 3. Aplicación

- [x] 3.1 Definir `IWbsRepository` e `IWbsProjectAccessReader` en `Application/Wbs/Repositories` e
  implementar `WbsAccessService` con las reglas de `REQ-EDT-13` y `REQ-EDT-14`, incluida la exclusión del
  Auditor en actividad; verificación: pruebas unitarias de Administrador, Director, Presupuestador asignado
  y no asignado y Auditor (`SC-EDT-42`…`50`, `76`).
- [x] 3.2 Implementar los comandos de capítulo (alta con código manual, edición de nombre y código, y baja
  con cascada confirmada) con sus validadores y `rowVersion`; verificación: pruebas unitarias de `SC-EDT-01`,
  `05`, `08`…`19`, `52`…`54` y `WBS_CODE_INVALID`/`WBS_CONCURRENCY_CONFLICT`.
- [x] 3.3 Implementar los comandos de ítem (alta dentro de capítulo o en la raíz con código manual, edición
  de código y cantidad, y baja con `rowVersion`) con cantidad y cascada; verificación: pruebas unitarias de
  `SC-EDT-06`, `25`…`29`, `51`, `55` y `WBS_CONCURRENCY_CONFLICT`.
- [x] 3.4 Implementar el comando de movimiento con ciclo, destino, `targetIndex` después de retirar el nodo
  y compactación, y el reordenamiento por padre; verificación: pruebas unitarias de `SC-EDT-33`…`36`,
  `62`…`70`, `75` y `WBS_CONCURRENCY_CONFLICT`.
- [x] 3.5 Implementar las consultas de árbol (con ítems raíz), búsqueda, validación de código y actividad con
  `ListingQueryPipeline` y `PaginationValidator`; verificación: pruebas unitarias de `SC-EDT-30`…`32`, `36`,
  `58`…`61`, `71`…`74`, `76` y respuesta `PAGE_SIZE_INVALID`.
- [x] 3.6 Escribir las pruebas de los manejadores con dobles de `IWbsRepository`; verificación:
  `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde.

## 4. API, contrato y permisos

- [x] 4.1 Añadir `AuthorizationPolicies.WbsRead` y `WbsWrite` y sus políticas en `Program.cs` con
  `RequirePermission("WBS_READ")` y `RequirePermission("WBS_WRITE")`; verificación: la aplicación arranca y
  ambas políticas resuelven.
- [x] 4.2 Crear `WbsController` en `api/v1/projects/{projectId:guid}/wbs` con las doce operaciones de
  `design.md §D8` y DTOs `camelCase`; verificación: Swagger muestra las rutas y `401` sin token.
- [x] 4.3 Añadir `WBS_READ` y `WBS_WRITE` a `SecurityBaselineSeeder` con la asignación de la matriz y subir
  el `SeedVersion` a `v7`; verificación: `SeedIdempotencyTests` en verde y el inventario activo refleja las
  dos claves nuevas.
- [x] 4.4 Verificar que `.github/docs/05 §8` cita la versión del `seed` y las migraciones reales;
  verificación: la sección no menciona migraciones inexistentes y el valor del `seed` coincide con el
  `SecurityBaselineSeeder`.
- [x] 4.5 Escribir la prueba de contrato de la EDT; verificación: `T-C-04` en verde con `camelCase`,
  `extensions.code`, paginación, `move`, `activity`, `codes/validate`, `cascade` y `rowVersion` en baja.
- [x] 4.6 Añadir la regla de arquitectura del módulo `M-03`; verificación: `T-A-03` en verde y
  `dotnet test backend/tests/BudgetCivil.Tests.Architecture` sin fallos.

## 5. Verificación de integración y seguridad

- [x] 5.1 Ejecutar jerarquía, orden, búsqueda, movimiento, cascada, actividad y participantes con dos
  proyectos de la misma compañía contra SQL Server local; verificación: `T-I-40` firmado con evidencia por
  escenario (`SC-EDT-04`, `05`, `30`…`36`, `51`…`76`).
- [x] 5.2 Ejecutar las pruebas de aislamiento entre dos compañías; verificación: `T-S-16` firmado y `403` en
  lectura, escritura y movimiento cruzados (`SC-EDT-56`, `57`).
- [x] 5.3 Ejecutar las pruebas de fail-closed y de la matriz de permisos; verificación: `T-S-17` firmado con
  `403` del Administrador y del Auditor en escritura y evento de seguridad registrado.
- [x] 5.4 Revisar Swagger con la API levantada; verificación: las doce operaciones aparecen bajo
  `/api/v1/projects/{projectId}/wbs` y `403` sin permiso.

## Estado verificado del repositorio

- **Implementado:** `backend/src` contiene el módulo `M-03` (`Domain/Wbs`, `Application/Wbs`,
  `Persistence/Configurations/Wbs`, `WbsRepository`, `WbsProjectAccessReader` y `WbsController`); la
  migración `20261114065800_wbs_add_i2b` está aplicada en la base local y el `seed` subió a `v7` con
  `WBS_READ` y `WBS_WRITE`.
- **Pruebas:** `T-U-18`/`T-U-19` (66 casos unitarios del EDT), `T-C-04` (contrato), `T-A-03`
  (arquitectura), `T-I-39` (7 casos de persistencia) y `T-I-40`/`T-S-16`/`T-S-17` (3 casos de flujo,
  aislamiento y permisos) ejecutados contra SQL Server local. Swagger expone 12 operaciones y responde
  `401` sin token.
- **Documentación:** `.github/docs/05` cita la migración real y el `seed` `v7`; `docs/runbook-migraciones.md`
  registra la migración aplicada y su reversión.

## Trazabilidad

| Requisito | Escenarios | RF/BR/R | Decisión | Prueba |
|---|---|---|---|---|
| `REQ-EDT-01` | `SC-EDT-01`…`03` | `RF-04`; `R-18` | `D-30`, `D-51` | `T-I-40`, `T-U-18` |
| `REQ-EDT-02` | `SC-EDT-04`…`07` | `RF-04`; `R-18` | `D-30`, `D-118` | `T-U-18`, `T-I-40` |
| `REQ-EDT-03` | `SC-EDT-08`…`12` | `RF-04`; `BR-015` | `D-30`, `D-51`, `D-117` | `T-U-18` |
| `REQ-EDT-04` | `SC-EDT-13`…`15` | `RF-04`; `R-19` | `D-30`, `D-117` | `T-U-18` |
| `REQ-EDT-05` | `SC-EDT-16`…`19` | `RF-04` | `D-30`, `D-117` | `T-U-18`, `T-I-40` |
| `REQ-EDT-06` | `SC-EDT-20`…`24` | `RF-04`; `BR-015`; `R-19` | `D-30`, `D-115` | `T-U-19`, `T-I-39` |
| `REQ-EDT-07` | `SC-EDT-25`…`27` | `RF-04`, `RF-22`; `R-21` | `D-113` | `T-U-19`, `T-I-39` |
| `REQ-EDT-08` | `SC-EDT-28`, `29` | `RF-07`; `BR-016` | `D-113` | `T-U-19`, `T-I-39` |
| `REQ-EDT-09` | `SC-EDT-30`…`32` | `RF-04` | `D-101` | `T-I-40`, `T-C-04` |
| `REQ-EDT-10` | `SC-EDT-33`…`36` | `RF-04` | `D-30`, `D-117` | `T-U-18`, `T-I-40` |
| `REQ-EDT-11` | `SC-EDT-37`…`39` | `RF-04` | `D-50`, `D-97` | `T-I-39`, `T-I-40` |
| `REQ-EDT-12` | `SC-EDT-40`, `41` | `RF-14` | `D-08`, `D-116` | `T-U-19` |
| `REQ-EDT-13` | `SC-EDT-42`…`46` | `RF-04` | `D-34`, `D-82` | `T-S-17` |
| `REQ-EDT-14` | `SC-EDT-47`…`50`, `76` | `RF-04` | `D-34`, `D-83`, `D-84`, `D-120` | `T-S-17`, `T-I-40` |
| `REQ-EDT-15` | `SC-EDT-51`…`55` | `RF-04` | `D-114` (revisada), `D-119` | `T-I-40`, `T-U-19` |
| `REQ-EDT-16` | `SC-EDT-56`, `57` | `RF-04`; `R-10`, `R-14` | `D-02`, `D-36` | `T-S-16` |
| `REQ-EDT-17` | `SC-EDT-58`…`61` | `RF-04` | `D-115`, `D-117` | `T-U-18`, `T-C-04` |
| `REQ-EDT-18` | `SC-EDT-62`…`70`, `75` | `RF-04`; `R-18` | `D-30`, `D-118` | `T-U-19`, `T-I-40` |
| `REQ-EDT-19` | `SC-EDT-71`…`74`, `76` | `RF-14` | `D-08`, `D-116`, `D-120` | `T-I-40`, `T-C-04` |

## Bloqueos

- La FK `wbs_items.apu_id → apus` no puede crearse en este cambio: la tabla `apus` llega en `I-3b`
  (`D-113`). El índice único filtrado sí queda activo; el APU desvinculado de `D-119` y su búsqueda se
  resuelven en `I-3b`.
- La base local tiene aplicadas las seis migraciones vigentes, incluida
  `20261114065800_wbs_add_i2b`; no existen tablas `wbs_*` de la implementación anterior. El respaldo previo
  está en `H:\Program Files\MSSQL17.SQLEXPRESS\MSSQL\Backup\BudgetCivilDb_pre_reset_20261114.bak`.

## Workflow follow-up

- Archivar el cambio solo con la aprobación explícita del estudiante.
- Sincronizar `specs/edt/spec.md` a `openspec/specs/edt/` al archivar.
- Actualizar `openspec/project.md §8` (fila `edt` → Vigente) y la «Regla de crecimiento» al archivar.
