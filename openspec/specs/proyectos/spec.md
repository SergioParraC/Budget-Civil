# proyectos Specification

## Purpose
Define la pertenencia de usuarios a proyectos, el ciclo de vida del proyecto con sus estados y
transiciones explícitas, la configuración como entidad propia, el cierre y el registro de historial de
estado y visibilidad dentro de una compañía.

## Requirements

### Requirement: REQ-PROYECTOS-01 Creación del proyecto en estado inicial

El sistema DEBE (MUST) crear el proyecto dentro de una compañía y en estado `POR_COTIZAR`, con el creador registrado y el Director responsable asignado. Citas: RF-03; D-85, D-86, D-88.

#### Scenario: SC-PROYECTOS-01 Alta de un proyecto

- **Dado** un Director de proyectos autenticado en su compañía
- **Cuando** crea un proyecto con datos válidos
- **Entonces** el proyecto queda en estado `POR_COTIZAR`, dentro de esa compañía y con el creador registrado

#### Scenario: SC-PROYECTOS-02 El Administrador intenta crear un proyecto

- **Dado** un Administrador autenticado
- **Cuando** intenta crear un proyecto
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROYECTOS-03 Alta con datos inválidos

- **Dado** un actor autorizado que omite el nombre del proyecto
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza la creación con un error de validación y no deja ningún proyecto creado

### Requirement: REQ-PROYECTOS-02 Responsable del proyecto al crear sin ser Director

El sistema DEBE (MUST) asignar como responsable del proyecto al Director de proyectos activo más antiguo de la compañía cuando el creador no es Director. Citas: RF-03; D-85.

#### Scenario: SC-PROYECTOS-04 Un Presupuestador crea el proyecto

- **Dado** un Presupuestador autenticado y una compañía con al menos un Director activo
- **Cuando** crea un proyecto
- **Entonces** el proyecto queda con el Director activo más antiguo de la compañía como responsable

#### Scenario: SC-PROYECTOS-05 El creador intenta imponer otro responsable

- **Dado** un Presupuestador que envía un responsable distinto del calculado
- **Cuando** el sistema procesa el alta
- **Entonces** rechaza la solicitud y no crea el proyecto con un responsable distinto

### Requirement: REQ-PROYECTOS-03 Conjunto cerrado de cinco estados

El sistema DEBE (MUST) admitir únicamente los estados `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO` e `INHABILITADO` para el proyecto. Citas: RF-03; R-26; D-88.

#### Scenario: SC-PROYECTOS-06 Estado dentro del conjunto cerrado

- **Dado** un proyecto en cualquiera de los cinco estados
- **Cuando** se consulta su estado
- **Entonces** el sistema devuelve uno de los cinco valores admitidos

#### Scenario: SC-PROYECTOS-07 Intento de fijar un estado inexistente

- **Dado** un actor autorizado que solicita el estado `APROBADO`
- **Cuando** el sistema valida la solicitud
- **Entonces** la rechaza como valor inválido y el proyecto conserva su estado

### Requirement: REQ-PROYECTOS-04 Cambio de estado solo por comando explícito

El sistema DEBE (MUST) cambiar el estado del proyecto únicamente por un comando explícito de un actor autorizado, nunca como efecto de una edición de datos. Citas: RF-03; R-26; D-88.

#### Scenario: SC-PROYECTOS-08 La edición de datos no cambia el estado

- **Dado** un proyecto en estado `POR_COTIZAR`
- **Cuando** un actor autorizado modifica sus datos descriptivos
- **Entonces** el estado del proyecto no cambia

#### Scenario: SC-PROYECTOS-09 Comando explícito de cambio de estado

- **Dado** un proyecto en estado `POR_COTIZAR`
- **Cuando** un actor autorizado ejecuta el comando de cambio a `FALTA_INFORMACION`
- **Entonces** el proyecto queda en `FALTA_INFORMACION` y la transición queda registrada

### Requirement: REQ-PROYECTOS-05 Transición a COTIZADO

El sistema DEBE (MUST) permitir la transición a `COTIZADO` solo cuando el cien por ciento de los insumos del proyecto esté cotizado, y solo al Director de proyectos o al Presupuestador. Citas: RF-03, RF-21; BR-007; R-06; D-89.

