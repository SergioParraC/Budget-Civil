# Spec Delta: insumos

## ADDED Requirements

### Requirement: REQ-INSUMOS-23 Listado de insumos del proyecto en una sola pantalla

El cliente DEBE (MUST) presentar los insumos del proyecto en un listado paginado, ordenado y filtrado por el
servidor dentro de `/proyectos/:id/insumos`. Citas: RF-05, RF-16; D-101; `REQ-INSUMOS-06`…`08`.

#### Scenario: SC-INSUMOS-84 Listado con datos del insumo

- **Dado** un usuario con `RESOURCES_READ` que abre `/proyectos/:id/insumos`
- **Cuando** el cliente consulta `GET /resources`
- **Entonces** la tabla muestra descripción, tipo, unidad, precio unitario, marca, proveedor, municipio, estado de
  cotización, última modificación y acciones de cada insumo

#### Scenario: SC-INSUMOS-85 Paginación y orden en servidor

- **Dado** un listado con más registros que el tamaño de página
- **Cuando** el usuario cambia de página, el tamaño (10, 50 o 100) o el orden por descripción, precio, estado o
  última modificación
- **Entonces** el cliente envía `page`, `pageSize`, `sortBy` y `sortDirection` y muestra únicamente la página
  devuelta por el servidor

#### Scenario: SC-INSUMOS-86 Filtros del listado

- **Dado** un usuario que filtra por búsqueda, tipo, estado de cotización, marca, municipio o rango de precio
- **Cuando** aplica el filtro
- **Entonces** el cliente envía los criterios al servidor, vuelve a la primera página y el botón “Limpiar” restablece
  todos los filtros

#### Scenario: SC-INSUMOS-87 Filtros inválidos

- **Dado** un rango de precio con mínimo mayor que el máximo
- **Cuando** el usuario aplica el filtro
- **Entonces** el cliente muestra el mensaje en español, no envía la consulta y conserva los filtros anteriores

#### Scenario: SC-INSUMOS-88 Estados de carga, vacío y error

- **Dado** un listado en carga, sin insumos o con una consulta fallida
- **Cuando** la pantalla se muestra
- **Entonces** el cliente presenta un esqueleto, “Aún no hay insumos en este proyecto” con la acción de alta, o un
  mensaje de error con opción de reintentar

#### Scenario: SC-INSUMOS-89 Filtros conservados al cerrar un modal

- **Dado** un listado con filtros aplicados
- **Cuando** el usuario abre un modal y luego lo cierra sin guardar
- **Entonces** el cliente conserva los filtros, la página y el orden vigentes

### Requirement: REQ-INSUMOS-24 Alta y edición de insumo en modal

El cliente DEBE (MUST) crear y editar insumos en un modal con las secciones de identificación, costo y
abastecimiento, y detalles, enviando los campos admitidos por el contrato. Citas: RF-05; BR-009; D-16, D-96.

#### Scenario: SC-INSUMOS-90 Alta con datos válidos

- **Dado** un usuario con `RESOURCES_WRITE` que completa tipo, descripción y municipio
- **Cuando** guarda y `POST /resources` responde `201`
- **Entonces** el cliente cierra el modal, confirma el alta y refresca el listado conservando los filtros

#### Scenario: SC-INSUMOS-91 Cascada de ubicación

- **Dado** un modal de alta con la ubicación del proyecto como valor inicial
- **Cuando** el usuario cambia el país o el departamento
- **Entonces** el cliente recarga departamentos y municipios de los catálogos y limpia la selección dependiente que
  deja de ser válida

#### Scenario: SC-INSUMOS-92 Validaciones locales del insumo

- **Dado** un formulario sin tipo, sin descripción, sin municipio, con descripción de más de 255 caracteres o con
  precio negativo o de más de dos decimales
- **Cuando** el usuario intenta guardar
- **Entonces** el cliente marca el campo con su mensaje en español y no envía la solicitud

#### Scenario: SC-INSUMOS-93 Edición de un insumo existente

- **Dado** un usuario con `RESOURCES_WRITE` que abre “Editar” en un insumo
- **Cuando** cambia un campo y guarda
- **Entonces** el cliente envía `PUT /resources/{resourceId}` con el `rowVersion` vigente y refresca el listado y el
  detalle al responder

#### Scenario: SC-INSUMOS-94 Conflicto de concurrencia del insumo

