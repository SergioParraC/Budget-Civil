# Impacto en el frontend: i-3b-apus-y-matrices

`D-56` y `D-109`: el cliente Angular **no** se regenera en `E-3b`; este documento registra el delta del
contrato v1 que el cliente deberá consumir cuando se abra la etapa de interfaz.

## Rutas nuevas (todas bajo `/api/v1/projects/{projectId}`)

| Método | Ruta | Permiso | Respuesta |
|---|---|---|---|
| `GET` | `/apus` | `APUS_READ` | `items`, `page`, `pageSize`, `totalItems` |
| `GET` | `/apus/{apuId}` | `APUS_READ` | Detalle con `resources`, `directCost`, `indirectCosts` y `total`, sin `yield` |
| `POST` | `/wbs/items` | `WBS_WRITE` + `APUS_WRITE` | `201` con la respuesta plana de ítem + APU |
| `PUT` | `/wbs/items/{itemId}` | `WBS_WRITE` + `APUS_WRITE` | `200` con la respuesta plana de ítem + APU |
| `GET` | `/apus/copy/preview?sourceApuId=...` | `APUS_WRITE` | `items[]` con `match`, `requiresResolution`, `unitPrice` y `candidates[]` con comparativo |
| `POST` | `/apus/copy` | `APUS_WRITE` | `201` con la respuesta plana del ítem + APU copiado |
| `GET` | `/matrices/{matrixResourceId}/components` | `APUS_READ` | `resources[]`, `directCost`, `rowVersion` |
| `POST` | `/matrices/{matrixResourceId}/components` | `APUS_WRITE` | `201` con la Matriz |
| `PUT` | `/matrices/{matrixResourceId}/components/{componentId}` | `APUS_WRITE` | `200` con la Matriz |
| `DELETE` | `/matrices/{matrixResourceId}/components/{componentId}?rowVersion=...` | `APUS_WRITE` | `204` |
| `POST` | `/matrices/copy` | `APUS_WRITE` | `201` con `matrixResourceId` |
| `GET` | `/resource-usage` | `RESOURCES_READ` | `resources[]` agrupado por tipo, con `unitPrice`, `quantity`, `totalPrice`, `subtotal` y `totalCost` |

## Cambios en rutas existentes

- `GET /projects/{id}/wbs`: cada ítem incluye
  `apu: { id, status, description, unit, unitPrice, rowVersion }` o `null`. `unitPrice` siempre es `0`
  hasta `E-4a`; `apu.rowVersion` se envía en la edición unificada.
- `POST /projects/{id}/wbs/items`: crea ítem + APU + líneas y el vínculo 1:1. El bloque `apu` es
  obligatorio, con `description` y `unit` obligatorios y `resources` opcional.
- `PUT /projects/{id}/wbs/items/{itemId}`: `quantityWork` obligatorio, `code` opcional y bloque `apu`
  opcional. `resources` presente reemplaza la composición completa; `resources: []` la vacía; ausente la
  conserva. `apuRowVersion` es obligatorio si se envía `apu`.
- `DELETE /projects/{id}/wbs/chapters/{chapterId}`: se elimina el parámetro `cascade`; un capítulo solo se
  elimina vacío y puede responder `409 WBS_CHAPTER_HAS_APU` o `409 WBS_NODE_HAS_CHILDREN`.
- `DELETE /projects/{id}/wbs/items/{itemId}`: si el ítem tiene APU, también se elimina el APU.
- `POST /projects/{id}/resources/copy`: rechaza el tipo Matriz con
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`; la Matriz se copia con `POST /matrices/copy`.
- `GET /projects/{id}/configuration` y `PUT /projects/{id}`: incorporan `roundingDecimals` y
  `quantityDecimals` (0–10, default 2).

## Flujo obligatorio de copia de APU

1. `GET /apus/copy/preview` con `sourceApuId`.
2. Por cada `item`:
   - `match = NONE`: no enviar resolución; la copia crea el insumo automáticamente.
   - `match = EXACT` o `SIMILAR`: `requiresResolution = true`. Comparar el insumo de origen (`unit`,
     `unitPrice`) con cada `candidate` (`unit`, `unitPrice`, `quotationStatus`) usando
     `comparison.unitMatches`, `comparison.unitPriceMatches` y
     `comparison.unitPriceDifference` (`candidato − origen`; `null` si falta un precio).
3. Enviar `POST /apus/copy` con el `target` (`code`, `wbsChapterId`, `quantityWork`) y una `resolution` por
   cada `sourceResourceId` que requiera decisión:
   - `decision = USE_EXISTING` + `existingResourceId` del candidato elegido (reutiliza el insumo del
     destino y conserva su precio).
   - `decision = KEEP_ORIGIN` (crea un insumo nuevo con los datos del origen y `originResourceId`).
4. Las líneas de Matriz no aparecen en la reconciliación y se copian siempre como registros nuevos.
5. Si falta una resolución el servidor responde `400 APU_COMPOSITION_RESOLUTION_REQUIRED` y no copia nada.
   Un código duplicado responde `409 WBS_CODE_DUPLICATED`; un APU de otra compañía responde
   `404 APU_NOT_FOUND`.

### Respuesta del preview

- `items[]`: `sourceResourceId`, `description`, `resourceTypeCode`, `unit` y `unitPrice` del **origen**,
  `match` y `requiresResolution`.
- `candidates[]`: `resourceId`, `description`, `resourceTypeCode`, `unit`, `unitPrice` y
  `quotationStatus` del **destino**, más `comparison`.
- `comparison`: `unitMatches`, `unitPriceMatches` y `unitPriceDifference` (`candidato − origen`, `null`
  si falta cualquiera de los dos precios).
- `EXACT` significa misma descripción normalizada; puede tener unidad o precio distintos y aun así exige
  resolución. `SIMILAR` es una coincidencia parcial de descripción. `NONE` no trae candidatos.

## Reporte de uso (`GET /resource-usage`)

```text
{ projectId, resources: [
    { resourceType, resourceTypeName, subtotal,
      items: [ { resourceId, description, unit, unitPrice, quantity, totalPrice } ] } ],
  totalCost }
