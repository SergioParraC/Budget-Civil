# Spec Delta: apus

## ADDED Requirements

### Requirement: REQ-APUS-23 Página de alta que crea el ítem del EDT y su APU

El cliente DEBE (MUST) ofrecer el alta de APU en una página propia que cree el ítem del EDT y el APU en una sola
llamada a `POST /wbs/items`, porque no existe alta individual de APU. Citas: RF-07; BR-016; D-16, D-31, D-117.

#### Scenario: SC-APUS-111 Alta conjunta exitosa

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE` que completa código, ubicación, cantidad de obra, descripción,
  unidad y al menos una línea de composición
- **Cuando** confirma y `POST /wbs/items` responde `201`
- **Entonces** el cliente muestra el resumen devuelto por el servidor (ítem, APU, composición y costos) y ofrece
  “Volver al EDT” o “Crear otro”, e invalida el árbol para que se recargue

#### Scenario: SC-APUS-112 Alta sin líneas de composición

- **Dado** un usuario que registra descripción y unidad sin agregar líneas
- **Cuando** confirma el alta
- **Entonces** el cliente envía `resources: []`, guarda el APU y avisa que quedará en estado `FALTA_INFORMACION`

#### Scenario: SC-APUS-113 Ubicación inicial según la fila de origen

- **Dado** un usuario que llegó al alta desde el menú de un capítulo del árbol
- **Cuando** la página se abre
- **Entonces** el campo de ubicación queda preseleccionado con ese capítulo y sigue admitiendo “Raíz” u otro capítulo

#### Scenario: SC-APUS-114 Código rechazado en el alta

- **Dado** un alta con un código duplicado, inválido o una cantidad de obra no positiva
- **Cuando** el servidor responde `WBS_CODE_DUPLICATED`, `WBS_CODE_INVALID` o `WBS_QUANTITY_INVALID`
- **Entonces** el cliente muestra el mensaje en español correspondiente y conserva los datos del formulario

#### Scenario: SC-APUS-115 Validación local previa al envío

- **Dado** un formulario sin código, con cantidad de obra igual a cero o con una línea de consumo no positivo
- **Cuando** el usuario intenta guardar
- **Entonces** el cliente marca los campos inválidos con su mensaje en español y no envía la solicitud

#### Scenario: SC-APUS-116 Sin permiso para crear el APU

- **Dado** un usuario con `WBS_READ` que carece de `WBS_WRITE` o de `APUS_WRITE`
- **Cuando** intenta abrir `/proyectos/:id/edt/items/nuevo`
- **Entonces** la página no se abre y el acceso “Nuevo APU” no se muestra en el árbol

### Requirement: REQ-APUS-24 Composición editable en la página de alta

El cliente DEBE (MUST) permitir agregar, editar y quitar líneas de composición con insumos del proyecto, consumo
mayor que cero y desperdicio entre 0 y 100 con valor inicial 0, sin calcular subtotales. Citas: RF-07; `REQ-APUS-04`.

#### Scenario: SC-APUS-117 Selección de insumos del proyecto

- **Dado** un usuario que abre el selector de insumos de una línea
- **Cuando** busca por descripción o filtra por tipo
- **Entonces** el cliente consulta los insumos del proyecto y muestra descripción, tipo, unidad y precio unitario de
  cada uno, sin permitir insumos de otro proyecto

#### Scenario: SC-APUS-118 Filtro por la unidad del APU

- **Dado** un APU con unidad `M2` y el filtro “Solo insumos con la unidad del APU” activo
- **Cuando** el cliente carga las opciones
- **Entonces** usa `GET /resources/by-unit/M2` y muestra una lista vacía con un aviso cuando no hay coincidencias

#### Scenario: SC-APUS-119 Validación de consumo y desperdicio

- **Dado** una línea con consumo igual a cero o con desperdicio negativo o mayor que 100
- **Cuando** el usuario intenta agregarla o guardarla
- **Entonces** el cliente muestra el mensaje en español, no agrega la línea y mantiene el formulario abierto

#### Scenario: SC-APUS-120 Sin datos calculados en la composición

- **Dado** una línea con precio unitario, consumo y desperdicio
- **Cuando** la línea se muestra en el formulario
- **Entonces** el cliente presenta solo los valores del insumo y los datos escritos, y no muestra un subtotal
  calculado en el navegador

### Requirement: REQ-APUS-25 Unidad del APU con catálogo y texto libre

El cliente DEBE (MUST) ofrecer la unidad del APU con las sugerencias de `GET /catalogs/units` y DEBE (MUST) aceptar
una unidad fuera del catálogo. Citas: RF-05, RF-07; D-16, D-31.

#### Scenario: SC-APUS-121 Selección desde el catálogo

- **Dado** un catálogo con `M2` y `ML`
- **Cuando** el usuario abre el selector de unidad
- **Entonces** el cliente muestra las unidades con su sigla y descripción y guarda la sigla elegida en `unit`

#### Scenario: SC-APUS-122 Unidad escrita fuera del catálogo

- **Dado** un usuario que escribe la unidad `GL` que no existe en el catálogo
- **Cuando** confirma el alta
- **Entonces** el cliente envía `unit = GL` sin intentar crear la unidad y el APU se guarda con ese valor

#### Scenario: SC-APUS-123 Acceso al alta de unidad desde el formulario

- **Dado** un usuario con `CATALOGS_WRITE` que no encuentra la unidad que necesita
- **Cuando** elige la opción “Nueva unidad”
- **Entonces** el cliente abre un submodal con `shortUnit` y `unit`, crea la unidad con `POST /catalogs/units` y deja
  la nueva sigla seleccionada en el formulario

#### Scenario: SC-APUS-124 Alta de unidad sin permiso

- **Dado** un usuario sin `CATALOGS_WRITE`
- **Cuando** abre el selector de unidad
- **Entonces** la opción “Nueva unidad” no se muestra y el usuario puede seguir escribiendo una unidad libre

### Requirement: REQ-APUS-26 Página nueva de edición del APU del ítem

El cliente DEBE (MUST) editar el APU en una página propia, distinta de un modal, que combine el ítem del EDT con el
detalle del APU. Citas: RF-04, RF-07; D-31, D-50; `REQ-EDT-22`.

#### Scenario: SC-APUS-125 Carga de la página de edición

- **Dado** un usuario con `APUS_READ` que abre `/proyectos/:id/edt/items/:itemId/apu`
- **Cuando** la página se inicializa
- **Entonces** el cliente consulta `GET /wbs` para ubicar el ítem y obtener `rowVersion` y `apu.id`, y consulta
  `GET /apus/{apuId}` para mostrar descripción, unidad, composición y costos

#### Scenario: SC-APUS-126 Secciones de la página

- **Dado** un APU cargado
- **Cuando** la página se muestra
- **Entonces** presenta los datos del ítem (código editable, ubicación de solo lectura, cantidad de obra), los datos del APU,
  la composición editable y el resumen de costos del servidor

#### Scenario: SC-APUS-142 Código del ítem editable en la página del APU

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE` que cambia el código del ítem en la página del APU
- **Cuando** sale del campo y guarda
- **Entonces** el cliente valida el código con `GET /wbs/codes/validate` enviando `excludeNodeId` con el nodo actual
  y, si es válido y disponible, lo envía en `PUT /wbs/items/{itemId}`; si está duplicado o es inválido, muestra el
  mensaje en español y conserva los datos escritos sin enviar la solicitud

