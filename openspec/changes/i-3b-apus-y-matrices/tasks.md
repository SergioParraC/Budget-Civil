# Tareas: i-3b-apus-y-matrices

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores nuevos
(`T-U-21`…`T-U-24`, `T-I-42`…`T-I-45`, `T-S-18`, `T-S-19`, `T-C-05`, `T-A-04`) se registran en la tarea 5.3
y se proponen para `PLAN §5.6.8` (`INC-12`). `T-U-18`, `T-U-19`, `T-I-39`, `T-I-40`, `T-S-16`, `T-S-17`,
`T-C-04` y `T-A-03` ya pertenecen a `E-2b` (`PLAN §5.6.7`). Se conservan los ya asignados por
`PLAN §5.6.1`: `T-U-03`, `T-U-04`, `T-U-11`, `T-I-07`, `T-I-08`, `T-I-09`, `T-I-20`, `T-S-06` y `T-E-01`.

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-03` | Reconciliación de insumos simples y decisiones del usuario | Unitaria |
| `T-U-04` | Composición de Matriz y no anidamiento | Unitaria |
| `T-U-11` | Conteo de uso, desperdicio y expansión de un nivel | Unitaria |
| `T-U-21` | Completitud, herencia de la Matriz, estado y autoría del APU | Unitaria |
| `T-U-22` | Vínculo 1:1, baja del ítem y vinculación del APU | Unitaria |
| `T-U-23` | Alcance por rol en lectura y escritura de APUs y Matrices | Unitaria |
| `T-U-24` | Costo directo, subtotales por tipo, AIU, redondeo y `yield` | Unitaria |
| `T-I-07` | Alta, copia y detalle del APU con su ítem | Integración manual |
| `T-I-08` | Reconciliación y copia transaccional de insumos simples | Integración manual |
| `T-I-09` | Persistencia y copia de Matrices sin validación | Integración manual |
| `T-I-20` | Conteo de uso contra datos reales | Integración manual |
| `T-I-42` | Persistencia de `apus`, `apu_resources`, `matrix_components`, decimales y FK de `wbs_items.apu_id` | Integración manual |
| `T-I-43` | Copia de APU y de Matriz con independencia en ambos sentidos | Integración manual |
| `T-I-44` | Conteo de uso con Matriz y con insumo sin uso | Integración manual |
| `T-I-45` | Vista previa de costo, AIU y baja del ítem con APU | Integración manual |
| `T-S-06` | Prohibición de copia y consulta de otra compañía | Seguridad manual |
| `T-S-18` | Aislamiento, solo lectura y `ProjectMember` en escritura | Seguridad manual |
| `T-S-19` | Fail-closed y matriz de permisos de APUs | Seguridad manual |
| `T-C-05` | Contrato v1 de APUs, Matrices, árbol, costos y conteo | Contrato en CI |
| `T-A-04` | Reglas de capas y aislamiento de módulos de `M-05` | Arquitectura en CI |
| `T-E-01` | Flujo manual: ítem → APU → Matriz → copia → costo → conteo | Extremo a extremo manual |

**Puerta de entrada:** `E-2b` está archivado (`2026-11-14-i-2b-rf04-edt`) y su especificación se sincronizó
en `openspec/specs/edt/`. `INC-02` quedó aplicado por `D-113` en `20261114065800_wbs_add_i2b`. La tarea 1.1
verifica la cadena, el índice filtrado y la ausencia de la FK.

## 0. Decisiones y documentación

- [x] 0.1 Sincronizar `openspec/specs/edt/spec.md` desde el cambio archivado y actualizar la fila `EDT` de
  `openspec/project.md §8`. Verificación: `openspec validate --all --strict` y `openspec list --specs`
  incluyen `edt`.
- [x] 0.2 Registrar en `PLAN_INGENIERIA.md` la revisión de `D-49` (baja física del APU), `D-114`/`D-119`
  (capítulo vacío y baja del APU con el ítem), `D-06`/`D-32`/`X-15` (fórmula F1 del costo unitario con
  `yield`), `R-21`/`D-33` (conteo con desperdicio), `D-121` (decimales de redondeo y cantidades) y el ajuste
  de `§2.4.2`, `§3.3.2`, `§3.3.3`, `§3.3.5`, `§3.3.6`, `§4.2`, `§5.6` y `§5.6.8`. Verificación: `git diff
  PLAN_INGENIERIA.md` y `openspec validate --all --strict` en verde.
- [x] 0.3 Actualizar `.github/docs/02` (rutas, matriz de permisos y campos), `.github/docs/05`
  (baja del ítem con APU, capítulo vacío, parámetros de redondeo y `apus.unit` de 5 caracteres) y
  `docs/runbook-migraciones.md` con la migración y el `seed` `v8`.

## 1. Preparación y dominio

- [x] 1.1 Verificar `E-2b` y la cadena. Verificación: `dotnet build` sin errores, `dotnet ef migrations list`
  con `20261114065800_wbs_add_i2b`, `wbs_items.apu_id` nullable con `UQ_wbs_items_apu_id` filtrado y sin FK,
  y `WbsItem`/`WbsCode` con cantidad de obra.
- [x] 1.2 Crear `Apu` y `ApuResource` en `Domain/Apus` con los campos del diseño, `Status`, autoría,
  `RowVersion` y `CalculateStatus()` con la completitud de las Matrices referenciadas; sin
  `is_active`/`deleted_at`. Verificación: pruebas unitarias `SC-APUS-06`…`SC-APUS-14`, `SC-APUS-63`,
  `SC-APUS-64`, `SC-APUS-84`, `SC-APUS-96` y `SC-APUS-97` en verde (`T-U-21`).
- [x] 1.3 Crear `MatrixComponent` con las validaciones de consumo y desperdicio (default 0) y las reglas de
  tipo `MATRIX`, no anidamiento y autorreferencia. Verificación: pruebas unitarias `SC-APUS-26`…`SC-APUS-34`
  en verde (`T-U-04`).
- [x] 1.4 Implementar la creación y la edición del APU con vínculo obligatorio al ítem libre y el recálculo
  de estado. Verificación: pruebas unitarias `SC-APUS-01`…`SC-APUS-05`, `SC-APUS-15`…`SC-APUS-20` en verde
  (`T-U-22`).
- [x] 1.5 Implementar la lógica de dominio de la copia: origen independiente, sin aprobación, actor como
  último modificador y estado recalculado; las Matrices se copian sin validación. Verificación: pruebas
  unitarias `SC-APUS-42`, `SC-APUS-46`, `SC-APUS-53`…`SC-APUS-55` en verde (`T-U-03`).
- [x] 1.6 Cubrir la expansión de un nivel del conteo de uso en el dominio, con desperdicio en la línea y en
  el componente y sin `yield`. Verificación: pruebas unitarias `SC-APUS-56`…`SC-APUS-62` y `SC-APUS-98` en
  verde (`T-U-11`) y cobertura ≥ 80 % en `Domain/Apus`.
- [x] 1.7 Añadir a `WbsItem` (`M-03`) el método `LinkApu(Guid apuId)` que exige un APU no vacío y un ítem
  sin APU, y retirar el método de prueba que anulaba el vínculo. Verificación: pruebas unitarias del enlace
  y de la unicidad 1:1 en verde (`T-U-22`).
- [x] 1.8 Implementar el cálculo del costo unitario del APU y de la Matriz con la fórmula de `design.md D8`:
  cantidad efectiva con desperdicio y `yield`, valor por línea, subtotal por tipo, AIU discriminado, total y
  `FALTA_INFORMACION` sin precio. Verificación: pruebas unitarias `SC-APUS-86`…`SC-APUS-90`, `SC-APUS-92`,
  `SC-APUS-93` y `SC-APUS-99` en verde (`T-U-24`).

## 2. Persistencia

- [x] 2.1 Configurar `Apu` en EF Core: tabla `apus`, longitudes, `CK_apus_yield`, `CK_apus_status`, FK de
  proyecto y usuario `RESTRICT`, `rowversion` e índices. Verificación: el script contiene la tabla y los dos
  `CHECK` (`T-I-42`).
- [x] 2.2 Configurar `ApuResource` y `MatrixComponent`: tablas, `consumption` `DECIMAL(18,10)`,
  `waste_percentage` con default 0, `CHECK` de consumo y desperdicio, FK en `CASCADE`/`RESTRICT` e índices.
  Verificación: el script contiene los cuatro `CHECK`, las FK con el borrado esperado y los cinco índices
  (`T-I-42`).
- [x] 2.3 Añadir la FK `FK_wbs_items_apus_apu_id` (`RESTRICT`) sobre `wbs_items.apu_id`; no repetir la
  nulabilidad ni `UQ_wbs_items_apu_id`, ya aplicadas por `wbs_add_i2b`. Verificación: el script contiene la
  FK, el `Down` la elimina y dos ítems no pueden apuntar al mismo APU (`T-I-42`).
- [x] 2.4 Implementar `IWbsItemReader` e `IWbsItemApuCleaner` en `Infrastructure`: ítem por proyecto, ítem
  por APU, cantidades, borrado de APUs por ítem y detección de APUs en un subárbol. El vínculo se hace con
  `WbsItem.LinkApu` en la transacción unificada de alta/copia (`D-122`; `T-I-07`, `T-I-42`, `T-S-06`).
- [x] 2.5 Implementar `IApuResourceCopyWriter` en `Infrastructure` para crear insumos copiados y Matrices
  copiadas dentro del mismo `DbContext`, con el actor como autor. Verificación: una transacción fallida no
  deja insumos ni APU parciales (`T-I-08`, `T-I-43`).
- [x] 2.6 Generar la migración `YYYYMMDDHHMM_apus_add_e3b`: tablas, FK de `wbs_items`, `ALTER` de
  `quantity_work`/`consumption` a `DECIMAL(18,10)`, columnas `rounding_decimals` y `quantity_decimals` con
  default 2 y `CHECK` 0–10, y actualización del runbook (`D-40`, `D-107`). Verificación: `dotnet ef
  migrations script` revisado y `Down` revierte el `ALTER` y las columnas.
- [x] 2.7 Ampliar `SecurityBaselineSeeder` a `v8` con `APUS_READ` y `APUS_WRITE` y la asignación del diseño.
  Verificación: ejecutar el `seed` dos veces deja 37 permisos y no duplica asignaciones (`T-S-19`).
- [x] 2.8 Añadir `rounding_decimals` y `quantity_decimals` a `ProjectConfiguration`, `CreateProjectCommand`,
  `UpdateProjectCommand` y `GetProjectConfigurationResponse`, con normalización 0–10. Verificación:
  `SC-PROYECTOS-54`/`SC-PROYECTOS-55` en verde y el contrato devuelve los dos campos.

## 3. Aplicación

- [x] 3.1 Crear los DTOs y el mapeo manual de APU, líneas, Matriz, componentes, costo, referencia del APU en
  el árbol y conteo de uso (sin AutoMapper ni Mapster), con nombres de campo en inglés. Verificación: prueba
  de contrato que serializa `description`, `apu.status`, `apu.description`, `apu.unit`, `apuId`, `code`,
  `resources[].resourceType`, `resources[].items[].subtotal`, `directCost`, `indirectCosts` y `total`, con
  los campos del ítem nulos en un APU ajeno (`T-C-05`).
- [x] 3.2 Implementar el alta y la edición del APU con validador: `wbsItemId` obligatorio, descripción no
  vacía, unidad de 1 a 5 caracteres, `yield > 0` y `rowVersion` en la edición. Verificación: `SC-APUS-06`…`SC-APUS-10` y
  `SC-APUS-67`…`SC-APUS-69` en verde.
- [x] 3.3 Implementar los casos de uso de composición del APU con `rowVersion`, desperdicio por defecto 0 y
  recálculo del estado. Verificación: `SC-APUS-15`…`SC-APUS-20` en verde.
- [x] 3.4 Implementar los casos de uso de composición de la Matriz con `rowVersion` del insumo,
  actualización de su autoría y estado, y recálculo del `status` de los APUs que la usan. Verificación:
  `SC-APUS-26`…`SC-APUS-34`, `SC-APUS-66`, `SC-APUS-70` y `SC-APUS-97` en verde (`T-U-04`).
- [x] 3.5 Implementar la vista previa de reconciliación de APU con `EXACT`/`SIMILAR`/`NONE` y hasta diez
  candidatos, solo para insumos simples. Verificación: `SC-APUS-36`, `SC-APUS-48` y `SC-APUS-52` en verde
  (`T-U-03`).
- [x] 3.6 Implementar la copia de APU: ítem destino libre, resoluciones obligatorias para insumos simples,
  `USE_EXISTING`/`KEEP_ORIGIN`, copia de Matrices sin validación, copia transaccional y origen de otra
  compañía tratado como inexistente. Verificación: `SC-APUS-42`…`SC-APUS-52` en verde (`T-U-03`,
  `T-I-08`, `T-I-43`).
- [x] 3.7 Implementar la copia de Matriz sin validación de nombres ni de componentes, creando todos los
  registros nuevos. Verificación: `SC-APUS-35`…`SC-APUS-41` en verde (`T-I-09`, `T-I-43`).
- [x] 3.8 Implementar el listado de APUs de otros proyectos y el detalle del APU con búsqueda parcial por
  descripción o código del ítem, filtros `departmentId`/`municipalityId` del proyecto de origen, ubicación y
  paginación; el detalle agrupa `resources` por tipo con `subtotal` y deja `directCost`, `indirectCosts` y
  `total` fuera del listado, y oculta `quantityWork` en APUs ajenos. Verificación: `SC-APUS-21`…`SC-APUS-25`,
  `SC-APUS-94`, `SC-APUS-95`, `SC-APUS-05` y `SC-APUS-89` en verde (`T-I-07`, `T-C-05`).
- [x] 3.9 Implementar la consulta de conteo de uso en `Application/Calculation` con la proyección de consumo
  directo y la expansión de un nivel, con desperdicio y sin `yield`. Verificación: `SC-APUS-56`…`SC-APUS-62`
  y `SC-APUS-98` en verde (`T-U-11`, `T-I-20`, `T-I-44`).
- [x] 3.10 Implementar las reglas de alcance: lectura de toda la compañía para Director y Presupuestador,
  Auditor solo asignados, Administrador solo lectura; escritura solo con `ProjectMember`. Verificación:
  `SC-APUS-71`…`SC-APUS-83` y `SC-APUS-91` en verde (`T-U-23`, `T-S-18`).
- [x] 3.11 Implementar la vista previa de costo del APU con `resources` agrupado por tipo, `subtotal` por
  insumo, `directCost`, `indirectCosts` y `total` fuera del listado, y `FALTA_INFORMACION` sin precio.
  Verificación: `SC-APUS-86`…`SC-APUS-90` en verde (`T-U-24`, `T-I-45`).
- [x] 3.12 Implementar la vista de la Matriz con `resources` agrupado por tipo, `subtotal` por insumo y
  `directCost` fuera del listado, sin `indirectCosts` ni AIU. Verificación: `SC-APUS-92` y `SC-APUS-93` en
  verde (`T-U-24`, `T-I-45`).
- [x] 3.13 Implementar en `M-03` la baja del ítem con APU y el bloqueo del capítulo no vacío; retirar
  `cascade`. Verificación: `SC-EDT-81`…`SC-EDT-85` en verde y `WBS_CHAPTER_HAS_APU`/`WBS_NODE_HAS_CHILDREN`
  observados (`T-I-45`).
- [x] 3.14 Cubrir con pruebas unitarias los validadores, los casos de uso, el mapeo, el costo y los errores.
  Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.
- [x] 3.15 Declarar `IMatrixApuStatusUpdater` en `Application/Resources/Repositories`, implementarlo en
  `Infrastructure` e invocarlo desde la edición de un insumo `MATRIX` cuando cambia su unidad. Verificación:
  `SC-APUS-100` en verde y el recálculo dentro de la misma transacción (`T-I-42`).

## 4. API, contrato y permisos

- [x] 4.1 Exponer las quince operaciones del diseño bajo `/api/v1/projects/{projectId}` con `APUS_READ`,
  `APUS_WRITE` y `RESOURCES_READ`; el árbol de `GET /wbs` incluye `apu`. Verificación: Swagger muestra las
  rutas y una petición sin permiso responde `403` (`T-S-19`, `T-C-05`).
- [x] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` y `409` en concurrencia, con los
  códigos del diseño, incluidos `WBS_CHAPTER_HAS_APU` y `WBS_NODE_HAS_CHILDREN`. Verificación: `T-C-05` sin
  sobres propios ni `snake_case`.
