# insumos Specification

## Purpose
Define el registro, la tipología, el estado de cotización, la búsqueda y el filtrado de los insumos
propios de cada proyecto, así como su copia independiente desde otros proyectos de la misma compañía.
La transición a `COTIZADO`, la composición de matrices y el catálogo abierto pertenecen a etapas
posteriores.

## Requirements

### Requirement: REQ-INSUMOS-01 Insumo propio de un proyecto

El sistema DEBE (MUST) registrar cada insumo como registro propio de un proyecto existente y DEBE (MUST)
impedir que se mueva a otro proyecto; la reutilización se resuelve únicamente por copia. No existe un
registro de insumos a nivel de compañía. Citas: RF-05; R-02; D-05, D-27.

#### Scenario: SC-INSUMOS-01 Alta de un insumo en el proyecto

- **Dado** un Director de proyectos autenticado y un proyecto de su compañía
- **Cuando** registra un insumo con datos válidos
- **Entonces** el insumo queda asociado a ese proyecto y no a la compañía

#### Scenario: SC-INSUMOS-02 Intento de mover un insumo a otro proyecto

- **Dado** un insumo registrado en el proyecto A
- **Cuando** un actor autorizado envía un cambio de proyecto hacia el proyecto B
- **Entonces** el sistema rechaza la solicitud y el insumo permanece en el proyecto A

#### Scenario: SC-INSUMOS-03 Alta en un proyecto de otra compañía

- **Dado** un usuario de la compañía A
- **Cuando** intenta registrar un insumo en un proyecto de la compañía B
- **Entonces** el sistema rechaza la operación sin revelar si el proyecto existe

### Requirement: REQ-INSUMOS-02 Ocho tipos de insumo

El sistema DEBE (MUST) admitir únicamente los ocho tipos del catálogo `ResourceType`: Materiales, Mano
de obra, Herramienta, Equipos, Auxiliares, Trabajos, Transporte y Matriz. El tipo es obligatorio, se elige en el alta
y no se crea desde este módulo. Citas: RF-05; D-16; PLAN §3.3.7.

#### Scenario: SC-INSUMOS-04 Los ocho tipos son aceptados

- **Dado** un actor autorizado en un proyecto
- **Cuando** registra un insumo para cada uno de los ocho tipos del catálogo
- **Entonces** el sistema crea los ocho insumos con el tipo indicado

#### Scenario: SC-INSUMOS-05 Tipo fuera del catálogo

- **Dado** un actor autorizado que envía un identificador de tipo inexistente
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación con un error de validación y no deja el insumo creado

#### Scenario: SC-INSUMOS-06 Insumo de tipo Matriz sin composición

- **Dado** un actor autorizado que elige el tipo Matriz
- **Cuando** registra el insumo con los datos del insumo simple
- **Entonces** el sistema lo crea y no le exige componentes de matriz en este cambio

### Requirement: REQ-INSUMOS-03 Campos del insumo y validación

El sistema DEBE (MUST) exigir `description` y admitir `unit`, `unit_price`, `brand`, `distributor`,
`user_key`, `details`, `contact_number`, `municipality_id`, `quotation_status` y `origin_resource_id`.
`unit_price` DEBE (MUST) ser mayor o igual que cero y `municipality_id` DEBE (MUST) existir en el
catálogo. Citas: RF-05; BR-009, BR-010; D-96.

#### Scenario: SC-INSUMOS-07 Alta con todos los campos

- **Dado** un actor autorizado en un proyecto
- **Cuando** registra un insumo con descripción, unidad, precio, marca, distribuidor, clave de usuario,
  detalles, contacto y municipio
- **Entonces** el sistema guarda todos los valores y los devuelve al consultar el insumo

#### Scenario: SC-INSUMOS-08 Precio unitario negativo

- **Dado** un actor autorizado que envía un precio unitario negativo
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación y conserva el valor anterior

#### Scenario: SC-INSUMOS-09 Descripción ausente

- **Dado** un actor autorizado que omite la descripción
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación y no deja el insumo creado

#### Scenario: SC-INSUMOS-10 Municipio inexistente

- **Dado** un actor autorizado que envía un municipio que no existe en el catálogo
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

### Requirement: REQ-INSUMOS-04 Estado de cotización por completitud

