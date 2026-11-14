# Aclaraciones de implementación: i-2b-rf04-edt

Este archivo registra las decisiones tomadas por el autor el 14/11/2026. `PLAN_INGENIERIA.md` es la
fuente de verdad; este registro resume las reglas que deben reflejarse en `proposal.md`, `design.md`,
`spec.md`, `tasks.md` y la documentación de apoyo `.github/docs`.

## 1. Código del EDT: manual, único y validado

La decisión vigente es la del punto **1.3**: se elimina la numeración automática y el código de respaldo.
El punto 1.1 queda superado por 1.3.

- Al crear un capítulo o un ítem, el campo `code` **viene vacío en la interfaz y es obligatorio** en la API.
- El usuario ingresa el **código completo** (no un segmento): por ejemplo `CIM`, `CIM.1` o `EST-3`.
- El servidor **no compone prefijos, no renumera y no genera códigos**. La jerarquía se determina por
  `parent_chapter_id` / `wbs_chapter_id`, no por el texto del código (`R-19`).
- El código debe medir **1–10 caracteres** para capítulos e ítems y usar el conjunto de `R-19`.
- Se aceptan **mayúsculas y minúsculas**. El valor se guarda sin espacios externos (`Trim`) y se conserva
  tal como lo escribió el usuario; la unicidad se evalúa sin distinguir mayúsculas.
- El código es único **por proyecto** entre capítulos e ítems.
- El código no se recalcula al eliminar, reordenar o mover otros nodos. Solo se cambia con una edición
  explícita del propio nodo; esa edición no modifica los códigos de sus descendientes.
- La edición explícita del código usa la misma validación de formato y unicidad.
- Se elimina `WBS_CODE_GENERATION_FAILED` y toda la lógica de respaldo (`NextFallbackCode`,
  `NextNaturalSegment`, `ComposeEffectiveCode`, `OwnSegment`, `ExceedsLimit`).

### Endpoint de validación de código

```text
GET /api/v1/projects/{projectId}/wbs/codes/validate?code={code}&excludeNodeId={guid?}
Permiso: WBS_WRITE
```

- `code` es obligatorio; `excludeNodeId` es opcional y se usa al editar para ignorar el código actual.
- Responde `200` con un resultado de validación, sin usar `409` para el duplicado:

```json
{
  "code": "CIM.1",
  "isValid": true,
  "isAvailable": true,
  "reason": null
}
```

- `reason` vale `WBS_CODE_INVALID` si el formato o la longitud no cumplen, `WBS_CODE_DUPLICATED` si ya
  existe en el proyecto, o `null` si es válido y está disponible.
- El endpoint no crea ni modifica nada.

## 2. Autorización y aislamiento

## 3. Actividad y participantes (`wbs_project_activity`)

Se reemplaza `wbs_last_activity` por una tabla intermedia proyecto‑usuario que no depende de que existan
nodos en el EDT.

```text
wbs_project_activity
  project_id        uniqueidentifier  NOT NULL  FK_projects_wbs_project_activity  RESTRICT
  user_id           uniqueidentifier  NOT NULL  FK_users_wbs_project_activity     RESTRICT
  last_modified_at  datetime2         NOT NULL  (UTC)
  PK_wbs_project_activity (project_id, user_id)
  IX_wbs_project_activity_project_id_last_modified_at (project_id, last_modified_at)
```

- Cada operación de escritura (alta, edición, cambio de código, movimiento, reordenamiento, baja y
  cascada) hace *upsert* de la fila `(project_id, actorUserId)` con la fecha UTC actual, dentro de la
  misma transacción del comando.
- La fila del participante **no se borra** cuando se eliminan nodos; sigue existiendo aunque el EDT quede
  vacío.
- La lista de participantes son las filas del proyecto; la última actividad del proyecto es la fila con
  `MAX(last_modified_at)`.
