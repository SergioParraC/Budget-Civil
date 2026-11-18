# edt Specification

## Purpose

Define la estructura de trabajo (EDT) de un proyecto: capítulos e ítems recursivos, códigos manuales únicos
e inmutables, validación de disponibilidad, reorganización libre, baja en cascada, cantidad de obra, orden,
búsqueda, autoría, concurrencia, actividad y participantes, y autorización de lectura o escritura. La
creación de APUs y el cálculo pertenecen a etapas posteriores.

## Requirements

### Requirement: REQ-EDT-01 Estructura propia de un proyecto

El sistema DEBE (MUST) registrar la estructura de trabajo de un proyecto existente, con capítulos e ítems
que solo pertenecen a ese proyecto y que no se mueven a otro proyecto. Citas: RF-04; D-30, D-51.

#### Scenario: SC-EDT-01 Alta de un capítulo raíz

- **Dado** un Director de proyectos autenticado y un proyecto de su compañía
- **Cuando** crea un capítulo con nombre válido
- **Entonces** el capítulo queda asociado a ese proyecto y aparece como raíz del árbol

#### Scenario: SC-EDT-02 Alta en un proyecto inexistente

- **Dado** un actor autorizado que envía el identificador de un proyecto que no existe
- **Cuando** intenta crear un capítulo o un ítem
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND` y no crea ningún nodo

#### Scenario: SC-EDT-03 Intento de mover un nodo a otro proyecto

- **Dado** un capítulo del proyecto A con sus ítems
- **Cuando** un actor autorizado solicita moverlo al proyecto B
- **Entonces** el sistema rechaza la operación con un error de validación y el nodo permanece en el proyecto A

### Requirement: REQ-EDT-02 Recursividad y raíz

El sistema DEBE (MUST) admitir capítulos anidados sin límite de profundidad ni de cantidad de hijos y DEBE
(MUST) admitir ítems dentro de cualquier capítulo y en la raíz del presupuesto, sin capítulo padre. Citas:
RF-04; R-18; D-30, D-118.

#### Scenario: SC-EDT-04 Anidamiento profundo

- **Dado** un capítulo raíz del proyecto
- **Cuando** se agregan subcapítulos hasta diez niveles de profundidad
- **Entonces** el sistema crea todos los niveles y los devuelve ordenados en el árbol

#### Scenario: SC-EDT-05 Varios hijos en un mismo capítulo

- **Dado** un capítulo existente
- **Cuando** se agregan diez subcapítulos y diez ítems bajo ese capítulo
- **Entonces** el sistema acepta los veinte nodos y los ordena en una sola secuencia de posiciones

#### Scenario: SC-EDT-06 Ítem en la raíz del presupuesto

- **Dado** un proyecto con capítulos y sin ítems raíz
- **Cuando** un actor autorizado crea un ítem sin indicar capítulo padre
- **Entonces** el sistema lo guarda como ítem raíz y lo devuelve junto a los capítulos raíz

#### Scenario: SC-EDT-07 Ítem en un capítulo de otro proyecto

- **Dado** un capítulo del proyecto A y un actor autorizado en el proyecto B
- **Cuando** intenta crear un ítem indicando ese capítulo como padre
- **Entonces** el sistema rechaza la operación con un error de validación

### Requirement: REQ-EDT-03 Código manual y obligatorio

El sistema DEBE (MUST) exigir que el usuario ingrese el código completo al crear un capítulo o un ítem y
DEBE (MUST) validar que sea único en el proyecto, compartido entre capítulos e ítems. No hay numeración
automática ni códigos provisionales. Citas: RF-04; BR-015; D-30, D-51, D-117.

#### Scenario: SC-EDT-08 Código obligatorio

- **Dado** un proyecto existente
- **Cuando** el usuario intenta crear un capítulo o un ítem sin indicar código
- **Entonces** el sistema responde `400 WBS_CODE_INVALID` y no crea el nodo

#### Scenario: SC-EDT-09 Código manual en la raíz

- **Dado** un proyecto sin capítulos
- **Cuando** el usuario crea un capítulo raíz con el código `EST`
- **Entonces** el sistema guarda el capítulo con el código `EST`

#### Scenario: SC-EDT-10 Código manual en un hijo

- **Dado** un capítulo raíz con código `CIM`
- **Cuando** el usuario crea un subcapítulo con el código `EST`
- **Entonces** el sistema guarda el subcapítulo con el código `EST`, sin componerlo con `CIM`

#### Scenario: SC-EDT-11 El código no se reutiliza automáticamente

- **Dado** un capítulo con los hijos `1.1` y `1.3`
- **Cuando** el usuario crea un subcapítulo e indica el código `1.1`
- **Entonces** el sistema responde `409 WBS_CODE_DUPLICATED`

#### Scenario: SC-EDT-12 Unicidad compartida entre capítulos e ítems

- **Dado** un capítulo con código `EST`
- **Cuando** el usuario crea un ítem con el código `EST`
- **Entonces** el sistema responde `409 WBS_CODE_DUPLICATED` y no crea el ítem

### Requirement: REQ-EDT-04 Código independiente de la jerarquía

El sistema DEBE (MUST) tratar el código como un identificador libre del proyecto y DEBE (MUST) determinar la
ubicación por `parentChapterId`/`wbsChapterId`, no por el texto del código; el cambio de código de un nodo no
reescribe el de sus descendientes. Citas: RF-04; D-30; R-19.

#### Scenario: SC-EDT-13 Código distinto al del padre

- **Dado** un capítulo raíz con código `CIM`
- **Cuando** se crea un subcapítulo con código `EST`
- **Entonces** el subcapítulo queda dentro de `CIM` con código `EST`

#### Scenario: SC-EDT-14 Código en profundidad

- **Dado** el subcapítulo `EST` del escenario anterior
- **Cuando** se crea un ítem dentro de él con código `M-1`
- **Entonces** el ítem queda dentro de `EST` con código `M-1`

#### Scenario: SC-EDT-15 El renombrado no reescribe a los descendientes

- **Dado** un capítulo `CIM` con un subcapítulo `EST`
- **Cuando** el usuario cambia el código del capítulo a `ESTRUCTURA`
- **Entonces** el subcapítulo conserva `EST`

### Requirement: REQ-EDT-05 Inmutabilidad del código

El sistema DEBE (MUST) conservar el código de un nodo después de su creación ante movimientos,
reordenamientos, bajas de otros nodos y renombrados de otros capítulos; solo la edición explícita de ese
mismo nodo puede cambiarlo, sin afectar a ningún otro nodo. Citas: RF-04; D-30, D-117.

#### Scenario: SC-EDT-16 La baja de un hermano no renumera

- **Dado** un capítulo con los hijos `1`, `2` y `3`
- **Cuando** se elimina el hijo `1`
- **Entonces** los restantes conservan `2` y `3` y sus posiciones se compactan

#### Scenario: SC-EDT-17 El reordenamiento no renumera

- **Dado** un capítulo con tres hijos de códigos `1`, `2` y `3`
- **Cuando** se reordena la secuencia como `3`, `1`, `2`
- **Entonces** los códigos siguen siendo `1`, `2` y `3` y solo cambian sus posiciones

#### Scenario: SC-EDT-18 El movimiento no renumera

- **Dado** un capítulo `1.2` con el hijo `1.2.1`
- **Cuando** se mueve el capítulo a otro padre del mismo proyecto
- **Entonces** el capítulo conserva `1.2` y su hijo conserva `1.2.1`

#### Scenario: SC-EDT-19 La edición de un código no toca a los hijos

- **Dado** un capítulo `1` con el hijo `1.1`
- **Cuando** el usuario cambia el código del capítulo a `A`
- **Entonces** el hijo conserva `1.1` y el sistema no lo renumera

### Requirement: REQ-EDT-06 Formato y unicidad del código

El código DEBE (MUST) usar solo `A-Z`, `a-z`, `0-9`, `.`, `-`, `_`, `/`, `*`, `=`, `(`, `)`, `%`, `&`, `#`,
`¡`, `'`, `?` y `¿`; medir 1–10 caracteres tanto en capítulos como en ítems; y ser único por proyecto sin
distinguir mayúsculas. Se aceptan mayúsculas y minúsculas y se conserva el valor escrito. Citas: RF-04;
BR-015; R-19; D-30, D-115, D-117.