#### Scenario: SC-PROYECTOS-10 Transición con todos los insumos cotizados

- **Dado** un proyecto con el cien por ciento de sus insumos en estado `COTIZADO` y un Director o un Presupuestador autenticado
- **Cuando** ejecuta el cambio a `COTIZADO`
- **Entonces** el proyecto pasa a `COTIZADO` y la transición queda registrada

#### Scenario: SC-PROYECTOS-11 Transición con insumos sin cotizar

- **Dado** un proyecto con al menos un insumo que no está en estado `COTIZADO`
- **Cuando** un actor autorizado ejecuta el cambio a `COTIZADO`
- **Entonces** el sistema rechaza la transición con `409` y el proyecto conserva su estado

### Requirement: REQ-PROYECTOS-06 Transición a FINALIZADO

El sistema DEBE (MUST) permitir la transición a `FINALIZADO` solo desde `COTIZADO` y solo al Director de proyectos, sin exigir que el Director firme APUs. Citas: RF-21; BR-011; D-14, D-89, D-90.

#### Scenario: SC-PROYECTOS-12 Cierre desde COTIZADO

- **Dado** un proyecto en estado `COTIZADO` y un Director de proyectos autenticado
- **Cuando** ejecuta el cierre
- **Entonces** el proyecto pasa a `FINALIZADO` sin que el Director haya firmado ningún APU

#### Scenario: SC-PROYECTOS-13 Cierre desde otro estado

- **Dado** un proyecto en estado `POR_COTIZAR` o `FALTA_INFORMACION`
- **Cuando** el Director intenta el cierre
- **Entonces** el sistema rechaza la transición y el proyecto conserva su estado

#### Scenario: SC-PROYECTOS-14 Otro rol intenta el cierre

- **Dado** un Presupuestador, un Auditor o un Administrador autenticado
- **Cuando** intenta cerrar el proyecto
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-07 Estado INHABILITADO y su visibilidad

El sistema DEBE (MUST) mantener visible un proyecto `INHABILITADO` únicamente para el Administrador y el Director de proyectos, y rechazar el acceso de los demás roles. Citas: RF-03; R-27; D-88.

#### Scenario: SC-PROYECTOS-15 El Director inhabilita el proyecto

- **Dado** un proyecto habilitado
- **Cuando** un Director de proyectos autorizado lo inhabilita
- **Entonces** el proyecto queda en `INHABILITADO` y sus datos se conservan

#### Scenario: SC-PROYECTOS-16 Un Presupuestador consulta un proyecto INHABILITADO

- **Dado** un Presupuestador asignado a un proyecto que está en `INHABILITADO`
- **Cuando** intenta consultarlo
- **Entonces** el sistema rechaza el acceso sin revelar los datos del proyecto

#### Scenario: SC-PROYECTOS-17 Consulta del proyecto INHABILITADO por un rol autorizado

- **Dado** un proyecto en `INHABILITADO`
- **Cuando** el Administrador o el Director de proyectos lo consulta
- **Entonces** el sistema devuelve sus datos

### Requirement: REQ-PROYECTOS-08 Registro del historial de estado

El sistema DEBE (MUST) insertar un registro de historial por cada cambio de estado con el estado anterior, el nuevo, el actor, la fecha y el motivo. Citas: RF-03; R-29; D-92.

#### Scenario: SC-PROYECTOS-18 Registro de una transición

- **Dado** un proyecto que cambia de estado por un comando autorizado
- **Cuando** el sistema confirma el cambio
- **Entonces** existe un registro con estado anterior, estado nuevo, actor, fecha y motivo

#### Scenario: SC-PROYECTOS-19 Intento de alterar el historial

- **Dado** un registro de historial de estado existente
- **Cuando** un usuario intenta modificarlo o eliminarlo
- **Entonces** el sistema rechaza la operación

### Requirement: REQ-PROYECTOS-09 Registro del historial de visibilidad

El sistema DEBE (MUST) insertar un registro independiente por cada cambio de visibilidad pública o privada del proyecto. Citas: RF-03; R-29; D-92, D-22.

#### Scenario: SC-PROYECTOS-20 Cambio de visibilidad registrado

- **Dado** un proyecto privado
- **Cuando** un actor autorizado lo publica
- **Entonces** existe un registro de visibilidad con el valor anterior, el nuevo, el actor y la fecha

