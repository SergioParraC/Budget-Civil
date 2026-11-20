# Impacto en el frontend — e-4a-calculo-y-cotizacion

Contrato v1 aditivo. No hay migración de base ni cambios de esquema. El cliente Angular debe regenerarse
cuando el contrato v8 esté estable (`D-109`).

## 1. Árbol del EDT (`GET /api/v1/projects/{projectId}/wbs`)

Campos nuevos y cambios de valor:

| Campo | Nodo | Antes | Ahora |
|---|---|---|---|
| `unitCost` | ítem | no existía | costo directo unitario del APU; `null` si no hay APU o está incompleto |
| `directCost` | ítem | no existía | `ROUND(cantidad de obra × unitCost, roundingDecimals)`; `null` si `unitCost` es nulo |
| `directCost` | capítulo | no existía | suma de los costos directos de los ítems de su subárbol, cada uno una sola vez; `0` si no hay ítems; `null` si algún ítem es nulo |
| `apu.unitPrice` | ítem | siempre `0` | el mismo `unitCost`, sin AIU; `null` si el APU está incompleto |

Reglas que el cliente debe respetar:

- El AIU **no** se distribuye por ítem en el árbol. El valor oficial vive en el presupuesto del proyecto.
- Un capítulo nunca incluye los ítems raíz (`wbs_items.wbs_chapter_id = null`) en su subtotal.
- La semántica B no suma el subtotal publicado de un capítulo hijo como un ítem adicional: el cliente no debe
  recalcular subtotales sumando los `directCost` de los hijos más los ítems, porque el servidor ya lo hizo.
- `POST /wbs/move` y `POST /wbs/reorder` devuelven el nodo movido **sin costos**. Después de mover, el cliente
  debe volver a consultar `GET /wbs` para ver los subtotales de los ancestros.
- `GET /wbs/search` no incluye costos.

## 2. Presupuesto del proyecto (`GET /api/v1/projects/{projectId}/budget`)

Permiso `WBS_READ`. Mismo alcance que el árbol: Administrador cualquiera, Director su compañía, Presupuestador
y Auditor con `ProjectMember`. Otra compañía responde `403 WBS_READ_FORBIDDEN`; proyecto inexistente `404
PROJECT_NOT_FOUND`.

Respuesta completa:

```json
{
  "projectId": "...",
  "baseCurrency": { "id": "...", "isoCode": "COP", "name": "Peso colombiano" },
  "roundingDecimals": 2,
  "quantityDecimals": 2,
  "directCost": 1100.00,
  "indirectCosts": {
    "administration": { "percent": 10.00, "amount": 110.00 },
    "unforeseen": { "percent": 5.00, "amount": 55.00 },
    "profit": { "percent": 8.00, "amount": 88.00 },
    "total": 253.00
  },
  "total": 1353.00,
  "budgetLimit": 1500.00,
  "isOverBudget": false,
  "isComplete": true,
  "itemCount": 2,
  "itemsWithoutApu": 0,
  "itemsWithoutPrice": 0
}
```

- `directCost` suma todos los ítems del EDT, incluidos los ítems raíz; no se deriva de los capítulos.
- `indirectCosts` aplica el AIU una sola vez sobre el costo directo total.
- `budgetLimit` es nulo si el proyecto no tiene tope; `isOverBudget` solo es `true` con total calculable y
  total estrictamente mayor que el tope.
- Si `isComplete` es `false`, `directCost`, los importes del AIU y `total` son `null`. **No hay total
  parcial**: el cliente debe mostrar la causa con `itemsWithoutApu` e `itemsWithoutPrice`, no un valor
  incompleto como definitivo.
- Sin ítems, `directCost = 0`, AIU en `0`, `total = 0`, `isComplete = true`.

## 3. Cotización masiva (`PATCH /api/v1/projects/{projectId}/resources/quotation-status`)

Permiso `RESOURCES_WRITE`. El cuerpo es discriminado por `scope`:

```json
{ "scope": "SELECTION", "resourceIds": ["...", "..."] }
{ "scope": "FILTER", "filter": { "description": "cemento", "resourceTypeId": "...", "quotationStatus": "POR_COTIZAR", "brand": "...", "municipalityId": "...", "minPrice": 0, "maxPrice": 100000 } }
{ "scope": "ALL" }
```

Reglas:

- `scope` es obligatorio; `resourceIds` solo con `SELECTION` y `filter` solo con `FILTER`. Una selección vacía
  o una combinación inválida responde `400` con `ProblemDetails` (`extensions.code = VALIDATION_ERROR`).
- `FILTER` reutiliza los criterios del listado de insumos y se resuelve en el servidor dentro de la
  transacción: no se envían ids.
- Un `resourceId` de otro proyecto responde `404 RESOURCE_NOT_FOUND` y no aplica el lote.
- Los insumos incompletos (incluidas las Matrices sin componentes) se omiten con
  `RESOURCE_QUOTATION_INCOMPLETE` y no se les escribe precio ni estado.
- La operación es idempotente: los insumos ya `COTIZADO` se informan en `alreadyQuoted` y no se reescriben.
- No se envía `rowVersion`: el servidor reevalúa la completitud de cada fila.

Respuesta:

```json
{
  "projectId": "...",
  "quotationStatus": "COTIZADO",
  "requested": 300,
  "updated": 294,
  "alreadyQuoted": 0,
  "skipped": [{ "resourceId": "...", "code": "RESOURCE_QUOTATION_INCOMPLETE" }],
  "pendingResources": 6,
  "allQuoted": false
}
```

- La respuesta no repite los insumos: el cliente refresca el listado (`GET /projects/{id}/resources`) y el
  presupuesto (`GET /projects/{id}/budget`) después del lote.
- `pendingResources` cuenta todo el proyecto (no el alcance enviado) e indica si se puede pedir la transición
  del proyecto a `COTIZADO`; `allQuoted = pendingResources == 0`.
- Cotizar no congela precios: si un precio cambia después, el presupuesto se recalcula y el insumo sigue
  `COTIZADO` mientras conserve los cinco datos (`REQ-INSUMOS-04`).

## 4. Transición del proyecto a `COTIZADO`

`PATCH/POST` de cambio de estado (`PROJECTS_STATUS_CHANGE`) con destino `COTIZADO` exige el cien por ciento
cotizado. Si quedan pendientes responde `409` con:

```json
{
  "status": 409,
  "code": "PROJECT_QUOTATION_INCOMPLETE",
  "pendingResources": 6
}
```

El cliente debe usar `pendingResources` para explicar el bloqueo y refrescar el listado de insumos. La
cotización masiva **no** cambia el estado del proyecto: la transición es un comando aparte (`D-88`, `D-13`).
