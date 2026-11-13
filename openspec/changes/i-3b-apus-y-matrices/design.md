# Design: i-3b-apus-y-matrices

## Context

`E-1b` y `E-3a` dejaron la línea base de identidad, proyectos, insumos y proveedores
(`openspec/specs/identidad`, `proyectos`, `insumos`, `proveedores`). El código tiene `Resource`,
`ResourceType` con los siete tipos, `Supplier`, `Project` y `ProjectMember`; **no** tiene `Apu`,
`ApuResource`, `MatrixComponent`, `ApuApproval` ni entidades de EDT, y **no existe migración con tablas
`wbs*`** (verificado en `BudgetCivilDbContextModelSnapshot` y en `Persistence/Migrations`). La
autorización se resuelve por `permission_key` con `SecurityBaselineSeeder` (versión `v6`), el alcance por
proyecto con el rol del actor y `ProjectMember`, y los listados usan `ListingQueryPipeline` para
`page`/`pageSize`.

`E-3b` depende de `E-2` (proyecto y EDT). Como `E-2` no está implementado, este diseño **asume** que
`WbsItem` y `WbsCode` existirán con los campos de `PLAN §3.3.2` y deja el ajuste de `INC-02` como
precondición explícita de la migración. Ver `proposal.md` para la motivación.

## Goals / Non-Goals

**Goals:**

- Modelar `Apu`, `ApuResource` y `MatrixComponent` con autoría, `rowversion` y baja lógica reservada.
- Exponer APUs, composición de Matrices y conteo de uso bajo rutas con alcance de proyecto.
- Resolver el vínculo 1:1 con el ítem del EDT sin acoplar `M-05` a la infraestructura de `M-03`.
- Implementar la copia de APU y de Matriz con reconciliación de insumos en una sola transacción.
- Añadir permisos y cubrir con pruebas unitarias, de integración manual y de arquitectura.

**Non-Goals:**

- Costo directo, AIU, redondeo y cascada de recálculo (`RF-10`, `BR-003`, `X-15`, `E-4a`).
- Transición a `COTIZADO` y cambio masivo (`RF-11`, `E-4a`).
- `ApuApproval`, firma, cierre e invalidación de `D-53` (`RF-12`, `RF-13`, `RF-21`, `E-4b`).
- Catálogo público y copia desde el catálogo (`RF-15`, `E-5`).
- EDT: capítulos, ítems, numeración y cantidades (`RF-04`, `E-2`).
- Endpoint de baja lógica, frontend y cliente Angular (`D-49`, `D-109`).

## Decisions

### D1. Modelo de dominio

`Apu` vive en `BudgetCivil.Domain/Apus` y solo se crea o modifica por métodos que fijan el vínculo, el
estado y la autoría; no se exponen setters públicos para `ProjectId`, `Status`, `RowVersion`,
`LastModifiedByUserId` ni `LastModifiedAtUtc`.

```text
Apu
  Id, ProjectId, Description, Unit, Yield
  Status (COMPLETO | FALTA_INFORMACION)
  LastModifiedByUserId, LastModifiedAtUtc, RowVersion
  IsActive (baja lógica reservada, sin endpoint en E-3b)
  Lines: IReadOnlyCollection<ApuResource>

ApuResource
  Id, ApuId, ResourceId, Consumption, WastePercentage

MatrixComponent
  Id, MatrixResourceId (Resource de tipo MATRIX), ComponentResourceId, Consumption, WastePercentage
```

- `Apu.CalculateStatus()`: `COMPLETO` cuando `Description` y `Unit` no están vacías, `Yield > 0` y existe
  al menos una línea; en caso contrario `FALTA_INFORMACION` (`05 §3`, `INC-06`).
- `ApuResource` y `MatrixComponent` validan `Consumption > 0` y `WastePercentage` entre 0 y 100 en el
  constructor; el dominio es la única fuente de la regla y la base solo la refuerza con `CHECK`.
