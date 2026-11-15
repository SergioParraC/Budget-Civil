# Design: i-3b-apus-y-matrices

## Context

`E-1b`, `E-3a` y `E-2b` dejaron la línea base de identidad, proyectos, insumos, proveedores y EDT. El código
tiene `Resource`, `ResourceType` con los siete tipos, `Supplier`, `Project`, `ProjectMember`,
`ProjectConfiguration` y el módulo `M-03` (`Domain/Wbs`, `Application/Wbs`, `WbsRepository` y
`WbsController`); la especificación `edt` ya está sincronizada en `openspec/specs/edt/spec.md` y la
migración `20261114065800_wbs_add_i2b` crea `wbs_codes`, `wbs_chapters`, `wbs_items` y
`wbs_project_activity`, con `wbs_items.apu_id` **nullable**, el índice único filtrado
`UQ_wbs_items_apu_id` y **sin** clave foránea hacia `apus`. `SecurityBaselineSeeder` está en `v7` con 35
permisos, incluidos `WBS_READ` y `WBS_WRITE`. **No** existen `Apu`, `ApuResource` ni `MatrixComponent`.

`WbsItem` expone `ApuId` con setter privado y solo un método de prueba que lo anula; no hay forma de fijar
el vínculo desde `M-05`. La configuración del proyecto ya tiene los tres porcentajes del AIU, pero no los
decimales de redondeo ni de cantidades. `RF-10` sigue asignado a `E-4a` y `X-15` está parcialmente abierto;
este cambio solo define el costo unitario del APU y de la Matriz para la vista previa, no el total del
proyecto.

La dependencia `E-2` está cerrada. Lo que queda de `E-2b` para este cambio es añadir la clave foránea
`wbs_items.apu_id → apus`, el método de vinculación del ítem y la baja controlada del APU y de los capítulos
(`INC-15`, `INC-16`, `INC-17`).

## Goals / Non-Goals

**Goals:**

- Modelar `Apu`, `ApuResource` y `MatrixComponent` con autoría, `rowversion`, baja física y composición.
- Vincular el APU al ítem del EDT y eliminarlo con el ítem, conservando los insumos del proyecto.
- Exponer APUs de otros proyectos, composición de Matrices y conteo de uso bajo rutas con alcance de
  proyecto.
- Calcular la vista previa de costo directo, subtotales por tipo, AIU y total del APU, y el costo directo de
  la Matriz.
- Implementar la copia de APU y de Matriz con reconciliación solo de insumos simples.
- Añadir parámetros de redondeo y cantidades, permisos y pruebas.

**Non-Goals:**

- Costo total del proyecto, presupuesto consolidado, cascada de recálculo del presupuesto y alertas de tope
  (`RF-10` completo, `E-4a`).
- Transición a `COTIZADO` y cambio masivo (`RF-11`, `E-4a`).
- `ApuApproval`, firma, cierre e invalidación de `D-53` (`RF-12`, `RF-13`, `RF-21`, `E-4b`).
- Catálogo público y copia desde el catálogo (`RF-15`, `E-5`).
- Frontend y cliente Angular (`D-109`).

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
  Lines: IReadOnlyCollection<ApuResource>

ApuResource
  Id, ApuId, ResourceId, Consumption, WastePercentage

MatrixComponent
  Id, MatrixResourceId, ComponentResourceId, Consumption, WastePercentage
