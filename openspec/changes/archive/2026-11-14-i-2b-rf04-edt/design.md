# Design: i-2b-rf04-edt

## Context

`openspec/specs` contiene `identidad`, `proyectos`, `insumos` y `proveedores`; no existe la capacidad
`edt`. En `backend/src` no hay ninguna clase `Wbs*`, ni configuración EF, ni migración `wbs_add_i2b`: las
carpetas `Domain/Wbs`, `Application/Wbs` y `Persistence/Configurations/Wbs` están vacías. El módulo `M-02`
ya está implementado (`ProjectsController`, `REQ-PROYECTOS-01…21`), de modo que la brecha es solo la
estructura del trabajo y se implementa desde cero con las reglas de este diseño. Ver `proposal.md` para la
motivación.

Restricciones vigentes que condicionan el diseño:

- `BudgetCivilDbContext` es único (`D-95`) y las configuraciones EF viven en
  `Infrastructure/Persistence/Configurations/<Módulo>`.
- La autorización se resuelve por `permission_key` con `PermissionAuthorizationHandler` y fail-closed
  (`D-82`). El alcance por proyecto se resuelve en el caso de uso con `IActorAccessReader` y un contrato de
  lectura propio del módulo (`D-54`, `3.4.6`).
- `Apu` no existe todavía: la tabla `apus` llega en `I-3b`. Cualquier FK hacia ella es imposible hoy.
- `SecurityBaselineSeeder` está en `v6` y reconcilia por diferencia (`REQ-IDENTIDAD-30`).
- Las migraciones son manuales, con respaldo y reversión, y siguen `D-40` y `D-107`.

## Goals / Non-Goals

**Goals:**

- Modelar `WbsCode`, `WbsChapter`, `WbsItem` y `WbsProjectActivity` con las reglas de `D-30`, `D-51`,
  `D-113` a `D-120`.
- Aislar la validación manual de códigos y el movimiento como lógica pura de dominio, sin EF ni ASP.NET.
- Resolver el alcance por compañía y proyecto sin que `M-03` dependa de la infraestructura de `M-02`.
- Exponer las rutas project-scoped con `WBS_READ` y `WBS_WRITE`, y sembrar ambos permisos.
- Cubrir con pruebas unitarias, de arquitectura, de contrato e integración manual.
- Dejar documentado, sin carácter normativo, el contrato de interacción que usará la interfaz.

**Non-Goals:**

- Crear APUs, asignar `apu_id` o agregar la FK hacia `apus` (`I-3b`).
- Calcular costos, AIU o alertas (`I-4a`), y aprobar o cerrar (`I-4b`).
- Copiar ramas del árbol entre proyectos: `RF-04` no lo pide.
- Mover un nodo entre proyectos: prohibido por `REQ-EDT-01`.
- Implementar el frontend, sus componentes o el cliente Angular (`D-109`).
- Llevar historial de movimientos o de acciones: `D-08` y `D-120` lo prohíben.

## Decisions

### D1. Modelo de dominio

`BudgetCivil.Domain/Wbs` contiene cuatro entidades y dos elementos de apoyo:

```text
WbsNodeType            CHAPTER | ITEM   (persistido como VARCHAR con CHECK)
WbsNodeTypeCodes       conversión entre el enum y el código persistido

WbsCode                Id, ProjectId, Code, NodeType
WbsChapter             Id, ProjectId, ParentChapterId?, CodeId, Name, OrderIndex,
                       LastModifiedByUserId, LastModifiedAtUtc, RowVersion
WbsItem                Id, ProjectId, WbsChapterId?, ApuId?, CodeId, QuantityWork, OrderIndex,
                       LastModifiedByUserId, LastModifiedAtUtc, RowVersion
WbsProjectActivity     ProjectId, UserId, LastModifiedAtUtc
```

- `WbsCode` es la única fuente de unicidad: capítulos e ítems comparten la tabla (`D-51`). El nodo apunta a
  su código con `CodeId`; el código no se duplica en la entidad del nodo (`D-31`).
- `WbsItem.WbsChapterId` es **nullable**: un ítem sin capítulo es un ítem raíz (`D-118`).
- `WbsItem.ProjectId` existe aunque sea derivable de `WbsChapter.ProjectId`, por el principio
  `PLAN §3.3.1.2`; la aplicación lo fija desde la ruta y nunca desde el cliente.