- [x] 4.3 Rechazar el tipo `MATRIX` en `POST /projects/{projectId}/resources/copy` con
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`. Verificación: `SC-INSUMOS-56` en verde
  (`REQ-INSUMOS-09`).
- [x] 4.4 Actualizar la documentación del contrato v1 y el cliente generado de forma diferida (`D-56`,
  `D-109`). Verificación: `T-C-05` en CI y `IMPACTO_FRONTEND.md` actualizado.

## 5. Verificación de la etapa

- [x] 5.1 Ejecutar `dotnet build` y `dotnet test` de las suites unitaria y de arquitectura. Verificación:
  compilación sin errores ni advertencias, `T-U-03`, `T-U-04`, `T-U-11`, `T-U-21`…`T-U-24`, `T-C-05` y
  `T-A-04` en verde, y cobertura ≥ 80 % en `Domain/Apus`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local con dos compañías y dos proyectos:
  `T-I-07`, `T-I-08`, `T-I-09`, `T-I-20`, `T-I-42`…`T-I-45`, `T-S-06`, `T-S-18` y `T-S-19`. Verificación:
  checklist con evidencia por identificador, incluida la baja del ítem con APU, el bloqueo de capítulos, la
  copia de Matriz sin validación y la vista previa de costo.
- [x] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los
  veinte `REQ-APUS-*`, los dos `REQ-INSUMOS-*`, `REQ-EDT-13/20/21` y `REQ-PROYECTOS-10`, y proponer los
  identificadores para `PLAN §5.6.8`. Verificación: ningún requisito queda sin escenario ni sin prueba.
- [ ] 5.4 Ejecutar `openspec validate i-3b-apus-y-matrices --strict` y archivar solo con la aprobación del
  estudiante y las decisiones `INC-04`, `INC-06` e `INC-11` aplicadas. Verificación: validación sin errores,
  checklist firmado y `openspec archive` con `apus`, `edt`, `insumos` y `proyectos` sincronizadas.

## Trazabilidad

| Requisito | Escenarios | RF/BR/R | Decisiones | Prueba |
|---|---|---|---|---|
| `REQ-APUS-01` | `SC-APUS-01`…`05` | `RF-07`; `BR-016`; `R-13` | `D-16`, `D-31` | `T-U-22`, `T-I-07`, `T-I-42` |
| `REQ-APUS-02` | `SC-APUS-06`…`10` | `RF-07`; `BR-016` | `D-31` | `T-U-21`, `T-I-42` |
| `REQ-APUS-03` | `SC-APUS-11`…`14`, `84`, `85`, `96`, `97`, `100` | `RF-07` | `05 §3`; `D-48` | `T-U-21`, `T-I-42` |
| `REQ-APUS-04` | `SC-APUS-15`…`20` | `RF-07`; `R-01` | `D-05`, `D-49` | `T-U-22`, `T-I-42` |
| `REQ-APUS-05` | `SC-APUS-21`…`25`, `94`, `95` | `RF-07`; `R-18` | `D-30`, `D-31`, `D-101` | `T-I-07`, `T-C-05` |
| `REQ-APUS-06` | `SC-APUS-26`…`30` | `RF-09`; `BR-008` | `D-48` | `T-U-04`, `T-I-09` |
| `REQ-APUS-07` | `SC-APUS-31`…`34` | `RF-09`; `BR-008`; `R-11` | `D-16`, `D-48` | `T-U-04`, `T-I-09` |
| `REQ-APUS-08` | `SC-APUS-35`…`41` | `RF-09`; `BR-021` | `D-16`, `D-25`, `D-29` | `T-I-09`, `T-I-43` |
| `REQ-APUS-09` | `SC-APUS-42`…`46` | `RF-07`; `BR-018` | `D-13`, `D-16`, `D-24` | `T-I-07`, `T-I-08`, `T-I-43` |
| `REQ-APUS-10` | `SC-APUS-47`…`52`, `101` | `RF-08`; `BR-021` | `D-25` | `T-U-03`, `T-I-08` |
| `REQ-APUS-11` | `SC-APUS-53`…`55` | `RF-07`, `RF-08`; `R-17` | `D-29` | `T-I-43` |
| `REQ-APUS-12` | `SC-APUS-56`…`59`, `102` | `RF-22`; `R-21` | `D-33`, `D-121` | `T-U-11`, `T-I-20`, `T-I-44` |
| `REQ-APUS-13` | `SC-APUS-60`…`62`, `98` | `RF-22`; `R-21`; `BR-008` | `D-33` | `T-U-11`, `T-I-44` |
| `REQ-APUS-14` | `SC-APUS-63`…`66` | `RF-14`; `R-07` | `D-08` | `T-U-21`, `T-I-42` |
| `REQ-APUS-15` | `SC-APUS-67`…`70` | —; `R-14` | `D-50`, `D-97` | `T-I-42` |
| `REQ-APUS-16` | `SC-APUS-71`…`75`, `91` | `RF-07`; `R-08` | `D-34`, `D-82`, `D-83`, `D-84` | `T-U-23`, `T-S-18`, `T-S-19` |
| `REQ-APUS-17` | `SC-APUS-76`…`80` | `RF-07`, `RF-22` | `D-34`, `D-83`, `D-84` | `T-U-23`, `T-S-18` |
| `REQ-APUS-18` | `SC-APUS-81`…`83` | —; `R-10`, `R-14` | `D-02`, `D-36` | `T-S-06`, `T-S-18` |
| `REQ-APUS-19` | `SC-APUS-86`…`90`, `99` | `RF-07`, `RF-10`; `D-06`, `D-07` | `D-33` | `T-U-24`, `T-I-45` |
| `REQ-APUS-20` | `SC-APUS-92`, `93` | `RF-09`, `RF-10`; `BR-008` | `D-48` | `T-U-24`, `T-I-45` |
| `REQ-EDT-13` | `SC-EDT-42`…`46`, `77` | `RF-04` | `D-34`, `D-82` | `T-S-18` |
| `REQ-EDT-20` | `SC-EDT-78`…`80` | `RF-04`, `RF-07` | `D-30`, `D-113` | `T-C-05` |
| `REQ-EDT-21` | `SC-EDT-81`…`85` | `RF-04` | `D-114`, `D-119` | `T-I-45` |
| `REQ-PROYECTOS-10` | `SC-PROYECTOS-22`, `23`, `54`, `55` | `RF-03` | `D-90`, `D-93`, `D-94`, `D-112`, `D-121` | `T-U-24`, `T-C-05` |
| `REQ-INSUMOS-04` | `SC-INSUMOS-11`…`14`, `52`…`55` | `RF-05`, `RF-09`; `BR-009` | `D-89`, `D-48` | `T-U-21`, `T-I-33` |
| `REQ-INSUMOS-09` | `SC-INSUMOS-28`…`30`, `56` | `RF-06`, `RF-09`; `BR-021` | `D-27`, `D-29` | `T-I-34`, `T-I-43` |

## Dependencias y bloqueos

1. **`E-2b` está cerrado** y `openspec/specs/edt` sincronizada; `INC-01` e `INC-02` dejan de ser bloqueos.
2. **La FK de `wbs_items.apu_id` se añade aquí**: `INC-02` ya aplicó la nulabilidad y el índice filtrado.
3. **`X-15` queda parcial**: `F1` define el costo unitario del APU con `yield` como factor de cantidad; el
   total del proyecto y la semántica definitiva de `yield` pertenecen a `E-4a` (`INC-11`, `INC-18`).
4. **`E-4b` consume los puntos de invalidación**: `ApuApproval` y `D-53` se implementan allí.
5. **`E-5`** añadirá el origen público a la copia; no cambia el contrato de reconciliación.
6. **Decisiones cerradas (15/11/2026)**: `INC-04` (un nivel, desperdicio en línea y componente), `INC-06`
   (herencia de la completitud con recálculo de un nivel) e `INC-11` (`yield` multiplica la cantidad
   efectiva del costo, no la del conteo).
7. **Identificadores de prueba**: `PLAN §5.6.7` ya asignó los de `E-2b`; este cambio usa `T-U-21`…`T-U-24`,
   `T-I-42`…`T-I-45`, `T-S-18`, `T-S-19`, `T-C-05` y `T-A-04` (`INC-12`).

## Estado de implementación

- **Implementada** el 15/11/2026: dominio, persistencia, aplicación, API, `seed` `v8`, pruebas unitarias y
  de arquitectura, migración `20261115092848_apus_add_e3b` y documentación (`PLAN`, `.github/docs/02`,
  `.github/docs/05`, runbook e `IMPACTO_FRONTEND.md`).
- **Ajuste del 15/11/2026:** la vista previa de copia ahora expone el precio unitario del insumo de origen,
  `requiresResolution` y, por candidato, el comparativo `unitMatches`, `unitPriceMatches` y
  `unitPriceDifference` (`candidato − origen`). `EXACT` clasifica por descripción normalizada y el
  comparativo de unidad y precio se resuelve por separado. El flujo `preview → copy` quedó documentado en
  los XML docs de los endpoints y en `IMPACTO_FRONTEND.md`.
- **Ajuste del 15/11/2026:** `GET /resource-usage` devuelve `resources` agrupado por `resourceType`, con
  `resourceTypeName`, `unitPrice`, `quantity`, `totalPrice` por insumo, `subtotal` por tipo y `totalCost`;
  el total usa `roundingDecimals` y queda nulo si falta precio. Pruebas en
  `ResourceUsageReportServiceTests` y `T-C-05`.
- **Pendiente:** verificación manual contra SQL Server (tarea 5.2) y archivo del cambio con la aprobación
  final del estudiante (tarea 5.4). `openspec validate --all --strict` pasa; `dotnet build` sin errores y
  `dotnet test` de las suites unitaria (386) y de arquitectura (10) en verde.
- **Precondición cerrada:** `E-2b` está implementado y archivado; `openspec/specs/edt` sincronizada;
  `wbs_items.apu_id` nullable con `UQ_wbs_items_apu_id` filtrado y FK `RESTRICT` añadida aquí; `seed`
  `v8` con 37 permisos.
- **Verificado en el repositorio:** `dotnet ef migrations list` contiene
  `20261114065800_wbs_add_i2b` y `20261115092848_apus_add_e3b`; el script contiene las tres tablas, los
  cuatro `CHECK` de composición, los dos `CHECK` de decimales, la FK `FK_dbo_wbs_items_apus_apu_id` y los
  índices esperados.

## Ajuste de alcance: EDT + APU unificados (15/11/2026)

- [x] 8.1 Retirar `yield` del dominio, DTO, cálculo, persistencia y pruebas (`REQ-APUS-22`).
- [x] 8.2 Crear la migración `20261115230025_apus_remove_yield` con `Down` que restaura la columna y el `CHECK`.
- [x] 8.3 Implementar el alta unificada `POST /wbs/items` con `apu` obligatorio y `resources` opcional.
- [x] 8.4 Implementar la edición unificada `PUT /wbs/items/{itemId}` con `apu` parcial, doble `rowVersion` y reemplazo de recursos.
- [x] 8.5 Retirar `POST /apus`, `PUT /apus/{apuId}` y los tres endpoints de líneas; actualizar DI y controladores.
- [x] 8.6 Cambiar la copia para crear el ítem destino (`target { code, wbsChapterId, quantityWork }`) y el preview a solo `sourceApuId`.
- [x] 8.7 Exponer `apu.unitPrice = 0` y `apu.rowVersion` en `GET /wbs`; retirar `yield` del detalle.
- [x] 8.8 Actualizar pruebas unitarias (386), contrato y documentación; `openspec validate --all --strict` en verde.


## Workflow follow-up

- `INC-04`, `INC-06` e `INC-11` cerradas (15/11/2026); propagar `R-21`, `D-33`, `D-06` y `X-15` al `PLAN`.
- Sincronizar `specs/apus/spec.md` a `openspec/specs/apus/` y los cambios de `edt`, `insumos` y `proyectos`
  a sus especificaciones principales al archivar.
- Actualizar el índice de `openspec/project.md` (fila `APUs` → Vigente) y la tabla de rutas de
  `PLAN §3.4.3`.
- Registrar `T-U-21`…`T-U-24`, `T-I-42`…`T-I-45`, `T-S-18`, `T-S-19`, `T-C-05` y `T-A-04` en `PLAN §5.6.8`.
- Revisar `D-121` (decimales) y la revisión de `R-21`, `D-06`, `D-32`, `D-33`, `D-49`, `D-114`, `D-119` y
  `X-15` en el `PLAN`.