#### Scenario: SC-PROYECTOS-21 El cambio de visibilidad no crea registro de estado

- **Dado** un proyecto que solo cambia de visibilidad
- **Cuando** el sistema confirma el cambio
- **Entonces** no se inserta ningún registro de historial de estado

### Requirement: REQ-PROYECTOS-10 Configuración del proyecto como entidad propia

El sistema DEBE (MUST) mantener exactamente una configuración por proyecto, con divisa base, componentes del AIU, modo de firma y requisito de auditor. Solo el Director de proyectos puede modificar esos parámetros (`D-112`). Citas: RF-03; D-90, D-93, D-94, D-112.

#### Scenario: SC-PROYECTOS-22 Configuración única del proyecto

- **Dado** un proyecto creado
- **Cuando** se consulta su configuración
- **Entonces** el sistema devuelve una sola configuración con la divisa base, los tres componentes del AIU, el modo de firma y el requisito de auditor

#### Scenario: SC-PROYECTOS-23 Intento de crear una segunda configuración

- **Dado** un proyecto que ya tiene configuración
- **Cuando** se intenta crear otra configuración para ese proyecto
- **Entonces** el sistema rechaza la operación con conflicto y conserva la configuración existente

### Requirement: REQ-PROYECTOS-11 Concurrencia optimista en el proyecto

El sistema DEBE (MUST) rechazar con `409` la actualización de un proyecto cuya versión ya no es la vigente y aplicar solo la actualización que coincide con ella. Citas: RF-03; D-50, D-97.

#### Scenario: SC-PROYECTOS-24 Actualización con versión desactualizada

- **Dado** un proyecto que fue modificado por otra sesión después de que el actor lo leyó
- **Cuando** el actor envía su actualización con la versión anterior
- **Entonces** el sistema responde `409` y no aplica ningún cambio

#### Scenario: SC-PROYECTOS-25 Actualización con la versión vigente

- **Dado** un proyecto con la versión vigente en poder del actor
- **Cuando** envía su actualización
- **Entonces** el sistema la aplica y la versión avanza

### Requirement: REQ-PROYECTOS-12 Pertenencia de usuarios al proyecto

El sistema DEBE (MUST) mantener una sola pertenencia por usuario y proyecto, sin historial de asignaciones previas. Citas: RF-01; D-02, D-08, D-98.

#### Scenario: SC-PROYECTOS-26 Asignación de un usuario al proyecto

- **Dado** un usuario de la compañía que aún no pertenece al proyecto
- **Cuando** un actor autorizado lo asigna
- **Entonces** queda una única pertenencia entre ese usuario y ese proyecto

#### Scenario: SC-PROYECTOS-27 Asignación duplicada

- **Dado** un usuario que ya pertenece al proyecto
- **Cuando** se intenta asignarlo de nuevo
- **Entonces** el sistema rechaza la operación con conflicto

#### Scenario: SC-PROYECTOS-28 Dos asignaciones simultáneas del mismo usuario

- **Dado** dos solicitudes simultáneas que asignan al mismo usuario al mismo proyecto
- **Cuando** el sistema las procesa
- **Entonces** queda exactamente una pertenencia y la otra solicitud recibe conflicto

### Requirement: REQ-PROYECTOS-13 Asignación de usuarios al proyecto según la matriz

El sistema DEBE (MUST) permitir asignar usuarios al proyecto al Director con usuarios de su compañía y al Presupuestador solo con Presupuestadores y Auditores ya afiliados a su compañía. Citas: RF-01; D-34, D-85; matriz `1.3.2`.

#### Scenario: SC-PROYECTOS-29 El Director asigna usuarios de su compañía

- **Dado** un Director de proyectos autenticado
- **Cuando** asigna al proyecto un usuario de su compañía
- **Entonces** el sistema crea la pertenencia

#### Scenario: SC-PROYECTOS-30 El Presupuestador asigna Presupuestadores y Auditores

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** asigna a un Presupuestador o a un Auditor ya afiliado a su compañía
- **Entonces** el sistema crea la pertenencia

#### Scenario: SC-PROYECTOS-31 El Presupuestador intenta asignar un Director