```

- `resources` agrupa por `resourceType` en el orden del catálogo y permite pintar una sección por tipo.
- `quantity` es la cantidad física con desperdicio y expansión de un nivel, sin `yield`.
- `unitPrice` es el precio unitario actual del insumo en el proyecto.
- `totalPrice = ROUND(quantity × unitPrice, roundingDecimals)`.
- `subtotal` es la suma de `totalPrice` del grupo y `totalCost` la suma de los subtotales.
- Sin precio, `totalPrice`, `subtotal` y `totalCost` son `null`; un insumo sin uso se devuelve con
  `quantity = 0` (y `totalPrice = 0` si tiene precio).

## Formas de respuesta

- `resources` es un arreglo por `resourceType` (`MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`,
  `WORK`, `MATRIX`) y cada elemento lleva `subtotal`, ya redondeado con el consumo y el desperdicio. Cada
  insumo incluye `lineId` para identificar la línea en la edición unificada.
- `directCost`, `indirectCosts` y `total` viajan **fuera** de `resources`. La Matriz no devuelve
  `indirectCosts` ni aplica AIU.
- El detalle no incluye `yield`. `wbsItem.quantityWork` solo aparece si el APU pertenece al proyecto de la
  ruta.
- `rowVersion` viaja en Base64. La edición y todo cambio de composición exigen la versión vigente
  (`409 APU_CONCURRENCY_CONFLICT` o `409 MATRIX_CONCURRENCY_CONFLICT`).
- Sin precio, el APU se muestra `FALTA_INFORMACION`, el `subtotal` del insumo es `null` y los costos son
  `null`.
- Los errores usan `ProblemDetails` con `extensions.code` en mayúsculas: `APU_NOT_FOUND`,
  `APU_READ_FORBIDDEN`, `APU_WRITE_FORBIDDEN`, `APU_RESOURCE_NOT_FOUND`,
  `APU_RESOURCE_PROJECT_MISMATCH`, `APU_CONCURRENCY_CONFLICT`,
  `APU_COMPOSITION_RESOLUTION_REQUIRED`, `MATRIX_NOT_FOUND`, `MATRIX_TYPE_INVALID`,
  `MATRIX_NESTING_FORBIDDEN`, `MATRIX_CONCURRENCY_CONFLICT`,
  `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`, `WBS_CODE_DUPLICATED`, `WBS_CHAPTER_HAS_APU` y
  `WBS_NODE_HAS_CHILDREN`.

## Campos de entrada

- `POST /wbs/items`: `code`, `wbsChapterId`, `quantityWork` y `apu` obligatorio con `description`, `unit` y
  `resources[]` opcional; cada recurso lleva `resourceId`, `consumption` y `wastePercentage`.
- `PUT /wbs/items/{itemId}`: `quantityWork` obligatorio, `code` opcional, `rowVersion` del ítem y `apu`
  opcional con `description`, `unit`, `apuRowVersion` y `resources[]` opcional. Cada recurso lleva
  `lineId` (nulo para líneas nuevas), `resourceId`, `consumption` y `wastePercentage`.
- `AddMatrixComponentRequest`: `componentResourceId`, `consumption`, `wastePercentage`, `rowVersion`.
- `UpdateMatrixComponentRequest`: `consumption`, `wastePercentage`, `rowVersion`.
- `POST /apus/copy`: `sourceApuId`, `target` con `code`, `wbsChapterId` y `quantityWork`, y `resolutions[]`
  con `sourceResourceId`, `decision` (`USE_EXISTING` o `KEEP_ORIGIN`) y `existingResourceId`; cada
  `sourceResourceId` debe provenir de un `item` del preview con `requiresResolution = true`.

## Respuesta plana de alta, edición y copia

```text
{ id, projectId, nodeType, code, parentChapterId, name, quantityWork, orderIndex, rowVersion,
  lastModifiedBy, lastModifiedAt, children,
  apuId, apuStatus, apuRowVersion, apuLastModifiedBy, apuLastModifiedAt,
  description, unit, resources, directCost, indirectCosts, total }
```

`rowVersion` es la del ítem y `apuRowVersion` la del APU. `children` es `[]` y `name` es `null` para ítems.


## Cliente generado

Diferido por `D-56` y `D-109`. Cuando se regenere, debe tomar estos nombres en inglés y no introducir
`snake_case` ni sobres propios (`T-C-05`).
