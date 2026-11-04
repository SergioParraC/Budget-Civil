# Cambio: e3a-rf05-insumos-proyecto

## Why

`E-3a` abre el contenido técnico del presupuesto: el APU no existe sin insumos (`R-02`) y `M-04` debe
preceder a `M-05` (`PLAN §4.3`). Hoy no existe la capacidad `insumos` en `openspec/specs/` ni el modelo
`Resource`; sin un delta que fije la tipología, el estado de cotización, la copia independiente y la
autorización, `E-3b` construiría sobre comportamiento no especificado y el frontend no tendría contrato
estable (`D-109`).

La etapa depende de `E-2` (proyecto y EDT): este cambio deja la especificación lista y **no inicia la
implementación** hasta que `E-2` esté archivado (`PLAN §4.2`, `§4.5.1`).

## What Changes

- Se crea la entidad `Resource` **propia de cada proyecto** (`project_id`), sin registro de insumos a
  nivel de compañía (`D-05`, `D-27`).
- Se admiten **siete tipos de insumo** desde el catálogo `ResourceType`: Materiales, Mano de obra,
  Herramienta, Equipos, Auxiliares, Trabajos y Matriz; la Matriz se registra aquí como tipo y su
  composición pertenece a `E-3b` (`D-16`, `PLAN §3.3.7`).
- Se fijan los campos del insumo: `description`, `unit`, `unit_price`, `brand`, `distributor`,
  `user_key`, `details`, `contact_number`, `quotation_status`, `municipality_id` y
  `origin_resource_id`; `municipality_id` **sustituye a `city`** (`D-96`).
- `quotation_status` se deriva de la completitud —descripción, unidad, precio y marca— entre
  `FALTA_INFORMACION` y `POR_COTIZAR`; el paso a `COTIZADO` pertenece a `RF-11` (`E-4a`) y queda
  **fuera de este cambio** (`BR-009`, `D-89`).
- Se permite **copiar un insumo desde otro proyecto de la misma compañía**; la copia conserva el precio,
  nace con el usuario que copia como autor y es **totalmente independiente**: los cambios posteriores no
  se propagan (`RF-06`, `R-17`, `D-27`, `D-29`).
- Se incorpora **búsqueda parcial por descripción** y **filtrado por tipo y estado de cotización** —más
  marca, municipio y rango de precio— con paginación `page`/`pageSize` (`RF-16`, `RF-20`, `D-101`).
- La **unicidad de insumos por proyecto es una sugerencia**, nunca una restricción técnica: el sistema
  advierte similitudes y el usuario decide (`Q-09`, `D-16`).
- Se registra **autoría** (`last_modified_by`, `last_modified_at`) y se aplica **concurrencia optimista
  con `rowversion`** con respuesta `409` (`D-08`, `D-50`, `D-97`).
- **Autorización** (`Matriz 1.3.2`, `D-34`): Director de proyectos y Presupuestador crean, editan y
  consultan; Administrador y Auditor **solo leen** (`D-83`, `D-84`).
- Se siembran los siete `ResourceType`; el módulo consume el catálogo `Municipality` ya existente y
  `municipality_id` sustituye a `city` (`D-96`). La migración sigue el runbook manual (`D-40`, `D-107`).

## Capabilities

### New Capabilities

- `insumos`: registro y tipología de los insumos de un proyecto, estado de cotización por completitud,
  sugerencia de unicidad, listado con búsqueda y filtros, copia independiente desde otro proyecto de la
  misma compañía, autoría, concurrencia y autorización de lectura o escritura.

### Modified Capabilities

- Ninguna: `identidad` y `proyectos` conservan sus requisitos. El cambio solo añade la capacidad
  `insumos`, ya prevista en el índice de `openspec/project.md` (`PLAN §6.2.3`).

## Fuera de alcance

- `COTIZADO` y el cambio masivo de estado: `RF-11`, etapa `E-4a` (`D-04`, `D-15`, `D-89`).
- Composición de la Matriz, `ApuResource`, copia de APUs y **reconciliación de insumos al incorporar un
  APU**: `E-3b` (`RF-07`, `RF-08`, `RF-09`, `D-25`).
- Copia desde el catálogo abierto y publicación: `E-5` (`RF-15`, `D-28`, `D-35`). La copia de compañía
  registra su procedencia en `origin_resource_id`, sin crear un vínculo de actualización.
- Conteo de uso (`RF-22`), cálculo (`RF-10`) y cierre (`RF-21`).
- Baja lógica del insumo: `D-49` la fija, pero ninguna `RF` de `E-3a` la expone; se especifica cuando se
  asigne a una etapa.