#### Scenario: SC-EDT-20 Carácter fuera del conjunto

- **Dado** un actor autorizado que envía un código con el carácter `@`
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 WBS_CODE_INVALID` y no crea el nodo

#### Scenario: SC-EDT-21 Código de capítulo demasiado largo

- **Dado** un actor autorizado que envía un código de once caracteres para un capítulo
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 WBS_CODE_INVALID`

#### Scenario: SC-EDT-22 Código de ítem demasiado largo

- **Dado** un actor autorizado que envía un código de once caracteres para un ítem
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 WBS_CODE_INVALID`

#### Scenario: SC-EDT-23 Código vacío

- **Dado** un actor autorizado que no envía código
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 WBS_CODE_INVALID` y no crea el nodo

#### Scenario: SC-EDT-24 Unicidad sin distinguir mayúsculas

- **Dado** un capítulo con código `cim`
- **Cuando** el usuario crea otro nodo con código `CIM`
- **Entonces** el sistema responde `409 WBS_CODE_DUPLICATED`

### Requirement: REQ-EDT-07 Ítem de obra con cantidad

El sistema DEBE (MUST) exigir una cantidad de obra mayor que cero en cada ítem y DEBE (MUST) rechazar el
alta o la edición con cantidad nula, cero o negativa. Citas: RF-04, RF-22; R-21.

