# identidad Specification

## Purpose
Define cómo una persona obtiene una cuenta en la plataforma, cómo se le asigna compañía y rol, cómo se
autoriza cada operación privada y cómo se protege el acceso en un producto SaaS multiempresa.

## Requirements

### Requirement: REQ-IDENTIDAD-01 Autorregistro público y anónimo

El sistema DEBE (MUST) permitir que cualquier persona cree su propia cuenta desde el autorregistro público, sin sesión iniciada y sin pertenencia previa a ninguna compañía ni proyecto. Citas: RF-23; R-24; D-72.

#### Scenario: SC-IDENTIDAD-01 Registro anónimo exitoso

- **Dado** un visitante sin sesión iniciada y sin cuenta en la plataforma
- **Cuando** envía nombre de usuario, correo electrónico y contraseña válidos al autorregistro
- **Entonces** el sistema crea la cuenta sin compañía y sin proyecto, y confirma el alta

#### Scenario: SC-IDENTIDAD-02 El cliente intenta fijar rol, compañía o proyecto

- **Dado** un visitante que envía además un rol, una compañía o un proyecto en la solicitud de registro
- **Cuando** el sistema valida la solicitud
- **Entonces** rechaza el alta y no crea ninguna cuenta

### Requirement: REQ-IDENTIDAD-02 Datos obligatorios del autorregistro

El sistema DEBE (MUST) exigir en el autorregistro el tipo y el número de documento, el primer nombre y el primer apellido, la fecha de nacimiento, el correo electrónico, el nombre de usuario y la contraseña. Citas: RF-23; D-76 (modificada por este cambio), D-78.

#### Scenario: SC-IDENTIDAD-03 Alta con la identidad completa

- **Dado** un visitante que declara tipo y número de documento, primer nombre, primer apellido, fecha de nacimiento, correo, nombre de usuario y contraseña válidos
- **Cuando** envía el autorregistro
- **Entonces** el sistema crea la cuenta con esos datos y sin residencia registrada

#### Scenario: SC-IDENTIDAD-04 Segundo nombre y segundo apellido omitidos

- **Dado** un visitante que omite el segundo nombre y el segundo apellido
- **Cuando** envía el autorregistro con el resto de los datos válidos
- **Entonces** el sistema crea la cuenta

#### Scenario: SC-IDENTIDAD-05 Falta un dato obligatorio

- **Dado** un visitante que omite el tipo de documento, el número de documento o la fecha de nacimiento
- **Cuando** envía el autorregistro
- **Entonces** el sistema rechaza el alta con un error de validación que identifica el campo faltante y no crea la cuenta

### Requirement: REQ-IDENTIDAD-03 Rol inicial Presupuestador

El sistema DEBE (MUST) crear toda cuenta autorregistrada con el rol Presupuestador y sin compañía ni proyecto asignados. Citas: RF-23; D-72, D-75.

#### Scenario: SC-IDENTIDAD-06 Rol inicial de la cuenta

- **Dado** un autorregistro válido
- **Cuando** el sistema crea la cuenta
- **Entonces** la cuenta queda con rol Presupuestador y sin compañía ni proyecto

#### Scenario: SC-IDENTIDAD-07 Intento de nacer con otro rol

- **Dado** un visitante que declara el rol Administrador o Director de proyectos en el autorregistro
- **Cuando** el sistema procesa la solicitud
- **Entonces** la rechaza y ninguna cuenta queda con esos roles

### Requirement: REQ-IDENTIDAD-04 Unicidad de correo y nombre de usuario

El sistema DEBE (MUST) garantizar la unicidad del correo electrónico y del nombre de usuario en toda la plataforma, sin distinguir mayúsculas de minúsculas. Citas: RF-01, RF-23; R-25; D-73, D-77.

#### Scenario: SC-IDENTIDAD-08 Correo ya registrado

- **Dado** una cuenta existente con el correo `persona@dominio.test`
- **Cuando** un visitante intenta autorregistrarse con ese mismo correo
- **Entonces** el sistema rechaza el alta con conflicto y no crea la cuenta

#### Scenario: SC-IDENTIDAD-09 Nombre de usuario ya registrado con otra capitalización

- **Dado** una cuenta existente con el nombre de usuario `Presupuesto1`
- **Cuando** un visitante intenta autorregistrarse con `presupuesto1`
- **Entonces** el sistema rechaza el alta con conflicto y no crea la cuenta

