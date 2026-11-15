# Cambio: i-3b-apus-y-matrices

## Why

`E-3b` abre la composición técnica del presupuesto: sin APU no existe costo, firma ni cierre
(`RF-10`…`RF-13`) y el conteo de uso de insumos (`RF-22`) exige APUs vinculados a cantidades de obra
(`D-33`). Hoy no existe la capacidad `apus` en `openspec/specs/`, el código no tiene `Apu`,
`ApuResource` ni `MatrixComponent`, y `E-3a` difirió a esta etapa la composición de Matriz, su copia y la
reconciliación de sus componentes (`BR-021`). Sin este delta, `E-4a` construiría el cálculo sobre una
composición no especificada y el módulo `M-05` no tendría contrato estable (`D-109`).

**Dependencia cerrada:** `E-2b` está implementado y archivado (`2026-11-14-i-2b-rf04-edt`) y su
especificación se sincronizó a `openspec/specs/edt/`. `D-113` confirmó `apu_id` nullable con índice único
filtrado. Este cambio añade la clave foránea hacia `apus`, el método de vinculación del ítem y la baja
controlada del EDT (`INC-15`, `INC-16`).

## What Changes

- Se crea la entidad `Apu` **propia de cada proyecto**, sin `code` (`D-31`), sin `yield` (`D-123`) y sin
  baja lógica, vinculada **1:1 con un ítem del EDT** del mismo proyecto (`RF-07`, `BR-016`, `D-16`); el
  vínculo vive en `wbs_items.apu_id`.
- El **alta y la edición unifican ítem EDT + APU** en `POST /wbs/items` y `PUT /wbs/items/{itemId}`:
  `description`/`unit` obligatorios al crear, `quantityWork` obligatorio al editar y `resources` como
  reemplazo completo; se retiran `POST /apus`, `PUT /apus/{apuId}` y los endpoints de líneas (`D-122`).
- La **baja de un ítem elimina físicamente su APU y sus líneas de composición**, y conserva los insumos del
  proyecto. Un capítulo solo se elimina **vacío**; si contiene APUs responde `409 WBS_CHAPTER_HAS_APU` y si
  contiene otros nodos `409 WBS_NODE_HAS_CHILDREN` (`D-114`, `D-119` revisadas).
- El **árbol del EDT** expone por ítem la referencia del APU (`apuId`, estado, descripción, unidad,
  `unitPrice = 0` y `rowVersion`), que es la vía para llegar al contenido del APU del proyecto
  (`REQ-EDT-20`).
- Se crea `ApuResource` como composición con `consumption > 0` y `waste_percentage` entre 0 y 100
  (valor por defecto 0), y `MatrixComponent` para la composición de una Matriz, sin anidamiento
  (`RF-09`, `BR-008`, `D-48`).
- Se especifica la **copia de un APU desde otro proyecto de la misma compañía**: crea el ítem destino con
  `target { code, wbsChapterId, quantityWork }`, reconcilia los insumos simples y copia las Matrices y sus
  componentes siempre como registros nuevos, sin validar nombres ni similitudes (`RF-08`, `D-25`, `BR-021`,
  `D-122`).
- Se especifica la **búsqueda de APUs de otros proyectos** por descripción parcial, por código del ítem
  (`wbs_items`) y por departamento y municipio del proyecto de origen, con paginación y ubicación visible.
- Se especifica la **vista previa de costo del APU**: el detalle devuelve `description` y `resources`
  agrupado por `resourceType`, con el `subtotal` de cada insumo; fuera de `resources` devuelve `directCost`,
  `indirectCosts` (`administration`, `unforeseen`, `profit` y `total`) y `total`. El costo directo es
  `Σ(cantidad efectiva × precio unitario)`, con `cantidad efectiva = consumo × (1 + desperdicio/100) × yield`.
  La Matriz devuelve el mismo esquema de `resources`
  y `subtotal`, con `directCost` fuera del listado y **sin** `indirectCosts`. El **costo total del proyecto
  no se incluye** y permanece en `E-4a`.
- Se añaden los parámetros de proyecto `rounding_decimals` y `quantity_decimals` (0–10, por defecto 2) para
  el redondeo de todos los cálculos y el número de decimales de las cantidades; las columnas de cantidad
  pasan a `DECIMAL(18,10)`.
- Se añaden los permisos `APUS_READ` y `APUS_WRITE` al `seed` versionado e idempotente (`D-82`), con
  edición restringida a `ProjectMember`.
