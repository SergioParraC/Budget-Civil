# Feature Specification: Gestion de companias y proyectos

**Feature Branch**: `[002-gestion-companias-proyectos]`

**Created**: 2026-09-28

**Status**: Draft

**Input**: User description: "Companias y proyectos. Toma en cuenta la totalidad del contenido del archivo prompt file.md para realizar esta especificacion"

## Clarifications

### Session 2026-09-29

- Q: Como se realizan los cambios de estado del proyecto en el ciclo de vida? → A: Solo usuarios autorizados pueden cambiarlos de forma manual; no hay cambios automaticos por edicion, excepto el estado inicial `POR_COTIZAR` al crear el proyecto.
- Q: Que roles pueden cambiar cada estado del proyecto? → A: Matriz por estado: `FALTA_INFORMACION` y `COTIZADO` por Director de proyectos o Presupuestador; `FINALIZADO` e `INHABILITADO` solo por Director de proyectos.
- Q: Como sale un proyecto del estado `INHABILITADO`? → A: Solo Director de proyectos o Administrador pueden sacarlo de `INHABILITADO` y el nuevo estado siempre es `FALTA_INFORMACION`.
- Q: Cual es el flujo permitido de estados del proyecto? → A: Flujo estricto `POR_COTIZAR` -> `FALTA_INFORMACION` -> `COTIZADO` -> `FINALIZADO`, permitiendo reabrir `FINALIZADO` a `FALTA_INFORMACION` solo por Director de proyectos; y permitiendo cambiar a `INHABILITADO` desde cualquiera de las fases.

### Session 2026-09-30

- Q: Como se organiza la informacion del feature 002 para asegurar control y trazabilidad? → A: Se aprueba separar la informacion por dominios funcionales (catalogos, identidad/acceso, operacion y trazabilidad) y mantener los registros tecnicos apartados para monitoreo, preservando claridad y control de responsabilidades.
- Q: Que regla de integridad aplica entre dominios de informacion? → A: Se aprueba una politica de integridad con dependencias en un unico sentido para evitar acoplamientos circulares y proteger la consistencia de los datos de negocio.
- Q: Como se gestionan los cambios estructurales de datos? → A: Se aprueba una estrategia controlada, secuenciada y auditable para evolucionar la estructura de datos, con convenciones operativas documentadas en el plan tecnico.
- Q: Como se presentara esta especificacion para stakeholders no tecnicos? → A: Se acuerda mantener lenguaje de negocio en la especificacion y concentrar el detalle tecnico de implementacion en el plan.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gestion del ciclo de vida de companias (Priority: P1)

Como Administrador o Director de proyectos, quiero crear, editar, inhabilitar y rehabilitar companias para controlar que organizaciones pueden operar y mantener trazabilidad completa.

**Why this priority**: Sin companias correctamente gestionadas no existe aislamiento organizacional ni base para asignaciones de usuarios y proyectos.

**Independent Test**: Se valida creando una compania, editandola, inhabilitandola y rehabilitandola, comprobando trazabilidad, restricciones de acceso y conservacion historica.

**Acceptance Scenarios**:

1. **Given** un usuario autenticado con rol habilitado para gestion de companias, **When** registra una compania con datos obligatorios validos, **Then** la compania queda creada con estado habilitada y con registro del usuario creador y fecha de creacion.
2. **Given** una compania existente, **When** un actor autorizado actualiza datos permitidos, **Then** el sistema guarda los cambios y actualiza solo el ultimo usuario y fecha de modificacion.
3. **Given** una compania habilitada con usuarios vinculados, **When** un actor autorizado la inhabilita, **Then** la compania y sus datos derivados dejan de ser accesibles para usuarios no autorizados sin eliminar informacion historica.
4. **Given** una compania inhabilitada, **When** un usuario no autorizado intenta acceder, **Then** recibe una observacion indicando que su compania actual no esta habilitada y que debe ser reasignado.
5. **Given** una compania inhabilitada, **When** un Administrador o Director de proyectos la rehabilita, **Then** vuelve a ser accesible para usuarios activos vinculados a esa compania.

---

### User Story 2 - Gestion del ciclo de vida de proyectos y estados (Priority: P1)

Como Director de proyectos o Presupuestador, quiero administrar proyectos con estados controlados para reflejar avance real de cotizacion, aprobacion y cierre.

**Why this priority**: El proyecto es el eje operativo del negocio; sin estados confiables no se puede controlar avance, firma ni cierre.

**Independent Test**: Se valida creando proyecto, editandolo, moviendo estados segun reglas y verificando restricciones de visibilidad para proyectos inhabilitados.