```

- La baja del APU es **física**: no hay `is_active` ni `deleted_at`; el ítem lo elimina junto con sus
  líneas (`D-49` revisada, `INC-13`). Los `resources` referenciados no se borran.
- `Apu.CalculateStatus()` recibe la completitud de las Matrices referenciadas por sus líneas: `COMPLETO`
  cuando `Description` y `Unit` no están vacías, `Yield > 0`, existe al menos una línea y toda línea de
  Matriz apunta a una Matriz con unidad y al menos un componente; si una Matriz está incompleta, el APU
  hereda `FALTA_INFORMACION` (`INC-06`); el precio no interviene en el estado. El costo y el precio no
  forman parte del estado persistido; la vista previa informa `FALTA_INFORMACION` cuando falta un precio
  (`REQ-APUS-19`).
- `ApuResource` y `MatrixComponent` validan `Consumption > 0` y `WastePercentage` entre 0 y 100; el valor
  por defecto del desperdicio es 0.
- La Matriz es un `Resource` de tipo `MATRIX` (`D-05`, `BR-008`): no es una entidad nueva y su composición
  se guarda en `MatrixComponent` (`D-48`), nunca en `ApuResource`.
- El vínculo con el EDT no vive en `apus`: se guarda en `wbs_items.apu_id` (`D-113`), y el APU siempre se
  elimina con el ítem.

### D2. Persistencia

Tablas nuevas con nombres `snake_case`, esquema único y prefijos `PK_`/`FK_`/`IX_`/`UQ_`/`CK_`
(`D-95`, `D-107`).

| Tabla `apus` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `projects` `RESTRICT` |
| `description` | NVARCHAR(MAX) | No | No vacía |
| `unit` | VARCHAR(5) | No | 1–5 caracteres |
| `yield` | DECIMAL(18,4) | No | `CK_apus_yield`: `yield > 0` |
| `status` | VARCHAR(30) | No | `CK_apus_status`: `COMPLETO`, `FALTA_INFORMACION` |
| `last_modified_by` | UUID | No | FK `users` `RESTRICT` |
| `last_modified_at` | DATETIME2 | No | UTC |
| `row_version` | ROWVERSION | No | `D-50`, `D-97` |

| Tabla `apu_resources` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `apu_id` | UUID | No | FK `apus` `CASCADE` |
| `resource_id` | UUID | No | FK `resources` `RESTRICT` |
| `consumption` | DECIMAL(18,10) | No | `CK_apu_resources_consumption`: `> 0` |
| `waste_percentage` | DECIMAL(5,2) | No | `CK_apu_resources_waste`: `0–100`; default `0` |

| Tabla `matrix_components` | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `matrix_resource_id` | UUID | No | FK `resources` `CASCADE`; debe ser tipo `MATRIX` |
| `component_resource_id` | UUID | No | FK `resources` `RESTRICT`; no puede ser tipo `MATRIX` |
| `consumption` | DECIMAL(18,10) | No | `CK_matrix_components_consumption`: `> 0` |
| `waste_percentage` | DECIMAL(5,2) | No | `CK_matrix_components_waste`: `0–100`; default `0` |

- `wbs_items.quantity_work` pasa a `DECIMAL(18,10)` y conserva su `CHECK`; `apu_resources.consumption` y
  `matrix_components.consumption` también. `yield` se mantiene en `DECIMAL(18,4)` y se usa como factor de
  cantidad; el producto se redondea en `quantity_decimals` (`D-121`).
- `wbs_items` no tiene unidad propia: `quantity_work` se interpreta en `apus.unit` del APU vinculado y
  `GET /wbs` expone esa unidad (`REQ-EDT-20`). Cambiar la unidad después de capturar la cantidad reinterpreta
  el valor; el bloqueo o la confirmación explícita se evalúa en `E-4a`.
- `project_configurations` incorpora `rounding_decimals` y `quantity_decimals` (`INT`, `CHECK` entre 0 y
  10, default 2); `D-121` fija el redondeo `AwayFromZero` y su aplicación a cada resultado intermedio.
- `wbs_items.apu_id` ya es nullable y su índice único filtrado existe desde `wbs_add_i2b`; este cambio solo
  añade `FK_wbs_items_apus_apu_id` (`RESTRICT`).
- Índices: `IX_apus_project_id_status`, `IX_apu_resources_apu_id`, `IX_apu_resources_resource_id`,
  `IX_matrix_components_matrix_resource_id`, `IX_matrix_components_component_resource_id`
  (`PLAN §3.3.5`).

### D3. Vínculo con el EDT y baja

- `M-05` no usa `DbContext` ni repositorios de `M-03`. Declara en `Application/Apus/Repositories`
  `IWbsItemReader` (ítem por proyecto, ítem por APU y cantidades del proyecto) e `IWbsItemLinkWriter`
  (`TryLinkFreeItem(itemId, apuId)`), implementados en `Infrastructure`.
- `WbsItem` gana `LinkApu(Guid apuId)`, que exige un APU no vacío y un ítem sin APU; la asignación sigue
  siendo privada y el cliente nunca fija `apuId` (`REQ-EDT-20`). La vinculación usa una actualización
  condicional `WHERE apu_id IS NULL` después de persistir el APU, dentro de la misma transacción.
- La baja del ítem en el EDT invoca `IWbsItemApuCleaner.DeleteApusForItems(ids)` en
  `Application/Wbs/Repositories`, implementado en `Infrastructure`; elimina los `apus` referenciados y sus
  `apu_resources` en la misma transacción. La FK del ítem hacia el APU es `RESTRICT`, así que EF elimina
  primero el ítem y después el APU. Los `resources` no se tocan.
- La baja de un capítulo solo se permite si no tiene subcapítulos ni ítems. Antes de responder, el manejador
  consulta `IWbsItemApuCleaner.HasApusInSubtree(projectId, chapterId)`: si hay APUs en cualquier
  descendiente, `409 WBS_CHAPTER_HAS_APU`; si hay otros nodos, `409 WBS_NODE_HAS_CHILDREN`; si está vacío,
  elimina el capítulo y su código. Se retira `cascade` del contrato y el borrado en cascada (`REQ-EDT-21`).

### D4. Composición, estado y costo

- Agregar, editar o retirar una línea del APU recalcula `Apu.Status` y actualiza la autoría del APU; las
  líneas no llevan autoría propia.
- Cambiar la unidad o la composición de una Matriz recalcula, en la misma transacción, el `status` de todos
  los APUs del proyecto que la usan: el APU hereda la completitud de la Matriz. La composición se atiende en
  `M-05` y la unidad en `M-04`, que declara `IMatrixApuStatusUpdater` en
  `Application/Resources/Repositories`, implementado en `Infrastructure`. La propagación es de un solo nivel
  (una Matriz no contiene Matrices), no altera la autoría de los APUs y no invalida aprobaciones, que
  pertenecen a `E-4b` (`D-53`).
- Agregar, editar o retirar un componente de una Matriz actualiza la autoría del `Resource` Matriz y su
  `quotation_status` según `REQ-APUS-06`; la operación reutiliza el `rowversion` del insumo.
- La composición de un APU admite cualquier insumo activo del proyecto, incluida una Matriz; la de una
  Matriz admite cualquier insumo activo del proyecto **excepto** otra Matriz y ella misma (`BR-008`).
- El retiro de una línea o componente es físico (`DELETE`).

### D5. Superficie de API

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/projects/{projectId}/apus` | `APUS_READ` | APUs de otros proyectos de la compañía, con búsqueda, ubicación y paginación |
| `GET` | `/api/v1/projects/{projectId}/apus/{apuId}` | `APUS_READ` | Detalle con composición y vista previa de costo |
| `POST` | `/api/v1/projects/{projectId}/apus` | `APUS_WRITE` | Alta vinculada a un `wbsItemId` libre |
| `PUT` | `/api/v1/projects/{projectId}/apus/{apuId}` | `APUS_WRITE` | Edición de descripción, unidad y rendimiento |
| `POST` | `/api/v1/projects/{projectId}/apus/{apuId}/resources` | `APUS_WRITE` | Alta de una línea |
| `PUT` | `/api/v1/projects/{projectId}/apus/{apuId}/resources/{lineId}` | `APUS_WRITE` | Edición de una línea |
| `DELETE` | `/api/v1/projects/{projectId}/apus/{apuId}/resources/{lineId}?rowVersion=...` | `APUS_WRITE` | Retiro de una línea |
| `GET` | `/api/v1/projects/{projectId}/apus/copy/preview` | `APUS_WRITE` | Reconciliación de insumos simples |
| `POST` | `/api/v1/projects/{projectId}/apus/copy` | `APUS_WRITE` | Copia de un APU con decisiones por insumo simple |
| `GET` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components` | `APUS_READ` | Composición y costo directo de la Matriz |
| `POST` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components` | `APUS_WRITE` | Alta de un componente |
| `PUT` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components/{componentId}` | `APUS_WRITE` | Edición de un componente |
| `DELETE` | `/api/v1/projects/{projectId}/matrices/{matrixResourceId}/components/{componentId}?rowVersion=...` | `APUS_WRITE` | Retiro de un componente |
| `POST` | `/api/v1/projects/{projectId}/matrices/copy` | `APUS_WRITE` | Copia de una Matriz sin validación |
| `GET` | `/api/v1/projects/{projectId}/resource-usage` | `RESOURCES_READ` | Conteo de uso por insumo |

- El listado `/apus` **excluye** el proyecto de la ruta y acepta `search` (descripción parcial o código del
  ítem), `departmentId`, `municipalityId`, `page` y `pageSize`; devuelve `apuId`, `code`, `description`,
  `unit`, `unitPrice` (costo directo unitario, con `yield`), `projectId`, `projectName`, `municipalityName` y
  `departmentName`; no devuelve `quantityWork` ni AIU.
- El detalle del APU devuelve `description` y `resources` agrupado por `resourceType`, con el `subtotal` de
  cada insumo; fuera de `resources` devuelve `directCost`, `indirectCosts` y `total`, todos por unidad de
  APU (el valor del ítem en el presupuesto se calcula en `E-4a`). Para un APU de otro proyecto usa el AIU del
  proyecto de la ruta y oculta `quantityWork`.
- La composición de la Matriz devuelve `description` y `resources` agrupado por `resourceType`, con
  `subtotal` por insumo y `directCost` fuera de `resources`, sin `indirectCosts`.
- Las rutas y los campos de respuesta quedan en inglés: `apus`, `matrices`, `components`, `resources`,
  `subtotal`, `directCost`, `indirectCosts` y `total`.
- `GET /wbs` expone por ítem `apu: { id, status, description, unit }` o `null`; es la vía para listar los
  APUs del proyecto.
- `CreateApuRequest` lleva `wbsItemId`, `description`, `unit` y `yield`; `UpdateApuRequest` lleva
  `description`, `unit`, `yield` y `rowVersion`.
- `AddApuResourceRequest` y `UpdateApuResourceRequest` llevan `resourceId`, `consumption`,
  `wastePercentage` (default 0) y `rowVersion`; el retiro envía `rowVersion` por *query string*.
- Las respuestas de listado usan `items`, `page`, `pageSize`, `totalItems`; `rowVersion` viaja en Base64
  (`D-101`).
- El endpoint genérico `POST /projects/{projectId}/resources/copy` **rechaza** el tipo `MATRIX` con
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION` (`REQ-INSUMOS-09`).