- No hay restricción única sobre (`apu_id`, `resource_id`) ni (`matrix_resource_id`,
  `component_resource_id`): la unicidad de insumos es una sugerencia (`Q-09`, `D-16`) y ninguna fuente
  prohíbe repetir una línea.
- La Matriz es un `Resource` de tipo `MATRIX` (`D-05`, `BR-008`): no es una entidad nueva y su composición
  se guarda en `MatrixComponent` (`D-48`), nunca en `ApuResource`.

### D2. Persistencia

Tablas nuevas con nombres `snake_case`, esquema único y prefijos `PK_`/`FK_`/`IX_`/`UQ_`/`CK_`
(`D-95`, `D-107`).

| Tabla `apus` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `projects` `RESTRICT` |
| `description` | NVARCHAR(MAX) | No | No vacía |
| `unit` | VARCHAR(20) | No | No vacía |
| `yield` | DECIMAL(18,4) | No | `CK_apus_yield`: `yield > 0` |
| `status` | VARCHAR(30) | No | `CK_apus_status`: `COMPLETO`, `FALTA_INFORMACION` |
| `last_modified_by` | UUID | No | FK `users` `RESTRICT` |
| `last_modified_at` | DATETIME2 | No | UTC |
| `row_version` | ROWVERSION | No | `D-50`, `D-97` |
| `is_active` | BIT | No | Default `1`; filtro global (`D-49`) |
| `deleted_at` | DATETIME2 | Sí | Baja lógica reservada |

| Tabla `apu_resources` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `apu_id` | UUID | No | FK `apus` `CASCADE` (hijo de composición) |
| `resource_id` | UUID | No | FK `resources` `RESTRICT` |
| `consumption` | DECIMAL(18,4) | No | `CK_apu_resources_consumption`: `> 0` |
| `waste_percentage` | DECIMAL(5,2) | No | `CK_apu_resources_waste`: `0–100` |

| Tabla `matrix_components` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `matrix_resource_id` | UUID | No | FK `resources` `CASCADE`; debe ser tipo `MATRIX` |
| `component_resource_id` | UUID | No | FK `resources` `RESTRICT`; no puede ser tipo `MATRIX` |
| `consumption` | DECIMAL(18,4) | No | `CK_matrix_components_consumption`: `> 0` |
| `waste_percentage` | DECIMAL(5,2) | No | `CK_matrix_components_waste`: `0–100` |

- El tipo `MATRIX` de `matrix_resource_id` y la prohibición de anidar se validan en el dominio; la base no
  puede expresarlo con un `CHECK` porque depende de otra tabla (`BR-008`).
- Índices: `IX_apus_project_id_status`, `IX_apu_resources_apu_id`, `IX_apu_resources_resource_id`,
  `IX_matrix_components_matrix_resource_id`, `IX_matrix_components_component_resource_id`
  (`PLAN §3.3.5`).
- `description` es `NVARCHAR(MAX)` (`05 §3`): la búsqueda por descripción se resuelve dentro del proyecto y
  sin índice; la búsqueda por código usa el índice de `wbs_codes` (`D-30`, `D-51`).
- La tabla `wbs_items` requiere el ajuste de `D3` (`INC-02`).

### D3. Vínculo con el EDT sin acoplar módulos

- `M-05` no usa `DbContext` ni repositorios de `M-03`. Declara en `Application/Apus/Repositories` un
  contrato de lectura `IWbsItemReader` implementado en `Infrastructure`, con tres operaciones: obtener el
  ítem de un proyecto, obtener el ítem vinculado a un APU y obtener las cantidades de obra del proyecto.
- La creación del APU exige `wbsItemId`; dentro de la misma transacción se valida que el ítem pertenezca
  al proyecto y que no tenga APU y se fija la relación. Así un APU nunca existe sin ítem y un ítem nunca
  tiene dos APUs (`BR-016`, `REQ-APUS-01`).