#### Scenario: SC-APUS-127 Guardado con versiones vigentes

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE` que cambia la cantidad de obra
- **Cuando** guarda
- **Entonces** el cliente envía `PUT /wbs/items/{itemId}` con `quantityWork` y `rowVersion` del ítem y el bloque
  `apu` con `apuRowVersion`

#### Scenario: SC-APUS-128 Composición sin cambios

- **Dado** un usuario que solo modifica la descripción o la cantidad de obra
- **Cuando** guarda
- **Entonces** el cliente omite `apu.resources` para no reemplazar la composición y el servidor conserva las líneas

#### Scenario: SC-APUS-129 Composición modificada

- **Dado** un usuario que agrega, edita o quita líneas de composición
- **Cuando** guarda
- **Entonces** el cliente envía el arreglo `apu.resources` completo con `lineId` en las líneas existentes y sin
  `lineId` en las nuevas

#### Scenario: SC-APUS-130 Descarte de cambios

- **Dado** una página con cambios sin guardar
- **Cuando** el usuario elige “Descartar cambios” o sale de la página
- **Entonces** el cliente pide confirmación y, si acepta, vuelve al estado del servidor o al EDT sin enviar la
  solicitud

#### Scenario: SC-APUS-131 Página de edición sin APU

- **Dado** un `itemId` cuyo ítem no tiene APU
- **Cuando** la página se abre
- **Entonces** el cliente muestra “Este ítem no tiene APU” con un enlace a la página de alta y no habilita el
  formulario de composición

### Requirement: REQ-APUS-27 Conflictos y errores en la edición del APU

El cliente DEBE (MUST) manejar en español los conflictos de concurrencia y las referencias inválidas de la edición,
sin perder los datos escritos y refrescando cuando corresponde. Citas: RF-07; D-50, D-100; `REQ-APUS-15`.

#### Scenario: SC-APUS-132 Conflicto de versión del APU

- **Dado** un guardado que responde `409 APU_CONCURRENCY_CONFLICT`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Otro usuario modificó el APU mientras editabas. Actualizamos el APU; revisa los cambios y
  vuelve a intentarlo.”, refresca el detalle y conserva los valores escritos para reintentar

#### Scenario: SC-APUS-133 Conflicto de versión del ítem

- **Dado** un guardado que responde `409 WBS_CONCURRENCY_CONFLICT`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Otro usuario modificó el ítem del EDT mientras editabas. Actualizamos los datos; vuelve a
  intentarlo.” y refresca el ítem y sus versiones

#### Scenario: SC-APUS-134 Insumo inexistente o de otro proyecto

- **Dado** un guardado que responde `APU_RESOURCE_NOT_FOUND` o `APU_RESOURCE_PROJECT_MISMATCH`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Uno de los insumos de la composición ya no existe en el proyecto.” o “La composición solo
  admite insumos de este proyecto.” y refresca la lista de insumos sin guardar cambios parciales

#### Scenario: SC-APUS-135 Prohibición durante el guardado

- **Dado** un guardado que responde `403`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “No tienes permisos para realizar esta acción.” y no modifica la composición mostrada

### Requirement: REQ-APUS-28 Acceso y permisos de las páginas de APU

El cliente DEBE (MUST) proteger la página de alta con `WBS_WRITE` y `APUS_WRITE` y la página de edición con
`APUS_READ`, mostrando la edición en solo lectura sin `APUS_WRITE`. Citas: RF-07; D-109; `REQ-APUS-16`,
`REQ-APUS-17`.

#### Scenario: SC-APUS-136 Acceso a la página de alta

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE`
- **Cuando** abre `/proyectos/:id/edt/items/nuevo`
- **Entonces** la página se muestra con el bloque EDT y el bloque APU habilitados