#### Scenario: SC-EDT-25 Alta con cantidad válida

- **Dado** un capítulo existente
- **Cuando** se crea un ítem con cantidad de obra `12.5`
- **Entonces** el sistema guarda el ítem y devuelve la cantidad indicada

#### Scenario: SC-EDT-26 Alta con cantidad cero

- **Dado** un actor autorizado que envía cantidad de obra `0`
- **Cuando** confirma el alta del ítem
- **Entonces** el sistema responde `400 WBS_QUANTITY_INVALID` y no crea el ítem

#### Scenario: SC-EDT-27 Edición con cantidad negativa

- **Dado** un ítem existente con cantidad `5`
- **Cuando** se envía una edición con cantidad `-1`
- **Entonces** el sistema responde `400 WBS_QUANTITY_INVALID` y conserva `5`

### Requirement: REQ-EDT-08 Ítem sin APU en este incremento

El sistema DEBE (MUST) crear y mantener ítems sin APU asociado y DEBE (MUST) garantizar que un APU se
vincule a un único ítem cuando la relación exista. Citas: RF-07; BR-016; D-113.

#### Scenario: SC-EDT-28 El ítem se crea sin APU

- **Dado** un actor autorizado en un proyecto
- **Cuando** crea un ítem de obra
- **Entonces** el sistema lo guarda con el APU vacío y lo devuelve en el árbol

#### Scenario: SC-EDT-29 El cliente intenta fijar el APU

- **Dado** un actor autorizado que envía un identificador de APU en el alta del ítem
- **Cuando** el sistema procesa la solicitud
- **Entonces** no aplica el valor y el ítem queda sin APU

### Requirement: REQ-EDT-09 Búsqueda parcial

El sistema DEBE (MUST) permitir buscar por coincidencia parcial del código de un ítem o del nombre de un
capítulo, con paginación `page` y `pageSize`. Citas: RF-04; D-101.

#### Scenario: SC-EDT-30 Coincidencia parcial

- **Dado** un proyecto con un ítem `CIM.1.1` y un capítulo `Cimentación`
- **Cuando** el usuario busca el texto `cim`
- **Entonces** el sistema devuelve ambos nodos

#### Scenario: SC-EDT-31 Sin coincidencias