- `ApuId` es `Guid?` y **no tiene navegación ni FK** hasta `I-3b` (`D-113`).
- La autoría (`LastModifiedByUserId`, `LastModifiedAtUtc`) se fija en cada método de mutación (`D-116`); no
  hay setters públicos para `ProjectId`, `CodeId`, `RowVersion`, `LastModifiedByUserId`,
  `LastModifiedAtUtc`, `OrderIndex` ni `WbsChapterId`.
- Los constructores de fábrica (`WbsChapter.Create`, `WbsItem.Create`) validan nombre, cantidad y posición, y
  reciben el `WbsCode` creado a partir del código manual ya validado.
- Los métodos de mutación son `Rename(name)`, `ChangeCode(code)`, `MoveTo(parentChapterId, orderIndex)` y
  `ChangeQuantity(quantityWork)`; ninguno modifica el código ni los nodos vecinos.
- `WbsProjectActivity` se identifica por `(ProjectId, UserId)` como clave compuesta: una fila por proyecto y
  usuario, con la última modificación de ese usuario.

### D2. Código manual, único y validado

`Domain/Wbs/WbsCodePolicy` es lógica pura y expone dos operaciones:

```text
IsValidCode(code)          // 1-10 caracteres y conjunto cerrado de R-19 (A-Z, a-z, 0-9 y símbolos)
NormalizeCode(code)        // trim; conserva mayúsculas y minúsculas escritas por el usuario
```

**Código manual.** El usuario ingresa el **código completo** al crear o editar el nodo; no hay segmentos,
composición con el padre, herencia de prefijo ni numeración automática (`D-30`, `D-117`). El campo `code`
es obligatorio: un valor vacío produce `WBS_CODE_INVALID`. El código mide 1–10 caracteres tanto en capítulos
como en ítems y se valida con `R-19`. La jerarquía se determina por `parent_chapter_id` / `wbs_chapter_id`,
no por el texto del código; por eso `CIM.1` puede ser hijo de `EST` o de `CIM`.

**Mayúsculas y minúsculas.** Se aceptan ambas. El valor se guarda con `Trim` y conserva la caja escrita; la
unicidad se evalúa sin distinguir mayúsculas mediante la intercalación `Latin1_General_100_CI_AS`.

**Inmutabilidad.** El código no se recalcula después de la creación (`D-117`):

1. **Baja de un hermano:** se elimina el nodo y su código; los códigos restantes no se tocan.
2. **Reordenamiento:** solo cambian `orderIndex`; los códigos no.
3. **Movimiento:** cambia `parentChapterId`/`WbsChapterId` y la posición; los códigos no.
4. **Renombrado de un capítulo:** cambia únicamente su propio código. Los descendientes conservan su código.
5. **Edición explícita del código de un nodo:** solo ese nodo cambia, con la misma validación de formato y
   unicidad; no se reescribe ningún descendiente.

**Validación de disponibilidad.** `GET /api/v1/projects/{projectId}/wbs/codes/validate` recibe `code` y
`excludeNodeId` opcional (para edición) y responde `200`:

```json
{ "code": "CIM.1", "isValid": true, "isAvailable": true, "reason": null }
```

`reason` es `WBS_CODE_INVALID` (formato o longitud), `WBS_CODE_DUPLICATED` (ya existe en el proyecto) o
`null`. El endpoint no crea ni modifica datos.

**Eliminado.** No existen `ComposeEffectiveCode`, `NextNaturalSegment`, `NextFallbackCode`, `ExceedsLimit`,
`OwnSegment`, `WBS_CODE_GENERATION_FAILED` ni el código de respaldo. El punto 1.1 del autor queda superado
por la decisión 1.3.

### D3. Reorganización: mover y reordenar

**Conjunto de hermanos.** Los hijos de un padre son la unión de sus subcapítulos y sus ítems ordenada por
`orderIndex`; en la raíz se unen los capítulos raíz y los ítems raíz.

**Semántica de `targetIndex`.** Si el nodo ya está en el padre destino, se retira primero de la lista de
hermanos; después se inserta en la lista restante en la posición `targetIndex`. El índice válido va de `0` a
`n`, donde `n` es la cantidad de hermanos **restantes** del destino; `null` o `n` significan «al final». Los
hermanos del destino desde el índice se desplazan a la derecha y el origen se compacta si es otro padre.

