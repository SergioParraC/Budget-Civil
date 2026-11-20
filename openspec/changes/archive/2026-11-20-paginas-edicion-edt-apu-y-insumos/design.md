# Design: paginas-edicion-edt-apu-y-insumos

## Context

El cliente está implementado con Angular 20 standalone (`Frontend/src/app`), signals y Reactive Forms, sobre
PrimeNG 20 con el tema **Aura** configurado en `app.config.ts`; `@angular/cdk` ya figura en `package.json` aunque hoy
no se usa en ningún archivo. No hay NgRx, no hay carpeta `shared/` y no existe suite de pruebas del cliente.

Estado actual relevante:

- `app.routes.ts` declara `login`, `registro`, `recuperar-contrasena` y, dentro de `MainLayout` con `authGuard`,
  `dashboard`, `proyectos`, `proyectos/nuevo`, `proyectos/:id`, `companias`, `usuarios`, `perfil` y `apu`
  (placeholder del catálogo público).
- `core/auth/permissions.ts` solo declara los permisos de identidad, compañías, usuarios y proyectos; no existen
  `wbs*`, `apus*`, `resources*`, `suppliersRead` ni `catalogsWrite`.
- `core/http/api-error.ts` normaliza `ProblemDetails` a `ApiError` con un mapa de mensajes en español que aún no
  cubre `WBS_*`, `APU_*`, `MATRIX_*`, `RESOURCE_*` ni `UNIT_*`.
- `core/layout/main-layout.ts` construye `navGroups` con Dashboard, Proyectos, APU público, Compañías y Usuarios.
- `features/proyectos/proyecto-detalle` concentra la ficha del proyecto con `p-tabs`, formularios reactivos y modales;
  su SCSS usa la convención bloque/elemento (`&__header`, `&__field`).
- `styles.scss` solo importa `primeicons` y fija la tipografía; el resto del estilo viene del tema Aura.

Hechos del contrato que condicionan el diseño (verificados en `backend/src`):

| Punto | Hecho |
|---|---|
| Árbol | `GET /projects/{projectId}/wbs` devuelve `{ projectId, nodes[] }`; el nodo trae `id`, `projectId`, `nodeType`, `code`, `parentChapterId`, `name`, `quantityWork`, `orderIndex`, `rowVersion`, `lastModifiedBy`, `lastModifiedAt`, `apu`, `unitCost`, `directCost` y `children[]` |
| Búsqueda | `GET /wbs/search` devuelve `{ items[{ nodeId, nodeType, code, name, quantityWork, parentChapterId, orderIndex }], page, pageSize, totalItems }` |
| Actividad | `GET /wbs/activity` devuelve `{ projectId, lastActivity, participants[] }` y puede responder `403` al Auditor |
| Validación de código | `GET /wbs/codes/validate` exige `WBS_WRITE`, no `WBS_READ` |
| Movimiento | `POST /wbs/move` y `POST /wbs/reorder` devuelven el nodo sin costos; el árbol debe recargarse |
| Alta y edición unificadas | `POST /wbs/items` y `PUT /wbs/items/{itemId}` exigen `WBS_WRITE` **y** `APUS_WRITE`; `apu.resources` reemplaza la composición completa |
| Insumos | `GET /resources` usa envelope paginado con `page`, `pageSize` y `sortBy`/`sortDirection`; `GET /resources/suggestions` exige `RESOURCES_WRITE` |
| Tipos | `GET /projects/{projectId}/resource-types` es project-scoped y devuelve solo `{ id, name }` (sin `code`) |
| Unidades | `GET /catalogs/units` devuelve un arreglo plano `[{ id, shortUnit, unit }]`; a diferencia de países,
  departamentos, municipios y divisas, que usan envelope paginado con `pageNumber`/`pageSize` |
| Matrices | `GET /matrices/{id}/components` exige `APUS_READ`; agregar, editar y quitar exigen `APUS_WRITE`; la respuesta es `{ matrixResourceId, description, unit, status, resources[]{ resourceType, items[] }, directCost, rowVersion }` |
| Proveedores | `GET /suppliers/options` devuelve `{ items[{ supplierId, businessName }] }` y exige `SUPPLIERS_READ` |
| Proyectos | `GET /projects` lista los proyectos de la compañía con envelope paginado y `pageNumber`; sirve para elegir el origen de una copia |
| Matriz | El tipo Matriz se reconoce por `resourceTypeCode === 'MATRIX'` en un `ResourceDto`, no por el catálogo de tipos |