- **Dado** un proyecto sin nodos que coincidan con el texto buscado
- **Cuando** el usuario ejecuta la búsqueda
- **Entonces** el sistema devuelve una página vacía con el total en cero

#### Scenario: SC-EDT-32 Tamaño de página no permitido

- **Dado** un actor autorizado que solicita `pageSize=25`
- **Cuando** ejecuta la búsqueda
- **Entonces** el sistema responde `400 PAGE_SIZE_INVALID`

### Requirement: REQ-EDT-10 Orden y compactación de posiciones

El sistema DEBE (MUST) presentar los capítulos e ítems de un mismo padre según su posición, incluida la
raíz, DEBE (MUST) permitir reordenarlos en un comando por padre y DEBE (MUST) compactar las posiciones al
eliminar o mover un nodo. Citas: RF-04; D-30, D-117.

#### Scenario: SC-EDT-33 Orden devuelto

- **Dado** un capítulo con tres hijos creados en orden
- **Cuando** se consulta el árbol
- **Entonces** los hijos aparecen en el orden registrado

#### Scenario: SC-EDT-34 Reordenamiento

- **Dado** un capítulo con tres hijos
- **Cuando** se envía la nueva secuencia de esos tres hijos
- **Entonces** el sistema guarda el nuevo orden y lo devuelve en la siguiente consulta

#### Scenario: SC-EDT-35 Compactación después de una baja

- **Dado** un capítulo con hijos en las posiciones `0`, `1` y `2`
- **Cuando** se elimina el hijo de la posición `1`
- **Entonces** los restantes quedan en las posiciones `0` y `1`, sin huecos

#### Scenario: SC-EDT-36 Orden compartido en la raíz

- **Dado** un proyecto con dos capítulos raíz y un ítem raíz entre ellos
- **Cuando** se consulta el árbol
- **Entonces** los tres nodos aparecen en la raíz en el orden de sus posiciones

### Requirement: REQ-EDT-11 Concurrencia optimista

El sistema DEBE (MUST) aplicar concurrencia optimista con `rowversion` en capítulos e ítems y DEBE (MUST)
rechazar con `409` la edición, el movimiento o la baja cuya versión no sea la vigente, sin modificar la
estructura. La primera transacción que confirma gana; la segunda debe refrescar y reintentar. Citas: RF-04;
D-50, D-97.

#### Scenario: SC-EDT-37 Edición con la versión vigente

- **Dado** un nodo cuya versión conoce el actor autorizado
- **Cuando** envía una edición con esa versión
- **Entonces** el sistema aplica el cambio y devuelve la nueva versión

#### Scenario: SC-EDT-38 Edición con una versión desactualizada

- **Dado** un nodo modificado por otro actor después de que el primero lo leyó
- **Cuando** el primer actor envía su edición con la versión anterior
- **Entonces** el sistema responde `409 WBS_CONCURRENCY_CONFLICT` y no modifica el nodo

#### Scenario: SC-EDT-39 Movimiento o baja con una versión desactualizada

- **Dado** un nodo modificado por otro actor después de que el primero lo leyó
- **Cuando** el primer actor solicita moverlo o eliminarlo con la versión anterior
- **Entonces** el sistema responde `409 WBS_CONCURRENCY_CONFLICT` y no modifica ni elimina ningún nodo

### Requirement: REQ-EDT-12 Autoría de la última modificación

El sistema DEBE (MUST) registrar en cada alta, edición, movimiento y renombrado el actor autenticado y la
fecha en UTC, y el cliente no puede fijarlos. Citas: RF-14; D-08, D-116.

#### Scenario: SC-EDT-40 El alta registra al autor

- **Dado** un actor autorizado que crea un capítulo
- **Cuando** el sistema guarda el nodo
- **Entonces** la autoría contiene al actor autenticado y la fecha en UTC

#### Scenario: SC-EDT-41 El cliente intenta fijar la autoría