#### Scenario: SC-IDENTIDAD-10 Dos registros simultáneos con el mismo nombre de usuario

- **Dado** dos solicitudes de autorregistro simultáneas con el mismo nombre de usuario
- **Cuando** el sistema las procesa
- **Entonces** crea exactamente una cuenta y la otra solicitud recibe conflicto

### Requirement: REQ-IDENTIDAD-05 Documento de identidad no único

El sistema DEBE (MUST) aceptar cuentas distintas que declaren el mismo tipo y número de documento de identidad, sin devolver conflicto y sin generar un evento de seguridad. Citas: RF-01; R-30; D-77.

#### Scenario: SC-IDENTIDAD-11 Dos usuarios con el mismo documento

- **Dado** una cuenta con perfil completo y documento `CC 12345678`
- **Cuando** otra cuenta declara el mismo tipo y número de documento
- **Entonces** el sistema guarda el perfil sin conflicto y ambas cuentas quedan activas

#### Scenario: SC-IDENTIDAD-12 El documento compartido no comparte datos

- **Dado** dos cuentas con el mismo número de documento
- **Cuando** una de ellas consulta su perfil
- **Entonces** solo obtiene sus propios datos y nunca los de la otra cuenta

### Requirement: REQ-IDENTIDAD-06 Límite de intentos por IP en el autorregistro

El sistema DEBE (MUST) limitar los intentos de autorregistro por dirección IP y rechazar los intentos que superen el límite vigente, sin crear la cuenta. Citas: RF-23; D-80.

#### Scenario: SC-IDENTIDAD-13 Intentos dentro del límite

- **Dado** una dirección IP que no ha superado el límite de intentos
- **Cuando** envía un autorregistro válido
- **Entonces** el sistema crea la cuenta con normalidad

#### Scenario: SC-IDENTIDAD-14 Superación del límite por IP

- **Dado** una dirección IP que ya superó el límite de intentos
- **Cuando** envía un autorregistro válido
- **Entonces** el sistema rechaza la solicitud con un error de límite excedido y no crea la cuenta

### Requirement: REQ-IDENTIDAD-07 Verificación de correo previa a la habilitación

El sistema DEBE (MUST) mantener la cuenta autorregistrada sin habilitar hasta que se verifique el correo electrónico declarado, y rechazar la autenticación mientras no esté verificada. Citas: RF-23; D-80.

#### Scenario: SC-IDENTIDAD-15 Verificación del correo habilita la cuenta

- **Dado** una cuenta autorregistrada cuyo correo no está verificado
- **Cuando** la persona confirma la verificación enviada a su correo
- **Entonces** la cuenta queda habilitada y puede iniciar sesión

#### Scenario: SC-IDENTIDAD-16 Autenticación con correo sin verificar

- **Dado** una cuenta cuyo correo no está verificado
- **Cuando** intenta iniciar sesión con credenciales correctas
- **Entonces** el sistema rechaza el acceso como cuenta no habilitada y registra el evento

### Requirement: REQ-IDENTIDAD-08 Perfil completo obligatorio antes de operar

El sistema DEBE (MUST) exigir el perfil completo —incluida la residencia, que no se solicita en el autorregistro— antes de que la cuenta opere dentro de una compañía. Citas: RF-01, RF-23; D-76, D-78.

#### Scenario: SC-IDENTIDAD-17 Usuario con perfil completo asignado a compañía

- **Dado** un usuario con perfil completo, incluida su residencia, y una compañía asignada
- **Cuando** opera sobre los datos de su compañía
- **Entonces** el sistema autoriza la operación

#### Scenario: SC-IDENTIDAD-18 Usuario sin residencia intenta operar

- **Dado** un usuario autorregistrado que todavía no declaró su residencia
- **Cuando** intenta operar sobre datos de una compañía
- **Entonces** el sistema rechaza la operación con un error de perfil incompleto

#### Scenario: SC-IDENTIDAD-19 Perfil con residencia incoherente

- **Dado** un usuario que declara un municipio que no pertenece al departamento indicado
- **Cuando** guarda su perfil
- **Entonces** el sistema rechaza el perfil con un error de validación territorial

### Requirement: REQ-IDENTIDAD-09 Alta administrativa exclusiva del Administrador

El sistema DEBE (MUST) permitir la creación administrativa de usuarios únicamente al Administrador, con el rol y la compañía declarados por él. Citas: RF-01; D-34, D-82; matriz `1.3.2`.