#### Scenario: SC-APUS-137 Edición en solo lectura

- **Dado** un usuario con `APUS_READ` y sin `APUS_WRITE` ni `WBS_WRITE`
- **Cuando** abre la página de edición
- **Entonces** el cliente muestra los datos y la composición sin guardar y oculta “Guardar” y las acciones de
  composición

#### Scenario: SC-APUS-138 Proyecto no disponible en las páginas de APU

- **Dado** un `projectId` inexistente o de otra compañía
- **Cuando** la página valida el proyecto y recibe `404 PROJECT_NOT_FOUND` o `403`
- **Entonces** el cliente muestra “Proyecto no disponible” con un enlace a `/proyectos`

### Requirement: REQ-APUS-29 Costos del APU de solo lectura

El cliente NO DEBE (MUST NOT) calcular el costo directo, el AIU ni el total del APU y DEBE (MUST) presentar como “—”
los valores nulos devueltos por el servidor. Citas: RF-07, RF-10; D-06, D-07, D-121.

#### Scenario: SC-APUS-139 Resumen de costos del servidor

- **Dado** un APU con `directCost`, `indirectCosts` y `total` devueltos por el servidor
- **Cuando** la página de edición muestra el resumen
- **Entonces** el cliente presenta cada importe con el formato del proyecto y no recalcula porcentajes ni sumas

#### Scenario: SC-APUS-140 Costos nulos

- **Dado** un APU en `FALTA_INFORMACION` con costos nulos
- **Cuando** el resumen se muestra
- **Entonces** el cliente presenta “—” con la explicación de que el APU está incompleto

#### Scenario: SC-APUS-141 Estado del APU

- **Dado** un APU con `status` devuelto por el servidor
- **Cuando** la página lo muestra
- **Entonces** el cliente presenta la etiqueta del estado sin deducirla de la composición ni de los precios