- **Dado** un actor autorizado que envía una autoría distinta de la suya
- **Cuando** el sistema procesa el alta o la edición
- **Entonces** no aplica el valor enviado y conserva al actor autenticado como autor

### Requirement: REQ-EDT-13 Escritura de la estructura

El sistema DEBE (MUST) permitir crear, editar, mover y eliminar capítulos e ítems solo al Director y al
Presupuestador que tengan `ProjectMember` activo en el proyecto; el Director puede leer toda la compañía
pero no escribir sin pertenencia, y el Administrador y el Auditor no escriben. Citas: RF-04; D-34, D-82;
matriz `1.3.2`.

#### Scenario: SC-EDT-42 El Director escribe en su compañía

- **Dado** un Director de proyectos con `ProjectMember` activo en el proyecto
- **Cuando** crea, edita, mueve y elimina nodos
- **Entonces** el sistema aplica todas las operaciones

#### Scenario: SC-EDT-43 El Presupuestador asignado escribe

- **Dado** un Presupuestador con `ProjectMember` activo en el proyecto
- **Cuando** crea, edita, mueve y elimina nodos
- **Entonces** el sistema aplica todas las operaciones

#### Scenario: SC-EDT-44 El Presupuestador no asignado intenta escribir

- **Dado** un Presupuestador de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta crear, editar o mover un nodo
- **Entonces** el sistema responde `403 WBS_WRITE_FORBIDDEN`

#### Scenario: SC-EDT-45 El Administrador intenta escribir

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, editar o eliminar un nodo
- **Entonces** el sistema responde `403` porque solo lee la estructura

#### Scenario: SC-EDT-46 El Auditor intenta escribir

- **Dado** un Auditor asignado al proyecto
- **Cuando** intenta crear, editar, mover o eliminar un nodo
- **Entonces** el sistema responde `403`

#### Scenario: SC-EDT-77 El Director sin pertenencia intenta escribir

- **Dado** un Director de la compañía dueña del proyecto sin `ProjectMember` activo
- **Cuando** intenta crear, editar o eliminar un nodo
- **Entonces** el sistema responde `403 WBS_WRITE_FORBIDDEN`

### Requirement: REQ-EDT-14 Lectura de la estructura

El sistema DEBE (MUST) permitir al Administrador consultar cualquier proyecto, al Director los proyectos de
su compañía y al Presupuestador y al Auditor los proyectos asignados, tanto el árbol como la búsqueda. La
consulta de actividad y participantes (`GET /wbs/activity`) está disponible para Administrador, Director y
Presupuestador con su alcance; el Auditor no accede. Citas: RF-04; D-34, D-83, D-84, D-120; matriz `1.3.2`.

#### Scenario: SC-EDT-47 El Administrador consulta cualquier proyecto

- **Dado** un Administrador autenticado y un proyecto de cualquier compañía
- **Cuando** consulta el árbol y busca por código o nombre
- **Entonces** el sistema devuelve la estructura en solo lectura

#### Scenario: SC-EDT-48 El Director consulta su compañía

- **Dado** un Director de proyectos autenticado en un proyecto de su compañía
- **Cuando** consulta el árbol del proyecto
- **Entonces** el sistema devuelve la estructura completa

#### Scenario: SC-EDT-49 El Auditor asignado consulta el árbol

- **Dado** un Auditor asignado al proyecto
- **Cuando** consulta el árbol y busca por código o nombre
- **Entonces** el sistema devuelve la estructura en solo lectura

#### Scenario: SC-EDT-50 El Auditor no asignado intenta consultar

- **Dado** un Auditor de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta consultar el árbol
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN`

### Requirement: REQ-EDT-16 Aislamiento entre compañías

El sistema DEBE (MUST) impedir que un actor lea o modifique la estructura de un proyecto de otra compañía
con `403 WBS_READ_FORBIDDEN` o `403 WBS_WRITE_FORBIDDEN`. Citas: RF-04; R-10, R-14; D-02, D-36.

#### Scenario: SC-EDT-56 Lectura de otra compañía

- **Dado** un usuario de la compañía A y un proyecto de la compañía B
- **Cuando** solicita el árbol del proyecto
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN`