#### Scenario: SC-IDENTIDAD-20 Alta administrativa de un usuario

- **Dado** un Administrador autenticado
- **Cuando** crea un usuario indicando rol y compañía válidos
- **Entonces** el sistema crea la cuenta con el rol y la compañía indicados

#### Scenario: SC-IDENTIDAD-21 Otro rol intenta el alta administrativa

- **Dado** un Director de proyectos, un Presupuestador o un Auditor autenticado
- **Cuando** intenta crear un usuario por la vía administrativa
- **Entonces** el sistema rechaza la operación con `403` y registra el evento de seguridad

### Requirement: REQ-IDENTIDAD-10 Búsqueda de usuarios según la matriz

El sistema DEBE (MUST) permitir la búsqueda de usuarios al Administrador sobre toda la plataforma, al Director sobre su compañía y al Auditor en solo lectura sobre la suya. Citas: RF-01; D-34, D-84; matriz `1.3.2`.

#### Scenario: SC-IDENTIDAD-22 Búsqueda del Director en su compañía

- **Dado** un Director de proyectos autenticado
- **Cuando** busca usuarios por correo, nombre de usuario o documento
- **Entonces** obtiene solo usuarios de su compañía, sin importar los filtros aplicados

#### Scenario: SC-IDENTIDAD-23 El Director busca en otra compañía

- **Dado** un Director de proyectos de la compañía A
- **Cuando** solicita usuarios de la compañía B
- **Entonces** el sistema rechaza la solicitud sin revelar si la compañía B existe

#### Scenario: SC-IDENTIDAD-24 El Presupuestador intenta buscar usuarios

- **Dado** un Presupuestador autenticado
- **Cuando** intenta buscar usuarios
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-IDENTIDAD-11 Cambio de rol según la matriz

El sistema DEBE (MUST) permitir el cambio de rol al Administrador sobre cualquier usuario y al Director solo dentro de su compañía y solo entre Director, Presupuestador y Auditor. Citas: RF-01; D-21, D-34; matriz `1.3.2`.

#### Scenario: SC-IDENTIDAD-25 El Administrador cambia un rol

- **Dado** un Administrador autenticado y un usuario de cualquier compañía
- **Cuando** cambia su rol a uno de los cuatro roles base
- **Entonces** el sistema aplica el cambio y registra el evento de seguridad

#### Scenario: SC-IDENTIDAD-26 El Director asigna el rol Administrador

- **Dado** un Director de proyectos autenticado
- **Cuando** intenta cambiar el rol de un usuario de su compañía a Administrador
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-IDENTIDAD-27 El Director cambia el rol de otra compañía

- **Dado** un Director de proyectos de la compañía A
- **Cuando** intenta cambiar el rol de un usuario de la compañía B
- **Entonces** el sistema rechaza la operación sin revelar los datos del usuario

### Requirement: REQ-IDENTIDAD-12 Asignación de usuario a compañía

El sistema DEBE (MUST) mantener a cada usuario en una sola compañía vigente y permitir la asignación solo al Administrador y al Director, y solo a usuarios que no pertenezcan ya a otra. La pertenencia vive en la tabla `users_company` (`user_id`, `company_id`, `account_enable`, `created_at`, `updated_at`): las bajas cierran la fila (`account_enable = 0`) sin borrarla, de modo que se conserva el historial de compañías del usuario; al re-vincularlo a la misma compañía se reactiva la fila existente y, si cambia de compañía, se inserta una fila nueva. Citas: RF-01; BR-001; R-09, R-10; D-01, D-02, D-98.

#### Scenario: SC-IDENTIDAD-28 Asignación de un usuario sin compañía

- **Dado** un usuario autorregistrado sin compañía
- **Cuando** un Administrador o el Director de la compañía destino lo asigna a esa compañía
- **Entonces** el usuario queda con esa única compañía

#### Scenario: SC-IDENTIDAD-29 Usuario que ya pertenece a una compañía

- **Dado** un usuario que ya pertenece a la compañía A
- **Cuando** se intenta asignarlo a la compañía B
- **Entonces** el sistema rechaza la asignación y el usuario permanece en la compañía A

#### Scenario: SC-IDENTIDAD-30 Asignación del Administrador a una compañía

- **Dado** un usuario con rol Administrador
- **Cuando** se intenta asignarlo a cualquier compañía
- **Entonces** el sistema rechaza la asignación