### D6. Autorización y alcance

- Permisos nuevos en `SecurityBaselineSeeder` (`v8`, 37 permisos): `APUS_READ` y `APUS_WRITE`.
- Asignación: Administrador → `APUS_READ`; Director → `APUS_READ`, `APUS_WRITE`; Presupuestador →
  `APUS_READ`, `APUS_WRITE`; Auditor → `APUS_READ`. El conteo de uso usa `RESOURCES_READ`.
- Lectura: Administrador cualquier proyecto; Director y Presupuestador cualquier APU de su compañía;
  Auditor solo sus proyectos asignados. La lectura de APUs de otros proyectos **no** habilita el árbol ni
  el EDT de origen.
- Escritura: Director y Presupuestador **solo** con `ProjectMember` activo en el proyecto del APU; el
  Administrador y el Auditor no escriben. La copia exige lectura sobre el origen y escritura sobre el
  destino.
- La edición se rige por `apus.project_id`; el dueño del proyecto (`DirectorOwnerUserId`) no cuenta como
  relacionado si no tiene fila en `ProjectMember` (`INC-20`).
- Política *fail-closed*: rol inactivo o permiso ausente ⇒ `403` y evento de seguridad (`D-82`, `D-111`).

### D7. Consultas de lectura

- Listado de APUs ajenos: búsqueda parcial por `description` o por código del ítem (`wbs_items`), filtros
  opcionales `departmentId` y `municipalityId` del proyecto de origen, y paginación `page`/`pageSize`; sin
  filtro por estado. `search` es OR entre descripción y código y se combina con los filtros de ubicación
  mediante AND. La ubicación se toma del `Project` de origen (`MunicipalityId` y su `DepartmentId`). El
  costo unitario se calcula con la misma fórmula de `D8` y el redondeo del proyecto de la ruta.