#### Scenario: SC-EDT-57 Escritura en otra compañía

- **Dado** un usuario de la compañía A
- **Cuando** intenta crear, editar, mover o eliminar un nodo en un proyecto de la compañía B
- **Entonces** el sistema responde `403 WBS_WRITE_FORBIDDEN`

### Requirement: REQ-EDT-17 Validación de disponibilidad del código

El sistema DEBE (MUST) exponer una consulta que valide el formato y la disponibilidad del código antes del
alta o la edición, sin crear ni modificar datos. Citas: RF-04; D-115, D-117.

#### Scenario: SC-EDT-58 Código válido y disponible

- **Dado** un proyecto sin el código `CIM.1`
- **Cuando** el usuario consulta `GET /wbs/codes/validate?code=CIM.1`
- **Entonces** el sistema responde `200` con `isValid=true`, `isAvailable=true` y `reason=null`

#### Scenario: SC-EDT-59 Código duplicado

- **Dado** un proyecto con el código `CIM.1`
- **Cuando** el usuario consulta `GET /wbs/codes/validate?code=cim.1`
- **Entonces** el sistema responde `200` con `isValid=true`, `isAvailable=false` y
  `reason=WBS_CODE_DUPLICATED`

#### Scenario: SC-EDT-60 Código inválido

- **Dado** un código de once caracteres o con un carácter fuera de `R-19`
- **Cuando** el usuario consulta la validación
- **Entonces** el sistema responde `200` con `isValid=false`, `isAvailable=false` y
  `reason=WBS_CODE_INVALID`

#### Scenario: SC-EDT-61 Edición con exclusión del nodo actual

- **Dado** un nodo con código `CIM.1`
- **Cuando** el usuario consulta la validación con el mismo código y su `excludeNodeId`
- **Entonces** el sistema responde `200` con `isValid=true`, `isAvailable=true` y `reason=null`

### Requirement: REQ-EDT-18 Movimiento de nodos dentro del proyecto

El sistema DEBE (MUST) permitir mover un capítulo con todo su subárbol o un ítem a otro capítulo del mismo
proyecto o a la raíz, en la posición indicada, conservando su código y los de sus descendientes. Citas:
RF-04; R-18; D-30, D-118.

#### Scenario: SC-EDT-62 Mover un capítulo hoja

- **Dado** un capítulo sin hijos y otro capítulo del mismo proyecto
- **Cuando** se mueve el primero dentro del segundo
- **Entonces** el capítulo aparece como hijo del destino y su código no cambia

#### Scenario: SC-EDT-63 Mover un capítulo con subárbol

- **Dado** un capítulo con subcapítulos e ítems en varios niveles
- **Cuando** se mueve a otro capítulo del proyecto
- **Entonces** todo el subárbol se traslada, conserva sus códigos y sus posiciones relativas

#### Scenario: SC-EDT-64 Mover un ítem a otro capítulo

- **Dado** un ítem en un capítulo de un nivel y otro capítulo de distinta jerarquía
- **Cuando** se mueve el ítem al segundo capítulo
- **Entonces** el ítem queda dentro del destino con su código y su cantidad intactos

#### Scenario: SC-EDT-65 Mover un capítulo a la raíz

- **Dado** un subcapítulo con subárbol
- **Cuando** se mueve a la raíz del presupuesto
- **Entonces** queda como nodo raíz, sin capítulo padre, y conserva su subárbol y sus códigos

#### Scenario: SC-EDT-66 Mover un ítem a la raíz

- **Dado** un ítem dentro de un capítulo
- **Cuando** se mueve a la raíz del presupuesto
- **Entonces** queda como ítem raíz, sin capítulo padre, y conserva su código

#### Scenario: SC-EDT-67 Mover un capítulo dentro de su propio descendiente

- **Dado** un capítulo con un subcapítulo anidado
- **Cuando** se intenta mover el capítulo dentro de ese subcapítulo
- **Entonces** el sistema responde `400 WBS_MOVE_CYCLE` y no mueve ningún nodo