**Acceptance Scenarios**:

1. **Given** una compania con al menos un Director de proyectos activo, **When** un actor autorizado crea un proyecto, **Then** el proyecto inicia en estado `POR_COTIZAR`.
2. **Given** un proyecto existente en estado distinto de `INHABILITADO`, **When** un Director de proyectos o Presupuestador autorizado ejecuta de forma explicita el cambio de estado a `FALTA_INFORMACION` despues de editar informacion relevante, **Then** el proyecto queda en estado `FALTA_INFORMACION`.
3. **Given** un proyecto con informacion completa y todos los insumos en estado cotizado, **When** un Director de proyectos o Presupuestador autorizado ejecuta el cambio, **Then** el proyecto pasa a estado `COTIZADO`.
4. **Given** un proyecto en estado `COTIZADO`, **When** se cumplen reglas de firma configuradas y un Director de proyectos autorizado tiene firma cargada, **Then** el proyecto pasa a `FINALIZADO`.
5. **Given** un proyecto en cualquier fase del ciclo de vida, **When** un Director de proyectos autorizado ejecuta el cambio a `INHABILITADO`, **Then** el proyecto queda inhabilitado y aplica la restriccion de visualizacion por rol definida.
6. **Given** un proyecto en estado `INHABILITADO`, **When** un usuario intenta consultarlo, **Then** solo Administrador y Director de proyectos pueden verlo junto con los APUs asignados.
7. **Given** un proyecto en estado `INHABILITADO`, **When** un Director de proyectos o Administrador autorizado ejecuta la reactivacion, **Then** el proyecto sale de `INHABILITADO` y pasa a `FALTA_INFORMACION`.
8. **Given** un proyecto en estado `FINALIZADO`, **When** un Director de proyectos autorizado ejecuta la reapertura, **Then** el proyecto pasa a `FALTA_INFORMACION`.

---

### User Story 3 - Asignaciones y permisos por rol en companias y proyectos (Priority: P1)

Como actor del sistema, quiero que cada operacion de vinculacion y administracion respete una matriz fija de permisos por rol para evitar accesos o cambios no autorizados.

**Why this priority**: El control de permisos garantiza seguridad funcional, segregacion de responsabilidades y cumplimiento de las reglas de negocio.

**Independent Test**: Se valida con una matriz de pruebas de autorizacion por rol sobre asignaciones de compania, asignaciones de proyecto, cambios de estado y consultas.

**Acceptance Scenarios**:

1. **Given** un usuario que no sea Administrador, **When** se vincula a compania, **Then** el sistema permite solo una compania activa por usuario.
2. **Given** un Administrador, **When** consulta companias, proyectos o usuarios, **Then** puede leer informacion global segun los listados definidos y sin restriccion por compania en lectura de proyectos.
3. **Given** un Director de proyectos, **When** vincula usuarios a compania o proyecto, **Then** solo puede vincular usuarios permitidos y dentro de su compania.
4. **Given** un Presupuestador, **When** intenta vincular usuarios a compania, **Then** la operacion es rechazada.
5. **Given** un Presupuestador, **When** vincula usuarios a proyecto, **Then** solo puede vincular o deshabilitar Presupuestadores y Auditores ya afiliados a su compania.
6. **Given** un Auditor, **When** intenta asignar usuarios a compania o proyecto, **Then** la operacion es rechazada y se mantiene acceso de solo lectura.

---

### User Story 4 - Listados reutilizables para operacion y administracion (Priority: P2)

Como usuario autorizado, quiero listados filtrables y paginados de catalogos, usuarios, companias y proyectos para operar el sistema sin ambiguedad y con reglas de visibilidad por rol.

**Why this priority**: Los listados son base para administracion diaria y para reutilizacion en modulos posteriores.

**Independent Test**: Se valida cada listado con filtros, paginacion configurable y restricciones por rol/compania.

**Acceptance Scenarios**:

1. **Given** un usuario autenticado, **When** consulta catalogos geograficos y monedas, **Then** obtiene resultados filtrables segun datos disponibles.
2. **Given** un Administrador, **When** lista todos los usuarios registrados, **Then** obtiene resultados con filtros y paginacion permitida.
3. **Given** un Administrador o Director de proyectos, **When** lista usuarios de una compania especifica, **Then** solo obtiene usuarios de esa compania con filtros y paginacion.
4. **Given** un Presupuestador o Director de proyectos, **When** lista usuarios de su compania, **Then** solo obtiene usuarios vinculados a su compania.
5. **Given** un Presupuestador o Auditor, **When** solicita el listado restringido por rol, **Then** solo visualiza usuarios con rol Presupuestador o Auditor de su compania.
6. **Given** un Administrador, **When** lista proyectos globales, **Then** puede ver proyectos de todas las companias con filtros y paginacion.
7. **Given** un Administrador o Director de proyectos, **When** lista proyectos por compania del contexto autenticado, **Then** obtiene solo proyectos permitidos para esa compania.
8. **Given** cualquier listado de este feature, **When** el usuario define tamano de pagina, **Then** solo se aceptan valores 10, 50 o 100.