El sistema DEBE (MUST) asignar `FALTA_INFORMACION` cuando falte descripción, unidad, precio, marca o
proveedor, y `POR_COTIZAR` cuando los cinco estén presentes; DEBE (MUST) recalcularlo en cada alta y
edición. El proveedor es opcional para crear el insumo, pero sin él el insumo permanece en
`FALTA_INFORMACION` (`REQ-PROVEEDORES-05`). La Matriz se evalúa según `REQ-APUS-06`. La transición a
`COTIZADO` pertenece a `RF-11` y no se especifica aquí. Citas: RF-05; BR-009; D-89.

#### Scenario: SC-INSUMOS-11 Alta incompleta

- **Dado** un actor autorizado que registra un insumo con descripción y precio pero sin unidad
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-12 Alta completa

- **Dado** un actor autorizado que registra un insumo con descripción, unidad, precio, marca y proveedor
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `POR_COTIZAR`

#### Scenario: SC-INSUMOS-13 Edición que completa el insumo

- **Dado** un insumo en estado `FALTA_INFORMACION` al que le faltaba la marca y el proveedor
- **Cuando** un actor autorizado registra la marca y asocia un proveedor activo
- **Entonces** el insumo pasa a `POR_COTIZAR`

#### Scenario: SC-INSUMOS-14 Edición que retira un campo obligatorio

- **Dado** un insumo en estado `POR_COTIZAR`
- **Cuando** un actor autorizado deja la unidad vacía
- **Entonces** el insumo pasa a `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-52 Alta completa sin proveedor

- **Dado** un actor autorizado que registra un insumo con descripción, unidad, precio y marca, sin proveedor
- **Cuando** confirma el alta
- **Entonces** el insumo se guarda y queda en estado `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-53 Retiro del proveedor