```text
[A, B, C]  mover A a targetIndex=1  -> [B, A, C]
[A, B, C]  mover C a targetIndex=0  -> [C, A, B]
[A, B, C]  mover B a targetIndex=2  -> [A, C, B]
[A, B, C]  mover B a targetIndex=3  -> 400 VALIDATION_ERROR
```

**Comando de movimiento** (`MoveWbsNodeCommand`):

1. Resolver el actor y el proyecto con `EnsureWriteAsync`.
2. Cargar el nodo movido y validar `rowVersion`; si no coincide, `409 WBS_CONCURRENCY_CONFLICT`.
3. Si el destino no es nulo, cargarlo en el mismo proyecto y exigir que sea un capítulo; si no existe o es de
   otro proyecto, `404 WBS_TARGET_NOT_FOUND`; si es un ítem, `400 VALIDATION_ERROR`.
4. Si el nodo es un capítulo y el destino es él mismo o un descendiente suyo, `400 WBS_MOVE_CYCLE`. La
   comprobación recorre la cadena de padres del destino, que es acotada por la profundidad del árbol.
5. Retirar el nodo de la lista de origen si ya está en el padre destino; validar el índice destino (`0…n`
   sobre la lista restante); fuera de rango, `400 VALIDATION_ERROR`.
6. Abrir una transacción, insertar el nodo en el destino en el índice indicado, desplazar las posiciones de
   los hermanos del destino, fijar el nuevo padre y compactar el origen si es distinto; actualizar
   `wbs_project_activity` en la misma transacción.
7. Devolver el nodo movido (`200`) con su nueva posición y su `rowVersion`; los códigos no se recalculan.

**Mover a la raíz.** `targetParentChapterId = null` deja el capítulo como raíz o el ítem como ítem raíz, sin
capítulo padre (`D-118`). El nodo conserva su código, aunque su prefijo ya no corresponda a la raíz.

**Reordenamiento por padre.** `POST reorder` recibe `parentChapterId` (nulo para la raíz) y la lista ordenada
de `(nodeType, nodeId)`; exige que la lista contenga exactamente los hijos vigentes del padre, sin agregados
ni faltantes. Guarda `orderIndex` de `0` a `n-1` y no toca códigos.

**Concurrencia.** `PUT`, `POST move` y `DELETE` exigen `rowVersion` del nodo objetivo; la primera transacción
que confirma actualiza la versión y la segunda recibe `409 WBS_CONCURRENCY_CONFLICT` sin modificar nada
(`D-50`). En una cascada, el `rowVersion` es el del capítulo raíz. `POST reorder` no tiene un nodo único: se
mantiene *last-write-wins* y la lista completa se valida contra los hijos vigentes; si no coincide, responde
`400 VALIDATION_ERROR`.

**Alternativa descartada:** un único endpoint que reciba el árbol completo y lo reemplace. Es más simple de
programar, pero pierde la validación por nodo, la concurrencia optimista y el rastro de qué cambió.

### D4. Baja en cascada

La base conserva `RESTRICT` en `wbs_chapters.parent_chapter_id`, `wbs_items.wbs_chapter_id` y
`wbs_codes`; la cascada la ejecuta la aplicación dentro de una transacción (`D-119`):

1. Cargar el subárbol del capítulo o el ítem objetivo y validar el `rowVersion` recibido del nodo objetivo
   (en cascada, del capítulo raíz); si no coincide, `409 WBS_CONCURRENCY_CONFLICT` sin borrar nada.
2. Sin `cascade = true` y con descendientes, responder `409 WBS_NODE_HAS_CHILDREN` sin borrar nada.
3. Con cascada confirmada, borrar primero los ítems, luego los capítulos de mayor profundidad a menor y por
   último la raíz del subárbol; después borrar los `wbs_codes` de todos los nodos eliminados.
4. Compactar las posiciones de los hermanos del nodo eliminado en su padre original.
5. Hacer *upsert* de `wbs_project_activity` para el actor y confirmar la transacción; responder `204`.

