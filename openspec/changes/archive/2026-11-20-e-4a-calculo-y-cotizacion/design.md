# Design: e-4a-calculo-y-cotizacion

## Context

`E-3b` dejó `ApuCostCalculator` como fórmula única del costo directo, el AIU y el redondeo del APU y de la
Matriz; `ApuDetailService` y `GetApusQueryHandler` ya la usan. El árbol `GET /wbs` se arma en
`Application/Wbs` con `IApuSummaryReader` (solo metadatos del APU) y `WbsMapper.ToItemNode` fija
`unitPrice = 0`; no existe una consulta de presupuesto. La configuración del proyecto ya tiene AIU,
`rounding_decimals`, `quantity_decimals` y `budget_limit`. La prueba `T-A-03` prohíbe que
`Application/Wbs` dependa de `Application/Apus`, pero `Application/Calculation` no está en esa lista y ya
aloja el calculador compartido. No hay columnas de costo ni migraciones pendientes.

En `M-04`, `resources.quotation_status` solo se deriva por completitud: `Resource.CalculateQuotationStatus`
produce `FALTA_INFORMACION` o `POR_COTIZAR` y conserva `COTIZADO` si ya lo estaba, pero ningún comando lo
asigna; `ResourcesController` no tiene operación de estado y `ChangeProjectStatusCommandHandler` no valida la
precondición de `REQ-PROYECTOS-05`. `ResourceAccessService` y `RESOURCES_WRITE` ya resuelven alcance,
membresía y roles de escritura, y `Resource.IsComplete` concentra la regla de completitud.

Ver `proposal.md - Why` para la motivación; las reglas de comportamiento están en
`specs/calculo/spec.md`, `specs/cotizacion/spec.md` y `specs/edt/spec.md`.

## Goals / Non-Goals

**Goals:**

- Reutilizar `ApuCostCalculator` sin duplicar la fórmula ni el redondeo.
- Derivar en lectura el costo unitario y directo de cada ítem y el costo directo de cada capítulo.
- Exponer un presupuesto de proyecto con costo directo, AIU discriminado, costo indirecto y total.
- Evaluar el tope presupuestal (`RF-17`) sobre el total calculable.
- Entregar el cambio masivo a `COTIZADO` por selección, filtro o totalidad en una sola operación
  transaccional, con resumen por insumo.
- Implementar la precondición de `REQ-PROYECTOS-05` que hoy no se valida, sin mover el proyecto desde el
  comando de cotización.
- Mantener `Application/Wbs` sin dependencia de `Application/Apus` y sin lógica de negocio en Infrastructure.
- Cubrir cálculo, contrato, seguridad y arquitectura con pruebas.

**Non-Goals:**

- `RF-12`, `RF-13`, `RF-21` (aprobación por APU, firma y cierre, `E-4b`).
- El retroceso de un insumo cotizado a `POR_COTIZAR`: `RF-11` solo define el paso a `COTIZADO`.
- Persistir costos, agregar columnas o crear una migración.
- Cambiar `GET /wbs/search` para incluir costos.
- Frontend y cliente Angular (`D-109`).

## Decisions

### D1. Una sola fórmula y un solo orden de redondeo

Se reutilizan `ApuCostCalculator.CalculateApu`, `CalculateIndirectCosts` y `Round` (`D-121`). El orden es:

```text
1. cantidad efectiva = ROUND(consumo x (1 + desperdicio/100), quantityDecimals)   [ya existe]
2. unitCost (APU)    = costo directo unitario del APU                             [ya existe]
3. item.unitCost     = unitCost del APU
4. item.directCost   = ROUND(cantidad de obra x item.unitCost, roundingDecimals)
5. chapter.directCost = ROUND(Sum(item.directCost de su subarbol), roundingDecimals)
6. proyecto.directCost = ROUND(Sum(item.directCost de todos los items), roundingDecimals)
7. AIU = CalculateIndirectCosts(proyecto.directCost, settings)                    [ya existe]
8. proyecto.total = ROUND(proyecto.directCost + AIU.total, roundingDecimals)
```

El AIU **no** se calcula por ítem para el presupuesto. `CalculateApu` devuelve su AIU unitario de vista
previa, pero el árbol y el presupuesto solo consumen `DirectCost`; el AIU oficial se aplica una vez sobre el
costo directo total del proyecto (`D-07`, `docs/05 §5`).