- **Dado** un Presupuestador autenticado
- **Cuando** intenta asignar al proyecto a un Director de proyectos
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROYECTOS-32 Un Auditor intenta asignar usuarios

- **Dado** un Auditor autenticado
- **Cuando** intenta asignar un usuario al proyecto
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROYECTOS-33 El Director asigna un usuario de otra compañía

- **Dado** un Director de proyectos de la compañía A
- **Cuando** intenta asignar al proyecto un usuario de la compañía B
- **Entonces** el sistema rechaza la operación y no crea la pertenencia

#### Scenario: SC-PROYECTOS-52 El Administrador intenta asignar usuarios

- **Dado** un Administrador autenticado
- **Cuando** intenta asignar o retirar un usuario del proyecto
- **Entonces** el sistema rechaza la operación con `403` porque solo lee información (`D-112`)

### Requirement: REQ-PROYECTOS-14 Aislamiento por proyecto

El sistema DEBE (MUST) limitar el acceso al proyecto a su compañía y, para Presupuestador y Auditor, a los proyectos a los que están asignados. Citas: RF-01; R-14; D-02, D-34, D-36.

#### Scenario: SC-PROYECTOS-34 Presupuestador no asignado en su propia compañía

- **Dado** un Presupuestador de la compañía A sin pertenencia a un proyecto de esa compañía
- **Cuando** intenta consultarlo
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-PROYECTOS-35 Acceso desde otra compañía

- **Dado** un usuario de la compañía A
- **Cuando** solicita un proyecto de la compañía B por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el proyecto existe

### Requirement: REQ-PROYECTOS-15 Administrador en solo lectura sobre proyectos

El sistema DEBE (MUST) permitir al Administrador consultar proyectos sin crear, editar ni configurar ninguno. Citas: RF-03; R-08; D-86.

#### Scenario: SC-PROYECTOS-36 El Administrador consulta proyectos

- **Dado** un Administrador autenticado
- **Cuando** consulta el listado global de proyectos
- **Entonces** obtiene los proyectos de todas las compañías

#### Scenario: SC-PROYECTOS-37 El Administrador intenta editar o configurar

- **Dado** un Administrador autenticado y un proyecto existente
- **Cuando** intenta modificar sus datos o su configuración
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-16 Inhabilitación y reactivación por el Director

El sistema DEBE (MUST) permitir inhabilitar y reactivar un proyecto únicamente al Director de proyectos; al reactivarlo, el proyecto vuelve a `FALTA_INFORMACION`. Citas: RF-03; D-88.

#### Scenario: SC-PROYECTOS-38 Reactivación del proyecto inhabilitado

- **Dado** un proyecto en estado `INHABILITADO` y un Director de proyectos autenticado
- **Cuando** ejecuta la reactivación
- **Entonces** el proyecto pasa a `FALTA_INFORMACION` y la transición queda registrada

#### Scenario: SC-PROYECTOS-39 Otro rol intenta inhabilitar o reactivar

- **Dado** un Presupuestador o un Auditor autenticado
- **Cuando** intenta inhabilitar o reactivar el proyecto
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-17 Retroceso de COTIZADO a FALTA_INFORMACION

El sistema DEBE (MUST) permitir el retroceso de `COTIZADO` a `FALTA_INFORMACION` al Director de proyectos o al Presupuestador, sin precondición de insumos. Citas: RF-03; D-88.

#### Scenario: SC-PROYECTOS-40 Retroceso ejecutado por el Presupuestador

- **Dado** un proyecto en estado `COTIZADO` y un Presupuestador autenticado
- **Cuando** ejecuta el retroceso a `FALTA_INFORMACION`
- **Entonces** el proyecto queda en `FALTA_INFORMACION` y la transición queda registrada

#### Scenario: SC-PROYECTOS-41 El Auditor intenta el retroceso

- **Dado** un proyecto en estado `COTIZADO` y un Auditor autenticado
- **Cuando** intenta el retroceso a `FALTA_INFORMACION`
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-18 Alta rechazada sin Director activo en la compañía

El sistema DEBE (MUST) rechazar la creación de un proyecto cuando la compañía no tiene ningún Director de proyectos activo. Citas: RF-03; D-85.

#### Scenario: SC-PROYECTOS-42 Alta en una compañía sin Director activo

