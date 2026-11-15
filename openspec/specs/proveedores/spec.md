# proveedores Specification

## Purpose
Define el directorio de proveedores por compañía, reutilizable en todos sus proyectos, con datos de
contacto, ubicación y observaciones, la asociación opcional con los insumos, un selector ligero y una
alerta de ubicación no bloqueante. No define interfaz ni catálogo público.

## Requirements

### Requirement: REQ-PROVEEDORES-01 Proveedor de la compañía reutilizable

El sistema DEBE (MUST) registrar cada proveedor dentro de una compañía y DEBE (MUST) permitir reutilizarlo
en los insumos de todos los proyectos de esa compañía sin duplicar el registro. Citas: RF-05; D-02, D-27,
D-36.

#### Scenario: SC-PROVEEDORES-01 Alta de un proveedor en la compañía

- **Dado** un Director de proyectos autenticado en su compañía
- **Cuando** registra un proveedor con datos válidos
- **Entonces** el proveedor queda asociado a esa compañía y no a un proyecto

#### Scenario: SC-PROVEEDORES-02 Reutilización en dos proyectos

- **Dado** un proveedor de la compañía y dos proyectos de esa compañía
- **Cuando** se asocia el mismo proveedor a un insumo de cada proyecto
- **Entonces** ambos insumos referencian el mismo `supplier_id`

#### Scenario: SC-PROVEEDORES-03 Consulta de un proveedor de otra compañía

- **Dado** un usuario de la compañía A y un proveedor de la compañía B
- **Cuando** solicita el proveedor por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el proveedor existe

### Requirement: REQ-PROVEEDORES-02 Datos del proveedor

El sistema DEBE (MUST) exigir solo `business_name` y DEBE (MUST) admitir `contact_name`, `contact_phone`,
`email`, `address` y `observations` como opcionales; el email DEBE (MUST) validarse cuando se informe.
Citas: RF-05; D-28, D-100.

#### Scenario: SC-PROVEEDORES-04 Alta solo con razón social

- **Dado** un actor autorizado en su compañía
- **Cuando** registra un proveedor únicamente con la razón social
- **Entonces** el sistema crea el proveedor y deja los demás campos nulos

#### Scenario: SC-PROVEEDORES-05 Alta con todos los datos

- **Dado** un actor autorizado en su compañía
- **Cuando** registra un proveedor con razón social, contacto, teléfono, email, dirección y observaciones
- **Entonces** el sistema guarda y devuelve todos los valores

#### Scenario: SC-PROVEEDORES-06 Alta sin razón social

- **Dado** un actor autorizado que omite la razón social
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación con un error de validación

#### Scenario: SC-PROVEEDORES-07 Correo electrónico inválido

- **Dado** un actor autorizado que envía un correo sin formato válido
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-PROVEEDORES-08 Campos opcionales ausentes

- **Dado** un actor autorizado que no envía contacto, teléfono, dirección ni observaciones
- **Cuando** registra el proveedor con razón social y email válido
- **Entonces** el sistema crea el proveedor sin exigir los campos opcionales

### Requirement: REQ-PROVEEDORES-03 Ubicación del proveedor

El sistema DEBE (MUST) admitir `country_id`, `department_id` y `municipality_id` opcionales, referenciando
los catálogos existentes, y DEBE (MUST) validar su coherencia jerárquica cuando se informen. Citas: RF-05;
D-96.

#### Scenario: SC-PROVEEDORES-09 Alta con ubicación completa

- **Dado** un actor autorizado en su compañía
- **Cuando** registra un proveedor con país, departamento y municipio válidos
- **Entonces** el sistema guarda las tres referencias y las devuelve al consultar

#### Scenario: SC-PROVEEDORES-10 Municipio fuera del departamento

- **Dado** un actor autorizado que envía un municipio que no pertenece al departamento indicado
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-PROVEEDORES-11 Ubicación parcial

- **Dado** un actor autorizado que informa solo el país
- **Cuando** registra el proveedor
- **Entonces** el sistema acepta la ubicación parcial y deja departamento y municipio nulos

#### Scenario: SC-PROVEEDORES-12 Municipio inexistente

- **Dado** un actor autorizado que envía un municipio que no existe en el catálogo
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

### Requirement: REQ-PROVEEDORES-04 Asociación de uno a muchos y proveedor opcional