### D2. Semántica B del capítulo: ítems del subárbol, una sola vez

```text
C1
  1.1        item: 100 x 10 = 1.000
  C1.1
    1.1.1    item: 50 x 2  =   100
    C1.1.1
      1.1.1.1 item: 25 x 1 =    25

C1.1.1   = 25
C1.1     = 100 + 25 = 125
C1       = 1.000 + 100 + 25 = 1.125
Proyecto = 1.125
```

Se acumulan **costos de ítems**, nunca el `directCost` publicado de un capítulo hijo como si fuera un ítem
adicional. La implementación recorre el árbol una vez, de abajo hacia arriba, y devuelve el acumulado de
ítems del subárbol:

```text
AcumularItems(capitulo):
  total = 0
  para cada item hijo:
    si item.directCost es nulo -> propagar nulo
    total += item.directCost
  para cada capitulo hijo:
    sub = AcumularItems(capitulo hijo)
    si sub es nulo -> propagar nulo
    total += sub
  devolver ROUND(total, roundingDecimals)
```

El resultado coincide con sumar los subtotales de los capítulos hijos una vez, pero el código no toma el
subtotal publicado como unidad: así se cumple la regla y se evita el doble conteo. Un capítulo sin ítems en
su subárbol devuelve `0`; si algún ítem del subárbol es nulo, el capítulo devuelve `null` (no un total
parcial).

### D3. Contrato del árbol (`GET /wbs`)

Los cambios son aditivos; no se rompe el contrato de `WbsNodeDto` ni el de la alta/edición unificada.

```json
{
  "id": "...",
  "nodeType": "ITEM",
  "code": "1.1",
  "quantityWork": 10,
  "unitCost": 15000.00,
  "directCost": 150000.00,
  "apu": {
    "id": "...",
    "status": "POR_COTIZAR",
    "description": "Concreto 3000 psi",
    "unit": "M3",
    "unitPrice": 15000.00,
    "rowVersion": "..."
  },
  "children": []
}
```

```json
{
  "id": "...",
  "nodeType": "CHAPTER",
  "code": "1",
  "name": "Preliminares",
  "directCost": 1125.00,
  "children": []
}
```

| Campo | Nodo | Valor |
|---|---|---|
| `unitCost` | ítem | Costo directo unitario del APU; `null` si no hay APU o está incompleto |
| `directCost` | ítem | `ROUND(cantidad de obra x unitCost, roundingDecimals)`; `null` si `unitCost` es nulo |
| `apu.unitPrice` | ítem | Se llena con el mismo `unitCost` (hoy `0`); no incluye AIU |
| `directCost` | capítulo | Acumulado de ítems del subárbol según D2; `0` si no hay ítems; `null` si alguno es nulo |

`WbsMapper.ToItemNode` recibe el resultado del cálculo y deja de fijar `unitPrice = 0`. El ítem sin APU
mantiene `apu = null` (`REQ-EDT-20`) y agrega `unitCost = null` y `directCost = null`. El árbol no expone
`indirectCosts` ni un total con AIU por ítem; eso vive en el presupuesto del proyecto.

### D4. Endpoint de presupuesto

```text
GET /api/v1/projects/{projectId}/budget        permiso WBS_READ
```