- La autoría por nodo (`last_modified_by`, `last_modified_at`) se conserva como metadato de la fila; la
  vista de auditoría del proyecto usa `wbs_project_activity`.

### Endpoint de actividad

```text
GET /api/v1/projects/{projectId}/wbs/activity
Permiso: WBS_READ, con roles Administrador, Director o Presupuestador
```

- Responde `200` siempre que el proyecto exista y el actor tenga alcance, incluso sin actividad:

```json
{
  "projectId": "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
  "lastActivity": {
    "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "userName": "jdoe",
    "lastModifiedAt": "2026-11-14T15:04:05Z"
  },
  "participants": [
    {
      "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "userName": "jdoe",
      "lastModifiedAt": "2026-11-14T15:04:05Z"
    }
  ]
}
```

- Sin actividad: `lastActivity: null` y `participants: []`.
- El Auditor recibe `403 WBS_READ_FORBIDDEN` en este endpoint.
- Se elimina `GET /wbs/last-activity`; este endpoint lo sustituye.

## 4. Movimiento: semántica de `targetIndex`

Regla única para movimiento dentro del mismo padre y entre padres:

1. Validar `rowVersion` del nodo movido.
2. Si el nodo ya está en el padre destino, **retirarlo primero** de la lista de hermanos.
3. Insertar el nodo en la lista restante del destino en la posición `targetIndex`.
4. `targetIndex` válido va de `0` a `n`, donde `n` es la cantidad de hermanos **restantes** del destino;

## 5. Concurrencia optimista

- `PUT` de capítulo, `PUT` de ítem, `POST move` y `DELETE` de capítulo o ítem exigen `rowVersion`.
- En `DELETE`, el `rowVersion` viaja como query parameter (`?rowVersion=...`) y se refiere al nodo
  objetivo; en una cascada, al capítulo raíz del subárbol.
- El servidor compara el valor recibido con `row_version` de la fila. La primera transacción que confirma
  actualiza la versión; la segunda recibe `409 WBS_CONCURRENCY_CONFLICT` y no modifica nada.
- Dos usuarios que editan el mismo ítem: gana el primero que confirma; el segundo debe refrescar y
  reintentar. No hay fusión de campos.
- `POST reorder` no tiene un nodo único: se mantiene *last-write-wins* con validación de la lista completa
  de hijos; una lista que no coincide con los hijos vigentes responde `400 VALIDATION_ERROR`.
- `WbsProjectActivity` no lleva `rowversion`: la última escritura gana por definición.

## 6. Baja y APU

- La baja de un capítulo con descendientes exige `cascade=true`; sin confirmación responde
  `409 WBS_NODE_HAS_CHILDREN`.
- Un ítem con APU se elimina y el APU **no se elimina**. El APU queda desvinculado y disponible para
  volver a seleccionarse en el proyecto; la búsqueda y el estado del APU se definen en `I-3b`.
- La baja en cascada elimina ítems, capítulos de mayor a menor profundidad y códigos, en una sola
  transacción, y compacta las posiciones de los hermanos del nodo eliminado.

## 7. Errores vigentes

| Código | HTTP | Cuándo |
|---|---|---|
| `WBS_NOT_FOUND` | 404 | El capítulo o ítem no existe en el proyecto |
| `WBS_READ_FORBIDDEN` | 403 | Sin alcance de lectura, o Auditor en actividad |
| `WBS_WRITE_FORBIDDEN` | 403 | Sin alcance de escritura |
| `WBS_CODE_DUPLICATED` | 409 | El código ya existe en el proyecto |
| `WBS_CODE_INVALID` | 400 | Código vacío, fuera del conjunto o de 1–10 caracteres |
| `WBS_NODE_HAS_CHILDREN` | 409 | Baja sin confirmar la cascada |
| `WBS_MOVE_CYCLE` | 400 | Destino igual o descendiente del nodo movido |
| `WBS_TARGET_NOT_FOUND` | 404 | Destino inexistente o de otro proyecto |
| `WBS_CONCURRENCY_CONFLICT` | 409 | `rowVersion` desactualizado |
| `WBS_QUANTITY_INVALID` | 400 | Cantidad nula, cero o negativa |
| `VALIDATION_ERROR` | 400 | Nombre vacío, destino que no es capítulo, índice fuera de rango o secuencia inválida |
| `PAGE_SIZE_INVALID` | 400 | `pageSize` fuera de `{10, 50, 100}` |
| `PROJECT_NOT_FOUND` | 404 | Proyecto inexistente |