El sistema DEBE (MUST) permitir que un proveedor suministre varios insumos y que un insumo referencie como
máximo un proveedor; `supplier_id` no es único y puede ser nulo. Citas: RF-05, RF-06; D-27, D-29.

#### Scenario: SC-PROVEEDORES-13 Un proveedor con varios insumos

- **Dado** un proveedor de la compañía y tres insumos del mismo proyecto
- **Cuando** se asocian los tres insumos al proveedor
- **Entonces** los tres guardan el mismo `supplier_id` sin conflicto

#### Scenario: SC-PROVEEDORES-14 Insumo sin proveedor

- **Dado** un actor autorizado que registra un insumo sin proveedor
- **Cuando** confirma el alta
- **Entonces** el sistema guarda el insumo con `supplier_id` nulo sin error

#### Scenario: SC-PROVEEDORES-15 Reasignación del proveedor

- **Dado** un insumo con el proveedor A y un proveedor B de la misma compañía
- **Cuando** un actor autorizado cambia el insumo al proveedor B
- **Entonces** el insumo queda asociado a B y el proveedor A conserva sus demás insumos

#### Scenario: SC-PROVEEDORES-16 Un insumo referencia un solo proveedor

- **Dado** un insumo con un proveedor asociado
- **Cuando** se consulta el insumo
- **Entonces** devuelve un único proveedor

### Requirement: REQ-PROVEEDORES-05 Estado del insumo sin proveedor

El sistema DEBE (MUST) mantener en `FALTA_INFORMACION` el insumo sin proveedor aunque tenga descripción,
unidad, precio y marca; al asociar un proveedor y completar los datos, DEBE (MUST) pasar a `POR_COTIZAR`.
Citas: RF-05; BR-009; D-89.

#### Scenario: SC-PROVEEDORES-17 Insumo completo sin proveedor

- **Dado** un insumo con descripción, unidad, precio y marca, pero sin proveedor
- **Cuando** se guarda
- **Entonces** su estado es `FALTA_INFORMACION`

#### Scenario: SC-PROVEEDORES-18 Asociación que completa el insumo

- **Dado** un insumo en `FALTA_INFORMACION` con los cuatro campos completos y sin proveedor
- **Cuando** un actor autorizado le asocia un proveedor activo
- **Entonces** el insumo pasa a `POR_COTIZAR`

#### Scenario: SC-PROVEEDORES-19 Retiro del proveedor

- **Dado** un insumo en `POR_COTIZAR` con proveedor
- **Cuando** un actor autorizado retira el proveedor
- **Entonces** el insumo vuelve a `FALTA_INFORMACION`

### Requirement: REQ-PROVEEDORES-06 Razón social no única

El sistema DEBE (MUST) permitir que existan dos proveedores con la misma razón social y no DEBE (MUST)
imponer unicidad sobre `business_name`. Citas: RF-05; Q-09.

#### Scenario: SC-PROVEEDORES-20 Dos proveedores con la misma razón social

- **Dado** un proveedor registrado con una razón social
- **Cuando** se registra otro proveedor con la misma razón social
- **Entonces** el sistema crea el segundo proveedor sin conflicto

#### Scenario: SC-PROVEEDORES-21 El alta no se bloquea por nombre repetido

- **Dado** un actor autorizado que registra un proveedor con una razón social ya existente
- **Cuando** confirma el alta
- **Entonces** el sistema no devuelve un error de unicidad y crea el proveedor

### Requirement: REQ-PROVEEDORES-07 Autorización de proveedores

El sistema DEBE (MUST) permitir crear, editar, dar de baja y reactivar proveedores al Director de proyectos
y al Presupuestador de la compañía, y DEBE (MUST) permitir solo lectura al Administrador y al Auditor.
Citas: RF-05; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-PROVEEDORES-22 El Director crea y edita

- **Dado** un Director de proyectos autenticado en su compañía
- **Cuando** crea un proveedor y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-PROVEEDORES-23 El Presupuestador crea y edita

- **Dado** un Presupuestador autenticado en su compañía
- **Cuando** crea un proveedor y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-PROVEEDORES-24 El Administrador intenta escribir

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, editar o dar de baja un proveedor
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROVEEDORES-25 El Auditor intenta escribir

- **Dado** un Auditor autenticado
- **Cuando** intenta crear, editar o dar de baja un proveedor
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROVEEDORES-26 El Administrador consulta proveedores