Un ítem con `apu_id` se elimina y el APU **no** se elimina (`D-119`): el APU queda desvinculado y disponible
para volver a seleccionarse en el proyecto; la búsqueda y el estado del APU se definen en `I-3b`. En este
incremento la columna `apu_id` no tiene FK y la baja del ítem elimina la fila del ítem; `I-3b` deberá limpiar
el vínculo del APU y habilitar su reutilización.

**Alternativa descartada:** `ON DELETE CASCADE` en la base. Borra sin dejar rastro de la confirmación, no
distingue ítems con APU y choca con `D-114`; se mantiene `RESTRICT` como red de seguridad.

### D5. Actividad y participantes del EDT

`wbs_project_activity` relaciona proyectos y usuarios y no depende de que existan nodos en el EDT
(`D-120`):

```text
wbs_project_activity
  project_id        uniqueidentifier  NOT NULL  FK_projects_wbs_project_activity  RESTRICT
  user_id           uniqueidentifier  NOT NULL  FK_users_wbs_project_activity     RESTRICT
  last_modified_at  datetime2         NOT NULL  (UTC)
  PK_wbs_project_activity (project_id, user_id)
  IX_wbs_project_activity_project_id_last_modified_at (project_id, last_modified_at)
```

- Cada comando de escritura de `M-03` (alta, edición, cambio de código, movimiento, reordenamiento, baja y
  cascada) hace un *upsert* de `(project_id, actorUserId)` con la fecha UTC actual, dentro de la misma
  transacción del comando.
- La fila del participante no se borra al eliminar nodos; sigue existiendo aunque el EDT quede vacío.
- Los participantes son las filas del proyecto; la última actividad es la fila con `MAX(last_modified_at)`.
- La autoría por nodo de `D-116` se mantiene como metadato; la vista de auditoría usa esta tabla.
- `GET /wbs/activity` (`WBS_READ`) devuelve `projectId`, `lastActivity` y `participants`; `lastActivity` es
  `null` y `participants` es `[]` si no hay filas. El Administrador, el Director y el Presupuestador acceden
  con su alcance; el Auditor recibe `403 WBS_READ_FORBIDDEN`.
- `userName` sale de `Username` de `UserAccount`. No existe `GET /wbs/last-activity`; el endpoint
`/wbs/activity` lo sustituye.

**Alternativa descartada:** derivar la actividad con `MAX(wbs_items.last_modified_at)` y
`MAX(wbs_chapters.last_modified_at)`. Evita una tabla, pero obliga a recorrer y unir dos tablas y no permite
listar participantes ni conservar la actividad cuando el EDT queda vacío.

### D6. Esquema exacto

Nombres `snake_case`, esquema único `dbo` (`D-95`), claves `uniqueidentifier` y enumeraciones `VARCHAR` con
`CHECK` (`3.3.1`).

```text
wbs_codes
  id                uniqueidentifier   PK_wbs_codes
  project_id        uniqueidentifier   NOT NULL  FK_projects_wbs_codes      RESTRICT
  code              varchar(10)        NOT NULL  COLLATE Latin1_General_100_CI_AS
  node_type         varchar(10)        NOT NULL  CHECK IN ('CHAPTER','ITEM')
  CK_wbs_codes_code_length  LEN(code) BETWEEN 1 AND 10
  UQ_wbs_codes_project_id_code  UNIQUE (project_id, code)

wbs_chapters
  id                   uniqueidentifier  PK_wbs_chapters
  project_id           uniqueidentifier  NOT NULL  FK_projects_wbs_chapters        RESTRICT
  parent_chapter_id    uniqueidentifier  NULL      FK_wbs_chapters_parent          RESTRICT
  code_id              uniqueidentifier  NOT NULL  FK_wbs_codes_wbs_chapters       RESTRICT
  name                 varchar(255)      NOT NULL
  order_index          int               NOT NULL  CHECK (order_index >= 0)
  last_modified_by     uniqueidentifier  NOT NULL  FK_users_wbs_chapters           RESTRICT
  last_modified_at     datetime2         NOT NULL
  row_version          rowversion        NOT NULL
  UQ_wbs_chapters_code_id  UNIQUE (code_id)
  IX_wbs_chapters_project_id_parent_chapter_id_order_index (project_id, parent_chapter_id, order_index)

wbs_items
  id                   uniqueidentifier  PK_wbs_items
  project_id           uniqueidentifier  NOT NULL  FK_projects_wbs_items          RESTRICT
  wbs_chapter_id       uniqueidentifier  NULL      FK_wbs_chapters_wbs_items      RESTRICT
  apu_id               uniqueidentifier  NULL      -- sin FK hasta I-3b (D-113)
  code_id              uniqueidentifier  NOT NULL  FK_wbs_codes_wbs_items         RESTRICT
  quantity_work        decimal(18,4)     NOT NULL  CHECK (quantity_work > 0)
  order_index          int               NOT NULL  CHECK (order_index >= 0)
  last_modified_by     uniqueidentifier  NOT NULL  FK_users_wbs_items             RESTRICT
  last_modified_at     datetime2         NOT NULL
  row_version          rowversion        NOT NULL
  UQ_wbs_items_code_id   UNIQUE (code_id)
  UQ_wbs_items_apu_id    UNIQUE (apu_id) WHERE apu_id IS NOT NULL
  IX_wbs_items_project_id_wbs_chapter_id_order_index (project_id, wbs_chapter_id, order_index)

wbs_project_activity
  project_id           uniqueidentifier  NOT NULL  FK_projects_wbs_project_activity  RESTRICT
  user_id              uniqueidentifier  NOT NULL  FK_users_wbs_project_activity     RESTRICT
  last_modified_at     datetime2         NOT NULL
  PK_wbs_project_activity (project_id, user_id)
  IX_wbs_project_activity_project_id_last_modified_at (project_id, last_modified_at)
```

