# Spec Delta: edt

## ADDED Requirements

### Requirement: REQ-EDT-24 Acceso a la pantalla del EDT desde la ficha del proyecto

El cliente Angular DEBE (MUST) exponer la pantalla del EDT únicamente como ruta hija del proyecto
(`/proyectos/:id/edt`), alcanzable desde la ficha del proyecto y sin entradas globales en el menú lateral ni en el
dashboard. Citas: RF-04; D-30, D-109, D-118.

#### Scenario: SC-EDT-101 Acceso desde la ficha del proyecto

- **Dado** un Director con `WBS_READ` que abre `/proyectos/:id`
- **Cuando** usa el acceso “EDT”
- **Entonces** el cliente navega a `/proyectos/:id/edt` con el `projectId` de la ruta como único contexto

#### Scenario: SC-EDT-102 Sin entradas globales de navegación

- **Dado** un usuario con `WBS_READ` y `APUS_WRITE` autenticado
- **Cuando** revisa el menú lateral y el dashboard
- **Entonces** no aparece una entrada para el EDT, para los insumos del proyecto ni para crear APUs, y el acceso
  `/apu` del catálogo público permanece sin cambios

#### Scenario: SC-EDT-103 Sin permiso de lectura del EDT

- **Dado** un usuario autenticado cuyo token no incluye `WBS_READ`
- **Cuando** abre la ficha del proyecto
- **Entonces** el acceso “EDT” no se muestra y la ruta `/proyectos/:id/edt` no se abre

#### Scenario: SC-EDT-104 Proyecto no disponible

- **Dado** un actor autorizado que abre `/proyectos/:id/edt` de un proyecto inexistente o de otra compañía
- **Cuando** el cliente consulta el proyecto y recibe `404 PROJECT_NOT_FOUND` o `403`
- **Entonces** muestra “Proyecto no disponible” con un enlace a `/proyectos` y no solicita el árbol

### Requirement: REQ-EDT-25 Árbol único del EDT en una sola pantalla

El cliente DEBE (MUST) presentar capítulos e ítems en un único árbol con columnas de código, tipo, descripción,
cantidad de obra, unidad, costo unitario, costo directo, estado del APU, última modificación y acciones. Citas:
RF-04, RF-10; D-30, D-118.

#### Scenario: SC-EDT-105 Árbol con capítulos e ítems

- **Dado** un proyecto con capítulos, subcapítulos e ítems
- **Cuando** se abre la pantalla del EDT
- **Entonces** el árbol muestra la jerarquía devuelta por `GET /wbs` con las columnas definidas y la descripción de
  cada ítem tomada de `apu.description`

#### Scenario: SC-EDT-106 Ítem raíz junto a los capítulos

- **Dado** un proyecto con capítulos raíz y un ítem sin capítulo padre
- **Cuando** se consulta el árbol
- **Entonces** el ítem raíz aparece en la colección raíz en la posición que indica su `orderIndex`, sin quedar
  dentro de ningún capítulo

#### Scenario: SC-EDT-107 Estados de carga, vacío y error

- **Dado** un proyecto sin capítulos ni ítems, o una consulta del árbol que falla
- **Cuando** se abre la pantalla
- **Entonces** el cliente muestra un esqueleto durante la carga, “El proyecto aún no tiene capítulos ni ítems” con
  las acciones de alta cuando la respuesta está vacía, y un mensaje de error con opción de reintentar cuando la
  consulta falla

### Requirement: REQ-EDT-26 Alta de capítulo en modal con validación de código

El cliente DEBE (MUST) crear capítulos desde un modal con código validado contra `GET /wbs/codes/validate`, nombre
y ubicación (raíz o un capítulo existente). Citas: RF-04; BR-015; D-117.

#### Scenario: SC-EDT-108 Validación de disponibilidad al salir del campo

- **Dado** un usuario con `WBS_WRITE` que escribe un código de 1 a 10 caracteres
- **Cuando** abandona el campo de código
- **Entonces** el cliente consulta `GET /wbs/codes/validate` y habilita el guardado solo si `isValid` e
  `isAvailable` son verdaderos

#### Scenario: SC-EDT-109 Código inválido o duplicado en el alta