#### Scenario: SC-IDENTIDAD-31 Dos asignaciones simultáneas a compañías distintas

- **Dado** dos solicitudes simultáneas que asignan al mismo usuario a compañías distintas
- **Cuando** el sistema las procesa
- **Entonces** solo una asignación queda efectiva y la otra es rechazada

### Requirement: REQ-IDENTIDAD-13 Creación de compañía

El sistema DEBE (MUST) permitir al Administrador crear compañías sin límite y al Director crear una única compañía propia y solo si no pertenece a ninguna. Citas: RF-02; D-01; matriz `1.3.2`.

#### Scenario: SC-IDENTIDAD-32 El Administrador crea compañías

- **Dado** un Administrador autenticado
- **Cuando** crea una compañía con datos válidos, más de una vez
- **Entonces** el sistema crea todas las compañías solicitadas

#### Scenario: SC-IDENTIDAD-33 El Director crea su primera compañía

- **Dado** un Director de proyectos sin compañía
- **Cuando** crea una compañía con datos válidos
- **Entonces** el sistema la crea y lo vincula como Director de esa compañía

#### Scenario: SC-IDENTIDAD-34 El Director con compañía intenta crear otra

- **Dado** un Director de proyectos que ya pertenece a una compañía
- **Cuando** intenta crear una segunda compañía
- **Entonces** el sistema rechaza la creación

### Requirement: REQ-IDENTIDAD-14 Datos de registro de la compañía

El sistema DEBE (MUST) registrar la compañía con tipo, ubicación territorial coherente, identificación fiscal única y representante legal por referencia de usuario o por datos manuales. Citas: RF-02; D-87, D-96.

#### Scenario: SC-IDENTIDAD-35 Alta con representante legal manual

- **Dado** un actor autorizado que registra una compañía con los datos del representante legal escritos manualmente
- **Cuando** confirma el alta
- **Entonces** el sistema guarda la compañía sin exigir que el representante tenga cuenta

#### Scenario: SC-IDENTIDAD-36 Alta con representante legal por referencia

- **Dado** un actor autorizado que referencia a un usuario registrado como representante legal
- **Cuando** confirma el alta
- **Entonces** el sistema guarda la compañía con esa referencia

#### Scenario: SC-IDENTIDAD-37 Compañía pública sin nivel territorial

- **Dado** un actor autorizado que registra una compañía de tipo pública sin nivel territorial
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza el alta con un error de validación

#### Scenario: SC-IDENTIDAD-38 Municipio fuera del departamento declarado

- **Dado** un actor autorizado que declara un municipio que no pertenece al departamento indicado
- **Cuando** confirma el alta
- **Entonces** el sistema rechaza el alta con un error de coherencia territorial

### Requirement: REQ-IDENTIDAD-15 Edición de datos de registro de la compañía

El sistema DEBE (MUST) permitir editar los datos de registro de la compañía al Administrador sobre cualquier compañía y al Director solo sobre la propia, conservando la última modificación. Citas: RF-02; D-15, D-87.

#### Scenario: SC-IDENTIDAD-39 El Administrador edita una compañía

- **Dado** un Administrador autenticado y una compañía existente
- **Cuando** actualiza sus datos de registro
- **Entonces** el sistema guarda los cambios con el usuario y la fecha de la última modificación

#### Scenario: SC-IDENTIDAD-40 El Director edita otra compañía

- **Dado** un Director de proyectos de la compañía A
- **Cuando** intenta editar los datos de la compañía B
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-IDENTIDAD-41 El Presupuestador intenta editar la compañía

- **Dado** un Presupuestador autenticado
- **Cuando** intenta editar los datos de su compañía
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-IDENTIDAD-16 Inhabilitación y reactivación de la compañía

El sistema DEBE (MUST) permitir inhabilitar y reactivar compañías al Administrador sobre cualquiera y al Director solo sobre la propia, sin borrado físico de sus datos. Citas: RF-02; D-87.

#### Scenario: SC-IDENTIDAD-42 El Administrador inhabilita una compañía

- **Dado** un Administrador autenticado y una compañía habilitada
- **Cuando** la inhabilita
- **Entonces** la compañía queda inhabilitada y sus datos se conservan

#### Scenario: SC-IDENTIDAD-43 El Director inhabilita una compañía ajena