- La intercalación `Latin1_General_100_CI_AS` en `code` garantiza la unicidad sin distinguir mayúsculas
  (`BR-015`); se declara a nivel de columna para no depender de la intercalación de la base.
- `wbs_items.wbs_chapter_id` es `NULL` para el ítem raíz; el índice no es único, de modo que admite varios
  ítems raíz ordenados por `order_index`.
- El índice único filtrado de `apu_id` es la garantía 1:1 de `BR-016` (`D-113`); la FK hacia `apus` se añade
  en `I-3b` cuando la tabla exista.
- `row_version` en capítulos e ítems (`D-50`, `D-97`); `WbsCode` y `WbsProjectActivity` no llevan
  concurrencia propia porque cambian siempre junto con su nodo o representan el último valor.
- La baja es física y `RESTRICT` impide borrar un capítulo con descendientes o un ítem referenciado; la
  cascada la ejecuta la aplicación (`D-119`).

### D7. Autorización y alcance

- Permisos nuevos en `SecurityBaselineSeeder`: `WBS_READ` y `WBS_WRITE`. El `SeedVersion` sube al siguiente
  valor libre **en el momento de implementar**; hoy `v6` está tomado y otro cambio puede consumir `v7`, así
  que el valor se fija al escribir el código y se refleja en `.github/docs/05 §8`.
- Asignación (`Matriz 1.3.2`, `D-82`): Administrador → `WBS_READ`; Director → `WBS_READ`, `WBS_WRITE`;
  Presupuestador → `WBS_READ`, `WBS_WRITE`; Auditor → `WBS_READ`.
- El permiso se exige en el endpoint. El alcance se resuelve en el caso de uso:
  - Administrador: lectura de cualquier proyecto.
  - Director: lectura y escritura en los proyectos de su compañía.
  - Presupuestador: lectura y escritura solo con `ProjectMember` en el proyecto.
  - Auditor: lectura solo con `ProjectMember` en el proyecto.
- `M-03` define `IWbsProjectAccessReader` en `Application/Wbs/Repositories` y lo implementa en Infrastructure
  (`WbsProjectAccessReader`), igual que `M-04` con `IResourceProjectAccessReader`. Así la prueba de
  arquitectura `T-A-03` sigue en verde.
- La lectura de actividad (`GET /wbs/activity`) exige `WBS_READ` y el mismo alcance, pero el Auditor no
  accede: recibe `403 WBS_READ_FORBIDDEN` (`D-120`). El Administrador, el Director y el Presupuestador sí.
- El aislamiento entre compañías responde `403 WBS_READ_FORBIDDEN`/`403 WBS_WRITE_FORBIDDEN`; el
  `404 PROJECT_NOT_FOUND` queda para proyectos inexistentes (decisión del autor, 14/11/2026).

### D8. Superficie de endpoints

