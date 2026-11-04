# Spec Delta: proveedores

## Purpose

Define el directorio de proveedores por compañía, sus datos mínimos de contacto y la asociación de uno a
muchos con los insumos de los proyectos, dentro del alcance de backend de `E-3a`. No define interfaz ni
catálogo público.

## ADDED Requirements

### Requirement: REQ-PROVEEDORES-01 Proveedor propio de la compañía

El sistema DEBE (MUST) registrar cada proveedor dentro de una compañía y DEBE (MUST) limitar su consulta y
su uso a esa compañía. Citas: RF-05; D-02, D-36.

#### Scenario: SC-PROVEEDORES-01 Alta de un proveedor en la compañía

- **Dado** un Director de proyectos autenticado en su compañía
- **Cuando** registra un proveedor con datos válidos
- **Entonces** el proveedor queda asociado a esa compañía

#### Scenario: SC-PROVEEDORES-02 Consulta de un proveedor de otra compañía

- **Dado** un usuario de la compañía A y un proveedor de la compañía B
- **Cuando** solicita el proveedor por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el proveedor existe

#### Scenario: SC-PROVEEDORES-03 Uso de un proveedor de otra compañía en un insumo

- **Dado** un insumo de la compañía A
- **Cuando** un actor autorizado intenta asociarle un proveedor de la compañía B
- **Entonces** el sistema rechaza la operación sin revelar si el proveedor existe

### Requirement: REQ-PROVEEDORES-02 Datos mínimos del proveedor

El sistema DEBE (MUST) exigir razón social, nombre de contacto, teléfono del contacto, correo electrónico y
dirección, y DEBE (MUST) validar el formato del correo. Citas: RF-05; D-28, D-100.

#### Scenario: SC-PROVEEDORES-04 Alta con los cinco datos

- **Dado** un actor autorizado en su compañía
- **Cuando** registra un proveedor con razón social, contacto, teléfono, email y dirección
- **Entonces** el sistema guarda y devuelve los cinco valores

#### Scenario: SC-PROVEEDORES-05 Alta sin razón social

- **Dado** un actor autorizado que omite la razón social
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación con un error de validación

#### Scenario: SC-PROVEEDORES-06 Correo electrónico inválido

- **Dado** un actor autorizado que envía un correo sin formato válido
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-PROVEEDORES-07 Teléfono de contacto ausente

- **Dado** un actor autorizado que omite el teléfono del contacto
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación y no deja el proveedor creado

### Requirement: REQ-PROVEEDORES-03 Asociación de uno a muchos con los insumos

El sistema DEBE (MUST) permitir que un proveedor tenga múltiples insumos asociados y que un insumo
referencie como máximo un proveedor; `supplier_id` no es único y puede ser nulo. Citas: RF-05, RF-06; D-27,
D-29.

#### Scenario: SC-PROVEEDORES-08 Un proveedor con varios insumos

- **Dado** un proveedor de la compañía y tres insumos del mismo proyecto
- **Cuando** se asocian los tres insumos al proveedor
- **Entonces** los tres guardan el mismo `supplier_id` sin conflicto

#### Scenario: SC-PROVEEDORES-09 Un insumo referencia un solo proveedor

- **Dado** un insumo con un proveedor asociado
- **Cuando** se consulta el insumo
- **Entonces** devuelve un único proveedor

#### Scenario: SC-PROVEEDORES-10 Reasignación del proveedor

- **Dado** un insumo con el proveedor A y un proveedor B de la misma compañía
- **Cuando** un actor autorizado cambia el insumo al proveedor B
- **Entonces** el insumo queda asociado a B y el proveedor A conserva sus demás insumos

#### Scenario: SC-PROVEEDORES-11 Insumo sin proveedor

- **Dado** un insumo registrado sin proveedor
- **Cuando** se consulta el insumo
- **Entonces** el sistema devuelve `supplier_id` nulo sin error

### Requirement: REQ-PROVEEDORES-04 Razón social no única

El sistema DEBE (MUST) permitir que existan dos proveedores con la misma razón social y no DEBE (MUST)
imponer unicidad sobre `business_name`. Citas: RF-05; Q-09.

#### Scenario: SC-PROVEEDORES-12 Dos proveedores con la misma razón social

- **Dado** un proveedor registrado con una razón social
- **Cuando** se registra otro proveedor con la misma razón social
- **Entonces** el sistema crea el segundo proveedor sin conflicto

#### Scenario: SC-PROVEEDORES-13 El alta no se bloquea por nombre repetido

- **Dado** un actor autorizado que registra un proveedor con una razón social ya existente
- **Cuando** confirma el alta
- **Entonces** el sistema no devuelve un error de unicidad y crea el proveedor

### Requirement: REQ-PROVEEDORES-05 Autorización de proveedores

El sistema DEBE (MUST) permitir crear, editar, dar de baja y reactivar proveedores al Director de proyectos
y al Presupuestador de la compañía, y DEBE (MUST) permitir solo lectura al Administrador y al Auditor.
Citas: RF-05; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-PROVEEDORES-14 El Director crea y edita

- **Dado** un Director de proyectos autenticado en su compañía
- **Cuando** crea un proveedor y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-PROVEEDORES-15 El Presupuestador crea y edita

- **Dado** un Presupuestador autenticado en su compañía
- **Cuando** crea un proveedor y después lo edita
- **Entonces** el sistema aplica ambas operaciones

#### Scenario: SC-PROVEEDORES-16 El Administrador intenta escribir

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, editar o dar de baja un proveedor
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROVEEDORES-17 El Auditor intenta escribir

- **Dado** un Auditor autenticado
- **Cuando** intenta crear, editar o dar de baja un proveedor
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROVEEDORES-18 El Administrador consulta proveedores