Motivación y alcance: ver `proposal.md`. Comportamiento exigido: `specs/edt/spec.md`, `specs/apus/spec.md`,
`specs/insumos/spec.md` y `specs/unidades/spec.md`.

## Goals / Non-Goals

**Goals:**

- Ubicar las tres pantallas como rutas hijas del proyecto, con accesos desde la ficha y sin tocar la navegación
  global ni el catálogo público.
- Reutilizar el patrón existente del cliente: componentes standalone, signals, Reactive Forms, `p-table`,
  `p-dialog`, `p-select`, `p-inputnumber`, `p-message`, `MessageService`, `ConfirmationService` y el tema Aura.
- Delegar en el servidor todo cálculo y todo estado; el cliente solo presenta y valida formato.
- Resolver el arrastrar y soltar con `@angular/cdk/drag-drop`, ya instalado, con acciones explícitas equivalentes.
- Centralizar la traducción de errores de dominio a mensajes en español en un solo mapa reutilizable.

**Non-Goals:**

- Cambios de contrato, endpoints nuevos, migraciones o ajustes en `backend/`.
- Copia de APU entre proyectos, aprobaciones, presupuesto (`GET /budget`), alertas de tope y cierre.
- Catálogo público `/apu`, autenticación, exportación e importación.
- Pruebas automatizadas del cliente (no existe infraestructura de pruebas en `Frontend/`).
- Edición de la ubicación de un insumo o de un nodo fuera del proyecto: el cliente no ofrece esa operación.

## Decisions

### D1. Rutas hijas del proyecto con accesos solo desde la ficha

```text
/login, /registro, /recuperar-contrasena          (sin cambios)
MainLayout  [authGuard]
  /dashboard                                      (sin entradas nuevas)
  /proyectos
  /proyectos/nuevo
  /proyectos/:id                                  ficha actual + accesos "EDT" e "Insumos"
  /proyectos/:id/edt                              permissionGuard(WBS_READ)
  /proyectos/:id/edt/items/nuevo                  permissionGuard(WBS_READ) + WBS_WRITE y APUS_WRITE en la pagina
  /proyectos/:id/edt/items/:itemId/apu            permissionGuard(APUS_READ)
  /proyectos/:id/insumos                          permissionGuard(RESOURCES_READ)
  /companias, /usuarios, /perfil, /apu            (sin cambios)
```

- `projectId` sale de `ActivatedRoute` (`paramMap`) y es el único contexto de datos. No se agrega
  `ActiveProjectService`: cada página resuelve su proyecto con `GET /projects/{projectId}`.
- Los accesos de la ficha se agregan como una tarjeta de “Módulos del proyecto” en `proyecto-detalle.html`, con
  `routerLink` relativo y visibles según `authService.can(...)`.
- **Alternativa descartada:** un módulo global con selector de proyecto en el menú lateral. Se descarta porque el
  requisito prohíbe entradas globales y porque duplicaría el contexto que ya aporta la ruta.

### D2. Estructura por feature y nombres

```text
Frontend/src/app/
  core/models/
    wbs.model.ts              nodos, busqueda, actividad, comandos de alta/edicion/movimiento
    apu.model.ts              detalle del APU, grupos de insumos, costos indirectos
    resource.model.ts         insumo, tipos, sugerencias, matrices, cotizacion masiva
    unit.model.ts             unidad del catalogo
  core/services/
    wbs.service.ts            arbol, busqueda, actividad, validacion de codigo, capitulos, items, move, reorder
    apu.service.ts            GET /apus, GET /apus/{apuId}, matrices y componentes
    resource.service.ts       listado, detalle, alta, edicion, baja, copia, sugerencias, cotizacion, by-unit, tipos
    unit.service.ts           catalogo de unidades y alta
    supplier.service.ts       opciones de proveedor
    project-context.ts        helper para validar el proyecto de la ruta y el mensaje "Proyecto no disponible"
  features/wbs/
    edt/                      pantalla unica del arbol
      edt.ts|html|scss
      chapter-form-dialog/    alta y edicion de capitulo
      item-form-dialog/       edicion de item
      move-node-dialog/       "Mover a..."
      conflict-dialog/        alerta de conflicto reutilizable
      activity-card/          tarjeta de actividad
      search-panel/           busqueda con resultados
    apu-create/               pagina "Nuevo APU"
    apu-edit/                 pagina de edicion del APU
    composition-editor/       bloque de composicion compartido por alta y edicion
  features/resources/
    resource-list/            listado con filtros
    resource-form-dialog/     alta y edicion
    resource-detail-dialog/   detalle y composicion de Matriz
    matrix-component-dialog/  submodal agregar/editar componente
    resource-copy-dialog/     copia desde otro proyecto
    quotation-dialog/         cotizacion masiva
    unit-create-dialog/       submodal "Nueva unidad"
```