- **Dado** un Director de proyectos de la compañía A
- **Cuando** intenta inhabilitar la compañía B
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-IDENTIDAD-44 Operación sobre una compañía inhabilitada

- **Dado** una compañía inhabilitada y un usuario no autorizado sobre ella
- **Cuando** intenta operar sobre sus datos
- **Entonces** el sistema rechaza la operación e indica que la compañía no está habilitada

#### Scenario: SC-IDENTIDAD-45 Reactivación de la compañía

- **Dado** una compañía inhabilitada
- **Cuando** el Administrador o su Director la reactiva
- **Entonces** la compañía queda habilitada y los usuarios con pertenencia activa recuperan el acceso

### Requirement: REQ-IDENTIDAD-17 Autorización por permiso activo

El sistema DEBE (MUST) decidir la autorización de toda operación privada por la existencia de un permiso activo asociado al rol vigente del actor, no por listas fijas de roles. Citas: RF-01; D-82.

#### Scenario: SC-IDENTIDAD-46 Permiso activo autoriza la operación

- **Dado** un usuario cuyo rol tiene activo el permiso requerido por la operación
- **Cuando** ejecuta la operación
- **Entonces** el sistema la autoriza

#### Scenario: SC-IDENTIDAD-47 Permiso inexistente o inactivo

- **Dado** un usuario cuyo rol no tiene el permiso requerido o lo tiene revocado
- **Cuando** ejecuta la operación
- **Entonces** el sistema la rechaza con `403` aunque el rol figure en la matriz de referencia

### Requirement: REQ-IDENTIDAD-18 Política fail-closed

El sistema DEBE (MUST) rechazar con `403` toda operación privada cuando el rol del actor esté inactivo o falte el permiso requerido, y registrar el evento de seguridad correspondiente. Citas: RF-01; D-82, D-111.

#### Scenario: SC-IDENTIDAD-48 Rol inactivo

- **Dado** un usuario cuyo rol está inactivo
- **Cuando** ejecuta una operación privada
- **Entonces** el sistema responde `403` y registra la denegación

#### Scenario: SC-IDENTIDAD-49 Permiso revocado

- **Dado** un usuario cuyo rol tenía el permiso y la asignación fue revocada
- **Cuando** ejecuta la operación
- **Entonces** el sistema responde `403` sin crear ni reactivar la asignación de permiso

### Requirement: REQ-IDENTIDAD-19 Cuatro roles base inmutables

El sistema DEBE (MUST) mantener exactamente cuatro roles base —Administrador, Director de proyectos, Presupuestador y Auditor— con el código `DIRECTOR` para la dirección de proyectos, sin alta, renombrado ni desactivación. Citas: RF-01; D-74, D-82.

#### Scenario: SC-IDENTIDAD-50 Catálogo de roles con el nombre canónico

- **Dado** un actor que consulta el catálogo de roles
- **Cuando** el sistema responde
- **Entonces** aparecen los cuatro roles base y el rol de dirección de proyectos se identifica como Director de proyectos con código `DIRECTOR`

#### Scenario: SC-IDENTIDAD-51 Intento de modificar un rol base

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, renombrar o desactivar un rol base
- **Entonces** el sistema rechaza la operación

#### Scenario: SC-IDENTIDAD-52 El cliente envía el nombre histórico del rol

- **Dado** un cliente que envía el término "Gerente de proyectos" al asignar un rol
- **Cuando** el sistema valida la solicitud
- **Entonces** la rechaza como valor inexistente y no asigna ningún rol

### Requirement: REQ-IDENTIDAD-20 Aislamiento entre compañías

El sistema DEBE (MUST) impedir que un usuario lea o modifique datos de una compañía distinta de la suya y responder sin revelar la existencia del recurso. Citas: RF-01; R-10, R-14; D-36.

#### Scenario: SC-IDENTIDAD-53 Acceso a un recurso de otra compañía

- **Dado** un usuario autenticado de la compañía A
- **Cuando** solicita un recurso de la compañía B por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el recurso existe

#### Scenario: SC-IDENTIDAD-54 Usuario sin compañía consulta datos de compañía

- **Dado** un usuario autorregistrado sin compañía asignada
- **Cuando** intenta consultar usuarios, proyectos o compañías
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-IDENTIDAD-21 Eventos de seguridad registrados

El sistema DEBE (MUST) registrar un evento de seguridad para el inicio de sesión fallido, el bloqueo de cuenta, la denegación de autorización, el bootstrap y el cambio de rol, sin almacenar valores de negocio. Citas: RF-01; D-111.