- Detalle: composición con tipo y descripción del insumo, y el código y la cantidad de obra del ítem
  (esta última solo si el APU pertenece al proyecto de la ruta).
- `GET /wbs`: el árbol incluye la referencia del APU por ítem mediante `IApuSummaryReader` en
  `Application/Wbs/Repositories`, implementado en `Infrastructure`; una sola consulta por proyecto.
- Conteo de uso (`RF-22`, `D-33`, `D-47`): proyección de EF Core en `Application/Calculation`, sin
  duplicar la fórmula del APU y sin `DbContext` en la API. Usa el consumo efectivo (consumo con desperdicio)
  multiplicado por la cantidad de obra; `yield` no interviene porque el conteo es una cantidad física:
  1. Consumo directo: `Σ(apu_resources.consumption × (1 + apu_resources.waste_percentage/100) ×
     wbs_items.quantity_work)` agrupado por `resource_id`.
  2. Expansión de un nivel: `Σ(matrix_components.consumption ×
     (1 + matrix_components.waste_percentage/100) × apu_resources.consumption ×
     (1 + apu_resources.waste_percentage/100) × wbs_items.quantity_work)` para las líneas de Matriz.
  3. El resultado suma ambos; la Matriz conserva su consumo directo.
- El conteo **aplica** `waste_percentage` en la línea y en el componente, **no** incluye `yield` y **no**
  expande más de un nivel (`BR-008`). Revisa `R-21` y la nota de `D-33` en el `PLAN`.