- **Dado** un guardado que responde `409 RESOURCE_CONCURRENCY_CONFLICT`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Otro usuario modificó este insumo mientras editabas. Actualizamos los datos; vuelve a
  intentarlo.”, recarga el insumo y conserva lo escrito para reintentar

#### Scenario: SC-INSUMOS-95 Tipo o municipio inválido

- **Dado** un guardado que responde `RESOURCE_TYPE_INVALID` o `MUNICIPALITY_INVALID`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “El tipo de insumo seleccionado no es válido.” o “El municipio seleccionado no es válido.” y
  mantiene el modal abierto

### Requirement: REQ-INSUMOS-25 Sugerencias de duplicados no bloqueantes

El cliente DEBE (MUST) consultar `GET /resources/suggestions` al escribir la descripción o la clave de usuario y
DEBE (MUST) tratarlas como informativas, sin bloquear el guardado. Citas: RF-16; D-16; `REQ-INSUMOS-05`.

#### Scenario: SC-INSUMOS-96 Sugerencias durante el alta

- **Dado** un usuario con `RESOURCES_WRITE` que escribe una descripción o clave parecida a un insumo existente
- **Cuando** el cliente consulta las sugerencias
- **Entonces** muestra los insumos similares con su descripción, clave, precio y estado, y permite continuar con el
  alta

#### Scenario: SC-INSUMOS-97 Guardado con coincidencias

- **Dado** un modal con sugerencias visibles
- **Cuando** el usuario confirma el alta
- **Entonces** el cliente envía la solicitud y no convierte la sugerencia en un error de unicidad

#### Scenario: SC-INSUMOS-98 Sugerencias sin permiso de escritura

- **Dado** un usuario sin `RESOURCES_WRITE`
- **Cuando** abre un modal de solo lectura
- **Entonces** el cliente no consulta `GET /resources/suggestions`

### Requirement: REQ-INSUMOS-26 Estado de cotización de solo lectura

El cliente DEBE (MUST) presentar el estado de cotización devuelto por el servidor como dato de solo lectura y NO DEBE
(MUST NOT) deducirlo de los campos del formulario. Citas: RF-05, RF-11; BR-009; D-89.

#### Scenario: SC-INSUMOS-99 Estado mostrado sin edición

- **Dado** un insumo en `FALTA_INFORMACION`, `POR_COTIZAR` o `COTIZADO`
- **Cuando** el cliente muestra el listado, el detalle o el modal de edición
- **Entonces** presenta el estado como etiqueta y no ofrece ningún control para cambiarlo desde el formulario

#### Scenario: SC-INSUMOS-100 Estado recalculado por el servidor

- **Dado** un insumo editado que completa o retira un campo obligatorio
- **Cuando** el cliente recibe la respuesta del servidor
- **Entonces** muestra el estado devuelto en esa respuesta sin recalcularlo localmente

### Requirement: REQ-INSUMOS-27 Detalle del insumo en modal

El cliente DEBE (MUST) mostrar el detalle completo de un insumo en un modal de solo lectura con las acciones
disponibles según el permiso. Citas: RF-05; D-109.

#### Scenario: SC-INSUMOS-101 Detalle de un insumo simple

- **Dado** un usuario con `RESOURCES_READ` que elige “Ver detalle” en un insumo simple
- **Cuando** el modal se abre
- **Entonces** muestra todos los datos del insumo, su proveedor, su estado de cotización, su última modificación y las
  acciones “Editar”, “Copiar”, “Eliminar” y “Cerrar” que correspondan a sus permisos

#### Scenario: SC-INSUMOS-102 Detalle sin permiso de escritura

- **Dado** un usuario con `RESOURCES_READ` y sin `RESOURCES_WRITE`
- **Cuando** abre el detalle de un insumo
- **Entonces** el cliente oculta “Editar”, “Copiar” y “Eliminar” y mantiene “Cerrar”

#### Scenario: SC-INSUMOS-103 Insumo inexistente en el detalle

- **Dado** un insumo eliminado por otro usuario
- **Cuando** el cliente consulta su detalle y recibe `404 RESOURCE_NOT_FOUND`
- **Entonces** muestra “Este insumo ya no existe en el proyecto.”, cierra el modal y refresca el listado

### Requirement: REQ-INSUMOS-28 Composición de una Matriz en submodales