No se implementan `WBS_CODE_GENERATION_FAILED` ni `WBS_ITEM_HAS_APU`.

## 8. Endpoints vigentes (12)

1. `GET /api/v1/projects/{projectId}/wbs`
2. `GET /api/v1/projects/{projectId}/wbs/search`
3. `GET /api/v1/projects/{projectId}/wbs/activity`
4. `GET /api/v1/projects/{projectId}/wbs/codes/validate`
5. `POST /api/v1/projects/{projectId}/wbs/chapters`
6. `PUT /api/v1/projects/{projectId}/wbs/chapters/{chapterId}`
7. `DELETE /api/v1/projects/{projectId}/wbs/chapters/{chapterId}?cascade=true&rowVersion=...`
8. `POST /api/v1/projects/{projectId}/wbs/items`
9. `PUT /api/v1/projects/{projectId}/wbs/items/{itemId}`
10. `DELETE /api/v1/projects/{projectId}/wbs/items/{itemId}?rowVersion=...`
11. `POST /api/v1/projects/{projectId}/wbs/reorder`
12. `POST /api/v1/projects/{projectId}/wbs/move`

## 9. Supuestos que se mantienen

- El código de un ítem también mide 1–10 caracteres: al ser manual y completo, el límite de 1–5 del
  esquema anterior deja de aplicar.
- `userName` de la actividad es `users.username`.
- El código se conserva con las mayúsculas/minúsculas escritas; la unicidad es case-insensitive.
- La autoría por nodo se mantiene; la auditoría de participantes se consulta en `wbs_project_activity`.
- La paginación de búsqueda sigue siendo `page`/`pageSize` con `{10, 50, 100}`.

   `null` o `n` significan “al final”.
5. Desplazar a la derecha las posiciones de los hermanos del destino desde el índice de inserción.
6. Compactar las posiciones del padre de origen si es distinto del destino.
7. Actualizar `wbs_project_activity` y confirmar la transacción.

Ejemplos con `[A, B, C]` en el mismo padre:

```text
Mover A a targetIndex=1 -> [B, A, C]
Mover C a targetIndex=0 -> [C, A, B]
Mover B a targetIndex=2 -> [A, C, B]
Mover B a targetIndex > 2 -> 400 VALIDATION_ERROR
```

El código del nodo no cambia al moverse.


- El Administrador **no escribe** la EDT; solo lee.
- El Auditor **no escribe** la EDT y **no accede al control de actividad** de la EDT.
- Director: lectura, escritura y actividad en los proyectos de su compañía.
- Presupuestador: lectura, escritura y actividad solo con `ProjectMember` en el proyecto.
- La EDT es **única por proyecto**: no se copia ni se mueve un nodo a otro proyecto.
- Lectura o escritura sobre un proyecto de otra compañía: **`403 WBS_READ_FORBIDDEN`** o
  **`403 WBS_WRITE_FORBIDDEN`**, respectivamente (decisión 1.5). Se acepta que el 403 revele que el
  proyecto existe; el `404 PROJECT_NOT_FOUND` queda solo para proyectos inexistentes.
- Un destino de movimiento que no existe en el proyecto, o que pertenece a otro proyecto, responde
  **`404 WBS_TARGET_NOT_FOUND`** aunque el proyecto destino exista en otra compañía.