- **Se modifica `insumos`**: la completitud de una Matriz no se evalúa con los cinco campos de `BR-009`, la
  copia genérica de un insumo de tipo Matriz deja de ser válida y la edición de la unidad de una Matriz
  recalcula los APUs que la usan.
- **Se modifican `edt` y `proyectos`**: baja controlada y referencia del APU en el árbol; parámetros de
  redondeo en la configuración del proyecto.

## Capabilities

### New Capabilities

- `apus`: composición de un APU propio de un proyecto y vínculo 1:1 con el ítem del EDT, composición de
  Matrices y su reutilización, copia de APU y de Matriz, conteo de uso, vista previa de costo directo y AIU,
  autoría, concurrencia y autorización.

### Modified Capabilities

- `edt`: la baja del ítem elimina su APU, el capítulo solo se elimina vacío y el árbol expone la referencia
  del APU; la escritura queda reservada a miembros del proyecto.
- `proyectos`: la configuración incorpora los decimales de redondeo y de cantidades.
- `insumos`: `REQ-INSUMOS-04` incorpora la excepción de completitud de la Matriz y `REQ-INSUMOS-09` deja de
  admitir la copia genérica de una Matriz.

## Fuera de alcance

- **Cálculo total del proyecto** (`RF-10` completo): el costo por ítem, el total del presupuesto, la cascada
  de recálculo y las alertas de tope pertenecen a `E-4a`; este cambio solo expone la vista previa del APU y
  de la Matriz.
- **Cotización** (`RF-11`) y el paso a `COTIZADO`, incluidas las Matrices (`E-4a`).
- **Aprobaciones y cierre** (`RF-12`, `RF-13`, `RF-21`): `ApuApproval` y `D-53` se implementan en `E-4b`.
- **Catálogo abierto** (`RF-15`): publicación y copia desde el catálogo público (`E-5`, `D-28`, `D-35`).
- **Frontend y cliente Angular** (`D-109`): el contrato se documenta; el cliente se regenera al abrir la
  etapa de interfaz (`D-56`).

## Decisiones aplicadas

| Tema | Decisiones |
|---|---|
| Modelo APU y Matriz | `D-05`, `D-16`, `D-22`, `D-31`, `D-48`, `D-113` |
| Copia y reconciliación | `D-13`, `D-24`, `D-25`, `D-27`, `D-29`, `R-17`, `BR-018`, `BR-021` |
| Costo y AIU | `D-06` (ajustada), `D-07`, `D-32`, `D-33`, `R-21`, `RF-10` |
| Redondeo y cantidades | `D-121` (nueva), `D-93` |
| Persistencia y concurrencia | `D-50`, `D-95`, `D-97`, `D-107` |
| Contrato y datos | `D-41`, `D-55`, `D-99`, `D-100`, `D-101` |
| Autorización | `D-02`, `D-34`, `D-82`, `D-83`, `D-84`; matriz `1.3.2` |
| Autoría | `D-08`, `RF-14` |

## Precisiones de alcance

| Punto | Decisión de este cambio | Motivo |
|---|---|---|
| Rutas | `/api/v1/projects/{projectId}/apus`, `.../matrices` y `.../resource-usage` | `D-05`/`R-01` y el contrato *project-scoped* (`INC-03`) |
| Listado de APUs propios | Se consulta en `GET /wbs`; `/apus` lista solo APUs de otros proyectos de la compañía | El EDT es dueño de las actividades y de su APU (`REQ-EDT-20`) |
| Alta del APU | Exige un `wbsItemId` libre del mismo proyecto | `BR-016` y `D-16`; `INC-09` |
| Baja del APU | Física, junto con el ítem y sus líneas; los insumos se conservan | `D-114`/`D-119` revisadas; `INC-15` |
| Capítulo | Solo se elimina vacío; sin cascada | Decisión del autor; `INC-17` |
| Costo del APU | `Σ(consumo × (1 + desperdicio/100) × yield × precio)`, por unidad; sin `quantity_work` | `F1` del autor; `X-15` parcial |
| Conteo de uso | `consumo × (1 + desperdicio/100) × quantity_work`; expande un nivel de Matriz y no incluye `yield` | `R-21` revisada; `D-33` |
| Unidad del APU | `VARCHAR(5)`, expuesta en el detalle, el listado y el árbol; el ítem hereda esa unidad para su cantidad | `REQ-APUS-02`; `REQ-EDT-20` |
| AIU | Porcentajes del proyecto actual; nulo = 0; se muestra cada componente y el total | `D-07`, `R-04` |
| Matriz en el APU | Una sola línea con el costo directo de la Matriz y su identificador | Decisión del autor |
| Copia de Matriz | Sin validación de nombres, similitudes ni duplicados; siempre nuevos | `BR-021` ajustada; `INC-10` |
| Autoría de copia | Actor que copia en el APU, la Matriz y cada insumo | `RF-14`; `INC-18` |
| Completitud del APU | La línea de Matriz cuenta solo si la Matriz tiene unidad y componentes; el APU hereda su completitud y recalcula los APUs que la usan; sin precio se muestra `FALTA_INFORMACION` | `REQ-APUS-03` ajustada; `D-48` |