El cliente DEBE (MUST) mostrar la composición de una Matriz en el modal de detalle y gestionar sus componentes en
submodales, respetando `APUS_READ` para consultar y `APUS_WRITE` para modificar. Citas: RF-07; D-48; `REQ-APUS-06`,
`REQ-APUS-07`.

#### Scenario: SC-INSUMOS-104 Composición visible para una Matriz

- **Dado** un insumo de tipo Matriz y un usuario con `APUS_READ`
- **Cuando** abre su detalle
- **Entonces** el cliente consulta `GET /matrices/{matrixResourceId}/components` y muestra cada componente con
  descripción, unidad, precio, cantidad, desperdicio y subtotal, más el costo directo devuelto por el servidor

#### Scenario: SC-INSUMOS-105 Agregar componente

- **Dado** un usuario con `APUS_WRITE` que elige “Agregar componente”
- **Cuando** selecciona un insumo simple del proyecto con consumo y desperdicio válidos y confirma
- **Entonces** el cliente envía `POST /matrices/{matrixResourceId}/components` con `componentResourceId`,
  `consumption`, `wastePercentage` y `rowVersion`, y refresca la composición

#### Scenario: SC-INSUMOS-106 Editar componente

- **Dado** un usuario con `APUS_WRITE` que edita un componente
- **Cuando** cambia consumo o desperdicio y confirma
- **Entonces** el cliente envía `PUT /matrices/{matrixResourceId}/components/{componentId}` y refresca la composición
  y el costo directo con los valores del servidor

#### Scenario: SC-INSUMOS-107 Quitar componente

- **Dado** un usuario con `APUS_WRITE` que quita un componente y confirma la advertencia
- **Cuando** el servidor responde `204`
- **Entonces** el cliente envía `DELETE /matrices/{matrixResourceId}/components/{componentId}?rowVersion=` y refresca
  la composición

#### Scenario: SC-INSUMOS-108 Destinos inválidos de un componente

- **Dado** un usuario que busca el insumo de un componente nuevo
- **Cuando** el cliente arma las opciones
- **Entonces** solo ofrece insumos simples del proyecto y excluye la propia Matriz y cualquier otra Matriz

#### Scenario: SC-INSUMOS-109 Composición sin permiso de lectura de APUs

- **Dado** un usuario con `RESOURCES_READ` y sin `APUS_READ`
- **Cuando** abre el detalle de una Matriz
- **Entonces** el cliente oculta la sección de composición con el aviso “No tienes permiso para ver la composición de
  esta Matriz.”

#### Scenario: SC-INSUMOS-110 Composición en solo lectura

- **Dado** un usuario con `APUS_READ` y sin `APUS_WRITE`
- **Cuando** abre el detalle de una Matriz
- **Entonces** el cliente muestra los componentes y oculta “Agregar componente”, “Editar componente” y “Quitar
  componente”

#### Scenario: SC-INSUMOS-111 Conflictos de la Matriz

- **Dado** una operación de componente que responde `409 MATRIX_CONCURRENCY_CONFLICT`, `MATRIX_NESTING_FORBIDDEN`,
  `MATRIX_TYPE_INVALID` o `APU_RESOURCE_NOT_FOUND`
- **Cuando** el cliente recibe el error
- **Entonces** muestra el mensaje en español correspondiente, conserva el submodal abierto y refresca la composición
  cuando la causa es la versión desactualizada

### Requirement: REQ-INSUMOS-29 Apertura de la composición al crear una Matriz

El cliente DEBE (MUST) abrir el modal de detalle en modo composición cuando el insumo recién creado es de tipo Matriz.
Citas: RF-07; D-48.

#### Scenario: SC-INSUMOS-112 Matriz creada desde el modal de alta

- **Dado** un usuario con `RESOURCES_WRITE` que guarda un insumo de tipo Matriz
- **Cuando** `POST /resources` responde `201`
- **Entonces** el modal de alta se cierra, se abre el modal de detalle del insumo y se ofrece agregar componentes si el
  usuario tiene `APUS_WRITE`

#### Scenario: SC-INSUMOS-113 Matriz creada sin APUS_WRITE

- **Dado** un usuario con `RESOURCES_WRITE` y sin `APUS_WRITE` que guarda una Matriz
- **Cuando** se abre el modal de detalle
- **Entonces** el cliente informa que la composición requiere permiso de APUs y no muestra las acciones de componentes