#### Scenario: SC-EDT-68 Destino inexistente o de otro proyecto

- **Dado** un nodo y un capítulo destino que no existe o pertenece a otro proyecto
- **Cuando** se solicita el movimiento
- **Entonces** el sistema responde `404 WBS_TARGET_NOT_FOUND` y no mueve el nodo

#### Scenario: SC-EDT-69 Destino que no es un capítulo

- **Dado** un ítem o un capítulo y un ítem como destino
- **Cuando** se solicita el movimiento
- **Entonces** el sistema responde `400 VALIDATION_ERROR` y no mueve el nodo

#### Scenario: SC-EDT-70 Se respeta la posición indicada

- **Dado** un capítulo destino con tres hijos
- **Cuando** se mueve un nodo a la posición `1` del destino
- **Entonces** el nodo queda en esa posición, los demás se desplazan y el origen se compacta

#### Scenario: SC-EDT-75 Movimiento dentro del mismo padre

- **Dado** un padre con los hijos `A`, `B` y `C`
- **Cuando** se mueve `A` a `targetIndex=1` dentro del mismo padre
- **Entonces** el orden final es `B`, `A`, `C`; los códigos no cambian

### Requirement: REQ-EDT-19 Actividad y participantes del EDT

El sistema DEBE (MUST) guardar una fila por proyecto y usuario con la fecha UTC de la última modificación de
la EDT, conservarla aunque se eliminen todos los nodos y permitir consultar la última actividad y la lista de
participantes sin exponer el detalle de las operaciones. Citas: RF-14; D-08, D-116, D-120.

#### Scenario: SC-EDT-71 El alta registra al participante

- **Dado** un proyecto sin actividad registrada
- **Cuando** se crea el primer nodo
- **Entonces** se crea la fila `(project_id, user_id)` con el actor autenticado y la fecha en UTC

#### Scenario: SC-EDT-72 Toda escritura actualiza la actividad

- **Dado** un proyecto con actividad registrada
- **Cuando** se crea, edita, mueve, reordena o elimina un nodo
- **Entonces** se actualiza la fila del actor y no se crea historial de operaciones

#### Scenario: SC-EDT-73 Consulta de actividad y participantes

- **Dado** un proyecto con actividad de varios usuarios
- **Cuando** el Director, el Presupuestador o el Administrador consulta `GET /wbs/activity`
- **Entonces** el sistema devuelve la última actividad (`MAX(last_modified_at)`) y la lista de participantes

#### Scenario: SC-EDT-74 Proyecto sin actividad

- **Dado** un proyecto sin filas de actividad
- **Cuando** un actor autorizado consulta la actividad
- **Entonces** el sistema responde `200` con `lastActivity=null` y `participants=[]`

#### Scenario: SC-EDT-76 El Auditor intenta consultar la actividad