### D8. Consulta de costo

- La vista previa se calcula en `Application/Calculation` con una sola fórmula reutilizada por el detalle
  del APU, el listado de APUs ajenos y la vista de la Matriz. No se persiste ningún costo.
- Para cada línea: `cantidadEfectiva = ROUND(consumption × (1 + waste/100) × yield, quantityDecimals)`;
  `valorLinea = ROUND(cantidadEfectiva × precioUnitario, roundingDecimals)`; ese valor es el `subtotal` del
  insumo.
- Para cada tipo: `valorTipo = ROUND(Σ(valorLinea), roundingDecimals)`; el tipo `MATRIZ` agrupa la línea de
  Matriz y no se expande.
- `costoDirecto = ROUND(Σ(valorTipo), roundingDecimals)`.
- AIU: `valorComponente = ROUND(costoDirecto × porcentaje/100, roundingDecimals)` para administración,
  imprevistos y utilidad; `aiuTotal = ROUND(Σ(valorComponente), roundingDecimals)`;
  `totalApu = ROUND(costoDirecto + aiuTotal, roundingDecimals)`.
- Los porcentajes del AIU son los del proyecto de la ruta; nulos se toman como 0.
- La Matriz: `costoUnitarioMatriz = ROUND(Σ(valorLinea de sus componentes), roundingDecimals)`, y la línea
  del APU usa ese costo como precio unitario. La Matriz no aplica AIU ni tiene `yield` propio; el `yield` del
  APU entra en la cantidad efectiva de la línea.
- Si un insumo o componente no tiene precio, el APU o la Matriz se informan `FALTA_INFORMACION`; el
  `subtotal` del insumo y los costos quedan nulos.
- `yield` multiplica la cantidad efectiva de cada línea y, con ella, el costo directo, el AIU y el total del
  APU (`F1` revisada, `X-15` parcial; el total del proyecto queda en `E-4a`).

#### Contrato de respuesta del APU

```json
{
  "apuId": "…",
  "description": "Excavación",
  "unit": "m3",
  "status": "COMPLETO",
  "wbsItem": { "code": "1.1", "quantityWork": 10 },
  "resources": [
    {
      "resourceType": "MATERIAL",
      "items": [
        {
          "resourceId": "…",
          "description": "Cemento",
          "unit": "kg",
          "unitPrice": 1200,
          "quantity": 2.5,
          "wastePercentage": 5,
          "subtotal": 3150
        }
      ]
    },
    { "resourceType": "LABOR", "items": [] }
  ],
  "directCost": 10000,
  "indirectCosts": {
    "administration": { "percent": 10, "amount": 1000 },
    "unforeseen": { "percent": 5, "amount": 500 },
    "profit": { "percent": 8, "amount": 800 },
    "total": 2300
  },
  "total": 12300
}
```

#### Contrato de respuesta de la Matriz

```json
{
  "matrixResourceId": "…",
  "description": "Mortero",
  "unit": "m3",
  "status": "POR_COTIZAR",
  "resources": [
    {
      "resourceType": "MATERIAL",
      "items": [
        { "resourceId": "…", "description": "Arena", "unit": "m3", "unitPrice": 5000, "quantity": 1, "wastePercentage": 0, "subtotal": 5000 }
      ]
    }
  ],
  "directCost": 5000
}
```

- `resources` solo incluye los tipos con insumos; el `subtotal` de cada insumo ya incorpora el desperdicio.
- En el APU, la Matriz aparece en el grupo `MATRIZ` con su `subtotal` y su `matrixResourceId`.
- El ejemplo del contrato supone `yield = 1`; con otro valor, `directCost`, `indirectCosts` y `total` se
  multiplican por `yield`.

### D9. Copia y reconciliación

Vista previa (`GET .../apus/copy/preview`) solo para insumos simples:

```text
{ sourceApuId, targetWbsItemId, items: [
    { sourceResourceId, description, unit, resourceTypeCode,
      match: EXACT | SIMILAR | NONE,
      candidates: [ { resourceId, description, unitPrice, quotationStatus } ] } ] }
```

Copia (`POST .../apus/copy`):

```text
{ sourceApuId, targetWbsItemId,
  resolutions: [ { sourceResourceId, decision: USE_EXISTING | KEEP_ORIGIN, existingResourceId? } ] }
```