### Requirement: REQ-INSUMOS-30 Copia de un insumo desde otro proyecto de la compañía

El cliente DEBE (MUST) copiar un insumo desde un proyecto de la misma compañía en un modal que identifique el insumo
de origen y llame `POST /resources/copy`. Citas: RF-05; D-16, D-27; `REQ-INSUMOS-09`.

#### Scenario: SC-INSUMOS-114 Copia exitosa

- **Dado** un usuario con `RESOURCES_WRITE` en el proyecto de destino que elige “Copiar insumo”
- **Cuando** selecciona el proyecto de origen, busca y elige un insumo simple y confirma
- **Entonces** el cliente envía `POST /resources/copy` con `sourceResourceId`, cierra el modal, confirma la copia y
  refresca el listado de destino

#### Scenario: SC-INSUMOS-115 Origen en otro proyecto de la compañía

- **Dado** un modal de copia abierto
- **Cuando** el usuario elige el proyecto de origen
- **Entonces** el cliente lista los insumos de ese proyecto con `GET /projects/{sourceProjectId}/resources` y usa
  `GET /projects` para construir la lista de proyectos de la compañía, sin permitir un origen del propio proyecto de
  destino

#### Scenario: SC-INSUMOS-116 Origen que es una Matriz

- **Dado** un origen de tipo Matriz
- **Cuando** el servidor responde `400 RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`
- **Entonces** el cliente muestra “La copia de una Matriz requiere la operación de copia de Matriz.” y no crea ningún
  insumo

#### Scenario: SC-INSUMOS-117 Origen de otra compañía o sin acceso

- **Dado** un origen de otra compañía o de un proyecto sin lectura para el actor
- **Cuando** el servidor responde `404 RESOURCE_NOT_FOUND` o `403`
- **Entonces** el cliente muestra “El insumo de origen no está disponible.” y mantiene el modal abierto

#### Scenario: SC-INSUMOS-118 Copia sin permiso de escritura

- **Dado** un usuario sin `RESOURCES_WRITE`
- **Cuando** revisa el listado o el detalle
- **Entonces** la acción “Copiar insumo” no se muestra

### Requirement: REQ-INSUMOS-31 Cotización masiva en modal

El cliente DEBE (MUST) ofrecer la cotización masiva en un modal con los alcances selección, filtro actual y totalidad,
confirmando el número de insumos y mostrando el resumen del servidor. Citas: RF-11; D-11, D-12; `REQ-COTIZACION-01`.

#### Scenario: SC-INSUMOS-119 Modos de la cotización masiva

- **Dado** un usuario con `RESOURCES_WRITE` que abre “Cotización masiva”
- **Cuando** elige uno de los tres modos
- **Entonces** el cliente envía `PATCH /resources/quotation-status` con `scope = SELECTION` e `resourceIds`,
  `scope = FILTER` con los filtros vigentes del listado, o `scope = ALL` sin criterios adicionales

#### Scenario: SC-INSUMOS-120 Selección vacía

- **Dado** el modo “Selección” sin insumos marcados
- **Cuando** el usuario revisa el modal
- **Entonces** la acción de confirmar no se habilita y se explica que debe seleccionar al menos un insumo

#### Scenario: SC-INSUMOS-121 Confirmación con el número de insumos

- **Dado** un modo elegido
- **Cuando** el usuario continúa
- **Entonces** el cliente pide confirmación indicando cuántos insumos se van a cotizar antes de enviar la solicitud

#### Scenario: SC-INSUMOS-122 Resumen del lote

- **Dado** una cotización masiva aceptada
- **Cuando** el servidor responde con `requested`, `updated`, `alreadyQuoted`, `skipped`, `pendingResources` y
  `allQuoted`
- **Entonces** el cliente muestra esos valores, indica los insumos omitidos por información incompleta y refresca el
  listado con los datos del servidor

#### Scenario: SC-INSUMOS-123 Insumo de otro proyecto en la selección

- **Dado** una selección con un `resourceId` que no pertenece al proyecto de la ruta
- **Cuando** el servidor responde `404 RESOURCE_NOT_FOUND`
- **Entonces** el cliente muestra “Uno de los insumos seleccionados ya no pertenece a este proyecto.” y refresca el
  listado sin aplicar el lote