- `wbs_items.apu_id` se propone **nullable** con índice único filtrado `UQ_wbs_items_apu_id`
  (`WHERE apu_id IS NOT NULL`), porque `E-2` crea el ítem antes de que exista ningún APU (`INC-02`). Sin
  este ajuste, `E-2` no podría crear ítems y `E-3b` no podría vincularlos.
- El código del ítem no se copia al APU (`D-31`): el detalle lo devuelve desde `IWbsItemReader`.


### D4. Composición y estado

- Agregar, editar o retirar una línea del APU recalcula `Apu.Status` y actualiza la autoría del APU; las
  líneas no llevan autoría propia porque `ApuResource` no la declara (`PLAN §3.3.2`, `RF-14`).
- Agregar, editar o retirar un componente de una Matriz actualiza la autoría del `Resource` Matriz
  (`last_modified_by`, `last_modified_at`) y su `quotation_status` según `REQ-APUS-06`; la operación
  reutiliza el `rowversion` del insumo (`D-50`).
- La composición de un APU admite cualquier insumo activo del proyecto, incluida una Matriz; la composición
  de una Matriz admite cualquier insumo activo del proyecto **excepto** otra Matriz y ella misma
  (`BR-008`, `REQ-APUS-07`).
- El retiro de una línea es físico (`DELETE` de la fila de composición), no baja lógica: `D-49` solo
  alcanza a `Project`, `Apu` y `Resource`.

### D5. Superficie de API

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/projects/{projectId}/apus` | `APUS_READ` | Listado con búsqueda, filtro por estado y paginación |
| `GET` | `/api/v1/projects/{projectId}/apus/{apuId}` | `APUS_READ` | Detalle con composición, código y cantidad del ítem |
| `POST` | `/api/v1/projects/{projectId}/apus` | `APUS_WRITE` | Alta vinculada a un `wbsItemId` |
| `PUT` | `/api/v1/projects/{projectId}/apus/{apuId}` | `APUS_WRITE` | Edición de descripción, unidad y rendimiento |
| `POST` | `/api/v1/projects/{projectId}/apus/{apuId}/resources` | `APUS_WRITE` | Alta de una línea de composición |
| `PUT` | `/api/v1/projects/{projectId}/apus/{apuId}/resources/{lineId}` | `APUS_WRITE` | Edición de una línea |
| `DELETE` | `/api/v1/projects/{projectId}/apus/{apuId}/resources/{lineId}` | `APUS_WRITE` | Retiro de una línea con `rowVersion` en la consulta |
| `GET` | `/api/v1/projects/{projectId}/apus/copy/preview` | `APUS_WRITE` | Reconciliación previa a la copia de un APU |
| `POST` | `/api/v1/projects/{projectId}/apus/copy` | `APUS_WRITE` | Copia de un APU con decisiones por insumo |
| `GET` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components` | `APUS_READ` | Composición de la Matriz |
| `POST` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components` | `APUS_WRITE` | Alta de un componente |
| `PUT` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components/{componentId}` | `APUS_WRITE` | Edición de un componente |
| `DELETE` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components/{componentId}` | `APUS_WRITE` | Retiro de un componente con `rowVersion` en la consulta |
| `GET` | `/api/v1/projects/{projectId}/matrices/copy/preview` | `APUS_WRITE` | Reconciliación previa a la copia de una Matriz |
| `POST` | `/api/v1/projects/{projectId}/matrices/copy` | `APUS_WRITE` | Copia de una Matriz con decisiones por componente |
| `GET` | `/api/v1/projects/{projectId}/resource-usage` | `RESOURCES_READ` | Conteo de uso por insumo, con búsqueda, tipo y paginación |

- `CreateApuRequest` solo lleva `wbsItemId`, `description`, `unit` y `yield`; el proyecto sale de la ruta.
- `UpdateApuRequest` lleva `description`, `unit`, `yield` y `rowVersion`; el vínculo y el proyecto no se
  pueden cambiar.
- `AddApuResourceRequest` y `UpdateApuResourceRequest` llevan `resourceId`, `consumption`,
  `wastePercentage` y `rowVersion`; el retiro envía `rowVersion` por *query string* porque `DELETE` no
  transporta cuerpo en todos los clientes.
- Las respuestas de listado usan `items`, `page`, `pageSize`, `totalItems`; `rowVersion` viaja en Base64
  (`D-101`).
- El endpoint genérico `POST /projects/{projectId}/resources/copy` **rechaza** el tipo `MATRIX` con
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION` y remite a `POST .../matrices/copy`
  (`REQ-INSUMOS-09`).