1. Resuelve el proyecto destino y exige escritura con `ProjectMember`; exige `targetWbsItemId` del
   proyecto y libre.
2. Carga el origen y exige lectura sobre su proyecto; si es de otra compañía responde `APU_NOT_FOUND`, sin
   revelar existencia.
3. Los insumos **simples** se clasifican `EXACT`/`SIMILAR`/`NONE`; `NONE` se copia siempre, `EXACT` y
   `SIMILAR` exigen una `resolution` o responden `APU_COMPOSITION_RESOLUTION_REQUIRED`.
4. Las líneas de Matriz se copian **sin** clasificación ni decisiones: se crea una Matriz nueva, se copian
   todos sus componentes y se copian los insumos que los componen como registros nuevos, aunque existan
   idénticos. El mismo comportamiento aplica a `POST .../matrices/copy`.
5. `KEEP_ORIGIN` crea el insumo con los datos del origen y `origin_resource_id`; `USE_EXISTING` referencia
   el insumo del destino.
6. El APU nuevo se crea con el actor como último modificador, sin aprobación y con estado recalculado según
   la completitud de su composición y de la Matriz copiada; la Matriz y cada insumo copiado también
   registran al actor.
7. Todo ocurre en una transacción (`BR-014`): un fallo no deja APU, Matriz ni insumos parciales.

- `M-05` no escribe `resources` con su propio `DbContext`: declara `IApuResourceCopyWriter` en
  `Application/Apus/Repositories` (crear insumo copiado, crear Matriz copiada, leer insumo) implementado en
  `Infrastructure` sobre el mismo `DbContext`.

### D10. Concurrencia y errores

- La edición del APU y cualquier cambio de composición comparan `rowversion` del APU; si no coincide,
  `ConflictAppException` con `APU_CONCURRENCY_CONFLICT` y respuesta `409`.
- La composición de una Matriz compara el `rowversion` del `Resource` Matriz y responde
  `MATRIX_CONCURRENCY_CONFLICT` con `409`.
