# Cambio: i-2b-rf04-edt

## Why

`E-2` («Proyecto y estructura») agrupaba `M-02` y `M-03`, pero `E-1b` absorbió `M-02` y la estructura del
trabajo (EDT) nunca se especificó ni se implementó. La consecuencia es que `I-3b` (APUs y matrices) queda
bloqueado: `RF-07` y `BR-016` exigen que el APU se vincule a un único ítem de la EDT y `RF-22` y `R-21`
exigen `quantity_work` para el conteo de uso. Además, `.github/docs/02 §4` deja la fila `EDT` sin
`permission_key` y `SecurityBaselineSeeder` (`v6`) no contiene claves `WBS_*`, así que ningún endpoint de
la EDT podría autorizarse sin violar `D-82` o caer en `403` por fail-closed.

El uso real de la EDT exige más que crear un árbol: el presupuestador reorganiza capítulos y actividades
mientras arma el presupuesto, y necesita que el código de cada ítem sea un identificador estable. Por eso
este cambio exige que el usuario ingrese el código completo al crear (la aplicación no numera ni renumera),
valida su formato y unicidad con un endpoint propio, habilita el movimiento libre de nodos —incluidos ítems
en la raíz—, convierte la baja de un capítulo en una cascada confirmada y registra la actividad por
proyecto y usuario. Para cerrar la brecha se registra el incremento `I-2b` / etapa `E-2b`
(`PLAN §2.4.2`, `§4.2`, v0.33), con `D-113` a `D-120`.

## What Changes

- Se crea la capacidad `edt` con `RF-04`: árbol de capítulos e ítems por proyecto, sin límite de
  profundidad ni de hijos (`R-18`, `D-30`), con ítems dentro de un capítulo **o en la raíz del presupuesto**.
- Se modela el código en una tabla propia por proyecto (`WbsCode`), compartida entre capítulos e ítems,
  con índice único sin distinguir mayúsculas (`D-30`, `D-51`, `BR-015`).
- Se implementa el **código manual obligatorio**: el usuario ingresa el código completo al crear o editar,
  sin numeración automática, sin composición de prefijos y sin código de respaldo. El sistema valida
  formato, longitud (1–10) y unicidad por proyecto con `GET /wbs/codes/validate` (`D-117`, `R-19`).
- **BREAKING**: el código de un nodo es **inmutable desde la aplicación**. Ni la baja de un hermano, ni el
  reordenamiento, ni el movimiento, ni el renombrado de otro capítulo, ni la edición de su propio padre
  reescriben un código existente; solo la edición explícita de ese mismo nodo puede cambiarlo (`D-117`).
- **BREAKING**: se elimina el **código de respaldo** y `WBS_CODE_GENERATION_FAILED`; el código se rechaza con
  `WBS_CODE_INVALID` si no cumple 1–10 caracteres o `R-19`.
- **BREAKING**: `wbs_items.wbs_chapter_id` pasa a **nullable** para admitir actividades en la raíz, sin
  capítulo padre (`D-118`).
- Se habilita la **reorganización libre**: mover un capítulo hoja, un capítulo con todo su subárbol o un
  ítem a cualquier capítulo del mismo proyecto o a la raíz, en la posición indicada, conservando códigos.
- **BREAKING**: la **baja de un capítulo con descendientes** deja de rechazarse y pasa a ser una **cascada
  confirmada**: sin confirmación explícita se responde `409 WBS_NODE_HAS_CHILDREN`; confirmada, se elimina el
  subárbol completo con sus códigos (`D-119`, revisa `D-114`). La base conserva `RESTRICT`.
- Se **compactan las posiciones** (`orderIndex`) al eliminar o mover, sin tocar códigos (`D-117`).
- Se registra la **actividad del EDT** en una fila por proyecto y usuario (`wbs_project_activity`), con la
  fecha UTC de la última modificación de ese usuario; se consultan la última actividad y la lista de
  participantes en `GET /wbs/activity`, sin historial de movimientos (`D-120`, `D-08`).
- Se documenta en `design.md`, como **guía no normativa**, el contrato de interacción que necesitará la
  interfaz para arrastrar, soltar, confirmar cascadas y validar códigos sin renumerar en el cliente.
- Se fijan el **conjunto cerrado de caracteres** (mayúsculas y minúsculas) y la longitud 1–10 del código
  completo tanto en capítulos como en ítems (`R-19`, `D-115`, `D-117`).
- Se registra la **cantidad de obra** del ítem (`quantity_work > 0`) y se deja `apu_id` **nullable**, con
  unicidad 1:1 garantizada por índice único filtrado para cuando llegue `I-3b` (`D-113`, `BR-016`).