- **Dado** una compañía sin ningún Director de proyectos activo
- **Cuando** un actor autorizado intenta crear un proyecto
- **Entonces** el sistema rechaza la creación con un error funcional que indica la ausencia de Director

#### Scenario: SC-PROYECTOS-43 Alta tras asignar un Director activo

- **Dado** una compañía que acaba de recibir un Director de proyectos activo
- **Cuando** el mismo actor autorizado reintenta el alta
- **Entonces** el sistema crea el proyecto en `POR_COTIZAR`

### Requirement: REQ-PROYECTOS-19 Reapertura del proyecto finalizado

El sistema DEBE (MUST) permitir al Director de proyectos reabrir un proyecto en `FINALIZADO` hacia `FALTA_INFORMACION`, sin más precondición que su autorización. Citas: RF-03; D-88.

#### Scenario: SC-PROYECTOS-44 El Director reabre un proyecto finalizado

- **Dado** un proyecto en estado `FINALIZADO` y un Director de proyectos autenticado
- **Cuando** ejecuta la reapertura
- **Entonces** el proyecto queda en `FALTA_INFORMACION` y la transición queda registrada

#### Scenario: SC-PROYECTOS-45 Otro rol intenta la reapertura

- **Dado** un Presupuestador, un Auditor o un Administrador autenticado
- **Cuando** intenta reabrir el proyecto finalizado
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-20 Configuración del proyecto reservada al Director

El sistema DEBE (MUST) reservar al Director de proyectos la edición de los parámetros del proyecto (ubicación, divisa base, tope presupuestal, AIU, modo de firma, firmante y requisito de auditor); el Presupuestador solo puede editar nombre, descripción y objetivo. Citas: RF-03; D-112; matriz `1.3.2`.

#### Scenario: SC-PROYECTOS-46 El Presupuestador intenta cambiar los parámetros

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** envía un cambio de divisa, AIU, tope o ubicación
- **Entonces** el sistema responde `403 PROJECT_CONFIGURATION_FORBIDDEN` y no aplica cambios

#### Scenario: SC-PROYECTOS-47 El Presupuestador edita los datos generales

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** cambia el nombre, la descripción o el objetivo sin tocar los parámetros
- **Entonces** el sistema aplica el cambio

#### Scenario: SC-PROYECTOS-48 El Administrador intenta configurar el proyecto

- **Dado** un Administrador autenticado
- **Cuando** intenta actualizar la configuración del proyecto
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-PROYECTOS-21 Auditoría independiente del modo de firma

El sistema DEBE (MUST) admitir auditores en el proyecto cuando `requires_auditor` está activo, con independencia del modo de firma, y conservarlos asignados si el requisito se desactiva. La aprobación del auditor por APU solo se exige cuando el requisito está activo. Citas: RF-12, RF-13; D-03, D-14, D-52, D-112.

#### Scenario: SC-PROYECTOS-49 Asignación de auditor con cualquier modo de firma

- **Dado** un proyecto con `requires_auditor = true` y cualquier modo de firma, incluido `SIN_FIRMA`
- **Cuando** el Director o el Presupuestador asignan un Auditor de la compañía
- **Entonces** el sistema crea la pertenencia y el proyecto queda auditable

#### Scenario: SC-PROYECTOS-50 Se desactiva el requisito de auditor con auditores asignados

- **Dado** un proyecto con auditores asignados y `requires_auditor = true`
- **Cuando** el Director desactiva el requisito
- **Entonces** el sistema guarda el cambio y conserva las pertenencias de los auditores en el proyecto

#### Scenario: SC-PROYECTOS-51 El Auditor consulta un proyecto sin auditoría habilitada

- **Dado** un Auditor asignado a un proyecto con `requires_auditor = false`
- **Cuando** abre el proyecto
- **Entonces** el sistema le muestra una advertencia de que la auditoría no está habilitada y mantiene la vista en solo lectura

#### Scenario: SC-PROYECTOS-53 Cierre sin aprobación del auditor cuando el requisito está inactivo

- **Dado** un proyecto con `requires_auditor = false`
- **Cuando** el Director lo cierra a `FINALIZADO` con los demás requisitos cumplidos
- **Entonces** el sistema no exige aprobación del auditor