- **Dado** un insumo en estado `POR_COTIZAR` con proveedor
- **Cuando** un actor autorizado retira el proveedor
- **Entonces** el insumo vuelve a `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-54 Matriz sin composición

- **Dado** un insumo de tipo Matriz con unidad pero sin componentes
- **Cuando** se consulta su estado de cotización
- **Entonces** el insumo está en `FALTA_INFORMACION` y no se le exigen precio, marca ni proveedor

#### Scenario: SC-INSUMOS-55 Matriz con unidad y composición

- **Dado** un insumo de tipo Matriz con unidad y al menos un componente
- **Cuando** se consulta su estado de cotización
- **Entonces** el insumo está en `POR_COTIZAR`

### Requirement: REQ-INSUMOS-05 Sugerencia de unicidad

El sistema DEBE (MUST) sugerir insumos similares del mismo proyecto por descripción o `user_key` al
registrar o editar, y DEBE (MUST) completar la operación aunque exista coincidencia. La unicidad es una
sugerencia, no una restricción técnica. Citas: RF-16; Q-09; D-16.

#### Scenario: SC-INSUMOS-15 Alta con descripción similar

- **Dado** un proyecto con un insumo cuya descripción es similar a la del insumo que se va a registrar
- **Cuando** el actor autorizado confirma el alta
- **Entonces** el sistema le muestra los insumos similares y crea el nuevo insumo

#### Scenario: SC-INSUMOS-16 Clave de usuario repetida

- **Dado** un proyecto con un insumo que ya usa una clave de usuario
- **Cuando** otro actor autorizado registra un insumo con la misma clave
- **Entonces** el sistema advierte la coincidencia y permite crear el insumo

#### Scenario: SC-INSUMOS-17 La sugerencia no modifica el insumo existente

- **Dado** un insumo que coincide con otro del proyecto
- **Cuando** se registra la nueva coincidencia
- **Entonces** el insumo existente conserva sus datos, su precio y su estado

### Requirement: REQ-INSUMOS-06 Búsqueda por descripción

El sistema DEBE (MUST) buscar insumos del proyecto por coincidencia parcial de descripción, sin
distinguir mayúsculas de minúsculas, y DEBE (MUST) devolver una lista vacía cuando no haya coincidencias.
Citas: RF-16, RF-20.

#### Scenario: SC-INSUMOS-18 Coincidencia parcial

- **Dado** un proyecto con un insumo cuya descripción contiene la palabra «cemento»
- **Cuando** un actor con lectura consulta el listado con la búsqueda «cemento»
- **Entonces** el resultado incluye ese insumo

#### Scenario: SC-INSUMOS-19 Sin coincidencias

- **Dado** un proyecto sin insumos que coincidan con la búsqueda
- **Cuando** un actor con lectura busca un término inexistente
- **Entonces** el sistema devuelve una lista vacía y no un error

#### Scenario: SC-INSUMOS-20 La búsqueda no cruza proyectos

- **Dado** dos proyectos de la misma compañía
- **Cuando** se busca en uno de ellos un término que solo aparece en el otro
- **Entonces** el resultado no incluye el insumo del otro proyecto

### Requirement: REQ-INSUMOS-07 Filtros del listado

El sistema DEBE (MUST) filtrar el listado por tipo de insumo, estado de cotización, marca, municipio y
rango de precio unitario, y DEBE (MUST) combinar los filtros con conjunción. Un valor de filtro inválido
DEBE (MUST) rechazarse. Citas: RF-20; D-96, D-101.

#### Scenario: SC-INSUMOS-21 Filtro por tipo

- **Dado** un proyecto con insumos de varios tipos
- **Cuando** se filtra por el tipo Materiales
- **Entonces** el resultado solo contiene insumos de tipo Materiales

#### Scenario: SC-INSUMOS-22 Filtro por estado de cotización

- **Dado** un proyecto con insumos en `FALTA_INFORMACION` y en `POR_COTIZAR`
- **Cuando** se filtra por `FALTA_INFORMACION`
- **Entonces** el resultado solo contiene los insumos en ese estado

#### Scenario: SC-INSUMOS-23 Filtros combinados

- **Dado** un proyecto con insumos de varios tipos, marcas y municipios
- **Cuando** se filtran a la vez por tipo, estado, marca y municipio
- **Entonces** el resultado solo contiene los insumos que cumplen los cuatro filtros

#### Scenario: SC-INSUMOS-24 Rango de precio unitario

- **Dado** un proyecto con insumos de precios distintos y un insumo sin precio
- **Cuando** se filtra por un rango mínimo y máximo de precio unitario
- **Entonces** el resultado solo contiene los insumos cuyo precio está dentro del rango y excluye los que
  no tienen precio

#### Scenario: SC-INSUMOS-25 Valor de filtro inválido

- **Dado** un actor con lectura que envía un estado de cotización inexistente o un rango con mínimo mayor
  que el máximo
- **Cuando** consulta el listado
- **Entonces** el sistema rechaza la consulta con un error de validación

### Requirement: REQ-INSUMOS-08 Paginación del listado

El sistema DEBE (MUST) paginar el listado con `page` desde 1 y `pageSize` del conjunto configurado
`{10, 50, 100}`, y DEBE (MUST) devolver el total de coincidencias. Un `pageSize` no admitido DEBE (MUST)
rechazarse. Citas: RF-20; D-101.

#### Scenario: SC-INSUMOS-26 Página válida

- **Dado** un proyecto con doce insumos
- **Cuando** se consulta la primera página con `pageSize` 10
- **Entonces** el sistema devuelve diez insumos y el total de doce coincidencias

#### Scenario: SC-INSUMOS-27 Tamaño de página no admitido

- **Dado** un actor con lectura que solicita un `pageSize` fuera del conjunto configurado
- **Cuando** consulta el listado
- **Entonces** el sistema rechaza la consulta con un error de validación

### Requirement: REQ-INSUMOS-09 Copia desde otro proyecto de la compañía

El sistema DEBE (MUST) permitir copiar un insumo desde otro proyecto de la misma compañía a un proyecto
destino con permiso de escritura, conservando tipo, datos, precio y municipio. La copia de un insumo de
tipo Matriz se realiza con la operación de copia de Matriz, que reconcilia sus componentes
(`REQ-APUS-08`); la copia genérica la rechaza. Citas: RF-06, RF-09; BR-021; D-27, D-29.

#### Scenario: SC-INSUMOS-28 Copia exitosa

- **Dado** un insumo del proyecto A de la misma compañía y un actor con permiso de escritura en el
  proyecto B
- **Cuando** solicita copiar el insumo al proyecto B
- **Entonces** el sistema crea un insumo nuevo en el proyecto B con los mismos datos y precio, y con un
  identificador distinto

#### Scenario: SC-INSUMOS-29 Copia desde otra compañía

- **Dado** un insumo de un proyecto de la compañía B y un actor de la compañía A
- **Cuando** intenta copiarlo a un proyecto de su compañía
- **Entonces** el sistema rechaza la operación sin revelar si el insumo de origen existe

#### Scenario: SC-INSUMOS-30 Destino sin permiso de escritura

- **Dado** un actor que solo puede leer el proyecto destino
- **Cuando** intenta copiar un insumo hacia ese proyecto
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-INSUMOS-56 Copia genérica de una Matriz

- **Dado** un insumo de tipo Matriz con componentes en el proyecto de origen
- **Cuando** un actor autorizado intenta copiarlo con la operación de copia de insumo
- **Entonces** el sistema rechaza la operación con un error de validación e indica que debe usarse la copia de Matriz

### Requirement: REQ-INSUMOS-10 Independencia de la copia

El sistema DEBE (MUST) crear la copia como registro independiente y DEBE (MUST) garantizar que los cambios
posteriores de precio o datos no se propaguen en ningún sentido. El estado de la copia DEBE (MUST)
derivarse de su completitud y `origin_resource_id` DEBE (MUST) apuntar al insumo de origen en la copia de
compañía. Citas: RF-06; R-17; D-27, D-29.

#### Scenario: SC-INSUMOS-31 Cambio de precio en el destino

- **Dado** un insumo copiado del proyecto A al proyecto B
- **Cuando** se cambia el precio del insumo en el proyecto B
- **Entonces** el insumo del proyecto A conserva su precio original

#### Scenario: SC-INSUMOS-32 Cambio de precio en el origen

- **Dado** un insumo copiado del proyecto A al proyecto B
- **Cuando** se cambia el precio del insumo original en el proyecto A
- **Entonces** la copia del proyecto B conserva el precio que tenía al copiarse

#### Scenario: SC-INSUMOS-33 Procedencia de la copia

- **Dado** un insumo copiado desde otro proyecto de la compañía
- **Cuando** se consulta la copia
- **Entonces** su `origin_resource_id` es el identificador del insumo de origen y no existe un vínculo de
  actualización con él

### Requirement: REQ-INSUMOS-11 Autoría de la última modificación

El sistema DEBE (MUST) registrar `last_modified_by` y `last_modified_at` en cada alta, edición y copia,
con el actor autenticado y la fecha en UTC; el cliente no puede fijarlos. Citas: RF-14; D-08.

#### Scenario: SC-INSUMOS-34 Alta registra al autor

- **Dado** un actor autorizado que registra un insumo
- **Cuando** el sistema guarda el insumo
- **Entonces** `last_modified_by` contiene al actor autenticado y `last_modified_at` la fecha en UTC

#### Scenario: SC-INSUMOS-35 Edición actualiza la autoría

- **Dado** un insumo registrado por un usuario y editado después por otro
- **Cuando** se consulta el insumo
- **Entonces** `last_modified_by` contiene al segundo usuario y `last_modified_at` la fecha de la edición

#### Scenario: SC-INSUMOS-36 El cliente intenta fijar la autoría

- **Dado** un actor autorizado que envía un `last_modified_by` distinto del suyo
- **Cuando** el sistema procesa el alta o la edición
- **Entonces** no aplica el valor enviado y conserva al actor autenticado como autor

### Requirement: REQ-INSUMOS-12 Concurrencia optimista

El sistema DEBE (MUST) aplicar concurrencia optimista con `rowversion`: una edición con una versión
desactualizada DEBE (MUST) rechazarse con `409` y no modificar el insumo. Citas: RF-05; D-50, D-97.

#### Scenario: SC-INSUMOS-37 Edición con la versión vigente

- **Dado** un insumo cuya versión conoce el actor autorizado
- **Cuando** envía una edición con esa versión
- **Entonces** el sistema aplica el cambio y devuelve la nueva versión

#### Scenario: SC-INSUMOS-38 Edición con una versión desactualizada

- **Dado** un insumo modificado por otro actor después de que el primero lo leyó
- **Cuando** el primer actor envía su edición con la versión anterior
- **Entonces** el sistema responde `409` y no modifica el insumo

### Requirement: REQ-INSUMOS-13 Escritura de insumos

El sistema DEBE (MUST) permitir crear, editar y copiar insumos al Director de proyectos en los proyectos
de su compañía y al Presupuestador en los proyectos que tiene asignados; los demás roles no escriben.
Citas: RF-05, RF-06; D-34; matriz `1.3.2`.

#### Scenario: SC-INSUMOS-39 El Director crea y edita

- **Dado** un Director de proyectos autenticado en un proyecto de su compañía
- **Cuando** crea un insumo y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-INSUMOS-40 El Presupuestador asignado crea y edita

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** crea un insumo y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-INSUMOS-41 El Presupuestador no asignado intenta escribir

- **Dado** un Presupuestador de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta crear o editar un insumo del proyecto
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-INSUMOS-42 El Administrador intenta escribir

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, editar o copiar un insumo
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-INSUMOS-43 El Auditor intenta escribir

- **Dado** un Auditor asignado al proyecto
- **Cuando** intenta crear, editar o copiar un insumo
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-INSUMOS-14 Lectura de insumos

El sistema DEBE (MUST) permitir consultar insumos al Director de proyectos en los proyectos de su
compañía, al Presupuestador y al Auditor en sus proyectos asignados, y al Administrador en cualquier
proyecto; el Administrador y el Auditor solo leen. Citas: RF-05, RF-20; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-INSUMOS-44 El Director consulta insumos

- **Dado** un Director de proyectos autenticado en un proyecto de su compañía
- **Cuando** consulta el listado y el detalle de sus insumos
- **Entonces** el sistema devuelve la información del proyecto

#### Scenario: SC-INSUMOS-45 El Presupuestador asignado consulta insumos

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** consulta el listado y el detalle de sus insumos
- **Entonces** el sistema devuelve la información del proyecto

#### Scenario: SC-INSUMOS-46 El Administrador consulta insumos

- **Dado** un Administrador autenticado y un proyecto de cualquier compañía
- **Cuando** consulta el listado y el detalle de sus insumos
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-INSUMOS-47 El Auditor asignado consulta insumos

- **Dado** un Auditor asignado al proyecto
- **Cuando** consulta el listado y el detalle de sus insumos
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-INSUMOS-48 El Auditor no asignado intenta consultar

- **Dado** un Auditor de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta consultar sus insumos
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-INSUMOS-49 El Presupuestador no asignado intenta consultar

- **Dado** un Presupuestador de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta consultar sus insumos
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-INSUMOS-15 Aislamiento entre compañías

El sistema DEBE (MUST) impedir que un actor consulte o modifique insumos de un proyecto de otra compañía,
sin revelar si el proyecto o el insumo existen. Citas: RF-05, RF-20; R-10, R-14; D-02, D-36.

#### Scenario: SC-INSUMOS-50 Consulta de un insumo de otra compañía

- **Dado** un usuario de la compañía A y un insumo de un proyecto de la compañía B
- **Cuando** solicita el insumo por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el insumo existe

#### Scenario: SC-INSUMOS-51 Listado de un proyecto de otra compañía

- **Dado** un usuario de la compañía A
- **Cuando** solicita el listado de insumos de un proyecto de la compañía B
- **Entonces** el sistema rechaza la solicitud sin revelar si el proyecto existe

### Requirement: REQ-INSUMOS-16 Tipos de insumo disponibles

El sistema DEBE (MUST) exponer `GET /api/v1/projects/{projectId}/resource-types` con los tipos de insumo
activos del catálogo, cada uno con `{ id, code, name }`, ordenados como el catálogo; DEBE (MUST) devolver todos
los tipos activos y no solo los usados en el proyecto. Citas: RF-05; D-16; PLAN §3.3.7.

#### Scenario: SC-INSUMOS-57 Lista de los ocho tipos

- **Dado** un proyecto existente y un actor autorizado
- **Cuando** consulta los tipos de insumo del proyecto
- **Entonces** el sistema devuelve los ocho tipos activos con su `id`, `code` y `name`

#### Scenario: SC-INSUMOS-58 Proyecto sin insumos

- **Dado** un proyecto sin insumos registrados
- **Cuando** se consultan los tipos de insumo del proyecto
- **Entonces** el sistema devuelve los ocho tipos activos, no una colección vacía

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

- **Dado** un proyecto con los ocho tipos activos
- **Cuando** se consultan los tipos de insumo
- **Entonces** el sistema los devuelve en el orden del catálogo: Materiales, Mano de obra, Herramienta,
  Equipos, Auxiliares, Trabajos, Transporte y Matriz

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