- Se sigue la convención existente: carpeta por componente con archivos `nombre.ts`, `nombre.html` y `nombre.scss`,
  clase sin sufijo `Component` (`ProyectoDetalle`, `MainLayout`), selectores `app-*` y SCSS con bloque/elemento.
- `features/apu/apu-placeholder.ts` y la ruta `/apu` no se modifican ni se reutilizan para crear APUs.

### D3. Estado con signals y una única fuente de verdad

- Cada página mantiene su estado local con `signal`/`computed`: `loading`, `error`, `nodes`, `selectedNodeId`,
  `expandedIds`, `filters`, `page`, `dialogState` y `saving`.
- Los servicios exponen métodos que devuelven `Observable` y **no** guardan estado de pantalla; la caché de
  catálogos (tipos, unidades, países, monedas) vive en el servicio con un `signal` interno y se refresca cuando el
  usuario crea una unidad o cuando cambia el proyecto.
- Invalidación explícita después de cada mutación: alta, edición, baja, movimiento o reordenamiento recargan
  `GET /wbs`; alta, edición, copia, cotización o baja de insumos recargan `GET /resources` conservando filtros,
  página y orden.
- La única mutación optimista es la del movimiento por arrastrar y soltar: se aplica al signal local, se guarda una
  copia del estado anterior y se restaura si el servidor rechaza (ver D5).
- **Alternativa descartada:** NgRx. El proyecto ya resolvió `Q-46` con servicios y signals (`D-57`) y este cambio no
  necesita historial de acciones.

### D4. Modelos y servicios HTTP sin DTOs inventados

- Los modelos del cliente replican uno a uno los DTOs verificados del backend, en `camelCase` (`D-55`), incluso los
  nombres que difieren entre listados: `nodeId` en la búsqueda del EDT y `resourceId` en insumos.
- `PagedResult<T>` de `core/models/api-envelope.model.ts` se reutiliza solo para los endpoints con envelope
  (`/resources`, `/wbs/search`, `/projects`, catálogos de ubicación y divisas). `GET /catalogs/units`,
  `GET /resource-types` y `GET /resources/by-unit/{shortUnit}` devuelven arreglos planos y se tipan como tales.
- `rowVersion` viaja siempre como cadena Base64 sin transformación, tanto en el ítem (`rowVersion`) como en el APU
  (`apuRowVersion`) y en la Matriz (`rowVersion`).
- Los servicios construyen `HttpParams` de forma explícita y omiten los filtros vacíos para no enviar cadenas vacías.
- **Alternativa descartada:** generar el cliente desde OpenAPI (`D-56`). El proyecto ya escribió a mano sus servicios
  actuales (`project.service.ts`, `catalog.service.ts`) y no hay generador configurado en `Frontend/`.

### D5. Arrastrar y soltar con `@angular/cdk/drag-drop` y acciones explícitas

La pantalla usa una tabla jerárquica (`p-treeTable`) con filas de capítulo e ítem y una zona de raíz al final. El
arrastre se implementa con `CdkDropList`/`CdkDrag` de `@angular/cdk/drag-drop`, que ya es dependencia del proyecto,
sin agregar librerías.

```text
+--------------------------------------------------------------+
| Zona de raiz (drop list)                                     |
|  +--------------------------------------------------------+  |
|  | C1  Capitulo                        [::] (drag handle)  |  |
|  |   1.1  Item                           [::]              |  |
|  |   C1.1 Capitulo                       [::]              |  |
|  +--------------------------------------------------------+  |
+--------------------------------------------------------------+
```

| Gesto | Destino calculado |
|---|---|
| Soltar sobre la fila de un capítulo | `targetParentChapterId = chapterId`, `targetIndex = null` (último hijo) |
| Soltar entre dos filas hermanas | `targetParentChapterId` del padre de esas filas, `targetIndex` de la posición |
| Soltar en la zona de raíz | `targetParentChapterId = null`, `targetIndex = cantidad de nodos raíz` |
| Soltar sobre un ítem, sobre sí mismo o sobre un descendiente | No se permite: cursor prohibido y texto de ayuda |

- El índice se calcula **después** de retirar el nodo de su lista actual, igual que el servidor (`D-118`), para que
  un movimiento dentro del mismo padre no se desfase.