- El alta no acepta `rowVersion`; el retiro sí lo exige por *query string*.
- Códigos: `APU_NOT_FOUND`, `APU_READ_FORBIDDEN`, `APU_WRITE_FORBIDDEN`, `APU_ITEM_NOT_FOUND`,
  `APU_ITEM_ALREADY_LINKED`, `APU_RESOURCE_NOT_FOUND`, `APU_RESOURCE_PROJECT_MISMATCH`,
  `APU_CONCURRENCY_CONFLICT`, `APU_COMPOSITION_RESOLUTION_REQUIRED`, `MATRIX_NOT_FOUND`,
  `MATRIX_TYPE_INVALID`, `MATRIX_NESTING_FORBIDDEN`, `MATRIX_CONCURRENCY_CONFLICT`,
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`,
  `PROJECT_NOT_FOUND`, `WBS_CHAPTER_HAS_APU`, `WBS_NODE_HAS_CHILDREN`.

### D11. Migración y datos iniciales

- Una migración `YYYYMMDDHHMM_apus_add_e3b` con `apus`, `apu_resources` y `matrix_components`, su `CHECK`,
  FKs e índices; el `ALTER` de `wbs_items.quantity_work`, `apu_resources.consumption` y
  `matrix_components.consumption` a `DECIMAL(18,10)`; la FK `FK_wbs_items_apus_apu_id` (`RESTRICT`); y las
  columnas `rounding_decimals` y `quantity_decimals` en `project_configurations`.
- No migra datos: las tablas nacen vacías; los valores por defecto de decimales son 2.
- `SecurityBaselineSeeder` sube a `v8` con `APUS_READ` y `APUS_WRITE`; el inventario pasa de 35 a 37
  permisos.
- Reversión: `Down` elimina la FK de `wbs_items`, los índices, las FK, las tablas y las columnas nuevas, y
  devuelve las columnas de cantidad a `DECIMAL(18,4)`; el `seed` es idempotente.

### D12. Pruebas

- **Unitarias**: completitud (incluida la herencia de la Matriz y el recálculo de los APUs que la usan),
  composición, no anidamiento, copia, reconciliación simple, costo por línea y tipo, AIU, redondeo, `yield`,
  conteo con desperdicio, autoría y alcance por rol.
- **Integración manual** (`D-66`): FKs, `CHECK`, índices, `rowversion`, copia transaccional, baja del ítem
  con APU, bloqueo de capítulos, copia de Matriz sin validación, recálculo del estado al completar una
  Matriz o editar su unidad, vista previa de costo y aislamiento.
- **Contrato**: `camelCase`, `ProblemDetails`, paginación, referencia del APU en el árbol y campos de costo.
- **Arquitectura**: `Domain` sin EF, `Application` sin `DbContext`, `Api` sin persistencia y `M-05` sin
  infraestructura de `M-02`, `M-03` ni `M-04`.

## Inconsistencias detectadas

| ID | Inconsistencia | Fuentes en conflicto | Resolución de este cambio | Estado |
|---|---|---|---|---|
| `INC-01` | `I-3b` dependía de `E-2` sin EDT implementada | `PLAN §2.4.2`, `§4.2` ↔ `RF-07`, `RF-22`, `BR-016`, `R-21` | `E-2b` implementado y archivado; `openspec/specs/edt` sincronizada | Resuelto |
| `INC-02` | `wbs_items.apu_id` obligatorio impedía crear ítems | `PLAN §3.3.3` ↔ `E-2` → `E-3b` | `D-113` aplicado en `wbs_add_i2b`; aquí solo se añade la FK | Resuelto |
| `INC-03` | Rutas `/apus` del `PLAN` sin alcance de proyecto | `PLAN §3.4.3` ↔ `D-05`, `R-01`, `D-99` | Rutas bajo `/api/v1/projects/{projectId}` | Resuelto |
| `INC-04` | `R-21` no precisa si el conteo expande Matrices ni si aplica desperdicio | `R-21`, `D-33` ↔ `BR-008`, `UC-05` | Un nivel; desperdicio en la línea y en el componente; sin `yield` | Resuelto (autor, 15/11/2026) |
| `INC-05` | `D-25` no define el criterio de similitud | `D-25` ↔ `REQ-INSUMOS-05` | Solo insumos simples; descripción parcial o `user_key` | **DERIVADO** |
| `INC-06` | `PLAN §3.3.2` no fija el conjunto de `Apu.status` | `PLAN §3.3.2` ↔ `05 §3` | `COMPLETO`/`FALTA_INFORMACION`; el APU hereda la completitud de la Matriz y recalcula los APUs que la usan; `POR_COTIZAR` no bloquea | Resuelto (autor, 15/11/2026) |
| `INC-07` | `D-53` exige invalidar aprobaciones que son de `E-4b` | `D-53` ↔ `PLAN §4.2` | `E-3b` no crea ni consulta aprobaciones | Resuelto por etapas |
| `INC-08` | `BR-009` dejaría toda Matriz incompleta | `BR-009`, `REQ-INSUMOS-04` ↔ `BR-008`, `BR-021` | Se modifica `REQ-INSUMOS-04`; Matriz por unidad y componentes | Resuelto |
| `INC-09` | `D-05` «se crea desde cero» admitiría APU sin ítem | `D-05` ↔ `BR-016`, `D-16` | El alta siempre exige ítem libre | **DERIVADO** |
| `INC-10` | `E-3a` copiaba Matrices como insumo simple | `REQ-INSUMOS-09` ↔ `BR-021` | La copia genérica rechaza `MATRIX`; la copia de Matriz no valida | Resuelto |
| `INC-11` | `yield` no tiene unidad de esfuerzo ni fórmula | `D-06`, `D-32` ↔ `PLAN §3.3.2`, `X-15` | Multiplica la cantidad efectiva del costo, no la del conteo (`F1` revisada); el total del proyecto sigue en `E-4a` | Resuelto (`X-15` parcial) |
| `INC-12` | Los identificadores de prueba de `E-2b` colisionaban con `E-3b` | `PLAN §5.6.1`, `§5.6.7` | `T-U-21`…`T-U-24`, `T-I-42`…`T-I-45`, `T-S-18`, `T-S-19`, `T-C-05`, `T-A-04` en `PLAN §5.6.8` | Resuelto como propuesta |
| `INC-13` | `PLAN §3.3.2` daba baja lógica al APU | `D-49` ↔ `PLAN §3.3.6` | Baja física del APU con el ítem; sin `is_active`/`deleted_at` | Resuelto (revisa `D-49`) |
| `INC-14` | `PLAN §3.3.1` exige `project_id` en toda entidad | `PLAN §3.3.1` ↔ `D-48` | Sin `project_id` redundante en `apu_resources`/`matrix_components` | Resuelto |
| `INC-15` | La EDT dejaba el APU desvinculado al borrar el ítem | `D-119`, `SC-EDT-85` ↔ `BR-016` | El ítem elimina el APU y sus líneas; los insumos se conservan | Resuelto (revisa `D-119`) |
| `INC-16` | `WbsItem` no permitía fijar `ApuId` | `WbsItem.ApuId` ↔ `BR-016`, `D-54` | `WbsItem.LinkApu` + `IWbsItemLinkWriter` condicional | Resuelto |
| `INC-17` | La cascada de capítulos borraba APUs | `D-119`, `SC-EDT-84` ↔ decisión del autor | Capítulo solo vacío; `WBS_CHAPTER_HAS_APU` si hay APUs; `WBS_NODE_HAS_CHILDREN` si hay nodos | Resuelto (revisa `D-114`/`D-119`) |
| `INC-18` | El costo y el AIU estaban en `E-4a` y `X-15` | `RF-10`, `D-06`, `PLAN §4.2` ↔ decisión del autor | Vista previa del APU/Matriz en `E-3b`; total del proyecto sigue en `E-4a` | Resuelto como alcance |
| `INC-19` | El árbol del EDT no expone el APU del ítem | `GET /wbs` ↔ `RF-07`, decisión del autor | `REQ-EDT-20` con `apu { id, status, description, unit }` | Resuelto |
| `INC-20` | El Director dueño no está necesariamente en `ProjectMember` | `Project.DirectorOwnerUserId` ↔ `REQ-APUS-16`, `REQ-EDT-13` | La pertenencia se valida solo con `project_members`; el dueño debe ser miembro para escribir | Resuelto (decisión del autor) |

## Risks / Trade-offs

| Riesgo | Mitigación |
|---|---|
| El costo unitario y el total del proyecto usan fórmulas distintas | `D-06`/`X-15` se revisan y `E-4a` reutiliza la fórmula del APU como base |
| El redondeo por cada cálculo produce diferencias acumuladas | Se define el orden exacto en `D8` y se prueba con `T-U-24` |
| La baja del APU obliga a que `M-03` conozca un contrato de `M-05` | `IWbsItemApuCleaner` vive en `Application/Wbs` y se implementa en `Infrastructure`; sin `DbContext` en `Application` |
| El árbol consulta APUs de `M-05` | `IApuSummaryReader` batch por proyecto; sin dependencia de infraestructura |
| Copiar Matrices sin validar duplica datos | Es una decisión del autor; la validación es manual y la independencia se conserva |
| `quantity_work` y `consumption` a 10 decimales amplían las filas | Migración `ALTER COLUMN` con respaldo y `Down` documentado |
| Permisos de APU y EDT cambian a `ProjectMember` | Se ajustan `REQ-EDT-13` y `REQ-APUS-16` y se prueba `T-S-18`/`T-S-19` |
| `yield` multiplicativo amplifica el costo si se captura como tasa de producción | Se documenta como factor adimensional; `E-4a` revisa `D-06`/`X-15`; se prueba con `yield ≠ 1` (`T-U-24`) |
| Heredar la completitud de la Matriz puede revertir un APU aprobado | El recálculo solo cambia `status`; la invalidación de aprobaciones pertenece a `E-4b` (`D-53`) |
| El conteo con desperdicio contradice `R-21`/`D-33`, confirmados en el `PLAN` | Se actualizan `R-21`, `D-33` y `D-06` en el `PLAN` al archivar (tarea 0.2) |
| Cambiar `apus.unit` reinterpreta `quantity_work` de los ítems ya vinculados | Se documenta en `D2`; el bloqueo o la confirmación explícita se evalúa en `E-4a` |

## Migration Plan

1. Verificar `E-2b` y la cadena: `20261114065800_wbs_add_i2b` aplicada, `apu_id` nullable con índice
   filtrado y sin FK.
2. Respaldo de la base y revisión de la cadena (`D-40`).
3. Generar `YYYYMMDDHHMM_apus_add_e3b`, revisar `Up`/`Down` y el `ALTER` de decimales.
4. Aplicar en ventana de mantenimiento; ejecutar el `seed` y verificar 37 permisos.
5. Verificar `CHECK`, FKs, índices, `rowversion`, la FK de `wbs_items` y los dos parámetros de configuración.
6. Ejecutar suites y la integración manual con dos compañías y dos proyectos.
7. Reversión: `Down` de la migración y restauración del respaldo si hubiera datos.

## Open Questions

Ninguna. `INC-04`, `INC-06` e `INC-11` quedaron resueltas el 15/11/2026; `X-15` permanece parcial y su
cierre pertenece a `E-4a`.