- Se añade **búsqueda parcial** por código de ítem o nombre de capítulo, con paginación `page`/`pageSize`
  (`RF-04`, `D-101`), y el **orden** de nodos e ítems por `orderIndex`.
- Se aplica **concurrencia optimista** con `rowversion` y respuesta `409` en capítulos, ítems y movimientos
  (`D-50`, `D-97`), y **autoría** de la última modificación (`D-116`).
- Se incorporan `WBS_READ` y `WBS_WRITE` al `seed` versionado e idempotente con la asignación de la matriz
  `1.3.2` (`D-82`): Administrador lectura, Director lectura y escritura en los proyectos de su compañía,
  Presupuestador lectura y escritura en los proyectos asignados, Auditor lectura en los proyectos asignados.

## Capabilities

### New Capabilities

- `edt`: estructura de trabajo del proyecto —árbol recursivo de capítulos e ítems, códigos manuales únicos
  e inmutables con validación de disponibilidad, reorganización libre, baja en cascada, cantidad de obra,
  orden, búsqueda, autoría, concurrencia, actividad y participantes, y autorización de lectura o escritura—.

### Modified Capabilities

- Ninguna: `proyectos` conserva sus requisitos (`RF-03` no cambia de comportamiento); el cambio solo añade
  la capacidad `edt`, prevista en `openspec/project.md §8` y en `PLAN §6.2.3`.

## Fuera de alcance

- Implementar la interfaz gráfica, sus componentes, rutas o el cliente Angular: el contrato de interacción
  de `design.md` es guía no normativa para una etapa posterior (`D-109`).
- Historial de movimientos o de acciones del EDT: `D-08` prohíbe el seguimiento y `D-120` solo guarda la
  actividad por proyecto y usuario.
- Creación, composición y copia de APUs, matrices y reconciliación de insumos: `I-3b` (`RF-07` a `RF-09`,
  `RF-22`, `D-25`).
- Asignación de `apu_id` a un ítem y su clave foránea: `I-3b`; en `I-2b` el ítem se crea **sin APU**
  (`D-113`).
- Cálculo de costos, AIU y alertas de tope: `I-4a` (`RF-10`, `RF-17`, `X-15`).
- Aprobaciones, firma y cierre: `I-4b` (`RF-12`, `RF-13`, `RF-21`).
- Importación, exportación y catálogo abierto: `RF-19`, `I-5` (`D-18`, `D-28`).

## Citas

| Ámbito | Fuentes |
|---|---|
| Funcionalidad y reglas | `RF-04`, `RF-14`; `BR-015`, `BR-016`; `R-18`, `R-19`, `R-20`, `R-21`; `S-04` |
| Decisiones del EDT | `D-30`, `D-31`, `D-51`; `X-24`, `Q-26` |
| Decisiones revisadas | `D-30` (código manual), `D-114` (baja en cascada), `D-115` (1–10 sin respaldo) |
| Decisiones nuevas | `D-117`, `D-118`, `D-119`, `D-120` |
| Modelo y persistencia | `D-93`, `D-95`, `D-113`, `D-115`, `D-116`, `D-118`, `D-119`, `D-120` |
| Autoría y concurrencia | `D-08`, `D-50`, `D-97`, `D-116`, `D-120` |
| Contrato y datos | `D-41`, `D-55`, `D-95`, `D-99`, `D-100`, `D-101`, `D-107` |
| Autorización | `D-02`, `D-34`, `D-82`, `D-83`, `D-84`; matriz `1.3.2` |
| Plan | `PLAN §2.4.2` (`I-2b`), `§2.4.3`, `§3.3.2`, `§3.3.3`, `§3.3.5`, `§3.3.6`, `§4.2` (`E-2b`), `§4.3`, `§4.5.3`, `§5.6.7`, `§6.2.3` |

**Precisiones de alcance de este cambio:**

