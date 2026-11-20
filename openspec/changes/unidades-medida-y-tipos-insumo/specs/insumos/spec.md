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
