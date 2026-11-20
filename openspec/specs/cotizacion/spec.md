# cotizacion Specification

## Purpose

Permite al Director y al Presupuestador marcar insumos como `COTIZADO` en una sola operación, por selección,
filtro o totalidad, y deja el proyecto en condiciones de pasar a `COTIZADO`.

## Requirements

### Requirement: REQ-COTIZACION-01 Cambio masivo por selección, filtro o totalidad

El sistema DEBE (MUST) marcar insumos como `COTIZADO` en una sola operación `PATCH /projects/{projectId}/resources/quotation-status`, con `scope` `SELECTION` (`resourceIds`), `FILTER` (filtros del listado) o `ALL` (todo el proyecto). Citas: RF-11; D-04, D-15, D-101.

#### Scenario: SC-COTIZACION-01 Selección explícita

- **Dado** un proyecto con cinco insumos completos en `POR_COTIZAR`
- **Cuando** un actor autorizado envía `scope = SELECTION` con tres `resourceIds`
- **Entonces** los tres quedan en `COTIZADO`, los otros dos conservan `POR_COTIZAR` y la respuesta informa
  `requested = 3` y `updated = 3`

#### Scenario: SC-COTIZACION-02 Filtro del listado

- **Dado** un proyecto con insumos de dos tipos y estados de cotización distintos
- **Cuando** el actor envía `scope = FILTER` con `resourceTypeId` y `quotationStatus = POR_COTIZAR`
- **Entonces** el sistema cotiza solo los insumos que cumplen el filtro evaluado en el servidor al ejecutar
  la operación

#### Scenario: SC-COTIZACION-03 Totalidad del proyecto

- **Dado** un proyecto con veinte insumos completos
- **Cuando** el actor envía `scope = ALL`
- **Entonces** los veinte quedan en `COTIZADO` dentro de una sola transacción

#### Scenario: SC-COTIZACION-04 Selección vacía

- **Dado** un actor autorizado que envía `scope = SELECTION` sin `resourceIds`
- **Cuando** el sistema valida la solicitud
- **Entonces** responde `400` con `ProblemDetails` y no modifica ningún insumo

#### Scenario: SC-COTIZACION-05 Insumo de otro proyecto

- **Dado** un actor autorizado que incluye el `resourceId` de un insumo de otro proyecto
- **Cuando** el sistema procesa la selección
- **Entonces** responde `404 RESOURCE_NOT_FOUND`, no aplica el lote y no revela el insumo ajeno

### Requirement: REQ-COTIZACION-02 Solo los insumos completos se cotizan

El sistema DEBE (MUST) marcar como `COTIZADO` únicamente los insumos con descripción, unidad, precio, marca y proveedor, y DEBE (MUST) omitir los que están en `FALTA_INFORMACION` sin modificar su estado ni su precio. Citas: RF-05, RF-11; BR-009; REQ-INSUMOS-04; D-89.

#### Scenario: SC-COTIZACION-06 Insumo incompleto omitido

- **Dado** un conjunto filtrado con un insumo al que le falta la marca
- **Cuando** el actor ejecuta la cotización masiva
- **Entonces** ese insumo conserva `FALTA_INFORMACION` y aparece en `skipped` con el código
  `RESOURCE_QUOTATION_INCOMPLETE`

#### Scenario: SC-COTIZACION-07 Matriz incompleta omitida

- **Dado** un insumo de tipo Matriz sin componentes
- **Cuando** el actor ejecuta la cotización masiva
- **Entonces** la Matriz se omite con `RESOURCE_QUOTATION_INCOMPLETE` y no se le asigna precio

#### Scenario: SC-COTIZACION-08 Insumo completo se cotiza

- **Dado** un insumo en `POR_COTIZAR` con los cinco datos completos y un proveedor activo
- **Cuando** el actor lo incluye en la cotización masiva
- **Entonces** pasa a `COTIZADO` y conserva su precio, su unidad y su proveedor

#### Scenario: SC-COTIZACION-09 Operación repetida

- **Dado** un lote ya cotizado en una ejecución anterior
- **Cuando** el actor repite la misma operación
- **Entonces** el sistema no falla, no cambia estados y devuelve `updated = 0` y `alreadyQuoted` con el
  total de insumos que ya estaban en `COTIZADO`

### Requirement: REQ-COTIZACION-03 Resumen por insumo y pendientes del proyecto

El sistema DEBE (MUST) devolver en cada operación `requested`, `updated`, `alreadyQuoted` y `skipped` con un código por insumo, y DEBE (MUST) informar `pendingResources` y `allQuoted` del proyecto después de aplicar el lote. Citas: RF-11; D-04, D-89, D-100.