- Las rutas siguen el alcance de proyecto del contrato vigente y no la tabla `/apus` de `PLAN §3.4.3`
  (`INC-03`).

### D6. Autorización y alcance

- Permisos nuevos en `SecurityBaselineSeeder` (subir `SeedVersion` a `v7`): `APUS_READ` y `APUS_WRITE`.
- Asignación: Administrador → `APUS_READ`; Director → `APUS_READ`, `APUS_WRITE`; Presupuestador →
  `APUS_READ`, `APUS_WRITE`; Auditor → `APUS_READ`. El conteo de uso usa `RESOURCES_READ`, que ya está
  asignado a los cuatro roles.
- El permiso se exige en el endpoint (`PermissionAuthorizationHandler`); el **alcance** se resuelve en el
  caso de uso con `IActorAccessReader` y el rol, igual que `M-04`:
  - Administrador: lectura de cualquier proyecto, sin escritura.
  - Director: lectura y escritura en proyectos de su compañía.
  - Presupuestador: lectura y escritura solo con `ProjectMember` activo.
  - Auditor: lectura solo con `ProjectMember` activo.
- `M-05` define `IApuProjectAccessReader` en `Application/Apus/Repositories`, implementado en
  `Infrastructure`; no consulta repositorios ni `DbContext` de `M-02` (`PLAN §3.4.1`, `§3.4.6`).
- Política *fail-closed*: rol inactivo o permiso ausente ⇒ `403` y evento de seguridad (`D-82`, `D-111`).


### D7. Consultas de lectura

- Listado de APUs: `search` parcial sobre `description` o sobre el código del ítem, `status` dentro del
  conjunto cerrado, orden por descripción, estado, ítem o última modificación, y paginación con
  `ListingQueryPipeline` (`D-101`). La búsqueda por código se resuelve con `IWbsItemReader`, que devuelve
  los identificadores de APU cuyo ítem coincide; el listado combina ese conjunto con el filtro de texto.
- Detalle: APU con sus líneas, tipo y descripción del insumo, y el código y la cantidad de obra del ítem.
- Conteo de uso (`RF-22`, `D-33`, `D-47`): proyección de EF Core en `Application/Calculation`, sin
  duplicar la fórmula del APU y sin `DbContext` en la API.
  1. Consumo directo: `Σ(apu_resources.consumption × wbs_items.quantity_work)` agrupado por
     `resource_id`, para los APUs activos del proyecto (`R-21`).
  2. Expansión de un nivel: `Σ(matrix_components.consumption × apu_resources.consumption ×
     wbs_items.quantity_work)` para las líneas del APU cuyo `resource_id` es una Matriz, agrupado por
     `component_resource_id` (`REQ-APUS-13`, `BR-008`).
  3. El resultado suma ambos componentes; la Matriz conserva además su consumo directo.
- El conteo **no** aplica `waste_percentage` (`R-21`) y **no** expande más de un nivel (`BR-008`).
- La consulta devuelve solo insumos activos del proyecto, con `usageCount` en `0` cuando no participan;
  el filtro por tipo y la búsqueda se resuelven en la misma proyección antes de paginar.
- `ResourceDto` de `M-04` **no** cambia: `D-33` asigna la consulta a `M-06` y evita que `M-04` lea datos
  de `M-05`.

### D8. Copia y reconciliación

Vista previa (`GET .../copy/preview`):

