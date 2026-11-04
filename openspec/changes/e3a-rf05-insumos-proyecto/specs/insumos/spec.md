# Spec Delta: insumos

## Purpose

Define el registro, la tipología, el estado de cotización, la búsqueda y el filtrado de los insumos
propios de cada proyecto, así como su copia independiente desde otros proyectos de la misma compañía.
La transición a `COTIZADO`, la composición de matrices y el catálogo abierto pertenecen a etapas
posteriores.

## ADDED Requirements

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

### Requirement: REQ-INSUMOS-02 Siete tipos de insumo

El sistema DEBE (MUST) admitir únicamente los siete tipos del catálogo `ResourceType`: Materiales, Mano
de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matriz. El tipo es obligatorio, se elige en el alta
y no se crea desde este módulo. Citas: RF-05; D-16; PLAN §3.3.7.

#### Scenario: SC-INSUMOS-04 Los siete tipos son aceptados

- **Dado** un actor autorizado en un proyecto
- **Cuando** registra un insumo para cada uno de los siete tipos del catálogo
- **Entonces** el sistema crea los siete insumos con el tipo indicado

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

El sistema DEBE (MUST) asignar `FALTA_INFORMACION` cuando falte descripción, unidad, precio o marca, y
`POR_COTIZAR` cuando los cuatro estén presentes; DEBE (MUST) recalcularlo en cada alta y edición. La
transición a `COTIZADO` pertenece a `RF-11` y no se especifica aquí. Citas: RF-05; BR-009; D-89.

#### Scenario: SC-INSUMOS-11 Alta incompleta

- **Dado** un actor autorizado que registra un insumo con descripción y precio pero sin unidad
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-12 Alta completa

- **Dado** un actor autorizado que registra un insumo con descripción, unidad, precio y marca
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `POR_COTIZAR`

#### Scenario: SC-INSUMOS-13 Edición que completa el insumo

- **Dado** un insumo en estado `FALTA_INFORMACION` al que le faltaba la marca
- **Cuando** un actor autorizado registra la marca y completa los cuatro campos
- **Entonces** el insumo pasa a `POR_COTIZAR`

#### Scenario: SC-INSUMOS-14 Edición que retira un campo obligatorio

- **Dado** un insumo en estado `POR_COTIZAR`
- **Cuando** un actor autorizado deja la unidad vacía
- **Entonces** el insumo pasa a `FALTA_INFORMACION`

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
destino con permiso de escritura, conservando tipo, datos, precio y municipio. Citas: RF-06; D-27, D-29.

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

### Requirement: REQ-INSUMOS-10 Independencia de la copia

El sistema DEBE (MUST) crear la copia como registro independiente y DEBE (MUST) garantizar que los cambios
posteriores de precio o datos no se propaguen en ningún sentido. El estado de la copia DEBE (MUST)
derivarse de su completitud y `origin_resource_id` DEBE (MUST) quedar nulo en la copia de compañía.
Citas: RF-06; R-17; D-27, D-29.

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
- **Entonces** su `origin_resource_id` es nulo y no existe un vínculo compartido con el insumo de origen

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