#### Scenario: SC-COTIZACION-10 Lote con aplicados y omitidos

- **Dado** una operación con trescientos insumos solicitados, de los cuales seis están incompletos
- **Cuando** el sistema aplica el lote
- **Entonces** devuelve `requested = 300`, `updated = 294`, seis entradas en `skipped` con `resourceId` y
  código, y `pendingResources = 6`

#### Scenario: SC-COTIZACION-11 Último insumo cotizado

- **Dado** un proyecto con `pendingResources = 1` y el insumo pendiente completo
- **Cuando** el actor lo cotiza
- **Entonces** la respuesta informa `pendingResources = 0` y `allQuoted = true`

#### Scenario: SC-COTIZACION-12 Proyecto sin insumos

- **Dado** un proyecto sin insumos registrados
- **Cuando** el actor ejecuta `scope = ALL`
- **Entonces** la respuesta informa `requested = 0`, `updated = 0`, `pendingResources = 0` y
  `allQuoted = true`

#### Scenario: SC-COTIZACION-13 Pendientes de otro alcance

- **Dado** un proyecto con diez insumos, de los cuales la selección alcanza tres
- **Cuando** se aplica el lote
- **Entonces** `updated = 3` y `pendingResources` cuenta los siete insumos del proyecto que siguen sin
  `COTIZADO`, no solo los del alcance enviado

### Requirement: REQ-COTIZACION-04 Permisos y alcance de la cotización

El sistema DEBE (MUST) permitir la cotización al Director de la compañía dueña y al Presupuestador con membresía activa en el proyecto, y DEBE (MUST) rechazar al Administrador, al Auditor y a los actores de otra compañía. Citas: RF-11; D-15, D-34, D-83, D-84; matriz `1.3.2`; `02 §3`.

#### Scenario: SC-COTIZACION-14 El Director cotiza en su compañía

- **Dado** un Director autenticado de la compañía dueña del proyecto
- **Cuando** ejecuta la cotización masiva con `RESOURCES_WRITE`
- **Entonces** el sistema aplica el lote y registra al Director como autor del cambio

#### Scenario: SC-COTIZACION-15 El Presupuestador asignado cotiza

- **Dado** un Presupuestador con membresía activa en el proyecto
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema aplica el lote

#### Scenario: SC-COTIZACION-16 El Presupuestador no asignado intenta cotizar

- **Dado** un Presupuestador de la compañía dueña sin membresía activa en el proyecto
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema responde `403 RESOURCES_WRITE_FORBIDDEN` y no modifica insumos

#### Scenario: SC-COTIZACION-17 El Administrador intenta cotizar

- **Dado** un Administrador autenticado
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema responde `403` y no modifica insumos

#### Scenario: SC-COTIZACION-18 El Auditor intenta cotizar

- **Dado** un Auditor con membresía activa en el proyecto
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema responde `403` y no modifica insumos

#### Scenario: SC-COTIZACION-19 Proyecto de otra compañía

- **Dado** un usuario de la compañía A y un proyecto de la compañía B
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema responde `403` sin revelar los insumos del proyecto

#### Scenario: SC-COTIZACION-20 Proyecto inexistente

- **Dado** un actor autorizado y un `projectId` que no existe
- **Cuando** ejecuta la cotización masiva
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND`

### Requirement: REQ-COTIZACION-05 Autoría y auditoría del cambio

El sistema DEBE (MUST) actualizar el usuario y la fecha de última modificación de cada insumo cotizado y DEBE (MUST) registrar un evento de auditoría de seguridad por operación con el número de insumos afectados, sin guardar historial de valores. Citas: RF-11, RF-14; D-08, D-92, D-111.

#### Scenario: SC-COTIZACION-21 Autoría por insumo

- **Dado** un actor autorizado que cotiza tres insumos
- **Cuando** el lote termina
- **Entonces** cada insumo cotizado guarda ese actor y la fecha de la operación como último modificador

#### Scenario: SC-COTIZACION-22 Los omitidos conservan su autoría

- **Dado** un lote con un insumo incompleto omitido
- **Cuando** el lote termina
- **Entonces** el insumo omitido conserva su último modificador y su fecha anteriores

#### Scenario: SC-COTIZACION-23 Evento de auditoría de la operación

- **Dado** una cotización masiva aplicada correctamente
- **Cuando** se consulta la auditoría de seguridad
- **Entonces** existe un evento con el actor, el proyecto, el permiso `RESOURCES_WRITE`, el resultado
  `Success`, el número de insumos actualizados y el número de omitidos