Rutas project-scoped con versionado `/api/v1`, coherentes con `ResourcesController` (`D-55`, `D-102`).

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/projects/{projectId}/wbs` | `WBS_READ` | Árbol completo ordenado |
| `GET` | `/api/v1/projects/{projectId}/wbs/search` | `WBS_READ` | Búsqueda parcial por código o nombre, paginada |
| `GET` | `/api/v1/projects/{projectId}/wbs/activity` | `WBS_READ` | Participantes y última actividad del EDT por proyecto |
| `GET` | `/api/v1/projects/{projectId}/wbs/codes/validate` | `WBS_WRITE` | Valida formato y disponibilidad de un código |
| `POST` | `/api/v1/projects/{projectId}/wbs/chapters` | `WBS_WRITE` | Alta de capítulo con código manual |
| `PUT` | `/api/v1/projects/{projectId}/wbs/chapters/{chapterId}` | `WBS_WRITE` | Edición de nombre y código |
| `DELETE` | `/api/v1/projects/{projectId}/wbs/chapters/{chapterId}?cascade=true&rowVersion=...` | `WBS_WRITE` | Baja física; cascada confirmada |
| `POST` | `/api/v1/projects/{projectId}/wbs/items` | `WBS_WRITE` | Alta de ítem con código manual y cantidad, dentro de un capítulo o en la raíz |
| `PUT` | `/api/v1/projects/{projectId}/wbs/items/{itemId}` | `WBS_WRITE` | Edición de código y cantidad |
| `DELETE` | `/api/v1/projects/{projectId}/wbs/items/{itemId}?rowVersion=...` | `WBS_WRITE` | Baja física |
| `POST` | `/api/v1/projects/{projectId}/wbs/reorder` | `WBS_WRITE` | Reordenamiento de los hijos de un padre |
| `POST` | `/api/v1/projects/{projectId}/wbs/move` | `WBS_WRITE` | Movimiento de un nodo a otro padre o a la raíz |

- `projectId` sale de la ruta; `code` es **obligatorio** en el alta de capítulo e ítem y se valida contra
  `R-19` y la unicidad del proyecto. El cliente nunca envía `projectId` en el cuerpo, ni `apuId`, ni
  autoría, ni `rowVersion` en el alta.
- `PUT` exige `rowVersion` y **no** cambia el padre: el traslado es siempre `POST move`. En el ítem, `PUT`
  acepta `code` y `quantityWork`; en el capítulo, `name` y `code`.
- `DELETE` exige `rowVersion` del nodo objetivo como query parameter; en cascada, el del capítulo raíz.
- `POST move` recibe `nodeType`, `nodeId`, `targetParentChapterId` (nulo para la raíz), `targetIndex`
  (nulo = al final) y `rowVersion` del nodo movido. La posición se interpreta después de retirar el nodo
  de su padre actual cuando el destino es el mismo.
- `POST reorder` recibe `parentChapterId` (nulo para raíces) y la lista ordenada de `(nodeType, nodeId)`;
  la lista debe contener exactamente los hijos vigentes.
- `POST items` con `wbsChapterId = null` crea un ítem raíz.
- `GET codes/validate` recibe `code` y `excludeNodeId?` y responde `200` con
  `{ code, isValid, isAvailable, reason }`.
- `GET activity` responde `200` con `{ projectId, lastActivity, participants }`; `lastActivity` es nulo y
  `participants` vacío si no hay actividad. El Auditor no accede.
- Los DTO devuelven `code`, `nodeType`, `id`, `parentChapterId` (nulo para raíz), `name` o
  `quantityWork`, `orderIndex`, `rowVersion`, `lastModifiedBy`, `lastModifiedAt` y, en el árbol, los hijos
  anidados. Los ítems raíz se devuelven en la colección raíz junto con los capítulos; nunca se exponen
  entidades (`D-41`).

### D9. Errores `ProblemDetails`

| Código (`extensions.code`) | Estado | Cuándo |
|---|---|---|
| `WBS_NOT_FOUND` | `404` | El capítulo o el ítem no existe en el proyecto |
| `WBS_READ_FORBIDDEN` | `403` | Sin alcance de lectura, proyecto de otra compañía o Auditor en actividad |
| `WBS_WRITE_FORBIDDEN` | `403` | Sin alcance de escritura o proyecto de otra compañía |
| `WBS_CODE_DUPLICATED` | `409` | El código ya existe en el proyecto (sin distinguir mayúsculas) |
| `WBS_CODE_INVALID` | `400` | Código vacío, fuera del conjunto de `R-19` o de longitud distinta de 1–10 |
| `WBS_NODE_HAS_CHILDREN` | `409` | Baja de un capítulo con descendientes sin confirmar la cascada |
| `WBS_MOVE_CYCLE` | `400` | El destino es el propio nodo o uno de sus descendientes |
| `WBS_TARGET_NOT_FOUND` | `404` | El capítulo destino no existe en el proyecto o es de otro proyecto |
| `WBS_CONCURRENCY_CONFLICT` | `409` | `rowVersion` desactualizado en edición, movimiento o baja |
| `WBS_QUANTITY_INVALID` | `400` | Cantidad de obra nula, cero o negativa |
| `VALIDATION_ERROR` | `400` | Nombre vacío, destino que no es capítulo, posición fuera de rango o secuencia de reorden inválida |
| `PAGE_SIZE_INVALID` | `400` | `pageSize` fuera de `{10, 50, 100}` |
| `PROJECT_NOT_FOUND` | `404` | El proyecto no existe |

`ExceptionHandlingMiddleware` ya normaliza los códigos a `UPPER_SNAKE` y los publica en
`extensions.code` (`D-100`); no se añade middleware nuevo. No se implementan
`WBS_CODE_GENERATION_FAILED` ni `WBS_ITEM_HAS_APU`: no hay respaldo y el ítem con APU se puede eliminar
(`D-119`).

### D10. Migración y `seed`

- Migración `wbs_add_i2b` con el formato `YYYYMMDDHHMM_<scope>_<change>` de `D-107`, generada sobre la cadena
  vigente (la última es `20261113040910_e3a_suppliers`).
- Se crean cuatro tablas, sus índices y sus FKs, sin migración de datos: no existen filas previas de EDT.
- `Down` elimina en orden inverso `wbs_project_activity`, `wbs_items`, `wbs_chapters` y `wbs_codes`, con
  sus índices y FKs; la reversión se documenta en `docs/runbook-migraciones.md`.
- `SecurityBaselineSeeder` añade `WBS_READ` y `WBS_WRITE` a `PermissionKeys` y a `RolePermissions` y sube el
  `SeedVersion` a `v7`; al reconciliar por diferencia, basta la versión nueva para crear los permisos y sus
  asignaciones en una base existente sin revocar nada más.
- El diseño no depende de la migración ficticia `20261113080043_wbs_add_i2b` que menciona
  `.github/docs/05`; ese documento se corrige en este cambio porque la migración no existe en el repositorio.

### D11. Contrato de interacción para la interfaz (no normativo)

Guía para la etapa de interfaz (`D-109`); el `Frontend` ya usa Angular 20 y PrimeNG 20, lo que cubre el
árbol y el arrastre sin dependencias nuevas.

**Árbol y gestos.** `p-tree` con `draggable`/`droppable` y selección por nodo:

| Gesto | Endpoint | Efecto |
|---|---|---|
| Soltar sobre un capítulo (`dropPoint = inside`) | `POST move` con `targetParentChapterId` y `targetIndex = null` | El nodo pasa a ser el último hijo del capítulo |
| Soltar entre dos hermanos (`dropPoint = between`) | `POST reorder` con la secuencia resultante | Solo cambia la posición |
| Soltar un nodo de otro padre entre dos hermanos | `POST move` con `targetParentChapterId` y `targetIndex` | Cambia de padre y de posición en una sola llamada |
| Soltar en la zona «Raíz del presupuesto» | `POST move` con `targetParentChapterId = null` | Capítulo o ítem raíz, sin capítulo padre |
| Reordenar dentro del mismo padre | `POST reorder` | Posiciones nuevas, códigos intactos |

**Reglas de presentación que la interfaz debe respetar:**

1. La interfaz nunca compone ni renumera códigos: el código lo escribe el usuario y se valida contra
   `GET /wbs/codes/validate` antes de enviar el alta o la edición; el servidor vuelve a validarlo.
2. El ítem raíz se muestra en el nivel raíz con la columna de capítulo vacía («—»).
3. El código se edita desde una acción explícita «Editar código»; si el usuario lo cambia, los descendientes
   conservan su código y la interfaz no deriva la jerarquía del texto.
4. La baja de un capítulo con descendientes abre una confirmación que muestra cuántos capítulos e ítems se
   eliminarán y, solo al aceptar, envía `DELETE ...?cascade=true&rowVersion=...`. Si el usuario no confirma,
   no se llama al endpoint.
5. Un `409 WBS_CONCURRENCY_CONFLICT`, `409 WBS_CODE_DUPLICATED` o `409 WBS_NODE_HAS_CHILDREN` refresca el
   árbol y muestra el mensaje; nunca se reintenta a ciegas.
6. La actividad se consulta al abrir el proyecto en `GET /wbs/activity` y se muestra la última modificación
   y los participantes; el Auditor no ve esta vista.
7. Para accesibilidad, cada movimiento debe tener una alternativa sin arrastre: un diálogo «Mover a…» con
   selector de capítulo destino y posición.

No se especifican componentes, rutas ni módulos Angular: son decisiones de la etapa de interfaz (`D-109`).

## Risks / Trade-offs

| Riesgo | Mitigación |
|---|---|
| Un ítem con APU se elimina y el APU queda desvinculado (`D-119`) | El APU no se borra y queda disponible para volver a seleccionarse; `I-3b` define su estado y búsqueda, y la FK se añade entonces |
| El código manual puede no reflejar la jerarquía | Es intencional: la jerarquía se determina por `parent_chapter_id`/`wbs_chapter_id`; la interfaz no deriva el padre del texto |
| El usuario puede escribir códigos parecidos o inconsistentes | El endpoint de validación y el índice `(project_id, code)` CI lo impiden; la interfaz valida antes de enviar |
| Los ítems raíz complican las consultas de orden y los índices | El índice es no único sobre `(project_id, wbs_chapter_id, order_index)` y la raíz se ordena por la misma columna con `wbs_chapter_id IS NULL` |
| Mover un capítulo profundo bajo un árbol profundo agranda el árbol | `R-18` no limita la profundidad; la comprobación de ciclo recorre solo la cadena de padres del destino |
| La cascada toca muchas filas | Una sola transacción, `rowVersion` del capítulo raíz, borrado de hijos a padres y prueba de integración con un subárbol de varios niveles (`T-I-40`) |
| `WbsProjectActivity` podría quedar desactualizada si una escritura no la actualiza | El repositorio expone un único método de escritura que siempre hace el *upsert*; la prueba de integración verifica las operaciones |
| Dos usuarios editan el mismo nodo | El segundo recibe `409 WBS_CONCURRENCY_CONFLICT` por `rowVersion` y debe refrescar; no hay fusión de campos |
| El estado de `tasks.md` afirmaba una implementación inexistente | Se corrige en este cambio y se verifica con `git status` y la ausencia de clases `Wbs*` antes de implementar |

## Migration Plan

1. Respaldo de la base (`D-40`).
2. Revisión del script generado con `dotnet ef migrations script`.
3. Aplicación en ventana de mantenimiento y verificación de tablas, índices, `CHECK`, FKs y nulabilidad de
   `wbs_items.wbs_chapter_id`.
4. Arranque de la API: el `seed` sube a la versión nueva y crea `WBS_READ`/`WBS_WRITE`.
5. Reversión: ejecutar `Down` y restaurar el respaldo si la verificación falla.

## Open Questions

- **APU desvinculado:** `I-3b` debe definir el estado y la búsqueda del APU de un ítem eliminado; el APU no
  se borra y debe poder volver a seleccionarse para el proyecto. Hasta entonces la columna `apu_id` no tiene
  FK.
- **`SeedVersion`:** se fija en `v7`; si otro cambio lo consume antes de implementar, se sube al siguiente
  valor libre y se actualiza `.github/docs/05 §8`.
- **Lecturas:** la actividad solo registra escrituras; consultar el árbol, la búsqueda o la actividad no la
  modifica.
- **Nombre del APU y su FK:** se deciden en `I-3b`, igual que el índice definitivo.
- **Actividad del Auditor:** el Auditor no accede a `GET /wbs/activity`; si la etapa de auditoría requiere
  otra vista, se define en `I-4b`.