- **Dado** un Administrador autenticado
- **Cuando** consulta el listado de proveedores de una compañía
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-PROVEEDORES-27 El Auditor consulta proveedores

- **Dado** un Auditor autenticado en su compañía
- **Cuando** consulta el listado de proveedores
- **Entonces** el sistema devuelve la información en solo lectura

### Requirement: REQ-PROVEEDORES-08 Listado completo y filtros

El sistema DEBE (MUST) listar proveedores con datos completos, filtrar por razón social, nombre de contacto
y ubicación, y paginar con `page`/`pageSize`. Citas: RF-05; D-96, D-101.

#### Scenario: SC-PROVEEDORES-28 Filtro por razón social

- **Dado** una compañía con proveedores que contienen «cemento» en su razón social
- **Cuando** se filtra por «cemento»
- **Entonces** el resultado incluye esos proveedores

#### Scenario: SC-PROVEEDORES-29 Filtro por nombre de contacto

- **Dado** un proveedor con un nombre de contacto concreto
- **Cuando** se filtra por ese contacto
- **Entonces** el resultado incluye el proveedor

#### Scenario: SC-PROVEEDORES-30 Filtro por ubicación

- **Dado** proveedores de municipios distintos
- **Cuando** se filtra por municipio, departamento o país
- **Entonces** el resultado solo contiene los proveedores de esa ubicación

#### Scenario: SC-PROVEEDORES-31 Paginación del listado completo

- **Dado** una compañía con doce proveedores
- **Cuando** se consulta la primera página con `pageSize` 10
- **Entonces** el sistema devuelve diez proveedores y el total de doce

### Requirement: REQ-PROVEEDORES-09 Selector ligero de proveedores

El sistema DEBE (MUST) exponer una consulta de selección que devuelva solo `supplier_id` y `business_name`
de proveedores activos, filtrable por razón social, contacto y ubicación. Citas: RF-05; D-101.

#### Scenario: SC-PROVEEDORES-32 Respuesta del selector

- **Dado** un proyecto con proveedores activos
- **Cuando** se consulta el selector
- **Entonces** cada elemento devuelve únicamente `supplierId` y `businessName`

#### Scenario: SC-PROVEEDORES-33 Filtros del selector

- **Dado** proveedores con distinta razón social, contacto y ubicación
- **Cuando** se filtra el selector por razón social, contacto o ubicación
- **Entonces** el resultado solo contiene los proveedores que cumplen el filtro

#### Scenario: SC-PROVEEDORES-34 Proveedor inactivo en el selector

- **Dado** un proveedor inactivo
- **Cuando** se consulta el selector
- **Entonces** el proveedor no aparece en el resultado

### Requirement: REQ-PROVEEDORES-10 Alerta de ubicación del proveedor

El sistema DEBE (MUST) exponer una alerta no bloqueante cuando la ubicación del proveedor difiera de la del
proyecto, comparando el nivel más específico disponible e informando cuando no haya datos suficientes.
Citas: RF-05; D-96.

#### Scenario: SC-PROVEEDORES-35 Misma ubicación

- **Dado** un proveedor y un proyecto en el mismo municipio
- **Cuando** se consulta la alerta de ubicación
- **Entonces** el sistema responde `SUPPLIER_LOCATION_MATCH` sin advertencia

#### Scenario: SC-PROVEEDORES-36 Municipios distintos

- **Dado** un proveedor en un municipio y un proyecto en otro municipio
- **Cuando** se consulta la alerta de ubicación
- **Entonces** el sistema responde `SUPPLIER_LOCATION_MISMATCH` y describe ambas ubicaciones

#### Scenario: SC-PROVEEDORES-37 Departamento o país distintos

- **Dado** un proveedor y un proyecto en departamentos o países distintos
- **Cuando** se consulta la alerta de ubicación
- **Entonces** el sistema responde `SUPPLIER_LOCATION_MISMATCH` con la comparación disponible

#### Scenario: SC-PROVEEDORES-38 Ubicación incompleta

- **Dado** un proveedor o un proyecto sin ubicación suficiente para comparar
- **Cuando** se consulta la alerta de ubicación
- **Entonces** el sistema responde `LOCATION_INCOMPLETE` con `canCompare` en falso

#### Scenario: SC-PROVEEDORES-39 La alerta no bloquea la asignación

- **Dado** un proveedor con ubicación distinta a la del proyecto
- **Cuando** un actor autorizado lo asocia a un insumo
- **Entonces** el sistema guarda la asociación y la alerta queda solo como advertencia