---

### Edge Cases

- Intentar crear un proyecto cuando la compania no tiene Director de proyectos activo debe ser rechazado para todos los roles.
- Inhabilitar una compania con proyectos activos no elimina proyectos ni sus historiales; solo bloquea acceso operativo segun permisos.
- Rehabilitar una compania no reactiva vinculaciones de usuarios desactivadas manualmente; solo restaura acceso de vinculaciones activas.
- Si un proyecto cambia de moneda base, no debe ocurrir recalculo automatico de valores ya registrados.
- Si el representante legal no tiene cuenta en la plataforma, la compania debe poder guardar los datos manuales sin bloqueo.
- Si el representante legal coincide con un Director de proyectos registrado, se permite almacenar su IdUser sin validacion de identidad adicional.
- Cada cambio de estado de proyecto y cada cambio de visibilidad publica/privada deben generar registros de seguimiento independientes.
- En estado `INHABILITADO`, un proyecto no debe ser visible para Presupuestador ni Auditor aunque este asignado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST requerir sesion iniciada para acceder a modulos de usuarios, companias y proyectos.
- **FR-002**: El sistema MUST aplicar una matriz fija de autorizacion por rol con exactamente cuatro roles: Administrador, Director de proyectos, Presupuestador y Auditor.
- **FR-003**: El sistema MUST permitir crear companias con los atributos minimos obligatorios definidos para identificacion, ubicacion, tipo de compania y trazabilidad de creacion.
- **FR-004**: El sistema MUST permitir editar companias registrando solo la ultima modificacion (usuario y fecha).
- **FR-005**: El sistema MUST permitir inhabilitar companias sin borrado fisico de la compania ni de sus datos derivados.
- **FR-006**: El sistema MUST impedir acceso operativo a compania y datos asociados cuando la compania este inhabilitada para usuarios no autorizados.
- **FR-007**: El sistema MUST devolver una observacion clara cuando un usuario intente operar sobre una compania inhabilitada, incluyendo que puede ser reasignado a otra compania.
- **FR-008**: El sistema MUST permitir rehabilitar companias para restablecer acceso de usuarios con vinculaciones activas.
- **FR-009**: El sistema MUST restringir la accion de inhabilitar o rehabilitar companias a Administrador y Director de proyectos.
- **FR-010**: El sistema MUST exigir nivel territorial cuando la compania sea publica y permitir omitirlo cuando la compania sea privada.
- **FR-011**: El sistema MUST permitir almacenar representante legal de compania con dos modalidades: referencia a usuario registrado o captura manual de datos.
- **FR-012**: El sistema MUST permitir crear proyectos en estado inicial `POR_COTIZAR`.
- **FR-013**: El sistema MUST permitir cambiar el estado del proyecto a `FALTA_INFORMACION` solo mediante accion explicita de Director de proyectos o Presupuestador autorizados y MUST NOT realizar este cambio de forma automatica por edicion del proyecto.
- **FR-014**: El sistema MUST permitir pasar un proyecto a `COTIZADO` solo cuando se complete la informacion requerida y todos los insumos esten cotizados, y solo si la accion la ejecuta un Director de proyectos o Presupuestador autorizado.
- **FR-015**: El sistema MUST permitir pasar un proyecto a `FINALIZADO` solo cuando se cumplan reglas de firma configuradas y un Director de proyectos autorizado tenga firma cargada.
- **FR-016**: El sistema MUST soportar configuracion de firma de proyecto en dos modos: firma unica por Presupuestador asignado o firma independiente por profesional responsable.
- **FR-017**: El sistema MUST permitir que el identificador de usuario firmante del proyecto quede vacio cuando el modo configurado no requiera un unico firmante.
- **FR-018**: El sistema MUST soportar el estado `INHABILITADO` en proyectos, permitir la transicion a este estado solo por Director de proyectos autorizado y restringir su visualizacion a Administrador y Director de proyectos.
- **FR-019**: El sistema MUST registrar trazabilidad de cambios de estado del proyecto con estado anterior, estado nuevo, fecha y usuario que realizo el cambio.
- **FR-020**: El sistema MUST registrar trazabilidad separada para cambios de visibilidad del proyecto hacia usuarios fuera de la compania.
- **FR-021**: El sistema MUST rechazar creacion de proyecto para cualquier rol cuando no exista un Director de proyectos activo en la compania.
- **FR-022**: El sistema MUST asignar automaticamente como director encargado al Director de proyectos activo mas antiguo de la compania cuando un Presupuestador cree un proyecto.
- **FR-023**: El sistema MUST permitir una sola compania activa por usuario para Director de proyectos, Presupuestador y Auditor.
- **FR-024**: El sistema MUST impedir pertenencia de usuarios con rol Administrador a companias.
- **FR-025**: El sistema MUST permitir que Director de proyectos vincule a compania usuarios de rol Director de proyectos, Presupuestador y Auditor.
- **FR-026**: El sistema MUST impedir que Presupuestador vincule usuarios a compania.
- **FR-027**: El sistema MUST permitir que Director de proyectos vincule usuarios a proyecto solo si ya estan afiliados a la compania.
- **FR-028**: El sistema MUST permitir que Presupuestador vincule o deshabilite en proyecto solo usuarios Presupuestador o Auditor previamente afiliados a la compania.
- **FR-029**: El sistema MUST impedir que Auditor asigne usuarios tanto en compania como en proyecto.
- **FR-030**: El sistema MUST permitir que Administrador asigne unicamente Directores de proyectos a proyectos.
- **FR-031**: El sistema MUST permitir self-registration publico de usuarios.
- **FR-032**: El sistema MUST restringir la creacion de usuarios administrativos exclusivamente a usuarios con rol Administrador.
- **FR-033**: El sistema MUST permitir que el Administrador consulte informacion completa de usuarios registrados, sin habilitar edicion de perfil de terceros por parte del Administrador.
- **FR-034**: El sistema MUST ofrecer listados filtrables de paises, departamentos y municipios para su reutilizacion en formularios del sistema.
- **FR-035**: El sistema MUST ofrecer listado filtrable de monedas base desde catalogo fijo.
- **FR-036**: El sistema MUST ofrecer listado de usuarios registrados globales para Administrador con filtro y paginacion.
- **FR-037**: El sistema MUST ofrecer listado de usuarios por compania especifica para Administrador y Director de proyectos con filtro y paginacion.
- **FR-038**: El sistema MUST ofrecer listado de usuarios de la compania del usuario autenticado para Presupuestador y Director de proyectos con filtro y paginacion.
- **FR-039**: El sistema MUST ofrecer listado de usuarios de la compania del usuario autenticado filtrado a roles Presupuestador y Auditor para Presupuestador y Auditor.
- **FR-040**: El sistema MUST ofrecer listado global de proyectos para Administrador con filtro y paginacion.
- **FR-041**: El sistema MUST ofrecer listado de proyectos por compania del contexto autenticado para Administrador y Director de proyectos con filtro y paginacion.
- **FR-042**: El sistema MUST ofrecer listado de companias para Administrador con filtro y paginacion.
- **FR-043**: El sistema MUST limitar los tamanos de pagina permitidos en todos los listados de este feature a 10, 50 o 100 elementos por pagina.
- **FR-044**: El sistema MUST permitir salida de estado `INHABILITADO` solo por accion de Director de proyectos o Administrador, y MUST forzar que el estado destino sea exclusivamente `FALTA_INFORMACION`.
- **FR-045**: El sistema MUST aplicar flujo estricto de avance `POR_COTIZAR` -> `FALTA_INFORMACION` -> `COTIZADO` -> `FINALIZADO` para cambios ordinarios de estado.
- **FR-046**: El sistema MUST permitir reapertura de `FINALIZADO` a `FALTA_INFORMACION` solo por accion de Director de proyectos autorizado.
- **FR-047**: El sistema MUST permitir transicion a `INHABILITADO` desde cualquiera de las fases del ciclo de vida, exclusivamente por accion de Director de proyectos autorizado.
- **FR-048**: El sistema MUST organizar la informacion en dominios funcionales separados (catalogos, identidad/acceso, operacion y trazabilidad) y mantener registros tecnicos de observabilidad independientes de los datos operativos.
- **FR-049**: El sistema MUST aplicar reglas de integridad entre dominios de informacion que eviten dependencias cruzadas no permitidas y preserven la consistencia y gobernanza de los datos.
- **FR-050**: El sistema MUST gestionar los cambios estructurales de datos mediante un proceso controlado, secuenciado y auditable, con convenciones operativas documentadas fuera de la especificacion funcional.

