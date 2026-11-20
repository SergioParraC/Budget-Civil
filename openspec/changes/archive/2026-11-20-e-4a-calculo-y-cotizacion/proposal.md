# Cambio: e-4a-calculo-y-cotizacion

## Why

`E-3b` ya calcula el costo directo del APU y de la Matriz, pero el árbol del EDT sigue devolviendo
`apu.unitPrice = 0` y no expone costo unitario ni costo directo por ítem o capítulo; además no existe una
consulta que consolide el presupuesto del proyecto. Sin ese cálculo el usuario no puede ver el valor del
EDT, comparar el presupuesto con el tope configurado (`RF-17`) ni continuar hacia la cotización y la
aprobación (`E-4a`/`E-4b`). Además, ningún comando lleva hoy un insumo a `COTIZADO`: el valor existe en el
contrato (`QuotationStatus`) pero solo se produce `FALTA_INFORMACION` o `POR_COTIZAR`, y la precondición de
`REQ-PROYECTOS-05` —cien por ciento cotizado para pasar el proyecto a `COTIZADO`— no está implementada
(`D-89`). Este cambio cierra `RF-10` para el EDT y el proyecto, la alerta de tope `RF-17` y la cotización
masiva `RF-11`; la aprobación por APU y el cierre (`RF-12`, `RF-13`, `RF-21`) siguen en `E-4b`.

**Dependencia cerrada:** `E-3b` está implementado y archivado (`2026-11-17-i-3b-apus-y-matrices`);
`openspec/specs/apus/` está vigente y `ApuCostCalculator` ya aplica `D-06`, `D-07` y `D-121` con
`roundingDecimals` y `quantityDecimals`.

## What Changes

- `GET /projects/{projectId}/wbs` DEBE devolver por ítem `unitCost` (costo directo unitario del APU) y
  `directCost = cantidad de obra × unitCost`, y DEBE llenar `apu.unitPrice` con el costo directo unitario
  del APU (hoy siempre `0`). El AIU no se distribuye por ítem en el árbol.
- `GET /projects/{projectId}/wbs` DEBE devolver por capítulo `directCost` como la suma de los costos
  directos de **todos los ítems de su subárbol, cada uno una sola vez**; no suma los subtotales de los
  capítulos hijos como un ítem adicional. Un capítulo sin ítems en su subárbol devuelve `0`.
- Los valores del árbol se calculan en cada lectura desde los APUs, las cantidades y la configuración
  vigentes; no se persisten columnas de costo y no hay migración. Un movimiento de capítulo o de ítem se
  refleja en la siguiente consulta.
- Se agrega `GET /api/v1/projects/{projectId}/budget`, que devuelve el costo directo del proyecto como
  `Sum(unitCost × cantidad)` sobre todos los ítems (incluidos los ítems raíz), el AIU del proyecto
  discriminado por componente (`administration`, `unforeseen`, `profit`) con porcentaje e importe, el costo
  indirecto total y el valor total `costo directo + costo indirecto`.
- El endpoint de presupuesto incluye `budgetLimit` (nullable) e `isOverBudget` para `RF-17`; la alerta solo
  se activa con un total calculable y un tope configurado.
- Se agrega `PATCH /api/v1/projects/{projectId}/resources/quotation-status`, que marca insumos como
  `COTIZADO` en una sola operación por **selección** de `resourceIds`, por **filtro** del listado o por
  **totalidad** del proyecto, y devuelve el resumen por insumo y los insumos pendientes del proyecto.
- Solo los insumos completos pasan a `COTIZADO`; los incompletos se omiten y se reportan con su código, sin
  inventar precio ni bloquear el resto del lote. La operación es idempotente y actualiza la autoría de cada
  insumo (`RF-14`).
- Se implementa la validación pendiente de `REQ-PROYECTOS-05`: la transición del proyecto a `COTIZADO`
  exige el cien por ciento de los insumos cotizados y responde `409` con el conteo pendiente (`D-89`).
- Si un ítem no tiene APU o su APU está incompleto por falta de precio, los costos del ítem son `null`, el
  capítulo que lo contiene es `null` y el presupuesto devuelve `isComplete = false` sin total parcial.
- Se reutiliza `ApuCostCalculator` como única fórmula; no se duplica el cálculo de costo directo, AIU ni
  redondeo.
- Se agrega la capacidad `calculo` (M-06), se agrega la capacidad `cotizacion` (M-07) y se modifica `edt`
  (M-03) para los valores del árbol.