- **Dado** un Auditor autenticado con acceso al proyecto
- **Cuando** consulta `GET /wbs/activity`
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN`

### Requirement: REQ-EDT-20 Referencia del APU en el árbol

El sistema DEBE (MUST) devolver en cada ítem del árbol la referencia del APU vinculado con `apuId`, `status`,
`description`, `unit`, `unitPrice` y `rowVersion`, o nula si el ítem no tiene APU; `unitPrice` es `0` hasta
`E-4a` y `rowVersion` permite la edición unificada. El cliente no puede fijar esos valores. Citas: RF-04,
RF-07; D-30, D-113, D-121.

#### Scenario: SC-EDT-78 Ítem con APU en el árbol

- **Dado** un ítem vinculado a un APU completo
- **Cuando** un actor con lectura consulta el árbol
- **Entonces** el ítem devuelve el identificador, el estado, la descripción, la unidad, `unitPrice = 0` y
  `apuRowVersion` del APU

#### Scenario: SC-EDT-79 Ítem sin APU en el árbol

- **Dado** un ítem sin APU
- **Cuando** un actor con lectura consulta el árbol
- **Entonces** el ítem devuelve la referencia del APU como nula

#### Scenario: SC-EDT-80 El cliente intenta fijar la referencia del APU

- **Dado** un actor autorizado que envía `apuId`, estado, descripción o unidad en el alta o la edición del ítem
- **Cuando** el sistema procesa la solicitud
- **Entonces** no aplica esos valores y el vínculo solo cambia por la creación o la baja del APU

### Requirement: REQ-EDT-21 Baja controlada de la estructura

El sistema DEBE (MUST) eliminar físicamente el ítem y su código; si el ítem tiene APU, DEBE (MUST) eliminar
también el APU y sus líneas de composición y DEBE (MUST) conservar los insumos del proyecto. La baja de un
capítulo DEBE (MUST) permitirse solo cuando no tenga subcapítulos ni ítems: si contiene APUs en cualquier
descendiente responde `409 WBS_CHAPTER_HAS_APU`, y si contiene otros nodos responde
`409 WBS_NODE_HAS_CHILDREN`. No existe baja en cascada. Citas: RF-04; D-114, D-119 (revisadas).

#### Scenario: SC-EDT-81 Baja de un ítem sin APU

- **Dado** un ítem sin APU asociado
- **Cuando** un actor autorizado lo elimina
- **Entonces** el sistema borra el ítem y su código, y compacta las posiciones de sus hermanos

#### Scenario: SC-EDT-82 Baja de un capítulo vacío

- **Dado** un capítulo sin subcapítulos ni ítems
- **Cuando** un actor autorizado lo elimina
- **Entonces** el sistema borra el capítulo y su código

#### Scenario: SC-EDT-83 Baja de un capítulo con ítems sin APU

- **Dado** un capítulo con subcapítulos o ítems y sin APUs en su subárbol
- **Cuando** un actor autorizado intenta eliminarlo
- **Entonces** el sistema responde `409 WBS_NODE_HAS_CHILDREN` y no borra ningún nodo

#### Scenario: SC-EDT-84 Baja de un capítulo con APUs

- **Dado** un capítulo con al menos un APU en su subárbol
- **Cuando** un actor autorizado intenta eliminarlo
- **Entonces** el sistema responde `409 WBS_CHAPTER_HAS_APU` y conserva el capítulo, los ítems y los APUs

#### Scenario: SC-EDT-85 Baja de un ítem con APU

- **Dado** un ítem ya vinculado a un APU
- **Cuando** un actor autorizado lo elimina
- **Entonces** el sistema borra el ítem, su código, el APU y sus líneas de composición, y conserva los
  insumos del proyecto

### Requirement: REQ-EDT-22 Alta y edición unificadas con el APU

El sistema DEBE (MUST) crear el ítem y su APU en `POST /projects/{projectId}/wbs/items`, con
`description` y `unit` del APU obligatorios y `resources` opcionales, y vincularlos 1:1 en una transacción.
DEBE (MUST) editar `quantityWork` obligatorio, `code` opcional y un bloque `apu` opcional en
`PUT /projects/{projectId}/wbs/items/{itemId}`, con `apuRowVersion` y `resources` como reemplazo completo.
El cliente no fija `apuId`. Citas: RF-04, RF-07; D-16, D-31.

#### Scenario: SC-EDT-86 Alta conjunta desde el EDT

- **Dado** un actor autorizado que envía código, cantidad y el bloque `apu`
- **Cuando** registra el ítem
- **Entonces** el sistema crea el ítem, el APU, sus líneas y el vínculo en una transacción

#### Scenario: SC-EDT-87 Edición conjunta desde el EDT

- **Dado** un ítem con su APU y ambas versiones vigentes
- **Cuando** se edita la cantidad y el contenido del APU
- **Entonces** el sistema aplica ambos cambios o ninguno

#### Scenario: SC-EDT-88 Código opcional en la edición

- **Dado** una edición que no envía `code`
- **Cuando** se actualiza el ítem
- **Entonces** el código vigente se conserva