#### Scenario: SC-INSUMOS-124 Cotización sin permiso de escritura

- **Dado** un usuario sin `RESOURCES_WRITE`
- **Cuando** abre el listado de insumos
- **Entonces** las acciones “Cotizar seleccionados” y “Cotización masiva” no se muestran

### Requirement: REQ-INSUMOS-32 Baja de insumo con confirmación y bloqueo por uso

El cliente DEBE (MUST) eliminar un insumo solo tras confirmación explícita y DEBE (MUST) mostrar el motivo cuando el
servidor rechaza la baja con `409 RESOURCE_IN_USE`. Citas: RF-05; D-49; `REQ-INSUMOS-18`…`REQ-INSUMOS-20`.

#### Scenario: SC-INSUMOS-125 Baja confirmada de un insumo simple

- **Dado** un usuario con `RESOURCES_WRITE` que elige “Eliminar” en un insumo
- **Cuando** confirma la advertencia y el servidor responde `204`
- **Entonces** el cliente cierra el modal de detalle si estaba abierto, confirma la baja y refresca el listado

#### Scenario: SC-INSUMOS-126 Baja de una Matriz

- **Dado** un insumo de tipo Matriz
- **Cuando** el usuario abre la confirmación de baja
- **Entonces** la advertencia incluye “Se eliminarán también sus componentes”

#### Scenario: SC-INSUMOS-127 Insumo en uso

- **Dado** un insumo referenciado por un APU, por una Matriz o como origen de una copia
- **Cuando** el servidor responde `409 RESOURCE_IN_USE`
- **Entonces** el cliente muestra “Este insumo está en uso en un APU, en una Matriz o como origen de una copia. No se
  puede eliminar.” y el listado no cambia

#### Scenario: SC-INSUMOS-128 Baja de un insumo inexistente

- **Dado** un insumo eliminado por otro usuario
- **Cuando** el servidor responde `404 RESOURCE_NOT_FOUND`
- **Entonces** el cliente muestra “Este insumo ya no existe en el proyecto.” y refresca el listado

#### Scenario: SC-INSUMOS-129 Cancelación de la baja

- **Dado** la confirmación de baja abierta
- **Cuando** el usuario cancela
- **Entonces** el cliente no envía la solicitud y el listado no cambia

### Requirement: REQ-INSUMOS-33 Acceso y permisos de la pantalla de insumos

El cliente DEBE (MUST) proteger la pantalla de insumos y sus acciones con `RESOURCES_READ` y `RESOURCES_WRITE`, y
DEBE (MUST) manejar en español el `403` del servidor. Citas: RF-05; D-109; `REQ-INSUMOS-13`, `REQ-INSUMOS-14`.

#### Scenario: SC-INSUMOS-130 Sin permiso de lectura

- **Dado** un usuario autenticado sin `RESOURCES_READ`
- **Cuando** abre la ficha del proyecto
- **Entonces** el acceso “Insumos” no se muestra y la ruta `/proyectos/:id/insumos` no se abre

#### Scenario: SC-INSUMOS-131 Acciones de escritura ocultas

- **Dado** un usuario con `RESOURCES_READ` y sin `RESOURCES_WRITE`
- **Cuando** abre el listado y el detalle de un insumo
- **Entonces** no se muestran “Nuevo insumo”, “Editar”, “Copiar”, “Eliminar”, “Cotizar seleccionados” ni “Cotización
  masiva”

#### Scenario: SC-INSUMOS-132 Proyecto no disponible

- **Dado** un `projectId` inexistente o de otra compañía
- **Cuando** la pantalla valida el proyecto y recibe `404 PROJECT_NOT_FOUND` o `403`
- **Entonces** el cliente muestra “Proyecto no disponible” con un enlace a `/proyectos` y no consulta los insumos

#### Scenario: SC-INSUMOS-133 Prohibición en una operación

- **Dado** una operación de insumos que el servidor rechaza con `403`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “No tienes permisos para realizar esta acción.”, cierra el modal afectado y conserva el
  listado vigente

#### Scenario: SC-INSUMOS-134 Proveedor no disponible

- **Dado** un usuario sin `SUPPLIERS_READ`
- **Cuando** abre el modal de alta o edición
- **Entonces** el selector de proveedor no se muestra y el insumo se puede guardar sin proveedor