- **Dado** un usuario que escribe un código con símbolos no permitidos o un código ya usado en el proyecto
- **Cuando** intenta guardar
- **Entonces** el cliente mantiene el formulario abierto y muestra “Ese código ya está en uso en el proyecto (sin
  distinguir mayúsculas). Elige otro.” o “El código debe tener entre 1 y 10 caracteres y solo los símbolos
  permitidos.” según corresponda

#### Scenario: SC-EDT-110 Capítulo creado en la raíz o dentro de un capítulo

- **Dado** un usuario con `WBS_WRITE` y un modal de alta con ubicación “Raíz” o un capítulo seleccionado
- **Cuando** confirma el alta y `POST /wbs/chapters` responde `201`
- **Entonces** el modal se cierra, el árbol se refresca y el capítulo creado queda expandido y seleccionado

#### Scenario: SC-EDT-111 Capítulo sin permiso de escritura

- **Dado** un usuario con `WBS_READ` y sin `WBS_WRITE`
- **Cuando** abre la pantalla del EDT
- **Entonces** el botón “Nuevo capítulo” no se muestra

### Requirement: REQ-EDT-27 Movimiento por arrastrar y soltar con destinos válidos

El cliente DEBE (MUST) permitir mover capítulos e ítems arrastrándolos sobre un capítulo, entre dos filas o en la
zona de raíz, y DEBE (MUST) rechazar el gesto cuando el destino no es un capítulo o está dentro del subárbol del
nodo arrastrado. Citas: RF-04; D-118.

#### Scenario: SC-EDT-112 Soltar sobre un capítulo

- **Dado** un usuario con `WBS_WRITE` que arrastra un nodo sobre el encabezado de un capítulo
- **Cuando** suelta el nodo
- **Entonces** el cliente llama `POST /wbs/move` con `targetParentChapterId` del capítulo y `targetIndex` nulo, para
  que el nodo quede como último hijo

#### Scenario: SC-EDT-113 Soltar entre dos filas

- **Dado** un usuario que arrastra un nodo y lo suelta entre dos filas hermanas
- **Cuando** suelta el nodo
- **Entonces** el cliente envía `targetParentChapterId` del padre de esas filas y el `targetIndex` que corresponde a
  la posición soltada

#### Scenario: SC-EDT-114 Soltar en la zona de raíz

- **Dado** un usuario que arrastra un nodo a la zona de raíz del árbol
- **Cuando** suelta el nodo
- **Entonces** el cliente envía `targetParentChapterId` nulo y el `targetIndex` final de la raíz

#### Scenario: SC-EDT-115 Destinos inválidos

- **Dado** un usuario que arrastra un nodo sobre un ítem, sobre sí mismo o sobre un capítulo de su propio subárbol
- **Cuando** mantiene el puntero sobre ese destino
- **Entonces** el cliente muestra el cursor prohibido y el texto “No puedes mover un capítulo dentro de su propio
  subárbol”, no marca el destino como válido y no llama al servidor al soltar

#### Scenario: SC-EDT-116 Reordenamiento dentro del mismo padre

- **Dado** un usuario que reordena hermanos sin cambiar de padre
- **Cuando** suelta el nodo
- **Entonces** el cliente puede usar `POST /wbs/reorder` con la lista completa de hermanos vigentes del padre en el
  nuevo orden y ningún código cambia

### Requirement: REQ-EDT-28 Acciones explícitas equivalentes al arrastrar y soltar

El cliente DEBE (MUST) ofrecer en cada fila un menú de acciones con “Mover a…”, “Subir”, “Bajar”, “Editar”,
“Eliminar”, “Abrir APU”, “Nuevo ítem aquí” y “Nuevo capítulo aquí”, de modo que el arrastrar y soltar no sea la
única vía. Citas: RF-04; D-118; `D-109`.

#### Scenario: SC-EDT-117 “Mover a…” con destino y posición

- **Dado** un usuario con `WBS_WRITE` que elige “Mover a…” en un nodo
- **Cuando** selecciona “Raíz” o un capítulo y una posición
- **Entonces** el cliente envía `POST /wbs/move` con el destino y el `targetIndex` elegidos y refresca el árbol al
  responder

#### Scenario: SC-EDT-118 Destinos excluidos en “Mover a…”

