# Spec Delta: insumos

## ADDED Requirements

### Requirement: REQ-INSUMOS-16 Tipos de insumo disponibles

El sistema DEBE (MUST) exponer `GET /api/v1/projects/{projectId}/resource-types` con los tipos de insumo
activos del catálogo, cada uno con `{ id, name }`, ordenados como el catálogo; DEBE (MUST) devolver todos
los tipos activos y no solo los usados en el proyecto. Citas: RF-05; D-16; PLAN §3.3.7.

#### Scenario: SC-INSUMOS-57 Lista de los siete tipos

- **Dado** un proyecto existente y un actor autorizado
- **Cuando** consulta los tipos de insumo del proyecto
- **Entonces** el sistema devuelve los siete tipos activos con su `id` y `name`

#### Scenario: SC-INSUMOS-58 Proyecto sin insumos

- **Dado** un proyecto sin insumos registrados
- **Cuando** se consultan los tipos de insumo del proyecto
- **Entonces** el sistema devuelve los siete tipos activos, no una colección vacía

#### Scenario: SC-INSUMOS-59 Proyecto de otra compañía

- **Dado** un usuario de la compañía A y un proyecto de la compañía B
- **Cuando** consulta los tipos de insumo del proyecto
- **Entonces** el sistema responde `403 RESOURCE_READ_FORBIDDEN`

#### Scenario: SC-INSUMOS-60 Proyecto inexistente

- **Dado** un actor autorizado y un `projectId` que no existe
- **Cuando** consulta los tipos de insumo
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND`

#### Scenario: SC-INSUMOS-61 Presupuestador no asignado

- **Dado** un Presupuestador de la compañía dueña sin `ProjectMember` activo
- **Cuando** consulta los tipos de insumo del proyecto
- **Entonces** el sistema responde `403 RESOURCE_READ_FORBIDDEN`

#### Scenario: SC-INSUMOS-62 Orden del catálogo

- **Dado** un proyecto con los siete tipos activos
- **Cuando** se consultan los tipos de insumo
- **Entonces** el sistema los devuelve en el orden del catálogo: Materiales, Mano de obra, Herramienta,
  Equipos, Auxiliares, Trabajos y Matriz

#### Scenario: SC-INSUMOS-63 Tipo inactivo

- **Dado** un proyecto y un tipo de insumo inactivo
- **Cuando** se consultan los tipos de insumo del proyecto
- **Entonces** el tipo inactivo no aparece en la respuesta

### Requirement: REQ-INSUMOS-17 Consulta de insumos por unidad

El sistema DEBE (MUST) exponer `GET /api/v1/projects/{projectId}/resources/by-unit/{shortUnit}` para
devolver los insumos del proyecto cuyo atributo `unit` coincide exactamente, sin distinguir mayúsculas de
minúsculas, con la sigla recibida sin espacios en los extremos; DEBE (MUST) devolver una colección vacía
cuando no haya coincidencias y NO DEBE (MUST NOT) exigir que la sigla exista en `measurement_units`. Citas:
RF-05, RF-20; D-16; PLAN §3.3.7.

#### Scenario: SC-INSUMOS-64 Coincidencia por unidad

- **Dado** un proyecto con insumos en `M2`, `ML` y `UND`
- **Cuando** un actor con lectura consulta la unidad `M2`
- **Entonces** el sistema devuelve únicamente los insumos con `unit = M2`

#### Scenario: SC-INSUMOS-65 Coincidencia sin distinguir mayúsculas

- **Dado** un proyecto con un insumo en `m2`
- **Cuando** un actor con lectura consulta la unidad `M2`
- **Entonces** el sistema devuelve ese insumo

#### Scenario: SC-INSUMOS-66 Unidad sin insumos

- **Dado** un proyecto sin insumos en la unidad `M3`
- **Cuando** un actor con lectura consulta la unidad `M3`
- **Entonces** el sistema devuelve una colección vacía y no un error

#### Scenario: SC-INSUMOS-67 Unidad que no está en el catálogo

- **Dado** un proyecto con un insumo en `GL` y un catálogo sin la sigla `GL`
- **Cuando** un actor con lectura consulta la unidad `GL`
- **Entonces** el sistema devuelve el insumo y no exige que `GL` exista en el catálogo

#### Scenario: SC-INSUMOS-68 Sigla inválida

- **Dado** un actor con lectura que consulta una sigla vacía o de más de 20 caracteres
- **Cuando** envía la consulta
- **Entonces** el sistema responde `400 VALIDATION_ERROR`

#### Scenario: SC-INSUMOS-69 Proyecto de otra compañía

- **Dado** un usuario de la compañía A y un proyecto de la compañía B
- **Cuando** consulta los insumos por unidad en ese proyecto
- **Entonces** el sistema rechaza la consulta sin revelar si el proyecto existe

#### Scenario: SC-INSUMOS-70 Proyecto inexistente

- **Dado** un actor autorizado y un `projectId` que no existe
- **Cuando** consulta los insumos por unidad
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND`

### Requirement: REQ-INSUMOS-18 Baja física de un insumo