#### Scenario: SC-IDENTIDAD-55 Denegación de autorización registrada

- **Dado** una operación privada rechazada por falta de permiso
- **Cuando** el sistema completa la respuesta
- **Entonces** existe un evento de seguridad con actor, acción, objetivo, fecha y resultado

#### Scenario: SC-IDENTIDAD-56 El evento no contiene valores de negocio

- **Dado** un cambio de rol registrado como evento de seguridad
- **Cuando** se consultan sus datos
- **Entonces** no aparecen precios, composiciones ni otros valores de negocio

### Requirement: REQ-IDENTIDAD-22 Bloqueo por intentos fallidos

El sistema DEBE (MUST) bloquear la cuenta al alcanzar los cinco intentos fallidos consecutivos y rechazar la autenticación hasta que se restablezca el acceso. Citas: RF-01; D-111.

#### Scenario: SC-IDENTIDAD-57 Alcanzar los cinco intentos bloquea la cuenta

- **Dado** una cuenta que acumula cinco intentos fallidos consecutivos
- **Cuando** se intenta un nuevo inicio de sesión
- **Entonces** el sistema rechaza el acceso e informa que la cuenta está bloqueada

#### Scenario: SC-IDENTIDAD-58 Credenciales correctas en cuenta bloqueada

- **Dado** una cuenta bloqueada por intentos fallidos
- **Cuando** se intenta iniciar sesión con la contraseña correcta
- **Entonces** el sistema mantiene el bloqueo, rechaza el acceso y registra el evento

### Requirement: REQ-IDENTIDAD-23 Hash de contraseña provisto por Identity

El sistema DEBE (MUST) almacenar las contraseñas con el hash provisto por ASP.NET Core Identity y sustituir cualquier hash heredado mediante rehash en el siguiente inicio de sesión correcto. Citas: RF-01; D-81, D-104.

#### Scenario: SC-IDENTIDAD-59 Contraseña almacenada como hash

- **Dado** un alta de cuenta con contraseña válida
- **Cuando** el sistema persiste la credencial
- **Entonces** solo guarda el hash provisto por Identity y la contraseña nunca se recupera en claro

#### Scenario: SC-IDENTIDAD-60 Usuario con hash heredado inicia sesión

- **Dado** una cuenta cuyo hash fue generado con el algoritmo anterior
- **Cuando** inicia sesión con la contraseña correcta
- **Entonces** el sistema la autentica y reemplaza el hash por el vigente

#### Scenario: SC-IDENTIDAD-61 Contraseña en claro en una respuesta o en el registro

- **Dado** un alta o un cambio de contraseña
- **Cuando** el sistema responde y registra eventos
- **Entonces** la contraseña no aparece en la respuesta, en los eventos de seguridad ni en los registros técnicos

### Requirement: REQ-IDENTIDAD-24 Correo de creación de cuenta

El sistema DEBE (MUST) enviar al correo declarado un único mensaje de creación de cuenta al dar de alta al usuario. Citas: RF-18; D-17, D-79.

#### Scenario: SC-IDENTIDAD-62 Alta con envío del correo de creación

- **Dado** un alta de cuenta por autorregistro o por vía administrativa
- **Cuando** la cuenta queda creada
- **Entonces** el sistema envía un mensaje de creación de cuenta al correo declarado

#### Scenario: SC-IDENTIDAD-63 El correo no incluye la contraseña

- **Dado** el mensaje de creación de cuenta enviado
- **Cuando** se revisa su contenido
- **Entonces** no incluye la contraseña ni un hash de ella

### Requirement: REQ-IDENTIDAD-25 Recuperación de contraseña con respaldo asistido

El sistema DEBE (MUST) permitir la recuperación de contraseña por correo y, cuando el correo no esté disponible, el respaldo asistido del Administrador, sin que este conozca la nueva contraseña. Citas: RF-18; D-17, D-79.

#### Scenario: SC-IDENTIDAD-64 Recuperación por correo

- **Dado** una cuenta con correo verificado
- **Cuando** la persona solicita la recuperación de su contraseña
- **Entonces** el sistema envía el mensaje de recuperación y permite establecer una contraseña nueva que cumpla la política vigente

#### Scenario: SC-IDENTIDAD-65 Respaldo asistido por el Administrador