- **Dado** un capítulo con subcapítulos
- **Cuando** el usuario abre “Mover a…” de ese capítulo
- **Entonces** la lista de destinos no incluye el propio capítulo ni ninguno de sus descendientes

#### Scenario: SC-EDT-119 “Subir” y “Bajar” en los extremos

- **Dado** un nodo que es el primero o el último de su padre
- **Cuando** el usuario revisa su menú de acciones
- **Entonces** “Subir” o “Bajar” no se ofrece para la posición que no existe y la otra acción mueve el nodo una
  posición dentro del mismo padre

#### Scenario: SC-EDT-120 Acciones del menú de fila disponibles

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE`
- **Cuando** abre el menú de un ítem y el de un capítulo
- **Entonces** el ítem ofrece “Editar”, “Eliminar” y “Abrir APU”, el capítulo ofrece “Nuevo ítem aquí” y “Nuevo
  capítulo aquí”, y ambos ofrecen “Mover a…”, “Subir” y “Bajar” cuando aplican

### Requirement: REQ-EDT-29 Alertas en español ante conflictos del EDT

El cliente DEBE (MUST) mostrar todo rechazo de movimiento, reordenamiento, alta o baja en un modal de alerta con
título, mensaje en español que explica la causa y botón “Entendido”, sin presentar el código técnico crudo. Citas:
RF-04; D-100, D-118, D-119.

#### Scenario: SC-EDT-121 Movimiento dentro del propio subárbol

- **Dado** un usuario que intenta mover un capítulo dentro de su propio subárbol y el servidor responde
  `WBS_MOVE_CYCLE`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “No puedes mover un capítulo dentro de sí mismo ni dentro de uno de sus subcapítulos.”,
  mantiene el árbol y resalta el nodo afectado

#### Scenario: SC-EDT-122 Destino inexistente

- **Dado** un movimiento cuyo destino ya no existe y el servidor responde `WBS_TARGET_NOT_FOUND`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “El destino ya no existe en el proyecto. Actualizamos el árbol; elige un capítulo vigente.” y
  refresca el árbol

#### Scenario: SC-EDT-123 Destino que no es un capítulo

- **Dado** un movimiento cuyo destino no es un capítulo y el servidor responde `VALIDATION_ERROR`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Solo puedes mover dentro de un capítulo o a la raíz del presupuesto.” sin refrescar

#### Scenario: SC-EDT-124 Conflicto de concurrencia

- **Dado** un movimiento con una versión desactualizada y el servidor responde `WBS_CONCURRENCY_CONFLICT`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “Otro usuario modificó el EDT mientras movías el nodo. Actualizamos el árbol; revisa los
  cambios y vuelve a intentarlo.” y refresca el árbol

#### Scenario: SC-EDT-125 Baja rechazada por contenido

- **Dado** la baja de un capítulo con ítems o con APUs en su subárbol
- **Cuando** el servidor responde `WBS_CHAPTER_HAS_APU` o `WBS_NODE_HAS_CHILDREN`
- **Entonces** el cliente muestra “El capítulo contiene ítems con APU. Elimina o mueve primero esos ítems.” o “El
  capítulo tiene subcapítulos o ítems; solo se elimina un capítulo vacío.” y el árbol no cambia

#### Scenario: SC-EDT-126 Alertas de código y cantidad

- **Dado** un alta o una edición rechazada con `WBS_CODE_DUPLICATED`, `WBS_CODE_INVALID` o `WBS_QUANTITY_INVALID`
- **Cuando** el cliente recibe el error
- **Entonces** muestra el mensaje en español correspondiente, conserva el formulario y sus datos y no cierra el
  modal

#### Scenario: SC-EDT-127 Detalle técnico del error

- **Dado** cualquier alerta de conflicto del EDT
- **Cuando** el usuario la revisa
- **Entonces** el modal ofrece el botón “Entendido”, agrega “Actualizar árbol” cuando la causa exige refresco y deja
  el código y el `traceId` solo como detalle técnico secundario

### Requirement: REQ-EDT-30 Reversión del cambio optimista y refresco del árbol

El cliente DEBE (MUST) revertir el movimiento mostrado de forma optimista cuando el servidor lo rechaza y DEBE
(MUST) volver a consultar `GET /wbs` después de aceptar un movimiento o un reordenamiento. Citas: RF-04, RF-10;
D-50, D-118.

#### Scenario: SC-EDT-128 Reversión ante rechazo

- **Dado** un movimiento mostrado en pantalla antes de la respuesta y un servidor que responde con error
- **Cuando** el cliente recibe el rechazo
- **Entonces** restaura la posición anterior del nodo, muestra la alerta en español y deja el árbol en el estado
  confirmado por el servidor

#### Scenario: SC-EDT-129 Refresco tras un movimiento aceptado

- **Dado** un `POST /wbs/move` o `POST /wbs/reorder` que responde `200` sin costos
- **Cuando** el cliente recibe la respuesta
- **Entonces** vuelve a consultar `GET /wbs` antes de mostrar los costos de los capítulos de origen y destino

### Requirement: REQ-EDT-31 Edición de ítem en modal desde el EDT

El cliente DEBE (MUST) editar un ítem desde un modal con cantidad de obra obligatoria, código opcional y bloque APU
opcional, enviando siempre `rowVersion` y `apuRowVersion`. Citas: RF-04, RF-07; D-50, D-118; `REQ-EDT-22`.

#### Scenario: SC-EDT-130 Edición de la cantidad de obra

- **Dado** un usuario con `WBS_WRITE` y `APUS_WRITE` que abre “Editar” en un ítem
- **Cuando** cambia la cantidad de obra y guarda
- **Entonces** el cliente envía `PUT /wbs/items/{itemId}` con `quantityWork`, `rowVersion` y el bloque `apu` con
  `apuRowVersion`, y refresca el árbol al responder

#### Scenario: SC-EDT-131 Código opcional en la edición

- **Dado** un ítem cuyo código no se modifica
- **Cuando** el usuario guarda solo la cantidad de obra
- **Entonces** el cliente omite `code` en la solicitud y el código del ítem no cambia

#### Scenario: SC-EDT-132 Apertura de la página del APU

- **Dado** un ítem con APU y un usuario con `APUS_READ`
- **Cuando** el usuario elige “Abrir APU”
- **Entonces** el cliente navega a `/proyectos/:id/edt/items/:itemId/apu`

#### Scenario: SC-EDT-133 Edición sin permiso de APU

- **Dado** un usuario con `WBS_WRITE` y sin `APUS_WRITE`
- **Cuando** abre el modal de edición de un ítem
- **Entonces** el bloque APU no se muestra y el cliente no envía el objeto `apu`

### Requirement: REQ-EDT-32 Baja controlada de nodos desde la interfaz

El cliente DEBE (MUST) pedir confirmación explícita antes de eliminar un ítem o un capítulo, advirtiendo que la baja
del ítem elimina también su APU y que un capítulo solo se elimina vacío. Citas: RF-04; D-114, D-119.

#### Scenario: SC-EDT-134 Baja de un ítem

- **Dado** un usuario con `WBS_WRITE` que elige “Eliminar” en un ítem
- **Cuando** confirma la advertencia que indica que se elimina su APU y que los insumos se conservan
- **Entonces** el cliente envía `DELETE /wbs/items/{itemId}?rowVersion=` y refresca el árbol al recibir `204`

#### Scenario: SC-EDT-135 Baja de un capítulo vacío

- **Dado** un capítulo sin subcapítulos ni ítems
- **Cuando** el usuario confirma la baja
- **Entonces** el cliente envía `DELETE /wbs/chapters/{chapterId}?rowVersion=` y refresca el árbol al recibir `204`

#### Scenario: SC-EDT-136 Baja rechazada sin cambios en el árbol

- **Dado** la baja de un capítulo que el servidor rechaza con `409 WBS_NODE_HAS_CHILDREN` o `409
  WBS_CHAPTER_HAS_APU`
- **Cuando** el cliente recibe el rechazo
- **Entonces** muestra la alerta en español definida, conserva el capítulo y no elimina ningún nodo de la pantalla

#### Scenario: SC-EDT-137 Cancelación de la confirmación

- **Dado** un usuario que abre la confirmación de baja y elige cancelar
- **Cuando** la confirmación se cierra
- **Entonces** el cliente no envía ninguna solicitud y el árbol no cambia

### Requirement: REQ-EDT-33 Búsqueda y actividad dentro de la pantalla del EDT

El cliente DEBE (MUST) ofrecer búsqueda parcial con `GET /wbs/search` y una tarjeta de actividad con
`GET /wbs/activity`, y DEBE (MUST) mantener la pantalla usable cuando la actividad responde `403`. Citas: RF-04;
D-109.

#### Scenario: SC-EDT-138 Resultado de búsqueda sin costos

- **Dado** un usuario que escribe un término en la búsqueda del EDT
- **Cuando** `GET /wbs/search` devuelve resultados
- **Entonces** el cliente muestra código, tipo, nombre y cantidad de obra de cada resultado sin costos y con
  paginación de 10, 50 o 100 registros

#### Scenario: SC-EDT-139 Navegación al nodo encontrado

- **Dado** un resultado de búsqueda visible
- **Cuando** el usuario lo elige
- **Entonces** el cliente expande los ancestros necesarios y resalta el nodo en el árbol

#### Scenario: SC-EDT-140 Actividad no disponible para el auditor

- **Dado** un Auditor cuyo `GET /wbs/activity` responde `403`
- **Cuando** se abre la pantalla del EDT
- **Entonces** la tarjeta de actividad se oculta con un aviso y el árbol, la búsqueda y las acciones siguen
  disponibles

#### Scenario: SC-EDT-141 Actividad con participantes

- **Dado** un proyecto con actividad registrada
- **Cuando** el cliente consulta `GET /wbs/activity`
- **Entonces** la tarjeta muestra la última modificación y la lista de participantes devuelta por el servidor

### Requirement: REQ-EDT-34 Costos y estados de solo lectura en el árbol

El cliente NO DEBE (MUST NOT) calcular costos, subtotales ni estados y DEBE (MUST) mostrar como “—” los costos
nulos que devuelve el servidor. Citas: RF-04, RF-10; D-06, D-121.

#### Scenario: SC-EDT-142 Costos nulos

- **Dado** un ítem con `unitCost` y `directCost` nulos
- **Cuando** el árbol se muestra
- **Entonces** el cliente presenta “—” en ambas columnas con la ayuda “APU incompleto o sin precio” y no muestra `0`

#### Scenario: SC-EDT-143 Costos del servidor sin recálculo

- **Dado** un ítem con `quantityWork`, `unitCost` y `directCost` devueltos por el servidor
- **Cuando** el árbol se muestra
- **Entonces** el cliente presenta exactamente esos valores con el formato numérico del proyecto y no multiplica
  cantidad por costo unitario

#### Scenario: SC-EDT-144 Estado del APU

- **Dado** un ítem cuyo APU devuelve `COMPLETO` o `FALTA_INFORMACION`
- **Cuando** el árbol se muestra
- **Entonces** el cliente presenta el estado como etiqueta de solo lectura y no lo deduce de la composición

### Requirement: REQ-EDT-35 Permisos de escritura en la pantalla del EDT

El cliente DEBE (MUST) ocultar las acciones de escritura cuando el token no incluye `WBS_WRITE` o `APUS_WRITE` y
DEBE (MUST) manejar en español un `403` inesperado. Citas: RF-04; D-109.

#### Scenario: SC-EDT-145 Acciones ocultas sin WBS_WRITE

- **Dado** un usuario con `WBS_READ` y sin `WBS_WRITE`
- **Cuando** abre la pantalla del EDT
- **Entonces** no se muestran “Nuevo capítulo”, “Nuevo APU”, “Editar”, “Eliminar”, “Mover a…”, “Subir” ni “Bajar”

#### Scenario: SC-EDT-146 Alta de APU sin APUS_WRITE

- **Dado** un usuario con `WBS_READ` y `WBS_WRITE` pero sin `APUS_WRITE`
- **Cuando** abre la pantalla del EDT
- **Entonces** el acceso “Nuevo APU” no se muestra y el modal de edición no envía el bloque `apu`

#### Scenario: SC-EDT-147 Prohibición inesperada

- **Dado** una acción del EDT que el servidor rechaza con `403`
- **Cuando** el cliente recibe el error
- **Entonces** muestra “No tienes permisos para realizar esta acción.” y el árbol conserva los datos vigentes