## Criterios de aceptación

1. `openspec validate i-3b-apus-y-matrices --strict` pasa sin errores ni advertencias.
2. Cada `REQ-APUS-*`, los `REQ-EDT-*` modificados y los `REQ-INSUMOS-*` tienen escenarios en formato
   `Dado / Cuando / Entonces` y citan sus `RF`, `BR`, `R` y `D`.
3. Los escenarios cubren permisos, datos inválidos, concurrencia, aislamiento entre compañías, baja del APU
   con el ítem, bloqueo de capítulos con APUs y copia sin validación de Matrices.
4. La vista previa devuelve costo directo, subtotales por tipo, AIU discriminado y total; con un precio
   nulo indica `FALTA_INFORMACION` y no calcula.
5. El esquema contiene `apus`, `apu_resources`, `matrix_components`, la FK `wbs_items.apu_id → apus` y los
   dos parámetros de decimales, con `CHECK`, FKs, índices y `rowversion`.
6. La copia demuestra independencia en ambos sentidos y la autoría del actor en todos los registros.
7. La matriz discriminada por tipo no aplica AIU y su costo directo es el precio unitario de la línea que
   la representa en el APU.
8. La matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` queda completa.

## Impact

- **Contrato de API**: 15 operaciones bajo `/api/v1/projects/{projectId}` (APUs, composición, copia de APU,
  Matrices, copia de Matriz y conteo de uso), más la referencia del APU en `GET /wbs` y los parámetros de
  configuración en `GET /projects/{id}/configuration` y `PUT /projects/{id}`, con `camelCase` y nombres de
  ruta/campo en inglés (`resources`, `subtotal`, `directCost`, `indirectCosts`, `total`),
  `ProblemDetails`, paginación `page`/`pageSize` y `409` en concurrencia.
- **Base de datos**: `apus` sin baja lógica y con `unit` `VARCHAR(5)`; `apu_resources` y `matrix_components`
  con `DECIMAL(18,10)` en `consumption`; `wbs_items.quantity_work` a `DECIMAL(18,10)`; FK
  `FK_wbs_items_apus_apu_id` (`RESTRICT`); `project_configurations.rounding_decimals` y
  `quantity_decimals`; migración `YYYYMMDDHHMM_apus_add_e3b` con respaldo y reversión.
- **Permisos**: `APUS_READ` para Administrador, Director, Presupuestador y Auditor; `APUS_WRITE` para
  Director y Presupuestador con `ProjectMember`; el conteo de uso se lee con `RESOURCES_READ`. `seed` `v8`
  con 37 permisos.
- **Código**: `Domain/Apus`, `Application/Apus` y `Application/Calculation`, `Infrastructure` con lecturas
  y escrituras de `wbs_items` y la composición, endpoints en `Api`, y los cambios de contrato en `M-03` y de
  recálculo de estado en `M-04`; sin dependencias de infraestructura de otros módulos (`D-54`, `D-95`).
- **Pruebas**: unitarias de dominio, costo y casos de uso; integración manual contra SQL Server local;
  `NetArchTest` para capas y módulos (`D-63`, `D-66`, `D-105`).
- **Documentación**: `PLAN_INGENIERIA.md` (`D-06`, `D-32`, `D-33`, `D-49`, `D-114`, `D-119`, `D-121`, `R-21`,
  `X-15`, `§2.4.2`, `§3.3.2`, `§3.3.3`, `§3.3.5`, `§3.3.6`, `§4.2`, `§5.6`), `.github/docs/02`, `05` y
  `docs/runbook-migraciones.md`.