### Key Entities *(include if feature involves data)*

- **Compania**: Organizacion operativa con datos legales, ubicacion, tipo (publica/privada), estado (habilitada/inhabilitada), representante legal y trazabilidad de creacion/modificacion.
- **TipoDocumento**: Catalogo de tipos documentales para identificar personas, incluyendo representante legal.
- **VinculacionCompaniaUsuario**: Relacion entre usuario y compania con creador de vinculacion, fechas de activacion/desactivacion y estado de vinculacion.
- **Proyecto**: Unidad de trabajo perteneciente a una compania, con datos contractuales, ubicacion, moneda base, parametros AIU, estado de ciclo de vida, visibilidad y configuracion de firma.
- **EstadoProyecto**: Catalogo de estados validos del proyecto (`POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`, `INHABILITADO`).
- **SeguimientoEstadoProyecto**: Historial de transiciones de estado y cambios de visibilidad del proyecto, con registro independiente por cada accion.
- **FirmaUsuario**: Registro de firma de usuario con nombre de firma e imagen digital para habilitar acciones de cierre.
- **VinculacionProyectoUsuario**: Relacion entre usuario y proyecto con datos de alta/baja y estado de vinculacion.
- **Pais**: Catalogo de paises.
- **Departamento**: Catalogo territorial asociado a un pais.
- **Municipio**: Catalogo territorial asociado a un departamento.
- **MonedaBase**: Catalogo fijo de monedas disponibles para proyectos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de accesos a modulos de usuarios, companias y proyectos requiere autenticacion valida.
- **SC-002**: El 100% de operaciones de inhabilitar/rehabilitar compania mantiene integridad historica sin eliminacion de datos existentes.
- **SC-003**: El 100% de intentos de crear proyecto sin Director de proyectos activo en la compania es rechazado con mensaje funcional claro.
- **SC-004**: Al menos el 98% de los casos de prueba de autorizacion por rol definidos para este feature se completan exitosamente en validacion funcional.
- **SC-005**: El 100% de transiciones de estado de proyecto y cambios de visibilidad queda registrado con actor y fecha en trazabilidad independiente.
- **SC-006**: El 100% de listados del feature valida correctamente tamanos de pagina permitidos (10, 50, 100) y rechaza valores fuera de ese conjunto.
- **SC-007**: El 100% de usuarios no administradores queda con maximo una compania activa durante pruebas de vinculacion.
- **SC-008**: El 100% de intentos de acceso a compania inhabilitada por usuarios no autorizados devuelve observacion de compania no habilitada.
- **SC-009**: El 100% de cambios de estado posteriores a la creacion del proyecto ocurre por accion explicita de usuario autorizado; no se generan cambios automaticos por edicion.
- **SC-010**: El 100% de transiciones a `COTIZADO` es ejecutado por Director de proyectos o Presupuestador autorizados, y el 100% de transiciones a `FINALIZADO` e `INHABILITADO` es ejecutado solo por Director de proyectos autorizado.
- **SC-011**: El 100% de salidas de `INHABILITADO` es ejecutado por Director de proyectos o Administrador autorizados y termina en estado `FALTA_INFORMACION`.
- **SC-012**: El 100% de reaperturas de `FINALIZADO` a `FALTA_INFORMACION` es ejecutado solo por Director de proyectos autorizado.
- **SC-013**: El 100% de transiciones a `INHABILITADO` desde cualquier fase del ciclo de vida es ejecutado solo por Director de proyectos autorizado.

## Assumptions

- El alcance actual se concentra en reglas y procesos de negocio del servicio; la experiencia visual de usuario queda fuera de esta fase.
- Se interpreta que "gerente de proyectos" y "director de proyectos" representan el mismo rol funcional dentro de este feature.
- En caso de conflicto documental sobre facultades del Administrador en companias, se adopta para este feature la matriz inicial declarada: Administrador puede crear, editar e inhabilitar companias.
- La regla "insumos en estado cotizado" para pasar a `COTIZADO` aplica al conjunto total de insumos requeridos por el proyecto.
- Los catalogos de paises, departamentos, municipios, tipos de documento y monedas existen previamente con datos suficientes para operacion.
- La captura y actualizacion de datos del representante legal desde datos de un Director de proyectos es una facilidad de experiencia de usuario y no una validacion obligatoria del servicio en este alcance.
- El cambio de moneda base de un proyecto no dispara conversiones automaticas ni recalculos de montos existentes.