El sistema DEBE (MUST) eliminar físicamente el registro del insumo con
`DELETE /api/v1/projects/{projectId}/resources/{resourceId}`, sin usar baja lógica ni conservar la fila, y
DEBE (MUST) responder `204`; si el insumo no existe en el proyecto DEBE (MUST) responder `404
RESOURCE_NOT_FOUND`. Citas: RF-05; D-49 revisada, D-50; `05 §3`.

#### Scenario: SC-INSUMOS-71 Baja exitosa

- **Dado** un insumo sin referencias en un proyecto
- **Cuando** un actor autorizado lo elimina
- **Entonces** el sistema responde `204` y el insumo deja de aparecer en el listado y en el detalle

#### Scenario: SC-INSUMOS-76 Baja repetida

- **Dado** un insumo que ya fue eliminado
- **Cuando** un actor autorizado intenta eliminarlo de nuevo
- **Entonces** el sistema responde `404 RESOURCE_NOT_FOUND`

### Requirement: REQ-INSUMOS-19 Bloqueo de la baja por referencias

El sistema DEBE (MUST) responder `409 RESOURCE_IN_USE` y no eliminar el insumo cuando esté referenciado por
una línea de APU, por un componente de Matriz o por una copia cuyo `origin_resource_id` lo apunta. Citas:
RF-05; D-49 revisada; `05 §3`.

#### Scenario: SC-INSUMOS-72 Insumo usado por una línea de APU

- **Dado** un insumo que es línea de un APU del proyecto
- **Cuando** un actor autorizado intenta eliminarlo
- **Entonces** el sistema responde `409 RESOURCE_IN_USE` y el insumo y la línea del APU se conservan

#### Scenario: SC-INSUMOS-73 Insumo usado por una Matriz

- **Dado** un insumo que es componente de una Matriz del proyecto
- **Cuando** un actor autorizado intenta eliminarlo
- **Entonces** el sistema responde `409 RESOURCE_IN_USE` y el componente de la Matriz se conserva

#### Scenario: SC-INSUMOS-74 Insumo origen de una copia

- **Dado** un insumo que es el origen de una copia en otro proyecto de la compañía
- **Cuando** un actor autorizado intenta eliminarlo
- **Entonces** el sistema responde `409 RESOURCE_IN_USE` y la copia y el origen se conservan

### Requirement: REQ-INSUMOS-20 Baja de una Matriz con componentes

Cuando el insumo es una Matriz, el sistema DEBE (MUST) eliminar físicamente sus propios componentes junto
con ella. Citas: RF-09; D-48; `05 §3`.

#### Scenario: SC-INSUMOS-75 Matriz con componentes

- **Dado** una Matriz con componentes que no está usada por ningún APU ni por otra Matriz
- **Cuando** un actor autorizado la elimina
- **Entonces** el sistema responde `204` y elimina físicamente la Matriz y sus componentes

### Requirement: REQ-INSUMOS-21 Autorización de la baja física

El sistema DEBE (MUST) permitir la baja física al Director de proyectos en los proyectos de su compañía y
al Presupuestador en los proyectos que tiene asignados; el Administrador y el Auditor NO DEBEN (MUST NOT)
eliminar insumos y reciben `403 RESOURCE_WRITE_FORBIDDEN`. Citas: RF-05; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-INSUMOS-77 El Director elimina un insumo

- **Dado** un Director de proyectos autenticado en un proyecto de su compañía
- **Cuando** elimina un insumo sin referencias
- **Entonces** el sistema aplica la baja física con `204`

#### Scenario: SC-INSUMOS-78 El Presupuestador asignado elimina un insumo

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** elimina un insumo sin referencias
- **Entonces** el sistema aplica la baja física con `204`

#### Scenario: SC-INSUMOS-79 El Administrador intenta eliminar

- **Dado** un Administrador autenticado
- **Cuando** intenta eliminar un insumo
- **Entonces** el sistema responde `403 RESOURCE_WRITE_FORBIDDEN`

#### Scenario: SC-INSUMOS-80 El Auditor intenta eliminar

- **Dado** un Auditor asignado al proyecto
- **Cuando** intenta eliminar un insumo
- **Entonces** el sistema responde `403 RESOURCE_WRITE_FORBIDDEN`

#### Scenario: SC-INSUMOS-81 El Presupuestador no asignado intenta eliminar

- **Dado** un Presupuestador de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta eliminar un insumo
- **Entonces** el sistema responde `403 RESOURCE_WRITE_FORBIDDEN`

### Requirement: REQ-INSUMOS-22 Aislamiento de la baja física

El sistema DEBE (MUST) rechazar la baja de un insumo de un proyecto de otra compañía sin revelar si el
proyecto o el insumo existen, y DEBE (MUST) responder `404 PROJECT_NOT_FOUND` cuando el proyecto no exista.
Citas: RF-05, RF-20; R-10, R-14; D-02, D-36.

#### Scenario: SC-INSUMOS-82 Baja en un proyecto de otra compañía

- **Dado** un usuario de la compañía A y un insumo de un proyecto de la compañía B
- **Cuando** intenta eliminarlo
- **Entonces** el sistema rechaza la operación sin revelar si el proyecto o el insumo existen

#### Scenario: SC-INSUMOS-83 Proyecto inexistente en la baja

- **Dado** un actor autorizado y un `projectId` que no existe
- **Cuando** intenta eliminar un insumo
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND`