| Punto | Decisión | Motivo |
|---|---|---|
| `apu_id` | Nullable y sin clave foránea hasta `I-3b` | `D-113`: la EDT se construye antes que el APU y la tabla `apus` no existe |
| `wbs_items.wbs_chapter_id` | Nullable: ítem raíz sin capítulo padre | `D-118`: reorganizar una actividad a la raíz del presupuesto |
| Código | Manual, obligatorio, completo, 1–10, mayúsculas/minúsculas y único por proyecto | `D-117`: el usuario es responsable del identificador; el sistema valida |
| Validación de código | `GET /wbs/codes/validate` devuelve `isValid`, `isAvailable` y `reason` | `D-117`: la interfaz valida antes de enviar y el servidor revalida |
| Prefijo al renombrar | No hay prefijos ni composición; los descendientes conservan su código | `D-117`: la jerarquía se determina por padre-hijo |
| Baja con hijos | Cascada confirmada; `409 WBS_NODE_HAS_CHILDREN` sin confirmación | `D-119` revisa `D-114`: la confirmación evita pérdidas accidentales |
| Ítem con APU | Se elimina el ítem y el APU no se elimina; queda desvinculado y seleccionable | Decisión del autor; `I-3b` define el estado y la búsqueda del APU |
| Actividad | Una fila por proyecto y usuario; participantes y `MAX(last_modified_at)`; sin historial | `D-120`, `D-08`: interesa quién participó y la última modificación |
| Respuesta del árbol | Códigos tal como se guardaron; el cliente no compone ni renumera | `D-117`: el código es un identificador libre |
| Frontend | Fuera de alcance; contrato de interacción solo en `design.md` | `D-109` |
| Dependencia de `I-3b` | Se traza por `RF-07`, `BR-016`, `RF-22` y `R-21` | `INC-01`/`INC-02` no existen en el repositorio (`POR CONFIRMAR`) |

## Criterios de aceptación

1. `openspec validate "i-2b-rf04-edt" --strict` pasa sin errores y sin advertencias.
2. Cada `REQ-EDT-*` tiene al menos un `SC-EDT-*` en formato `Dado / Cuando / Entonces` y cita sus `RF`,
   `BR`, `R` y `D`.
3. Los escenarios cubren permisos, datos inválidos, concurrencia y aislamiento (`PLAN §6.1.4`).
4. El código manual obligatorio, su formato 1–10, la aceptación de mayúsculas/minúsculas, la unicidad
   case-insensitive y el endpoint de validación cumplen `D-117` y `R-19`.
5. La reorganización cubre capítulo hoja, capítulo con subárbol, ítem entre capítulos, raíz para ambos,
   mismo padre, ciclo, destino inválido, `targetIndex` y compactación de posiciones (`D-118`).
6. La baja en cascada exige confirmación y `rowVersion`, elimina el subárbol y compacta posiciones; el ítem
   con APU se elimina y el APU queda desvinculado y seleccionable (`D-119`).
7. La actividad se guarda una fila por proyecto y usuario, se conserva sin nodos y se consulta con
   participantes y última actividad; el Auditor no accede (`D-120`).
8. La unicidad sin distinguir mayúsculas, el conjunto de caracteres, la cantidad de obra y la unicidad 1:1
   del APU quedan garantizadas (`D-113`, `D-115`, `D-117`).
9. `WBS_READ` y `WBS_WRITE` existen en el `seed` y en `.github/docs/02 §4`, con `403` verificable.
10. La matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` queda completa.

## Impact

- **Contrato de API**: rutas bajo `/api/v1/projects/{projectId}/wbs` (`GET` árbol, `GET search`,
  `GET activity`, `GET codes/validate`, `POST`/`PUT`/`DELETE` de capítulos e ítems, `POST reorder`,
  `POST move`), con `camelCase`, `ProblemDetails` con `extensions.code` y paginación `page`/`pageSize`
  (`D-99`, `D-100`, `D-101`).
- **Base de datos**: tablas `wbs_codes`, `wbs_chapters`, `wbs_items` y `wbs_project_activity` con
  `rowversion` donde aplica, autoría, `CHECK` de cantidad y de formato, FKs `RESTRICT`, índice único sin
  distinguir mayúsculas e índice único filtrado de `apu_id`; `wbs_items.wbs_chapter_id` nullable; migración
  manual `wbs_add_i2b` con respaldo y reversión (`D-40`, `D-107`).
- **Código**: módulo `M-03` en las cuatro capas (`Domain/Wbs`, `Application/Wbs`,
  `Infrastructure/Persistence`, `WbsController`), sin dependencia de la infraestructura de `M-02` (`D-54`).
- **Permisos**: `WBS_READ` y `WBS_WRITE` en `SecurityBaselineSeeder` con `SeedVersion = v7`; el Auditor no
  accede a la actividad.
- **Pruebas**: unitarias de código manual, validación, inmutabilidad, formato, unicidad, cantidad, movimiento
  y alcance; arquitectura de capas y de módulo; integración manual contra SQL Server local (`D-66`,
  `D-105`).
- **Documentación**: `PLAN_INGENIERIA.md` (`D-117`…`D-120`, `§2.4.2`, `§3.3.2`, `§3.3.3`, `§3.3.6`, `§4.2`,
  `§5.6.7`), `.github/docs/02` y `.github/docs/05`.
- **Dependencias**: ninguna nueva; se mantienen las prohibiciones de `D-104` y `D-106`.