```json
{
  "projectId": "...",
  "baseCurrency": { "id": "...", "isoCode": "COP", "name": "Peso colombiano" },
  "roundingDecimals": 2,
  "quantityDecimals": 2,
  "directCost": 1100.00,
  "indirectCosts": {
    "administration": { "percent": 10.00, "amount": 110.00 },
    "unforeseen":     { "percent": 5.00,  "amount": 55.00  },
    "profit":         { "percent": 8.00,  "amount": 88.00  },
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

- `directCost` usa todos los ítems del EDT, incluidos los ítems raíz; no se deriva de los capítulos.
- `indirectCosts` reutiliza `CalculateIndirectCosts`; `total = directCost + indirectCosts.total`.
- `budgetLimit` sale de `ProjectConfiguration.BudgetLimit`; `isOverBudget = total.HasValue &&
  budgetLimit.HasValue && total > budgetLimit`. Sin tope, total nulo o total igual al tope, es `false`.
- `isComplete` es `true` solo si todos los ítems tienen APU con precio completo; si no, los tres bloques
  monetarios son nulos y los contadores explican por qué.
- `baseCurrency` se lee de `BaseCurrency` (`isoCode`, `name`); los decimales son los de la configuración.
- El permiso `WBS_READ` y las reglas de alcance son las del árbol: Administrador cualquiera, Director su
  compañía, Presupuestador y Auditor con `ProjectMember`. Otra compañía `403 WBS_READ_FORBIDDEN`; proyecto
  inexistente `404 PROJECT_NOT_FOUND`.

### D5. Módulos, puertos y consultas

```text
+------------------------------+       +-------------------------------+
| Api.WbsController            |       | Api.BudgetController          |
| GET /projects/{id}/wbs       |       | GET /projects/{id}/budget     |
+--------------+---------------+       +---------------+---------------+
               |                                       |
   +-----------v-----------+               +-----------v----------------+
   | Application.Wbs       |               | Application.Calculation    |
   | GetWbsTreeQueryHandler|               | GetProjectBudgetQueryHandler|
   |  usa ApuCostService   |               |  usa ApuCostService        |
   +-----------+-----------+               +-----------+----------------+
               |                                       |
               +------------------+--------------------+
                                  |
                     Application.Calculation
                     ApuCostService / ApuCostCalculator
                                  |
                     IApuCostReader (Calculation)
                                  |
   +------------------------------v-----------------------------------+
   | Infrastructure: ProjectCostReader + CalculationProjectAccessReader|
   |  - items del EDT con cantidad y apu_id                            |
   |  - APUs + lineas + tipo + precio unitario                         |
   |  - componentes de matrices y unidad de la Matriz                  |
   |  - AIU, decimales, tope, divisa y `budget_limit`                  |
   +-------------------------------------------------------------------+