- Cada fila y la zona de raíz son `CdkDropList` con `enterPredicate` que evalúa tipo de nodo y subárbol; el nodo
  arrastrado se identifica por `nodeType` + `nodeId`. Un ítem nunca se registra como contenedor.
- Si el padre destino es el mismo y solo cambia el orden, se usa `POST /wbs/reorder` con la lista completa de
  hermanos; en cualquier otro caso, `POST /wbs/move`. Tras `200` se recarga `GET /wbs` porque la respuesta no trae
  costos.
- `move-node-dialog` ofrece la misma operación con dos selectores (destino y posición) y excluye el propio subárbol.
  “Subir” y “Bajar” calculan el índice de un hermano adyacente y usan `POST /wbs/reorder`.
- **Alternativa descartada:** `p-tree` de PrimeNG con `draggableNodes`. Se descarta porque la pantalla necesita
  columnas de costo y estado y `p-tree` no las soporta; `p-treeTable` no trae arrastre nativo.
- **Alternativa descartada:** arrastre HTML5 manual, por accesibilidad y por el marcador entre filas.

### D6. Alertas de conflicto en español con un mapa único de códigos

`resolveApiError` sigue siendo el punto de entrada. Se amplía `CODE_MESSAGES` en `core/http/api-error.ts` y se
agrega un `ConflictAlert` (`conflict-dialog`) que recibe `title`, `message`, `action` y `detail`.

| Código | Mensaje | Acción de la UI |
|---|---|---|
| `WBS_MOVE_CYCLE` | No puedes mover un capítulo dentro de sí mismo ni dentro de uno de sus subcapítulos. | Mantener el árbol y resaltar el nodo |
| `WBS_TARGET_NOT_FOUND` | El destino ya no existe en el proyecto. Actualizamos el árbol; elige un capítulo vigente. | Refrescar |
| `VALIDATION_ERROR` (destino no capítulo) | Solo puedes mover dentro de un capítulo o a la raíz del presupuesto. | Sin refresco |
| `WBS_CONCURRENCY_CONFLICT` | Otro usuario modificó el EDT mientras movías el nodo. Actualizamos el árbol; revisa los cambios y vuelve a intentarlo. | Refrescar |
| `WBS_CHAPTER_HAS_APU` | El capítulo contiene ítems con APU. Elimina o mueve primero esos ítems. | Sin cambios |
| `WBS_NODE_HAS_CHILDREN` | El capítulo tiene subcapítulos o ítems; solo se elimina un capítulo vacío. | Sin cambios |
| `WBS_CODE_DUPLICATED` | Ese código ya está en uso en el proyecto (sin distinguir mayúsculas). Elige otro. | Mantener el formulario |
| `WBS_CODE_INVALID` | El código debe tener entre 1 y 10 caracteres y solo los símbolos permitidos. | Mantener el formulario |
| `WBS_QUANTITY_INVALID` | La cantidad de obra debe ser mayor que cero. | Mantener el formulario |
| `WBS_NOT_FOUND` | El nodo ya no existe en el proyecto. Actualizamos el árbol. | Refrescar |
| `WBS_READ_FORBIDDEN` / `WBS_WRITE_FORBIDDEN` | No tienes permisos para consultar o modificar el EDT. | Volver a `/proyectos` |
| `APU_CONCURRENCY_CONFLICT` | Otro usuario modificó el APU mientras editabas. Actualizamos el APU; revisa los cambios y vuelve a intentarlo. | Refrescar y conservar lo escrito |
| `APU_RESOURCE_NOT_FOUND` | Uno de los insumos de la composición ya no existe en el proyecto. | Refrescar la lista de insumos |
| `APU_RESOURCE_PROJECT_MISMATCH` | La composición solo admite insumos de este proyecto. | Refrescar la lista de insumos |
| `APU_READ_FORBIDDEN` / `APU_WRITE_FORBIDDEN` | No tienes permisos para consultar o modificar APUs. | Ocultar acciones |
| `MATRIX_NOT_FOUND` | La Matriz ya no existe en el proyecto. | Refrescar el listado |
| `MATRIX_NESTING_FORBIDDEN` | Una Matriz no puede contener otra Matriz ni a sí misma. | Mantener el submodal |
| `MATRIX_TYPE_INVALID` | El insumo seleccionado no es un insumo simple del proyecto. | Mantener el submodal |
| `MATRIX_CONCURRENCY_CONFLICT` | Otro usuario modificó la Matriz. Actualizamos la composición; vuelve a intentarlo. | Refrescar la composición |
| `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION` | La copia de una Matriz requiere la operación de copia de Matriz. | Cerrar la copia sin cambios |
| `RESOURCE_IN_USE` | Este insumo está en uso en un APU, en una Matriz o como origen de una copia. No se puede eliminar. | Sin cambios |
| `RESOURCE_NOT_FOUND` | Este insumo ya no existe en el proyecto. | Refrescar el listado |
| `RESOURCE_CONCURRENCY_CONFLICT` | Otro usuario modificó este insumo mientras editabas. Actualizamos los datos; vuelve a intentarlo. | Recargar el insumo |
| `RESOURCE_TYPE_INVALID` | El tipo de insumo seleccionado no es válido. | Mantener el modal |
| `MUNICIPALITY_INVALID` | El municipio seleccionado no es válido. | Mantener el modal |
| `RESOURCE_READ_FORBIDDEN` / `RESOURCE_WRITE_FORBIDDEN` | No tienes permisos para consultar o modificar insumos. | Volver a `/proyectos` |
| `RESOURCE_QUOTATION_INCOMPLETE` | El insumo está incompleto; completa descripción, unidad, precio, marca y proveedor antes de cotizarlo. | Mostrar en el resumen del lote |
| `UNIT_ALREADY_EXISTS` | Ya existe una unidad con esa sigla. Elige otra o usa la existente. | Mantener el submodal |
| `UNIT_VALIDATION_ERROR` | La sigla debe tener de 1 a 10 caracteres y el nombre de 1 a 100. | Mantener el submodal |
| `PROJECT_NOT_FOUND` | El proyecto no existe o no está disponible. | Volver a `/proyectos` |
| `PAGE_SIZE_INVALID` | El tamaño de página debe ser 10, 50 o 100. | Corregir el tamaño |