- Frontend, componentes y rutas (`D-109`); impacto de contrato documentado, no especificado.

## Decisiones que implementa

| Tema | Decisiones |
|---|---|
| Modelo de insumos | `D-05`, `D-16`, `D-27`, `D-29`, `D-49`, `D-50`, `D-96`, `D-97` |
| Copia y costos | `D-25` (solo el principio de sugerencia), `D-27`, `D-29`, `R-17` |
| Contrato y datos | `D-41`, `D-55`, `D-95`, `D-99`, `D-100`, `D-101`, `D-107` |
| Autorización | `D-02`, `D-34`, `D-83`, `D-84`, matriz `1.3.2` |
| Autoría y concurrencia | `D-08`, `D-50`, `D-97` |

**Precisiones de alcance de este cambio:**

| Punto | Decisión | Motivo |
|---|---|---|
| Estado `COTIZADO` | No se especifica su transición; solo `FALTA_INFORMACION` y `POR_COTIZAR` | `RF-11` es de `E-4a` y `D-89` la separa de `RF-05` |
| `origin_resource_id` | Apunta al insumo de origen en la copia de compañía; no propaga cambios | El ajuste del estudiante pide registrar la procedencia; `PLAN §3.3.2` debe actualizarse en ese punto |
| Alcance del Director | Proyectos de su compañía; el Presupuestador, solo los asignados | `REQ-PROYECTOS-14` y matriz `1.3.2` ya fijan ese alcance |
| Filtros | Tipo y estado obligatorios; marca, municipio y rango de precio añadidos | `RF-20` y `02 §2` los enumeran; `E-3a` es el único cambio de `M-04` |
| Copia | Un insumo por operación; la copia múltiple no se especifica | `RF-06` no la exige y `E-3b` añade la copia de APU |

## Criterios de aceptación

1. `openspec validate e3a-rf05-insumos-proyecto` pasa sin errores y sin advertencias en modo `--strict`.
2. Cada `REQ-INSUMOS-*` tiene al menos un `SC-INSUMOS-*` en formato `Dado / Cuando / Entonces`, y cita
   sus `RF`, `BR`, `R` y `D`.
3. Los escenarios cubren permisos, datos inválidos, concurrencia y aislamiento entre compañías
   (`PLAN §6.1.4`).
4. La copia demuestra independencia: cambiar el precio en el proyecto destino no altera el origen
   (`R-17`, `D-29`).
5. El modelo especificado contiene los siete tipos y los campos de `Resource` del `PLAN §3.3.2`
   (incluidos autoría, `rowversion` y `municipality_id` en lugar de `city`) (`D-96`).
6. La matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` queda completa al
   cierre del cambio (`PLAN §6.1.4`, `§9`).
7. Ningún requisito depende de un `X` abierto.

## Impact

- **Contrato de API**: nuevos endpoints bajo `/api/v1/projects/{id}/resources` (listado, consulta, alta,
  edición, copia y sugerencia de similitud), con `camelCase`, `ProblemDetails` y paginación
  `page`/`pageSize` (`D-99`, `D-100`, `D-101`); el cliente Angular se regenera en `E-6` o al abrir la
  etapa de interfaz (`D-56`, `D-109`).
- **Base de datos**: tabla `resources` con `project_id`, `resource_type_id`, `municipality_id`,
  `rowversion`, autoría y `origin_resource_id`; índices `resources(project_id, resource_type_id)`,
  `resources(project_id, quotation_status)` y `resources(project_id, description)` (`PLAN §3.3.5`); sin
  restricción única de descripción ni de `user_key`. Migración manual con respaldo y reversión conocida
  (`D-40`, `D-107`).
- **Catálogos**: `ResourceType` con los siete valores y `Municipality` como reemplazo de `City` (`D-96`);
  son de solo lectura para `M-04`.
- **Código**: módulo `M-04` en las cuatro capas (`Domain/Resources`, `Application/Resources`,
  `Infrastructure/Persistence`, endpoints en `Api`), sin dependencias de otros módulos más allá de
  `M-02` (`D-54`, `D-95`).
- **Permisos**: se añaden las claves de lectura y escritura de insumos al `seed` versionado e idempotente,
  con Director y Presupuestador en escritura y Administrador y Auditor solo en lectura (`D-82`, `D-83`,
  `D-84`).
- **Pruebas**: unitarias de completitud, copia y autorización; integración manual contra SQL Server
  local para persistencia, `rowversion` y aislamiento (`D-66`, `D-105`).
- **Dependencias**: ninguna nueva; se mantienen las prohibiciones de `D-104` y `D-106`.
