# Spec Delta: edt

## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: REQ-EDT-15 Baja de la estructura

**Reason**: la baja en cascada se sustituye por una baja controlada: el capítulo solo se elimina vacío y el
ítem con APU elimina su APU.

**Migration**: los clientes dejan de enviar `cascade`; el capítulo no vacío responde `409` y debe vaciarse
ítem por ítem antes de eliminarse.

## ADDED Requirements

### Requirement: REQ-EDT-20 Referencia del APU en el árbol

El sistema DEBE (MUST) devolver en cada ítem del árbol la referencia del APU vinculado con `apuId`, `status`,
`description` y `unit`, o nula si el ítem no tiene APU; el cliente no puede fijar esos valores. Citas:
RF-04, RF-07; D-30, D-113.

#### Scenario: SC-EDT-78 Ítem con APU en el árbol

- **Dado** un ítem vinculado a un APU completo
- **Cuando** un actor con lectura consulta el árbol
- **Entonces** el ítem devuelve el identificador, el estado, la descripción y la unidad del APU

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