El código y el `traceId` se muestran solo en una sección plegable “Detalle técnico” del diálogo; nunca como texto
principal. Las confirmaciones destructivas usan `ConfirmationService` (`p-confirmDialog`), ya montado en
`MainLayout`.

### D7. Validación local espejo, sin reglas de negocio nuevas

- Los formularios son reactivos y declaran solo las restricciones del contrato: código 1–10 con el conjunto de
  `D-117`, cantidad de obra y consumo mayores que cero, desperdicio 0–100, descripción y unidad no vacías, longitudes
  del insumo y precio mayor o igual que cero con dos decimales.
- La validación del servidor sigue siendo la única autoridad: si el backend rechaza un valor que el cliente aceptó,
  se muestra el mensaje del mapa de D6 y se conserva el formulario.
- El código del EDT se valida al salir del campo con `GET /wbs/codes/validate` (que exige `WBS_WRITE`); en la edición
  se envía `excludeNodeId` con el nodo actual para que su propio código no aparezca como duplicado. Si el usuario no
  tiene ese permiso, el modal de capítulo no se abre.
- No se valida en el cliente la completitud del APU ni del insumo: eso produce `status` y `quotationStatus`, que son
  del servidor.

### D8. Modales y submodales de insumos

```text
resource-list
  |-- "Nuevo insumo"        -> resource-form-dialog (modo alta)
  |-- "Ver detalle"        -> resource-detail-dialog
  |                            |-- "Editar"   -> resource-form-dialog (modo edicion)
  |                            |-- "Copiar"   -> resource-copy-dialog
  |                            |-- "Eliminar" -> confirmacion
  |                            |-- Matriz: "Agregar componente"  -> matrix-component-dialog (alta)
  |                            |          "Editar componente"   -> matrix-component-dialog (edicion)
  |                            |          "Quitar componente"   -> confirmacion
  |-- "Cotizar seleccionados" / "Cotizacion masiva" -> quotation-dialog
  `-- "Nueva unidad" (dentro de los formularios)    -> unit-create-dialog