- **Dado** un Administrador autenticado
- **Cuando** consulta el listado de proveedores de una compañía
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-PROVEEDORES-19 El Auditor consulta proveedores

- **Dado** un Auditor autenticado en su compañía
- **Cuando** consulta el listado de proveedores
- **Entonces** el sistema devuelve la información en solo lectura

### Requirement: REQ-PROVEEDORES-06 Listado y búsqueda de proveedores

El sistema DEBE (MUST) listar y buscar proveedores por razón social, nombre de contacto o correo
electrónico, con paginación `page`/`pageSize`. Citas: RF-05; D-101.

#### Scenario: SC-PROVEEDORES-20 Búsqueda por razón social

- **Dado** una compañía con proveedores que contienen la palabra «cemento» en su razón social
- **Cuando** se busca por «cemento»
- **Entonces** el resultado incluye esos proveedores

#### Scenario: SC-PROVEEDORES-21 Búsqueda por contacto o correo

- **Dado** un proveedor con un nombre de contacto y un correo concretos
- **Cuando** se busca por ese contacto o por ese correo
- **Entonces** el resultado incluye el proveedor

#### Scenario: SC-PROVEEDORES-22 Paginación del listado

- **Dado** una compañía con doce proveedores
- **Cuando** se consulta la primera página con `pageSize` 10
- **Entonces** el sistema devuelve diez proveedores y el total de doce

### Requirement: REQ-PROVEEDORES-07 Baja lógica del proveedor

El sistema DEBE (MUST) dar de baja lógica a un proveedor sin eliminarlo y DEBE (MUST) impedir nuevas
asociaciones con proveedores inactivos; los insumos existentes conservan la referencia. Citas: RF-05; D-49.

#### Scenario: SC-PROVEEDORES-23 Baja lógica de un proveedor

- **Dado** un proveedor activo sin insumos asociados
- **Cuando** un actor autorizado lo da de baja
- **Entonces** el proveedor queda inactivo pero conserva su registro

#### Scenario: SC-PROVEEDORES-24 Un insumo conserva el proveedor inactivo

- **Dado** un insumo asociado a un proveedor y el proveedor dado de baja
- **Cuando** se consulta el insumo
- **Entonces** el insumo conserva el `supplier_id` y muestra el proveedor como inactivo

#### Scenario: SC-PROVEEDORES-25 Nueva asociación con un proveedor inactivo

- **Dado** un proveedor inactivo
- **Cuando** un actor autorizado intenta asociarlo a un insumo
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-PROVEEDORES-26 Reactivación del proveedor

- **Dado** un proveedor inactivo
- **Cuando** un actor autorizado lo reactiva
- **Entonces** el proveedor vuelve a estar disponible para nuevas asociaciones

### Requirement: REQ-PROVEEDORES-08 Concurrencia y autoría

El sistema DEBE (MUST) aplicar concurrencia optimista con `rowversion` y DEBE (MUST) registrar
`last_modified_by` y `last_modified_at` en cada alta y edición. Citas: RF-05; D-08, D-50, D-97.

#### Scenario: SC-PROVEEDORES-27 Edición con versión desactualizada

- **Dado** un proveedor modificado por otro actor después de que el primero lo leyó
- **Cuando** el primer actor envía su edición con la versión anterior
- **Entonces** el sistema responde `409` y no modifica el proveedor

#### Scenario: SC-PROVEEDORES-28 Alta y edición registran autoría

- **Dado** un actor autorizado que crea y luego edita un proveedor
- **Cuando** se consulta el proveedor
- **Entonces** `last_modified_by` contiene al actor y `last_modified_at` la fecha en UTC

#### Scenario: SC-PROVEEDORES-29 El cliente intenta fijar la autoría

- **Dado** un actor autorizado que envía un `last_modified_by` distinto del suyo
- **Cuando** el sistema procesa el alta o la edición
- **Entonces** no aplica el valor enviado y conserva al actor autenticado

### Requirement: REQ-PROVEEDORES-09 Copia de insumos con proveedor

El sistema DEBE (MUST) conservar el proveedor al copiar un insumo dentro de la misma compañía y DEBE (MUST)
rechazar la copia si el proveedor no pertenece a esa compañía. Citas: RF-06; D-27, D-29.

#### Scenario: SC-PROVEEDORES-30 Copia conserva el proveedor

- **Dado** un insumo con proveedor en el proyecto A de la compañía
- **Cuando** se copia el insumo al proyecto B de la misma compañía
- **Entonces** la copia conserva el mismo `supplier_id`

#### Scenario: SC-PROVEEDORES-31 Copia con proveedor de otra compañía

- **Dado** un insumo cuyo proveedor no pertenece a la compañía del proyecto destino
- **Cuando** se intenta copiar el insumo
- **Entonces** el sistema rechaza la operación sin crear la copia

### Requirement: REQ-PROVEEDORES-10 Datos de contacto del insumo

El sistema DEBE (MUST) obtener los datos de contacto del proveedor cuando el insumo lo tiene y DEBE (MUST)
tratar `distributor` y `contact_number` como campos heredados obsoletos. Citas: RF-05; D-28.

#### Scenario: SC-PROVEEDORES-32 Insumo con proveedor

- **Dado** un insumo asociado a un proveedor con sus datos de contacto
- **Cuando** se consulta el insumo
- **Entonces** el sistema devuelve la razón social, el contacto, el teléfono, el email y la dirección del
  proveedor

#### Scenario: SC-PROVEEDORES-33 Insumo sin proveedor

- **Dado** un insumo sin proveedor y con `distributor` y `contact_number` heredados
- **Cuando** se consulta el insumo
- **Entonces** el sistema devuelve esos campos heredados en solo lectura