### Requirement: REQ-PROVEEDORES-11 Baja lógica del proveedor

El sistema DEBE (MUST) dar de baja lógica a un proveedor sin eliminarlo y DEBE (MUST) impedir nuevas
asociaciones con proveedores inactivos; los insumos existentes conservan la referencia. Citas: RF-05; D-49.

#### Scenario: SC-PROVEEDORES-40 Baja lógica de un proveedor

- **Dado** un proveedor activo sin insumos asociados
- **Cuando** un actor autorizado lo da de baja
- **Entonces** el proveedor queda inactivo pero conserva su registro

#### Scenario: SC-PROVEEDORES-41 Un insumo conserva el proveedor inactivo

- **Dado** un insumo asociado a un proveedor y el proveedor dado de baja
- **Cuando** se consulta el insumo
- **Entonces** el insumo conserva el `supplier_id` y muestra el proveedor como inactivo

#### Scenario: SC-PROVEEDORES-42 Nueva asociación con un proveedor inactivo

- **Dado** un proveedor inactivo
- **Cuando** un actor autorizado intenta asociarlo a un insumo
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-PROVEEDORES-43 Reactivación del proveedor

- **Dado** un proveedor inactivo
- **Cuando** un actor autorizado lo reactiva
- **Entonces** el proveedor vuelve a estar disponible para nuevas asociaciones

### Requirement: REQ-PROVEEDORES-12 Concurrencia y autoría

El sistema DEBE (MUST) aplicar concurrencia optimista con `rowversion` y DEBE (MUST) registrar
`last_modified_by` y `last_modified_at` en cada alta y edición. Citas: RF-05; D-08, D-50, D-97.

#### Scenario: SC-PROVEEDORES-44 Edición con versión desactualizada

- **Dado** un proveedor modificado por otro actor después de que el primero lo leyó
- **Cuando** el primer actor envía su edición con la versión anterior
- **Entonces** el sistema responde `409` y no modifica el proveedor

#### Scenario: SC-PROVEEDORES-45 Alta y edición registran autoría

- **Dado** un actor autorizado que crea y luego edita un proveedor
- **Cuando** se consulta el proveedor
- **Entonces** `last_modified_by` contiene al actor y `last_modified_at` la fecha en UTC

#### Scenario: SC-PROVEEDORES-46 El cliente intenta fijar la autoría

- **Dado** un actor autorizado que envía un `last_modified_by` distinto del suyo
- **Cuando** el sistema procesa el alta o la edición
- **Entonces** no aplica el valor enviado y conserva al actor autenticado

### Requirement: REQ-PROVEEDORES-13 Copia de insumos con proveedor

El sistema DEBE (MUST) conservar el proveedor al copiar un insumo dentro de la misma compañía y DEBE (MUST)
rechazar la copia si el proveedor no pertenece a esa compañía. Citas: RF-06; D-27, D-29.

#### Scenario: SC-PROVEEDORES-47 Copia conserva el proveedor

- **Dado** un insumo con proveedor en el proyecto A de la compañía
- **Cuando** se copia el insumo al proyecto B de la misma compañía
- **Entonces** la copia conserva el mismo `supplier_id`

#### Scenario: SC-PROVEEDORES-48 Copia con proveedor de otra compañía

- **Dado** un insumo cuyo proveedor no pertenece a la compañía del proyecto destino
- **Cuando** se intenta copiar el insumo
- **Entonces** el sistema rechaza la operación sin crear la copia

### Requirement: REQ-PROVEEDORES-14 Datos de contacto del insumo

El sistema DEBE (MUST) obtener los datos de contacto del proveedor cuando el insumo lo tiene y DEBE (MUST)
tratar `distributor` y `contact_number` como campos heredados obsoletos. Citas: RF-05; D-28.

#### Scenario: SC-PROVEEDORES-49 Insumo con proveedor

- **Dado** un insumo asociado a un proveedor con sus datos de contacto
- **Cuando** se consulta el insumo
- **Entonces** el sistema devuelve la razón social, el contacto, el teléfono, el email, la dirección y la
  ubicación del proveedor

#### Scenario: SC-PROVEEDORES-50 Insumo sin proveedor

- **Dado** un insumo sin proveedor y con `distributor` y `contact_number` heredados
- **Cuando** se consulta el insumo
- **Entonces** el sistema devuelve esos campos heredados en solo lectura