```

- Un único `MessageService` global confirma altas, ediciones y bajas; el modal de detalle se reutiliza después del
  alta de una Matriz para encadenar la composición.
- Los submodales de componente usan el `rowVersion` de la Matriz que devuelve la composición vigente; después de
  cada operación se recarga la composición, nunca se parchea a mano el costo directo.
- La lista de insumos para un componente se obtiene del listado del proyecto y se filtra por
  `resourceTypeCode !== 'MATRIX'`; el propio insumo Matriz queda excluido por identificador.
- Los filtros, la página y el orden del listado viven en la pantalla y sobreviven al cierre de los modales.

### D9. Permisos: guardias de ruta y ocultamiento de acciones

`core/auth/permissions.ts` agrega:

```ts
wbsRead: 'WBS_READ',            wbsWrite: 'WBS_WRITE',
apusRead: 'APUS_READ',          apusWrite: 'APUS_WRITE',
resourcesRead: 'RESOURCES_READ', resourcesWrite: 'RESOURCES_WRITE',
suppliersRead: 'SUPPLIERS_READ', catalogsWrite: 'CATALOGS_WRITE'
```

| Elemento | Condición |
|---|---|
| Acceso “EDT” en la ficha | `wbsRead` |
| Acceso “Insumos” en la ficha | `resourcesRead` |
| Ruta `/edt` | `permissionGuard(wbsRead)` |
| Ruta `/edt/items/nuevo` | `permissionGuard(wbsRead)` y, dentro de la página, `wbsWrite && apusWrite` |
| Ruta `/edt/items/:itemId/apu` | `permissionGuard(apusRead)`; edición solo con `wbsWrite && apusWrite` |
| Ruta `/insumos` | `permissionGuard(resourcesRead)` |
| “Nuevo capítulo”, “Editar”, “Eliminar”, “Mover a…”, “Subir”, “Bajar” | `wbsWrite` |
| “Nuevo APU” y bloque APU del modal de ítem | `apusWrite` (además de `wbsWrite`) |
| Composición de Matriz visible / editable | `apusRead` / `apusWrite` |
| “Nuevo insumo”, “Editar”, “Copiar”, “Eliminar”, “Cotizar” | `resourcesWrite` |
| Selector de proveedor / sugerencias | `suppliersRead` / `resourcesWrite` |
| “Nueva unidad” | `catalogsWrite` |

Las acciones se **ocultan**, no se deshabilitan. Un `403` inesperado se maneja con el mensaje del mapa y refresco,
sin dejar la pantalla en estado inconsistente. No se agregan ítems a `navGroups` de `main-layout.ts`.

### D10. Desviación documentada: PrimeNG en lugar de Angular Material (`D-43`)

`D-43` declara Angular Material como biblioteca de UI, pero el cliente implementado en `Frontend/` usa **PrimeNG 20
con el tema Aura**: no hay dependencia de `@angular/material` en `package.json` y todos los componentes existentes son
`p-*` con `MessageService` y `ConfirmationService`. Este cambio mantiene PrimeNG para no introducir una segunda
biblioteca, una segunda paleta ni dos sistemas de diseño en el mismo producto. La discrepancia se registra en
`design.md` y se traslada a `PLAN_INGENIERIA.md` al archivar (`D-109` abrió la etapa de interfaz con el cliente ya
construido).

### D11. Presentación de valores: solo formato, nunca cálculo

- Los costos, subtotales, AIU, totales y estados se muestran con el valor recibido. Se usa `DecimalPipe`/`CurrencyPipe`
  con la configuración de la presentación (`baseCurrency` del proyecto cuando esté disponible en la ficha) y con los
  decimales del proyecto.
- Un valor `null` se presenta como “—” con la ayuda “APU incompleto o sin precio” en el EDT y “APU incompleto” en el
  APU. Un `0` del servidor se presenta como `0`: no se confunde con la ausencia de dato.
- Prohibido en todo el feature: multiplicar consumo por precio, aplicar desperdicio, sumar subtotales o derivar
  `COMPLETO`/`FALTA_INFORMACION`, `POR_COTIZAR`/`COTIZADO`.
- `unitPrice` del árbol es el mismo `unitCost` del servidor; el cliente no lo recalcula.

### D12. Accesibilidad y diseño usable entre 320 px y 1920 px

- Cada control tiene `label` asociado (`for`/`inputId`), como ya hace `proyecto-detalle.html`.
- Los diálogos de PrimeNG se abren con foco en el primer campo, se cierran con `Esc` y devuelven el foco al botón de
  origen; el arrastre no es la única vía porque “Mover a…”, “Subir” y “Bajar” son accesibles con teclado.
- El estado del arrastre se anuncia con texto (`aria-live`) indicando destino válido o el motivo de rechazo.
- Las tablas permiten desplazamiento horizontal en pantallas pequeñas; los formularios usan las rejillas
  `config-form__grid` existentes (`repeat(auto-fit, minmax(...))`).
- Los mensajes de error se asocian al campo y al resumen del modal; los colores de severidad salen del tema Aura.

### D13. Copia de insumo: proyecto de origen elegido con endpoints existentes

El contrato no tiene una búsqueda de insumos entre proyectos. Para no inventar endpoints, el modal de copia usa dos
operaciones que ya existen:

```text
1. GET /projects?pageNumber=1&pageSize=100        -> proyectos de la compañía del actor (PROJECTS_READ)
2. GET /projects/{sourceProjectId}/resources      -> insumos del proyecto elegido (RESOURCES_READ en el origen)
3. POST /projects/{targetProjectId}/resources/copy -> { sourceResourceId } (RESOURCES_WRITE en el destino)
```

- El proyecto de destino de la ruta se excluye de la lista de orígenes.
- Si el actor no tiene lectura sobre el proyecto de origen, el paso 2 responde `403` o no devuelve datos: el modal
  muestra “El insumo de origen no está disponible.” y permite elegir otro proyecto.
- **Limitación conocida:** un Presupuestador no asignado a un proyecto no puede leer sus insumos
  (`REQ-INSUMOS-14`), de modo que solo podrá copiar desde proyectos donde tenga lectura. Se documenta en riesgos; no
  se agrega un endpoint de búsqueda global.

### D14. Mapa endpoint → caso de uso

| Endpoint | Uso en el cliente | Servicio | Permiso |
|---|---|---|---|
| `GET /projects/{projectId}` | validar el proyecto de la ruta | `ProjectService` | `PROJECTS_READ` |
| `GET /projects?pageNumber=&pageSize=` | proyectos de origen para la copia | `ProjectService.list` | `PROJECTS_READ` |
| `GET /wbs` | árbol y versiones del ítem/APU | `WbsService.getTree` | `WBS_READ` |
| `GET /wbs/search` | búsqueda del EDT | `WbsService.search` | `WBS_READ` |
| `GET /wbs/activity` | tarjeta de actividad | `WbsService.getActivity` | `WBS_READ` |
| `GET /wbs/codes/validate` | validación del código | `WbsService.validateCode` | `WBS_WRITE` |
| `POST /wbs/chapters` | alta de capítulo | `WbsService.createChapter` | `WBS_WRITE` |
| `PUT /wbs/chapters/{chapterId}` | edición de capítulo | `WbsService.updateChapter` | `WBS_WRITE` |
| `DELETE /wbs/chapters/{chapterId}` | baja de capítulo vacío | `WbsService.deleteChapter` | `WBS_WRITE` |
| `POST /wbs/items` | alta conjunta ítem + APU | `WbsService.createItem` | `WBS_WRITE` + `APUS_WRITE` |
| `PUT /wbs/items/{itemId}` | edición conjunta | `WbsService.updateItem` | `WBS_WRITE` + `APUS_WRITE` |
| `DELETE /wbs/items/{itemId}` | baja de ítem y su APU | `WbsService.deleteItem` | `WBS_WRITE` |
| `POST /wbs/reorder` | reordenar hermanos y “Subir”/“Bajar” | `WbsService.reorder` | `WBS_WRITE` |
| `POST /wbs/move` | arrastrar y soltar y “Mover a…” | `WbsService.move` | `WBS_WRITE` |
| `GET /apus/{apuId}` | detalle del APU en la edición | `ApuService.getById` | `APUS_READ` |
| `GET /apus` | listado de APUs del proyecto (no se usa para alta) | `ApuService.list` | `APUS_READ` |
| `GET /resources` | listado de insumos y selector de composición | `ResourceService.list` | `RESOURCES_READ` |
| `GET /resources/{resourceId}` | detalle del insumo | `ResourceService.getById` | `RESOURCES_READ` |
| `GET /resources/by-unit/{shortUnit}` | filtro por unidad del APU | `ResourceService.byUnit` | `RESOURCES_READ` |
| `GET /resource-types` | filtro y formulario de tipo | `ResourceService.types` | `RESOURCES_READ` |
| `GET /resources/suggestions` | sugerencias de duplicados | `ResourceService.suggestions` | `RESOURCES_WRITE` |
| `POST /resources` | alta de insumo | `ResourceService.create` | `RESOURCES_WRITE` |
| `PUT /resources/{resourceId}` | edición de insumo | `ResourceService.update` | `RESOURCES_WRITE` |
| `DELETE /resources/{resourceId}` | baja de insumo y de Matriz | `ResourceService.delete` | `RESOURCES_WRITE` |
| `POST /resources/copy` | copia desde otro proyecto | `ResourceService.copy` | `RESOURCES_WRITE` |
| `PATCH /resources/quotation-status` | cotización masiva | `ResourceService.changeQuotationStatus` | `RESOURCES_WRITE` |
| `GET /matrices/{id}/components` | composición de la Matriz | `ApuService.getMatrixComponents` | `APUS_READ` |
| `POST /matrices/{id}/components` | agregar componente | `ApuService.addMatrixComponent` | `APUS_WRITE` |
| `PUT /matrices/{id}/components/{componentId}` | editar componente | `ApuService.updateMatrixComponent` | `APUS_WRITE` |
| `DELETE /matrices/{id}/components/{componentId}` | quitar componente | `ApuService.removeMatrixComponent` | `APUS_WRITE` |
| `GET /catalogs/units` | selector de unidad | `UnitService.list` | `CATALOGS_READ` |
| `POST /catalogs/units` | “Nueva unidad” | `UnitService.create` | `CATALOGS_WRITE` |
| `GET /catalogs/countries`, `/departments`, `/municipalities`, `/currencies` | ubicación del insumo y formato | `CatalogService` | `CATALOGS_READ` (países, departamentos y municipios son anónimos) |
| `GET /suppliers/options` | selector de proveedor | `SupplierService.options` | `SUPPLIERS_READ` |

Los nueve permisos ya existen en el `seed` de seguridad del backend (`SecurityBaselineSeeder`), por lo que el cambio
no requiere permisos nuevos ni volver a sembrar la base.

## Risks / Trade-offs

- **Arrastrar y soltar sobre una tabla jerárquica** → el CDK necesita un `CdkDropList` por contenedor (cada capítulo y
  la raíz) y filas registradas dinámicamente. Mitigación: usar un `id` de lista derivado de `parentChapterId` y un
  `enterPredicate` puro; si el gesto falla, “Mover a…”, “Subir” y “Bajar” siguen disponibles y cubren la operación.
- **Movimiento optimista y concurrencia** → dos gestos seguidos pueden desordenar la vista. Mitigación: un solo
  movimiento en vuelo por pantalla, arrastre deshabilitado mientras `saving` es `true` y recarga de `GET /wbs` al
  terminar.
- **`GET /wbs/codes/validate` exige `WBS_WRITE`** → un usuario de solo lectura no puede validar códigos. Mitigación:
  los formularios de alta y edición solo se muestran con `WBS_WRITE`; sin ese permiso no se llama al endpoint.
- **`GET /resources/suggestions` exige `RESOURCES_WRITE`** → un lector no puede ver sugerencias. Mitigación: la
  consulta se dispara solo desde los modales de alta y edición, que ya requieren escritura.
- **Copia limitada por lectura del proyecto de origen** → un Presupuestador no asignado no puede listar los insumos de
  otro proyecto. Mitigación: mensaje explícito y elección de otro proyecto; queda registrado como limitación, no se
  agrega contrato nuevo.
- **Composición enviada por error** → `PUT /wbs/items` reemplaza la composición completa cuando llega `apu.resources`.
  Mitigación: bandera `compositionDirty` que solo se activa con agregar, editar o quitar líneas; sin esa bandera el
  bloque `resources` no se envía.
- **Detección del tipo Matriz** → el catálogo de tipos no devuelve `code`. Mitigación: la decisión de abrir la
  composición se toma con `resourceTypeCode` del insumo creado o listado, no con el catálogo de tipos.
- **Árbol grande** → cargar todo el EDT para ubicar un ítem en la página de edición puede ser costoso. Mitigación:
  `p-treeTable` con desplazamiento y expansión bajo demanda en la vista; el costo se acepta porque el contrato no
  ofrece una consulta de nodo individual.
- **Sin pruebas automatizadas de frontend** → la verificación es manual. Mitigación: checklist de la tarea 15 y
  reutilización de los escenarios de los specs como guion de prueba.
- **Desviación `D-43` (Angular Material)** → el cliente real usa PrimeNG. Mitigación: mantener una sola biblioteca y
  registrar la desviación en `PLAN_INGENIERIA.md` al archivar.

## Migration Plan

- Sin migraciones de base de datos ni cambios de contrato: el despliegue publica únicamente el bundle del cliente.
- Orden de despliegue: backend sin cambios → cliente con las rutas nuevas. Las rutas nuevas no afectan a las
  existentes y `/apu` sigue igual.
- Reversión: volver al bundle anterior del cliente; no hay datos ni contratos que revertir.
- Verificación posterior: `ng build` sin errores y el checklist manual de la tarea 15 sobre un proyecto con capítulos,
  ítems con APU, insumos simples y al menos una Matriz.