- **Dado** una cuenta cuyo correo no está disponible
- **Cuando** el Administrador habilita la recuperación asistida
- **Entonces** el usuario puede establecer su contraseña nueva sin que el Administrador la conozca ni la fije

#### Scenario: SC-IDENTIDAD-66 Otro rol intenta habilitar el respaldo asistido

- **Dado** un Director de proyectos, un Presupuestador o un Auditor autenticado
- **Cuando** intenta habilitar la recuperación asistida de otra cuenta
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-IDENTIDAD-26 Solo dos correos transaccionales

El sistema DEBE (MUST) limitar el envío de correo a la creación de cuenta y a la recuperación de contraseña, sin mensajes por otros eventos. Citas: RF-18; E-09; D-17.

#### Scenario: SC-IDENTIDAD-67 Los dos eventos permitidos generan correo

- **Dado** el alta de una cuenta y una solicitud de recuperación de contraseña
- **Cuando** el sistema procesa cada evento
- **Entonces** envía un único mensaje por evento

#### Scenario: SC-IDENTIDAD-68 Un evento distinto no genera correo

- **Dado** un cambio de rol, una inhabilitación de compañía o un rechazo de autorización
- **Cuando** el sistema lo procesa
- **Entonces** no envía ningún correo

### Requirement: REQ-IDENTIDAD-27 Bootstrap del primer Administrador

El sistema DEBE (MUST) crear el primer Administrador por semilla en local o por bootstrap idempotente y auditado en demo, con ambos caminos excluyentes y el bootstrap cerrado al existir un Administrador. Citas: RF-01; D-110.

#### Scenario: SC-IDENTIDAD-69 Bootstrap sin Administrador previo

- **Dado** un entorno sin ningún usuario con rol Administrador
- **Cuando** se ejecuta el bootstrap habilitado
- **Entonces** el sistema crea exactamente un Administrador sin compañía y registra el evento

#### Scenario: SC-IDENTIDAD-70 Segundo intento de bootstrap

- **Dado** un entorno que ya tiene un Administrador
- **Cuando** se ejecuta de nuevo el bootstrap
- **Entonces** el sistema rechaza la operación y no crea ninguna cuenta

#### Scenario: SC-IDENTIDAD-71 Semilla local y bootstrap habilitados a la vez

- **Dado** un entorno con la semilla local y el bootstrap de demo habilitados simultáneamente
- **Cuando** el sistema arranca
- **Entonces** rechaza la configuración por ser excluyente

### Requirement: REQ-IDENTIDAD-28 Perfil propio del usuario

El sistema DEBE (MUST) permitir que todo usuario autenticado consulte y actualice su propio perfil y cambie su propia contraseña, y ningún rol edita el perfil de terceros salvo el Administrador (`REQ-IDENTIDAD-29`). Citas: RF-01; D-34.

#### Scenario: SC-IDENTIDAD-72 Actualización del perfil propio

- **Dado** un usuario autenticado de cualquier rol
- **Cuando** actualiza los datos de su propio perfil o cambia su contraseña
- **Entonces** el sistema guarda el cambio y conserva sus roles y su compañía

#### Scenario: SC-IDENTIDAD-73 Intento de editar el perfil de otro usuario

- **Dado** un usuario autenticado que no es Administrador
- **Cuando** intenta modificar el perfil o la contraseña de otra cuenta
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-IDENTIDAD-29 Edición y desactivación de usuarios

El sistema DEBE (MUST) permitir únicamente al Administrador editar los datos de un usuario y desactivar su cuenta, sin borrado físico, y rechazar esa edición a cualquier otro rol. Citas: RF-01; D-34.

#### Scenario: SC-IDENTIDAD-74 El Administrador edita y desactiva una cuenta

- **Dado** un Administrador autenticado y un usuario existente
- **Cuando** edita sus datos y después desactiva su cuenta
- **Entonces** el sistema guarda los cambios y la cuenta desactivada deja de poder iniciar sesión

#### Scenario: SC-IDENTIDAD-75 Otro rol intenta editar o desactivar una cuenta

- **Dado** un Director de proyectos, un Presupuestador o un Auditor autenticado
- **Cuando** intenta editar o desactivar la cuenta de otro usuario
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-IDENTIDAD-76 La cuenta desactivada conserva sus datos

- **Dado** una cuenta desactivada por el Administrador
- **Cuando** el Administrador la consulta
- **Entonces** el sistema la devuelve con sus datos y en estado desactivado