**Fuera de alcance:** `RF-12`/`RF-13`/`RF-21` (aprobación por APU y cierre, `E-4b`), `RF-15` (catálogo
público, `E-5`), el retroceso de un insumo cotizado a `POR_COTIZAR` (`RF-11` solo define el paso a
`COTIZADO`), frontend y cliente Angular (`D-109`).

## Capabilities

### New Capabilities

- `calculo`: cálculo del presupuesto de un proyecto (costo directo, AIU discriminado, costo indirecto y
  total) y alerta de tope presupuestal (`RF-10`, `RF-17`; M-06).
- `cotizacion`: cambio masivo de insumos a `COTIZADO` por selección, filtro o totalidad, con resumen por
  insumo y habilitación de la transición del proyecto (`RF-11`, `RF-14`; M-07).

### Modified Capabilities

- `edt`: el árbol del EDT expone costo unitario y costo directo por ítem y costo directo por capítulo,
  derivados de los APUs y de la cantidad de obra (`RF-04`, `RF-10`; M-03).

## Impact

- **Contrato de API**: `GET /wbs` agrega `unitCost` y `directCost` por ítem y `directCost` por capítulo y
  llena `apu.unitPrice`; `GET /projects/{id}/budget` es una operación nueva con `camelCase`,
  `ProblemDetails`, `budgetLimit`, `isOverBudget` e `isComplete`; `PATCH /projects/{id}/resources/quotation-status`
  es una operación nueva con `scope` (`SELECTION`/`FILTER`/`ALL`), resumen por insumo y `ProblemDetails`. El
  presupuesto usa `WBS_READ`; la cotización usa `RESOURCES_WRITE`; el alcance es por compañía y proyecto.
- **Base de datos**: sin cambios de esquema ni migración; los costos son derivados y el estado de cotización
  ya existe en `resources.quotation_status`.
- **Código**: `Application/Calculation` (consulta, DTOs, lector y servicio de acceso), lector de costos por
  proyecto en `Infrastructure`, enriquecimiento de `GetWbsTreeQueryHandler` y `WbsDtos`/`WbsMapper`, un
  controlador de presupuesto, el comando de cotización masiva en `Application/Resources` con su lector de
  candidatos por filtro, la validación de `REQ-PROYECTOS-05` en `ChangeProjectStatusCommandHandler` y el
  registro en DI. `Application/Wbs` no depende de `Application/Apus` (`T-A-03`); el motor compartido vive en
  `Application/Calculation`.
- **Pruebas**: unitarias del cálculo, de la agregación por capítulo y de los tres modos de cotización con su
  resumen; contrato del árbol, del presupuesto y de la cotización; seguridad de alcance y aislamiento en ambas
  operaciones; arquitectura de módulos. La integración se verifica manualmente contra SQL Server local
  (`D-66`).
- **Documentación**: `PLAN_INGENIERIA.md` (`RF-10`, `RF-11`, `RF-17`, `D-04`, `D-06`, `D-07`, `D-15`,
  `D-89`, `D-121`, `§3.4.3` y `§5.6.1`/`§5.6.8`), `docs/02`, `docs/05` y el índice de capacidades de
  `openspec/project.md §8`, donde la fila `RF-11` pasa de `aprobaciones`/`E-4b` a `cotizacion`/`E-4a`;
  `IMPACTO_FRONTEND.md` del change.

## Criterios de aceptación

1. `openspec validate e-4a-calculo-y-cotizacion --strict` pasa sin errores ni advertencias.
2. Cada `REQ-CALCULO-*`, `REQ-COTIZACION-*` y `REQ-EDT-23` tiene escenarios `Dado / Cuando / Entonces` y
   cita sus `RF`, `BR`, `R` y `D`.
3. El árbol devuelve costo unitario y costo directo por ítem y costo directo por capítulo; un capítulo con
   subcapítulos suma los ítems de su subárbol una sola vez y no duplica valores.
4. El presupuesto devuelve costo directo, AIU discriminado, costo indirecto y total; el AIU se aplica una
   sola vez sobre el costo directo total.
5. Un APU incompleto o un ítem sin APU devuelve `null` y `isComplete = false`, sin total parcial.
6. La alerta de tope se activa solo con total calculable y tope configurado.
7. La cotización masiva cotiza por selección, filtro y totalidad en una sola operación y devuelve
   `updated`, `alreadyQuoted`, `skipped` y los insumos pendientes del proyecto.
8. Un insumo incompleto nunca pasa a `COTIZADO`: se omite con su código y el resto del lote se aplica; la
   operación es idempotente.
9. La transición del proyecto a `COTIZADO` exige el cien por ciento cotizado y responde `409` con el conteo
   pendiente, sin cambiar el estado.
10. La matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` queda completa.