```text
{ sourceApuId | sourceMatrixId, targetWbsItemId?, items: [
    { sourceResourceId, description, unit, resourceTypeCode,
      match: EXACT | SIMILAR | NONE,
      candidates: [ { resourceId, description, unitPrice, quotationStatus } ] } ] }
```

Copia (`POST .../copy`):

```text
{ sourceApuId | sourceMatrixId, targetWbsItemId?,
  resolutions: [ { sourceResourceId, decision: USE_EXISTING | KEEP_ORIGIN, existingResourceId? } ] }
```

1. Resuelve el proyecto destino y exige escritura; en la copia de APU, exige que `targetWbsItemId`
   pertenezca al proyecto y no tenga APU.
2. Carga el origen y exige lectura sobre su proyecto. Si es de otra compañía responde
   `APU_NOT_FOUND`/`MATRIX_NOT_FOUND`, sin revelar existencia (`D-13`, `BR-018`).
3. Clasifica cada insumo del origen: `EXACT` si coincide descripción normalizada y unidad; `SIMILAR` si
   la descripción coincide parcialmente o el `user_key` es igual; `NONE` en otro caso. El criterio es el
   de `REQ-INSUMOS-05` (`INC-05`).
4. `NONE` se agrega siempre como insumo nuevo. `EXACT` y `SIMILAR` exigen una `resolution`; si falta, la
   operación responde `APU_COMPOSITION_RESOLUTION_REQUIRED` y no crea nada (`D-25`).
5. `USE_EXISTING` exige que `existingResourceId` pertenezca al destino, esté activo y sea compatible con
   el tipo del origen; la línea apunta a ese insumo y conserva su precio.
6. `KEEP_ORIGIN` crea un insumo nuevo en el destino con los datos del origen, `origin_resource_id`
   apuntando al origen y estado recalculado por completitud (`D-29`).
7. Crea el APU con descripción, unidad, rendimiento y líneas resueltas (o la Matriz con sus componentes),
   sin aprobación, con el actor como último modificador y estado recalculado.
8. Todo ocurre en una transacción (`BR-014`): un fallo no deja APU, Matriz ni insumos parciales.

- `M-05` no escribe la tabla `resources` con su propio `DbContext`: declara `IApuResourceCopyWriter` en
  `Application/Apus/Repositories` (crear insumo copiado, crear Matriz copiada, leer insumo) y
  `Infrastructure` lo implementa sobre el mismo `DbContext` para compartir la transacción.
- La copia de una Matriz usa el mismo mecanismo con sus componentes y **no** reutiliza
  `CopyResourceCommandHandler`, que rechaza el tipo `MATRIX` (`REQ-INSUMOS-09`, `INC-10`).
- La copia desde el catálogo público queda fuera: `E-5` reutilizará esta operación añadiendo el origen
  público.


### D9. Concurrencia y errores

- La edición del APU y cualquier cambio de composición comparan `rowversion` del APU; si no coincide,
  `ConflictAppException` con `APU_CONCURRENCY_CONFLICT` y respuesta `409`.
- La composición de una Matriz compara el `rowversion` del `Resource` Matriz y responde
  `MATRIX_CONCURRENCY_CONFLICT` con `409`.