```

Puertos y servicios:

| Pieza | Responsabilidad |
|---|---|
| `IApuCostReader.GetProjectAsync(projectId)` | Devuelve ítems, líneas de APU con matrices expandidas, `CostSettings`, tope y divisa en consultas set-based |
| `ApuCostService.GetProjectSnapshotAsync(projectId)` | Calcula `ApuCostResult` por APU y devuelve el snapshot del proyecto |
| `IWbsApuCostReader` o extensión de `IApuSummaryReader` | Entrega al árbol solo el diccionario `apuId -> costos`; no conoce `Application.Apus` |
| `CalculationAccessService` + `ICalculationProjectAccessReader` | Aplica el alcance de lectura sin depender de `Application.Wbs` ni de `Application.Apus` |
| `GetProjectBudgetQueryHandler` | Agrega ítems, aplica AIU, completa contadores y evalúa el tope |
| `BudgetController` | Expone `GET /projects/{id}/budget` con `AuthorizationPolicies.WbsRead` |

`Application/Wbs` puede depender de `Application/Calculation` (no está en la lista prohibida de `T-A-03`) y
sigue sin depender de `Application/Apus`. `Application/Calculation` no depende de `Apus` ni de `Wbs`: sus
modelos de entrada son `ApuCostLineInput`, `MatrixComponentInput` y `CostSettings`, que ya viven en
`Application/Calculation`.

### D6. Consultas y rendimiento

`GET /wbs` hoy ejecuta tres consultas (capítulos, ítems y resúmenes de APU). El snapshot de costos agrega,
como máximo:

1. Ítems con `quantity_work` y `apu_id`, y APUs con líneas, tipo y precio unitario.
2. Componentes de todas las Matrices referenciadas por las líneas.
3. Unidades de las Matrices referenciadas.
4. Configuración del proyecto, divisa y `budget_limit`.

Son consultas set-based por `project_id`, sin N+1; la complejidad es lineal en ítems + líneas + componentes.
`GET /budget` reutiliza el mismo snapshot y no vuelve a leer el árbol. No se agrega caché en el MVP: la
correctitud después de mover o editar es más importante que el ahorro, y el volumen académico no lo exige.

### D7. Completitud, nulos y contadores

`ApuCostCalculator.IsComplete` es `false` cuando el APU no tiene líneas o alguna línea no tiene precio
(incluida una Matriz sin componentes o sin unidad). A partir de ahí:

- `item.unitCost` y `item.directCost` son `null` si el ítem no tiene APU o el APU está incompleto.
- `chapter.directCost` es `null` si algún ítem de su subárbol es nulo; es `0` si el subárbol no tiene ítems.
- `budget.isComplete` es `true` solo si todos los ítems tienen APU completo; en caso contrario `directCost`,
  `indirectCosts` y `total` son nulos.
- `itemsWithoutApu` cuenta ítems con `apu_id` nulo; `itemsWithoutPrice` cuenta APUs con
  `IsComplete = false`. Ambos son informativos y no alteran la regla de nulos.

No se devuelve un total parcial: mostrar un presupuesto incompleto como si fuera definitivo es más riesgoso
que indicar `isComplete = false`. La alternativa de total parcial se descarta para el MVP.

### D8. Derivación, movimiento y búsqueda

No se agregan columnas ni tablas: el costo es una proyección de lectura. `POST /wbs/move` y
`POST /wbs/reorder` siguen devolviendo el nodo movido; el cliente debe volver a consultar `GET /wbs` para
ver los subtotales de los ancestros. La prueba de movimiento verifica que, tras el `GET`, ningún ítem se
cuenta dos veces y que el total del proyecto no cambia por un movimiento. `GET /wbs/search` no incluye
costos para no cargar la paginación con el árbol completo.

### D9. Compatibilidad y seguridad

Los campos nuevos son aditivos; `apu.unitPrice` deja de ser `0` y pasa a ser el costo directo unitario, que
es el valor que el cliente ya usa para calcular el costo del ítem. La alta y la edición unificadas
(`POST/PUT /wbs/items`) ya devuelven `directCost`, `indirectCosts` y `total` del APU; no cambian. El
presupuesto usa `WBS_READ`, el mismo permiso y alcance que el árbol; no se crea un permiso nuevo ni se
modifica el `seed`. La cotización masiva usa `RESOURCES_WRITE`, que ya separa al Director y al Presupuestador
del Administrador y del Auditor (`02 §3`), y el alcance de `ResourceAccessService`. `POST /apus/{id}/approval`
no se toca: la aprobación por APU sigue siendo individual y pertenece a `E-4b`.

### D10. Archivos impactados

| Capa | Archivos previstos |
|---|---|
| Application/Calculation | `Repositories/IApuCostReader.cs`, `Services/ApuCostService.cs`, `Services/CalculationAccessService.cs`, `Repositories/ICalculationProjectAccessReader.cs`, `Queries/GetProjectBudgetQueryHandler.cs`, `Dtos/ProjectBudgetDtos.cs`, `ProjectBudgetCalculator.cs` |
| Application/Wbs | `Queries/GetWbsTreeQueryHandler.cs`, `Dtos/WbsDtos.cs` (DTO y mapper), `Repositories/IApuSummaryReader.cs` o un lector de costos del árbol |
| Application/Resources | `Commands/ChangeResourceQuotationStatus/` (comando, validador, respuesta y handler) sobre `ResourceAccessService` y `Resource.IsComplete` |
| Application/Projects | `Commands/ChangeProjectStatus/ChangeProjectStatusCommandHandler.cs` (validación de `REQ-PROYECTOS-05`) |
| Infrastructure | `Persistence/Repositories/ProjectCostReader.cs`, `Persistence/Repositories/CalculationProjectAccessReader.cs`, `Persistence/Repositories/ResourceQuotationRepository.cs` (candidatos por filtro, `UPDATE` set-based y conteo de pendientes), `DependencyInjection.cs` |
| Api | `Controllers/BudgetController.cs`; operación nueva en `Controllers/ResourcesController.cs`; `WbsController` no cambia |
| Pruebas | Unitarias de cálculo, agregación y modos de cotización; contrato del árbol, del presupuesto y de la cotización; seguridad de alcance; arquitectura de módulos |
| Documentación | `PLAN_INGENIERIA.md`, `docs/02`, `docs/05`, `openspec/project.md §8`, `IMPACTO_FRONTEND.md` del change |

### D11. Un solo comando masivo con tres alcances

```text
PATCH /api/v1/projects/{projectId}/resources/quotation-status        permiso RESOURCES_WRITE
```

```json
{ "scope": "SELECTION", "resourceIds": ["...", "..."] }
{ "scope": "FILTER", "filter": { "description": "cemento", "resourceTypeId": "...", "quotationStatus": "POR_COTIZAR", "brand": "...", "municipalityId": "...", "minPrice": 0, "maxPrice": 100000 } }
{ "scope": "ALL" }
```

```json
{
  "projectId": "...",
  "quotationStatus": "COTIZADO",
  "requested": 300,
  "updated": 294,
  "alreadyQuoted": 0,
  "skipped": [ { "resourceId": "...", "code": "RESOURCE_QUOTATION_INCOMPLETE" } ],
  "pendingResources": 6,
  "allQuoted": false
}
```

- `scope` es obligatorio; `resourceIds` solo con `SELECTION` y `filter` solo con `FILTER`. Una selección
  vacía o una combinación inválida responde `400` con `ProblemDetails`.
- `filter` reutiliza los criterios de `GET /projects/{id}/resources` (`D-101`) sin paginación: el conjunto se
  resuelve en el servidor dentro de la transacción, de modo que no hay lista de ids desactualizada.
- La respuesta no repite los insumos; el cliente refresca el listado. `skipped` explica cada omisión.
- No se crea permiso ni se modifica el `seed`: `RESOURCES_WRITE` ya coincide con la matriz de `RF-11`.

Un comando masivo en lugar de N llamadas: con `FILTER` y `ALL` el cliente tendría que enumerar ids con
paginación, aplicarlos uno por uno y reconstruir el resumen, con carreras entre lectura y escritura y sin
transacción. Un `PATCH` unitario puede existir como azúcar del mismo caso de uso, pero no reemplaza al masivo.

### D12. El lote se evalúa en el servidor y no usa `rowVersion` por insumo

- El comando no recibe `rowVersion`: no edita valores, afirma un estado. Cada fila se reevalúa dentro de la
  transacción con `Resource.IsComplete`; si otro usuario la dejó incompleta, se omite y se reporta.
- Todo el lote corre en una transacción con un `UPDATE` set-based sobre los ids resueltos. `last_modified_by`
  y `last_modified_at` se escriben solo en las filas que cambian de estado (`RF-14`).
- Los insumos que ya están en `COTIZADO` no se reescriben: se cuentan en `alreadyQuoted` y la operación es
  idempotente.
- `pendingResources` se cuenta sobre todo el proyecto y no sobre el alcance, para que el cliente sepa si puede
  pedir la transición. `allQuoted = pendingResources == 0`.
- El retroceso a `POR_COTIZAR` no se implementa: `RF-11` solo define el paso a `COTIZADO`, y editar un insumo
  hasta dejarlo incompleto ya lo devuelve a `FALTA_INFORMACION` (`REQ-INSUMOS-04`).
- Se registra un evento de auditoría de seguridad por operación con `RESOURCE_QUOTATION_CHANGED` y los conteos;
  `SecurityAuditEvent.event_type` se guarda como texto, así que un valor nuevo del enum no requiere migración.

### D13. La precondición de cierre vive en la transición del proyecto

`REQ-PROYECTOS-05` ya especifica que la transición a `COTIZADO` exige el cien por ciento cotizado, pero el
handler no lo valida. Este cambio agrega la validación con un lector de conteo por proyecto y responde
`409 PROJECT_QUOTATION_INCOMPLETE` con `pendingResources` en `ProblemDetails.extensions`.

- El comando de cotización **no** cambia el estado del proyecto: `D-88` exige un comando explícito y así se
  evita escribir la fila `projects` (con `rowversion`) en cada lote.
- La validación se repite en la transición, de modo que una carrera entre el último lote y la transición no
  deja el proyecto en `COTIZADO` con insumos pendientes.
- `SC-PROYECTOS-10` y `SC-PROYECTOS-11` dejan de ser deuda: quedan cubiertos por `T-U-29` y `T-I-47`.

## Risks / Trade-offs

- **[Diferencia entre el AIU unitario del APU y el AIU del proyecto]** -> El árbol no muestra AIU por ítem;
  el AIU oficial se aplica una sola vez sobre el costo directo total. El detalle del APU conserva su AIU de
  vista previa como información, no como valor del presupuesto.
- **[Datos incompletos ocultan el total]** -> `isComplete = false` y los contadores explican la causa; no se
  muestra un total parcial que pueda inducir a error.
- **[Ítems heredados sin APU]** -> Devuelven `null` y bloquean el total; la creación de ítems sin APU ya no
  existe desde `E-3b`. La regularización de datos históricos queda fuera de alcance.
- **[Costo de cálculo por lectura]** -> Consultas set-based y cálculo lineal; se mide en la verificación
  manual contra SQL Server. Si el volumen crece, se evalúa una proyección persistida en otra etapa.
- **[Acoplamiento de módulos]** -> `Application/Wbs` solo agrega la dependencia a `Application/Calculation`;
  `T-A-05` verifica que no aparezca `Application/Apus`.
- **[Respuesta de `move` sin costos]** -> El cliente debe refrescar el árbol; se documenta en
  `IMPACTO_FRONTEND.md` y se cubre con una prueba de integración.
- **[Lotes grandes de cotización]** -> Un `UPDATE` set-based y una respuesta sin la lista completa; el cliente
  refresca el listado. Se mide con trescientos insumos en `T-I-47`.
- **[Carrera entre el último insumo y la transición]** -> La transición revalida `REQ-PROYECTOS-05`; gana la
  validación y el proyecto no queda `COTIZADO` con pendientes.
- **[Cambio de precio después de cotizar]** -> Cotizar no congela el precio ni el estado: el presupuesto se
  recalcula y el insumo sigue `COTIZADO` mientras conserve los cinco datos (`REQ-INSUMOS-04`). Se documenta
  para el cliente.
- **[Un comando con tres modos]** -> El cuerpo discriminado agrega superficie de validación; se cubre con un
  escenario por modo, una prueba de contrato y el rechazo de combinaciones inválidas.

## Migration Plan

1. Desplegar el código y el registro de DI; no hay migración de base de datos ni backfill.
2. Verificar con datos reales que `GET /wbs` y `GET /budget` responden con los mismos costos directos, que el
   AIU se aplica una vez y que la cotización masiva deja `pendingResources` en `0` antes de la transición.
3. Rollback: revertir el despliegue; no hay datos persistidos que limpiar. Un insumo que quedó `COTIZADO`
   conserva ese estado, que es válido para el contrato.
4. Al archivar el change, `openspec` incorpora `calculo` y `cotizacion` a `openspec/specs/` y fusiona el delta
   de `edt`.

## Open Questions

Ninguna que cambie las specs, el enfoque o las tareas. La semántica B del capítulo, el valor de
`apu.unitPrice`, los nulos por incompletitud, la ruta `/budget` y el permiso `WBS_READ`, la forma del comando
masivo con sus tres alcances, el rechazo por ítem incompleto y la ubicación de la precondición de cierre
quedaron cerrados en `specs/` y en este diseño. El retroceso de un insumo cotizado queda fuera por `RF-11`.

## Traceability

| Requisito | Escenarios | RF / D | Prueba |
|---|---|---|---|
| `REQ-CALCULO-01` | `SC-CALCULO-01`…`04` | RF-10; D-06, D-121 | `T-U-05`, `T-I-46` |
| `REQ-CALCULO-02` | `SC-CALCULO-01`, `02`, `05`, `06` | RF-10; D-07, D-121 | `T-U-05`, `T-C-06` |
| `REQ-CALCULO-03` | `SC-CALCULO-09`…`12` | RF-17; D-94 | `T-U-09`, `T-I-16` |
| `REQ-CALCULO-04` | `SC-CALCULO-07`, `08`, `13`…`18` | RF-10; D-34, D-83, D-84 | `T-S-20` |
| `REQ-CALCULO-05` | `SC-CALCULO-19`…`21` | RF-10; D-08 | `T-U-05`, `T-I-46` |
| `REQ-COTIZACION-01` | `SC-COTIZACION-01`…`05` | RF-11; D-04, D-15, D-101 | `T-U-06`, `T-U-28`, `T-C-07` |
| `REQ-COTIZACION-02` | `SC-COTIZACION-06`…`09` | RF-05, RF-11; BR-009; D-89 | `T-U-06`, `T-U-28` |
| `REQ-COTIZACION-03` | `SC-COTIZACION-10`…`13` | RF-11; D-04, D-89, D-100 | `T-U-28`, `T-C-07`, `T-I-10` |
| `REQ-COTIZACION-04` | `SC-COTIZACION-14`…`20` | RF-11; D-15, D-34, D-83, D-84 | `T-S-21` |
| `REQ-COTIZACION-05` | `SC-COTIZACION-21`…`23` | RF-11, RF-14; D-08, D-92, D-111 | `T-U-06`, `T-I-10` |
| `REQ-PROYECTOS-05` (implementación) | `SC-PROYECTOS-10`, `SC-PROYECTOS-11` | RF-21; BR-007; D-89 | `T-U-29`, `T-I-47` |
| `REQ-EDT-23` | `SC-EDT-89`…`100` | RF-04, RF-10; D-06, D-118, D-121 | `T-U-25`, `T-U-26`, `T-C-06`, `T-I-46` |