- El alta no acepta `rowVersion`; el retiro sí lo exige por *query string*.
- El middleware existente traduce a `ProblemDetails` con `extensions.code` (`D-100`). Códigos previstos:
  `APU_NOT_FOUND`, `APU_READ_FORBIDDEN`, `APU_WRITE_FORBIDDEN`, `APU_ITEM_NOT_FOUND`,
  `APU_ITEM_ALREADY_LINKED`, `APU_RESOURCE_NOT_FOUND`, `APU_RESOURCE_PROJECT_MISMATCH`,
  `APU_CONCURRENCY_CONFLICT`, `APU_COMPOSITION_RESOLUTION_REQUIRED`, `MATRIX_NOT_FOUND`,
  `MATRIX_TYPE_INVALID`, `MATRIX_NESTING_FORBIDDEN`, `MATRIX_CONCURRENCY_CONFLICT`,
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`,
  `PROJECT_NOT_FOUND`.

### D10. Migración y datos iniciales

- Una migración nueva `YYYYMMDDHHMM_apus_add_e3b` con `apus`, `apu_resources`, `matrix_components`, FKs,
  `CHECK` e índices (`D-107`), y el ajuste de `wbs_items.apu_id` + `UQ_wbs_items_apu_id` cuando `E-2`
  exista (`INC-02`).
- No migra datos: las tablas nacen vacías y no hay APUs previos.
- `SecurityBaselineSeeder` sube a `v7` con `APUS_READ` y `APUS_WRITE`; el inventario esperado pasa a 35
  permisos y las asignaciones se recalculan por diferencia, sin borrar ni recrear roles (`D-82`).
- Reversión: `Down` elimina índices, FKs y tablas en orden inverso; el `seed` es idempotente y no requiere
  reversión.

### D11. Pruebas

- **Unitarias** (`Tests.Unit`): completitud y estado, reglas de composición, no anidamiento, expansión del
  conteo, decisiones de reconciliación, autoría y alcance por rol.
- **Integración manual** (`Tests.Integration`, SQL Server local, `D-66`): FKs, `CHECK`, índices,
  `rowversion`, copia transaccional, reconciliación e independencia, conteo de uso y aislamiento entre dos
  compañías.
- **Contrato** (`T-C-04`): `camelCase`, `ProblemDetails` y paginación en las rutas nuevas.
- **Arquitectura** (`T-A-03`): `Domain` sin EF, `Application` sin `DbContext`, `Api` sin persistencia y
  `M-05` sin infraestructura de `M-02`, `M-03` ni `M-04`.

## Inconsistencias detectadas

| ID | Inconsistencia | Fuentes en conflicto | Resolución de este cambio | Estado |
|---|---|---|---|---|
| `INC-01` | `I-3b` se declara dependiente solo de `I-3a`, pero `RF-07`/`BR-016` exigen el ítem del EDT y `RF-22`/`R-21` exigen `quantity_work`. En el código **no existen** entidades ni migraciones `wbs*`: `E-2` no está implementado | `PLAN §2.4.2`, `§4.2` ↔ `RF-07`, `RF-22`, `BR-016`, `R-21`; código actual | Se declara la dependencia de `E-2` en `proposal.md` y la tarea `1.1` bloquea la implementación hasta su cierre | **BLOQUEANTE** (documentado) |
| `INC-02` | `wbs_items.apu_id` es obligatorio y único, pero `E-2` crea el ítem antes de que exista ningún APU: ningún ítem podría crearse | `PLAN §3.3.3`, `05 §4` ↔ orden `E-2` → `E-3b` | `apu_id` nullable + `UQ_wbs_items_apu_id` filtrado; el vínculo se garantiza en el alta del APU (`REQ-APUS-01`) | **POR CONFIRMAR** (ajustar `PLAN §3.3.3` y la futura spec `edt`) |
| `INC-03` | El `PLAN` propone `/apus`, `/apus/{id}/resources` y `/apus/{id}/copy`, sin alcance de proyecto, mientras el APU es propio de un proyecto y el contrato v1 ya implementado es *project-scoped* | `PLAN §3.4.3`, `02 §5` ↔ `D-05`, `R-01`, `D-99` | Rutas bajo `/api/v1/projects/{projectId}/...`; se corrige la tabla del `PLAN` al cerrar la etapa | Resuelto en este cambio |
| `INC-04` | `R-21`/`D-33` definen el conteo como `consumo × cantidad` sin decir si expande Matrices ni si aplica desperdicio; `UC-05` sí expande un nivel para el costo | `R-21`, `D-33` ↔ `BR-008`, `UC-05`, `RF-10` | Conteo literal sin desperdicio y con expansión de un nivel de la Matriz | **DERIVADO** (confirmar y precisar `R-21`) |
| `INC-05` | `D-25` exige detectar similitudes pero no define el criterio | `D-25` ↔ `REQ-INSUMOS-05` | Se reutiliza el criterio ya especificado: descripción parcial o `user_key` igual | **DERIVADO** (documentado) |
| `INC-06` | `PLAN §3.3.2` lista `Apu.status` sin conjunto cerrado; `05 §3` fija `COMPLETO`/`FALTA_INFORMACION` y no define cuándo aplica cada uno | `PLAN §3.3.2` ↔ `05 §3` | Se adopta el conjunto de `05 §3` y se define la completitud en `REQ-APUS-03` | **DERIVADO** (confirmar la regla) |
| `INC-07` | `D-53` exige que editar un APU aprobado elimine su aprobación, pero `ApuApproval` y la firma son de `E-4b` | `D-53` ↔ `PLAN §4.2` | `E-3b` no crea ni consulta aprobaciones; los endpoints de composición son los puntos de invalidación que `E-4b` usará | Resuelto por etapas |
| `INC-08` | `BR-009` exige precio, marca y proveedor para salir de `FALTA_INFORMACION`, lo que dejaría toda Matriz incompleta y bloquearía el paso del proyecto a `COTIZADO` | `BR-009`, `REQ-INSUMOS-04` ↔ `BR-008`, `BR-021` | Se modifica `REQ-INSUMOS-04` y la completitud de la Matriz se define en `REQ-APUS-06` | Resuelto en este cambio |
| `INC-09` | `D-05` dice que el APU "se crea desde cero", lo que podría leerse como APU sin ítem, pero `BR-016` y `D-16` fijan 1:1 | `D-05` ↔ `BR-016`, `D-16` | El alta siempre exige un ítem libre del proyecto; "desde cero" se interpreta como "sin copiar" | **DERIVADO** (confirmado por `UC-05`) |
| `INC-10` | `E-3a` copia un insumo de tipo Matriz como insumo simple, mientras `BR-021` exige reconciliar sus componentes; dos operaciones producirían Matrices diferentes | `E-3a` (`REQ-INSUMOS-09`) ↔ `BR-021`, `UC-05` | Se modifica `REQ-INSUMOS-09`, la copia genérica rechaza `MATRIX` y la copia de Matriz reconcilia sus componentes | Resuelto en este cambio |
| `INC-11` | El APU guarda un único `yield`, pero `D-06`/`D-32` piden una unidad de esfuerzo configurable para mano de obra y no existe campo en `ProjectConfiguration` | `D-06`, `D-32` ↔ `PLAN §3.3.2`, `X-15` | `E-3b` solo almacena `yield > 0`; la unidad de esfuerzo y la fórmula pertenecen a `E-4a` | **POR CONFIRMAR** (`X-15`) |
| `INC-12` | `PLAN §5.6.1` asigna pruebas a `RF-07`…`RF-22`, pero no cubre la Matriz ni la copia de Matriz, y no existe la sección de verificación de `E-3b` | `PLAN §5.6.1`, `§5.6.6` ↔ `RF-09`, `BR-021` | Se proponen `T-U-18`…`T-U-20`, `T-I-39`…`T-I-41`, `T-S-16`, `T-S-17`, `T-C-04` y `T-A-03` para `PLAN §5.6.7` | Resuelto como propuesta |
| `INC-13` | `PLAN §3.3.2` describe `Apu` sin `is_active`/`deleted_at`, pero `D-49` exige baja lógica en `Apu` y `R-02`/`PLAN §3.3.6` la dan por hecha | `PLAN §3.3.2` ↔ `D-49`, `§3.3.6` | Se añaden `is_active` y `deleted_at` a `apus`, sin endpoint de baja en `E-3b` | Resuelto en este cambio |
| `INC-14` | `PLAN §3.3.1` exige `project_id` en toda entidad con alcance de proyecto, incluso derivable, pero `apu_resources` y `matrix_components` solo se alcanzan por relación | `PLAN §3.3.1` ↔ `PLAN §3.3.2`, `D-48` | Se sigue `PLAN §3.3.2` y `D-48`: sin `project_id` redundante; el filtro por proyecto se resuelve por `apu_id` y `matrix_resource_id` con los índices del diseño | Resuelto en este cambio (documentar la excepción) |

## Risks / Trade-offs

| Riesgo | Mitigación |
|---|---|
| `E-2` no está implementado y `E-3b` no puede verificar el vínculo 1:1 | La tarea `1.1` bloquea la implementación; el diseño aísla la dependencia en `IWbsItemReader` para que `E-2` pueda llegar después sin rehacer `M-05` |
| `INC-02` exige cambiar la obligatoriedad de `wbs_items.apu_id` | Se documenta como precondición de la migración; si el estudiante decide mantener `NOT NULL`, `E-2` debe crear el ítem con su APU en la misma operación y este diseño cambia en un solo punto |
| Doble operación de copia de insumos (genérica y de Matriz) | La copia genérica rechaza `MATRIX` con un código de error explícito; una sola ruta válida por tipo |
| Reconciliación con muchos candidatos | La vista previa limita a diez candidatos por insumo y el cliente decide antes de copiar; la copia nunca adivina |
| Conteo con Matrices anidadas | `BR-008` prohíbe anidar, por lo que la expansión de un nivel es completa; el dominio lo impide en la composición |
| Búsqueda por descripción sin índice | Alcance por proyecto y volumen bajo; se revisa en `E-6` si crece (`RT-21`) |
| `apu_resources` sin `rowversion` | La concurrencia se controla con el `rowversion` del APU; toda mutación de composición lo exige (`D-50`) |
| Permisos compartidos con otros cambios en curso | El `seed` es versionado e idempotente; se sube a `v7` y se revisa el inventario antes de aplicar (`D-82`) |
| Cadena de migraciones con objetos ajenos a `E-3b` | Antes de generar, `dotnet ef migrations list` y revisión del snapshot; no se elimina ninguna migración |

## Migration Plan

1. Confirmar `INC-02` con el estudiante y verificar que `E-2` esté cerrado (`dotnet ef migrations list`).
2. Respaldo de la base y revisión de la cadena de migraciones (`D-40`).
3. Generar `YYYYMMDDHHMM_apus_add_e3b`, revisar el script (`dotnet ef migrations script`) y el `Down`.
4. Aplicar en ventana de mantenimiento; ejecutar el `seed` y verificar los 35 permisos y las asignaciones.
5. Verificar `CHECK`, FKs, índices, `rowversion` y el índice único filtrado de `wbs_items`.
6. Ejecutar la suite unitaria, las reglas de arquitectura y la integración manual con dos compañías.
7. Reversión: `Down` de la migración y restauración del respaldo si hubiera datos.

## Open Questions

- `INC-02`: ¿se acepta `wbs_items.apu_id` nullable con índice único filtrado, o `E-2` crea el ítem y su APU
  en la misma operación? Bloquea la migración de `E-2` y de `E-3b`.
- `INC-04`: ¿el conteo de uso debe expandir las Matrices, como aquí se especifica, o mantenerse literal
  como `consumo × cantidad` sin expansión? Afecta `RF-22` y la lectura de `R-21`.
- `INC-06`: ¿la completitud del APU exige al menos una línea, o basta con descripción, unidad y
  rendimiento?
- `INC-11`: ¿dónde se configura la unidad de esfuerzo de mano de obra (`D-06`, `D-32`) y cómo se refleja en
  `yield`? Pertenece a `E-4a`, pero condiciona el significado del campo que aquí se almacena.
- `E-4a` debe definir cómo una Matriz alcanza `COTIZADO` y si la edición de un insumo ya cotizado conserva
  el estado, porque `E-3b` solo produce `FALTA_INFORMACION` y `POR_COTIZAR`.

