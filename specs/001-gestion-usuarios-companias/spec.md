# Feature Specification: Sistema de Usuarios y Panel de Administracion de Usuarios/Companias

**Feature Branch**: `[001-gestion-usuarios-companias]`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: "Sitema de usuarios - Gestion inicial de usuarios y panel de administracion de usuarios/companias"

## Source Of Truth & Scope *(mandatory)*

### Governing Sources

List all sources used, in priority order defined by the project constitution.

1. `.specify/memory/constitution.md`
2. `.github/spec/SPEC.md`
3. `.github/spec/clarify.md`
4. `.github/spec/tasks.md`
5. `.github/docs/02-requerimientos.md`
6. `.github/docs/03-arquitectura-general.md`
7. `.github/docs/05-modelo-datos.md`
8. `.github/docs/06-flujos-casos-de-uso.md`
9. `.github/docs/09-operacion-copilot-spec-kit.md`

### Scope Boundary

- **Implementation Surface (this iteration)**:
  - Backend only: API endpoints, reglas de negocio, seguridad/autorizacion, persistencia y contratos de integracion para consumo frontend.
  - Configuracion de conexion a base de datos centralizada en un unico punto de configuracion de la app backend (app config del proyecto de conexion BD).
  - No incluye implementacion de interfaces frontend en esta iteracion.

- **In Scope**:
  - Login de usuarios mediante username o email y emision de sesion autenticada.
  - Registro autonomo de usuarios (self-registration) con rol por defecto Presupuestador.
  - Registro autonomo sin asignacion de compania ni proyecto en el alta inicial.
  - Flujo de bootstrap para alta del primer usuario Administrador, administrado, auditado e idempotente.
  - Capacidades backend para panel de administracion de usuarios: crear, editar, desactivar (sin borrado fisico), asignar/cambiar rol y gestionar asignacion de compania segun matriz.
  - Capacidades backend para panel de administracion de companias: crear, editar y desactivar (sin borrado fisico).
  - Definicion de matriz de permisos para los cuatro roles base (Administrador, Gerente de proyectos, Presupuestador y Auditor) en alcance usuarios/companias.
  - Regla de autoservicio de cuenta y cambio de contrasena con limites de seguridad por rol.
  - Restricciones de multitenancy y privacidad por CompanyId en operaciones privadas.
  - Filtros de busqueda de usuarios por email, username, nombre/apellidos y numero de documento.
  - Reglas funcionales para bloqueo/habilitacion de cuentas y recuperacion asistida por Administrador.
  - Consumo autenticado de APUs publicos como capacidad existente de solo lectura (sin desarrollo funcional de APU en este feature).
- **Out Of Scope**:
  - Implementacion de vistas, componentes, rutas y estado de frontend para login, paneles y autoservicio.
  - Automatizacion de pruebas UI/E2E frontend en esta iteracion de backend.
  - Construccion completa del modulo de proyectos.
  - Construccion completa del modulo APU/EDS/insumos y cualquier capacidad funcional de escritura/edicion sobre APUs.
  - Calculo de costos APU, reglas de cascada de cotizacion y cierre de presupuesto.
  - Asignacion de proyecto en flujos de registro o alta de usuarios dentro de este feature.
  - Cualquier funcionalidad fuera de usuarios, autenticacion y administracion de usuarios/companias.
- **Out-of-Scope Authorization**: None

## Clarifications

### Session 2026-09-20

- Q: El rol Auditor puede crear usuarios o debe mantenerse en solo lectura estricta? -> A: El rol Auditor se mantiene en solo lectura estricta y no puede crear usuarios.
- Q: El modo solo lectura del Auditor incluye su propia cuenta o conserva autoservicio? -> A: El Auditor conserva autoservicio de su propia cuenta (perfil y contrasena); el solo lectura aplica a datos de terceros y operaciones administrativas.

### Session 2026-09-21

- Q: El control de autorizacion en endpoints privados debe evaluarse por permission_key (data-driven) en lugar de listas fijas de roles? -> A: Si, todos los endpoints privados validan permission_key; las listas fijas de roles quedan como referencia documental o para endpoints publicos especiales.
- Q: Los roles del sistema deben ser editables en esta fase o mantenerse como catalogo base inmutable? -> A: Se mantienen como catalogo base inmutable en BD; no hay CRUD de roles base en esta fase y la administracion se concentra en asignaciones de permisos por RolePermission.
- Q: Que debe ocurrir si el rol del usuario esta inactivo o falta/inactiva la asignacion RolePermission para el permission_key requerido? -> A: Politica fail-closed: acceso denegado con 403 y registro obligatorio de evento de seguridad/auditoria.
- Q: Como se debe gobernar el seed de Role/Permission/RolePermission en esta fase? -> A: Seed obligatorio, idempotente y versionado como baseline; no se crean permisos ni asignaciones implicitas en runtime.
- Q: Que arquitectura de pruebas se adopta en esta fase para el feature backend-only? -> A: Un unico proyecto BlackBoxTests para todo el feature; no se separa en ContractTests/IntegrationTests en esta fase.
- Q: Debe existir registro autonomo de usuario? -> A: Si, el feature incluye self-registration como flujo publico de alta.
- Q: Que rol recibe por defecto un usuario de registro autonomo? -> A: Presupuestador.
- Q: En registro autonomo se permite asignar compania o proyecto? -> A: No; ambas asignaciones estan prohibidas en el alta inicial.
- Q: Que puede hacer un usuario recien registrado de forma autonoma? -> A: Puede autenticarse y consultar APUs publicos como capacidad existente de solo lectura.
- Q: Quien puede asignar compania a un usuario? -> A: Administrador y Gerente de proyectos pueden asignar compania dentro de su alcance autorizado.
- Q: Se pueden crear roles adicionales en este feature? -> A: No; solo existen Administrador, Gerente de proyectos, Presupuestador y Auditor.
- Q: El feature implementa capacidades funcionales completas de APUs en esta iteracion? -> A: No; APUs publicos solo se consumen como capacidad existente y quedan fuera de implementacion funcional en este feature.

### Session 2026-09-22

- Q: La escalacion a Gerente de proyectos y la asignacion de compania deben quedar solo en Administrador o tambien en Gerente de proyectos? -> A: Administrador y Gerente de proyectos pueden escalar a Gerente de proyectos y asignar compania dentro de su alcance autorizado.
- Q: El Administrador puede consultar APUs e insumos o debe quedar sin acceso a esa informacion de compania? -> A: El Administrador no puede consultar APUs ni insumos; ese acceso queda para roles operativos segun alcance autorizado.
- Q: La asignacion usuario-proyecto debe entrar en este feature o mantenerse fuera de alcance? -> A: Se mantiene fuera de alcance en este feature; solo se documenta como restriccion y debe rechazarse en endpoints de usuarios/companias.
- Q: La creacion administrativa de usuarios debe permitirse a Gerente de proyectos y Presupuestador o quedar solo en Administrador? -> A: Se deshabilita para Gerente de proyectos y Presupuestador; la creacion administrativa de usuarios queda solo en Administrador y el resto de altas se canaliza por self-registration.
- Q: El Gerente de proyectos debe poder crear su propia compania en este feature? -> A: Si; puede crear exactamente una compania propia y editar solo esa compania, sin crear una segunda.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ingreso Seguro y Estado de Cuenta (Priority: P1)

Como usuario activo, quiero iniciar sesion con username o email para acceder a funcionalidades permitidas por mi rol y estado de cuenta.

**Why this priority**: Sin autenticacion no existe acceso confiable ni aislamiento por compania en el sistema.

**Independent Test**: Se valida de forma independiente probando registro autonomo, login exitoso/fallido, mensaje obligatorio para cuenta bloqueada o deshabilitada y acceso de solo lectura a APUs publicos existentes.

**Acceptance Scenarios**:

1. **Given** una cuenta activa con credenciales validas, **When** el usuario inicia sesion con email o username y contrasena correcta, **Then** obtiene acceso autenticado con sus permisos de rol.
2. **Given** una cuenta bloqueada o deshabilitada, **When** el usuario intenta iniciar sesion, **Then** el sistema muestra exactamente: "Este no se encuentra habilitado, solicite al administrador su habilitacion".
3. **Given** credenciales invalidas, **When** el usuario intenta iniciar sesion, **Then** el sistema niega acceso y registra el intento fallido.
4. **Given** cinco intentos fallidos consecutivos de autenticacion, **When** el usuario vuelve a intentar iniciar sesion sin rehabilitacion administrativa, **Then** la cuenta queda bloqueada y se aplica el mensaje obligatorio de cuenta no habilitada.
5. **Given** un visitante sin cuenta, **When** completa el registro autonomo con datos validos, **Then** el sistema crea la cuenta con rol Presupuestador y sin compania ni proyecto asignados.
6. **Given** un usuario recien registrado por self-registration, **When** inicia sesion con credenciales validas, **Then** puede consultar APUs publicos existentes en modo solo lectura.

---

### User Story 2 - Bootstrap del Primer Administrador (Priority: P1)

Como propietario inicial del sistema, quiero crear el primer Administrador una sola vez para habilitar la gestion formal de usuarios y companias.

**Why this priority**: Sin primer Administrador no hay gobierno del tenant ni operacion controlada del sistema.

**Independent Test**: Se valida ejecutando el flujo de bootstrap antes y despues de existir un Administrador y verificando auditoria e idempotencia.

**Acceptance Scenarios**:

1. **Given** que no existe ningun usuario con rol Administrador, **When** se ejecuta el flujo de bootstrap, **Then** se crea exactamente un primer Administrador y se registra auditoria completa del evento.
2. **Given** que ya existe al menos un Administrador, **When** se reejecuta bootstrap, **Then** el sistema rechaza la creacion de un nuevo Administrador y mantiene idempotencia.
3. **Given** un evento de bootstrap ejecutado, **When** un usuario Administrador consulta auditoria de bootstrap, **Then** visualiza actor, accion, objetivo, fecha/hora y resultado.

---

### User Story 3 - Administracion de Usuarios por Rol y Compania (Priority: P1)

Como usuario autenticado con permisos de creacion, quiero crear cuentas y, segun mi rol, gestionar las asignaciones administrativas permitidas para mantener acceso seguro por compania.

**Why this priority**: La continuidad operativa depende del control de altas, cambios y desactivaciones de usuarios.

**Independent Test**: Se valida creando, editando, desactivando y reasignando rol/compania en cuentas objetivo permitidas por rol y CompanyId.

**Acceptance Scenarios**:

1. **Given** un actor autorizado y una compania objetivo permitida, **When** crea un usuario con datos obligatorios validos, **Then** la cuenta queda creada con rol y estado inicial trazables.
2. **Given** un usuario activo existente, **When** un actor autorizado edita su perfil administrativo o cambia su rol, **Then** el sistema guarda cambios y registra auditoria.
3. **Given** un usuario activo, **When** un actor autorizado lo desactiva, **Then** la cuenta deja de autenticarse sin borrado fisico de datos.
4. **Given** una solicitud de cambio de compania, **When** el usuario objetivo ya pertenece a una compania activa, **Then** el sistema rechaza el cambio segun regla de pertenencia unica.
5. **Given** un actor con rol Gerente de proyectos o Presupuestador, **When** intenta crear un usuario desde el panel administrativo, **Then** el sistema rechaza la operacion con `403` y mantiene el alta de no administradores por self-registration.

---

### User Story 4 - Administracion de Companias por Rol (Priority: P2)

Como Administrador o Gerente de proyectos, quiero gestionar companias segun permisos de rol para controlar que organizaciones pueden operar en la plataforma.

**Why this priority**: Define el limite organizacional del multitenancy y habilita la administracion de usuarios por tenant.

**Independent Test**: Se valida que Administrador crea/edita/desactiva companias y que Gerente de proyectos solo puede crear una compania propia y editarla, sin crear una segunda.

**Acceptance Scenarios**:

1. **Given** un Administrador autenticado, **When** registra una compania con datos obligatorios validos, **Then** la compania queda disponible para asignaciones.
2. **Given** un Gerente de proyectos sin compania asociada, **When** registra su compania con datos obligatorios validos, **Then** el sistema crea exactamente una compania propia y la asocia al gerente.
3. **Given** un Gerente de proyectos con compania ya asociada, **When** intenta crear una nueva compania, **Then** el sistema rechaza la solicitud por limite de una compania por gerente.
4. **Given** una compania existente activa del Gerente de proyectos, **When** el gerente actualiza su informacion, **Then** el sistema conserva trazabilidad de cambios y restringe la edicion a su propia compania.
5. **Given** una compania existente, **When** un Administrador la desactiva, **Then** queda inhabilitada para nuevas operaciones sin eliminar registros historicos.

---

### User Story 5 - Autoservicio de Cuenta y Contrasena (Priority: P2)

Como usuario autenticado de cualquier rol, quiero administrar mi propio perfil y cambiar mi contrasena dentro de limites de seguridad para mantener mi cuenta vigente.

**Why this priority**: Reduce dependencia operativa del equipo administrador y mejora continuidad de acceso.

**Independent Test**: Se valida que cada rol puede editar solo su propia cuenta y cambiar su contrasena cumpliendo politica minima.

**Acceptance Scenarios**:

1. **Given** un usuario autenticado, **When** actualiza su perfil propio permitido, **Then** el sistema aplica cambios solo sobre su cuenta.
2. **Given** un usuario autenticado, **When** intenta cambiar su contrasena sin cumplir politica minima, **Then** el sistema rechaza el cambio con validacion clara.
3. **Given** intentos fallidos de inicio de sesion, **When** un Administrador habilita recuperacion asistida, **Then** el usuario puede restablecer contrasena bajo el flujo autorizado.

---

### User Story 6 - Busqueda de Usuarios y Privacidad por CompanyId (Priority: P3)

Como actor autorizado, quiero buscar usuarios con filtros funcionales sin exponer datos de otras companias para acelerar gestion segura.

**Why this priority**: Aumenta eficiencia administrativa sin comprometer privacidad multitenant.

**Independent Test**: Se valida la busqueda por filtros y que los resultados respeten permisos de rol y aislamiento por CompanyId.

**Acceptance Scenarios**:

1. **Given** un actor autorizado, **When** consulta usuarios por email o username, **Then** obtiene coincidencias dentro del alcance de compania permitido.
2. **Given** un actor autorizado, **When** filtra por nombre/apellidos o numero de documento, **Then** el sistema devuelve resultados consistentes y trazables.
3. **Given** un actor sin alcance sobre otra compania, **When** intenta consultar usuarios de otro tenant, **Then** el sistema niega acceso o retorna vacio segun politica de privacidad.

---

### Edge Cases

- Solicitudes concurrentes de bootstrap inicial no deben crear mas de un Administrador.
- Intento de desactivar al ultimo Administrador activo debe controlarse para no dejar la plataforma sin gobierno.
- Un usuario desactivado debe perder acceso aunque tuviera sesion activa previa.
- Cambio de compania debe fallar si el usuario tiene membresia activa en otra compania.
- En self-registration, cualquier intento de enviar `company_id`, `project_id` o `rol_asignado` debe rechazarse.
- Busquedas con filtros vacios deben devolver resultados acotados por permisos, no todo el universo sin control.
- Intentos repetidos de login fallido deben activar la politica de bloqueo definida para esta fase.
- Si un Gerente de proyectos o Presupuestador intenta crear usuarios por endpoint administrativo, el sistema debe rechazar la operacion con `403`.
- Si un Auditor intenta crear usuarios, el sistema debe responder acceso denegado por matriz de permisos.
- Si un rol esta inactivo o falta/inactiva la asignacion `RolePermission` del `permission_key` requerido, la operacion privada debe denegarse con `403` y registrar evento de seguridad/auditoria.

## Endpoint Contracts *(mandatory when API is added or changed)*

- **Authorization Model Note**: Para todos los endpoints privados, la autorizacion se evalua por `permission_key` activo derivado del rol del usuario. Las listas de roles en cada contrato se mantienen como referencia de cobertura funcional y no como mecanismo hardcodeado de decision.
- **Role Catalog Note**: En esta fase no se exponen endpoints para crear, renombrar, eliminar o desactivar roles base del sistema.
- **Role Taxonomy Note**: El catalogo funcional de roles es cerrado y solo incluye Administrador, Gerente de proyectos, Presupuestador y Auditor.
- **Fail-Closed Note**: Si el rol del actor esta inactivo, o el `permission_key` requerido no existe o no esta activo en `RolePermission`, el endpoint privado responde `403` y registra evento de seguridad/auditoria.
- **Seed Governance Note**: El catalogo de `Role`, `Permission` y `RolePermission` se provisiona mediante seed idempotente y versionado; no se permite autocreacion de permisos o asignaciones en tiempo de ejecucion.
- **Self-Registration Note**: En `POST /api/v1/auth/register`, `rol_asignado`, `company_id` y `project_id` no son campos aceptados.

### Endpoint: POST /api/v1/auth/login

- **Auth Required**: No
- **Allowed Roles**: Public endpoint (aplica a usuarios registrados)
- **Company Ownership Rule**: N/A en entrada; el contexto de compania se resuelve despues de autenticar.
- **Request Schema**:
  - `identifier` (string, required): username o email.
  - `password` (string, required): contrasena en texto de entrada para validacion.
- **Response Schema**:
  - `access_token` (string)
  - `token_type` (string)
  - `expires_at` (datetime)
  - `user` (object): `user_id`, `email`, `username`, `role`, `company_id` (uuid/null), `estado_cuenta`.
- **Validations**:
  - Solo permite cuentas activas/habilitadas.
  - Debe verificar politica de bloqueo de cuenta.
  - Debe bloquear la cuenta al quinto intento fallido consecutivo.
- **Errors**:
  - `401` credenciales invalidas.
  - `423` cuenta bloqueada o deshabilitada con mensaje obligatorio.
- **Request Example**:
  ```json
  {
    "identifier": "usuario@dominio.com",
    "password": "<SECRET>"
  }
  ```
- **Response Example**:
  ```json
  {
    "access_token": "jwt-token",
    "token_type": "Bearer",
    "expires_at": "2026-09-20T12:00:00Z",
    "user": {
      "user_id": "uuid",
      "email": "usuario@dominio.com",
      "username": "usuario1",
      "role": "Presupuestador",
      "company_id": "uuid",
      "estado_cuenta": true
    }
  }
  ```

### Endpoint: POST /api/v1/auth/register

- **Auth Required**: No
- **Allowed Roles**: Public endpoint
- **Company Ownership Rule**: En self-registration no se permite asignar compania ni proyecto; `company_id` inicia sin asignar.
- **Request Schema**:
  - `primer_nombre` (string, required)
  - `primer_apellido` (string, required)
  - `email` (string, required)
  - `username` (string, required)
  - `password` (string, required)
  - `tipo_documento` (string, required)
  - `numero_documento` (string, required)
- **Response Schema**:
  - `user_id` (uuid)
  - `rol_asignado` (string, fixed = Presupuestador)
  - `company_id` (null)
  - `estado_cuenta` (boolean)
  - `fecha_creacion` (datetime)
- **Validations**:
  - Debe forzar `rol_asignado = Presupuestador`.
  - Debe rechazar cualquier intento de enviar `rol_asignado`, `company_id` o `project_id`.
  - Debe cumplir politica minima de contrasena y almacenar hash Argon2id.
- **Errors**:
  - `400` payload invalido o campos no permitidos en self-registration.
  - `409` conflicto por `username`, `email` o documento existente.
- **Request Example**:
  ```json
  {
    "primer_nombre": "Laura",
    "primer_apellido": "Gomez",
    "tipo_documento": "CC",
    "numero_documento": "10007890",
    "email": "laura.gomez@correo.com",
    "username": "laura.gomez",
    "password": "<SECRET>"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "rol_asignado": "Presupuestador",
    "company_id": null,
    "estado_cuenta": true,
    "fecha_creacion": "2026-09-21T10:00:00Z"
  }
  ```

### Endpoint: POST /api/v1/admin/bootstrap/first-administrator

- **Auth Required**: No (solo mientras no exista ningun Administrador)
- **Allowed Roles**: Public bootstrap controlado por estado del sistema
- **Company Ownership Rule**: La compania se crea o vincula dentro del mismo acto de bootstrap segun reglas de alta inicial.
- **Request Schema**:
  - `admin_profile` (object, required)
  - `company` (object, required)
- **Response Schema**:
  - `bootstrap_enabled` (boolean)
  - `administrator_created` (boolean)
  - `administrator_id` (uuid)
  - `audit_event_id` (uuid)
- **Validations**:
  - Debe verificar ausencia total de Administradores antes de crear.
  - Debe ser idempotente ante reintentos.
- **Errors**:
  - `409` bootstrap deshabilitado porque ya existe Administrador.
- **Request Example**:
  ```json
  {
    "admin_profile": {
      "email": "admin@empresa.com",
      "username": "admin.principal",
      "password": "<SECRET>"
    },
    "company": {
      "business_name": "Compania Inicial",
      "tax_id": "123456789"
    }
  }
  ```
- **Response Example**:
  ```json
  {
    "bootstrap_enabled": false,
    "administrator_created": true,
    "administrator_id": "uuid",
    "audit_event_id": "uuid"
  }
  ```

### Endpoint: POST /api/v1/users

- **Auth Required**: Yes
- **Allowed Roles**: Administrador
- **Company Ownership Rule**:
  - Administrador puede asignar `company_id` dentro de su alcance autorizado.
  - Gerente de proyectos y Presupuestador no pueden crear usuarios por este endpoint.
  - Auditor no puede ejecutar creacion de usuarios.
- **Request Schema**:
  - `primer_nombre` (string, required)
  - `segundo_nombre` (string, optional)
  - `primer_apellido` (string, required)
  - `segundo_apellido` (string, optional)
  - `tipo_documento` (string, required)
  - `numero_documento` (string, required)
  - `fecha_nacimiento` (date, required)
  - `residencia` (object, required): `municipio`, `departamento`, `pais`
  - `email` (string, required)
  - `username` (string, required)
  - `password` (string, required)
  - `rol_asignado` (string, optional; solo asignable por Administrador)
  - `tarjeta_profesional` (string, optional)
  - `fecha_expedicion_tarjeta_profesional` (date, optional)
  - `archivo_tarjeta_profesional` (string, optional)
  - `company_id` (uuid, optional; solo asignable por Administrador)
- **Response Schema**:
  - `user_id` (uuid)
  - `rol_asignado` (string)
  - `company_id` (uuid/null)
  - `estado_cuenta` (boolean)
  - `fecha_creacion` (datetime)
- **Validations**:
  - Sin borrado fisico en ciclo de vida posterior.
  - Contrasena cumple politica minima y se almacena como hash seguro.
  - Si el actor no es Administrador, la creacion administrativa debe rechazarse.
- **Errors**:
  - `400` datos invalidos o contrasena fuera de politica.
  - `403` actor sin permiso para crear usuarios por endpoint administrativo.
  - `403` actor sin permiso para asignar `rol_asignado` o `company_id` manualmente.
  - `409` conflicto por dato unico.
- **Request Example (creador Administrador)**:
  ```json
  {
    "primer_nombre": "Ana",
    "primer_apellido": "Ruiz",
    "tipo_documento": "CC",
    "numero_documento": "10001234",
    "fecha_nacimiento": "1990-03-01",
    "residencia": {
      "municipio": "Bogota",
      "departamento": "Cundinamarca",
      "pais": "Colombia"
    },
    "email": "ana.ruiz@empresa.com",
    "username": "ana.ruiz",
    "password": "<SECRET>",
    "rol_asignado": "Presupuestador",
    "company_id": "uuid"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "rol_asignado": "Presupuestador",
    "company_id": null,
    "estado_cuenta": true,
    "fecha_creacion": "2026-09-20T12:00:00Z"
  }
  ```

### Endpoint: PUT /api/v1/users/{userId}

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos (alcance restringido)
- **Company Ownership Rule**: El actor solo puede editar usuarios dentro del alcance de compania permitido.
- **Request Schema**: mismos campos administrativos editables del perfil de usuario (sin password directa).
- **Response Schema**:
  - `user_id` (uuid)
  - `fecha_edicion` (datetime)
- **Validations**:
  - Debe respetar matriz de permisos por rol.
  - Debe registrar auditoria de cambio.
- **Errors**:
  - `403` sin permisos por rol o compania.
  - `404` usuario no encontrado en alcance.
- **Request Example**:
  ```json
  {
    "primer_nombre": "Ana Maria",
    "segundo_nombre": "",
    "primer_apellido": "Ruiz",
    "segundo_apellido": "Lopez",
    "tarjeta_profesional": "TP-1234"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "fecha_edicion": "2026-09-20T13:00:00Z"
  }
  ```

### Endpoint: PATCH /api/v1/users/{userId}/deactivate

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos (alcance restringido)
- **Company Ownership Rule**: Solo usuarios en companias permitidas para el actor.
- **Request Schema**:
  - `motivo` (string, required)
- **Response Schema**:
  - `user_id` (uuid)
  - `estado_cuenta` (boolean)
- **Validations**:
  - Prohibido borrado fisico.
  - Debe invalidar acceso de autenticacion para cuenta desactivada.
- **Errors**:
  - `403` sin permiso.
  - `409` desactivacion no permitida por regla de continuidad administrativa.
- **Request Example**:
  ```json
  {
    "motivo": "Baja temporal"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "estado_cuenta": false
  }
  ```

### Endpoint: PATCH /api/v1/users/{userId}/role

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos (sin elevar a Administrador)
- **Company Ownership Rule**: No permite cambios sobre usuarios de otra compania.
- **Request Schema**:
  - `rol_asignado` (string, required)
- **Response Schema**:
  - `user_id` (uuid)
  - `rol_asignado` (string)
- **Validations**:
  - Debe respetar la matriz de permisos por rol.
- **Errors**:
  - `403` sin permiso por rol/compania.
- **Request Example**:
  ```json
  {
    "rol_asignado": "Auditor"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "rol_asignado": "Auditor"
  }
  ```

### Endpoint: PATCH /api/v1/users/{userId}/company

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos
- **Company Ownership Rule**: Administrador y Gerente de proyectos pueden asignar o reasignar compania dentro de su alcance autorizado; Gerente de proyectos no puede operar fuera de su propia compania.
- **Request Schema**:
  - `company_id` (uuid, required)
- **Response Schema**:
  - `user_id` (uuid)
  - `company_id` (uuid)
- **Validations**:
  - Un usuario solo puede pertenecer a una compania al tiempo.
  - Cambio solo permitido si actualmente no pertenece a ninguna compania.
- **Errors**:
  - `403` actor sin rol Administrador/Gerente de proyectos o fuera de alcance de compania.
  - `409` usuario ya pertenece a una compania activa.
- **Request Example**:
  ```json
  {
    "company_id": "uuid"
  }
  ```
- **Response Example**:
  ```json
  {
    "user_id": "uuid",
    "company_id": "uuid"
  }
  ```

### Endpoint: GET /api/v1/users

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos, Auditor (solo lectura dentro de su CompanyId)
- **Company Ownership Rule**: Resultados restringidos por alcance de compania del actor, salvo excepciones explicitas autorizadas.
- **Request Schema**:
  - Query params opcionales: `email`, `username`, `nombre_apellidos`, `numero_documento`, `estado_cuenta`.
- **Response Schema**:
  - Lista de usuarios con campos de consulta administrativa.
- **Validations**:
  - Filtros combinables y auditables.
- **Errors**:
  - `403` sin permiso de lectura.
- **Request Example**: `GET /api/v1/users?email=ana.ruiz@empresa.com`
- **Response Example**:
  ```json
  [
    {
      "user_id": "uuid",
      "primer_nombre": "Ana",
      "primer_apellido": "Ruiz",
      "email": "ana.ruiz@empresa.com",
      "username": "ana.ruiz",
      "rol_asignado": "Presupuestador",
      "company_id": "uuid",
      "estado_cuenta": true
    }
  ]
  ```

### Endpoint: POST /api/v1/companies

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos
- **Company Ownership Rule**: Administrador puede crear companias segun alcance autorizado; Gerente de proyectos solo puede crear una compania propia si no tiene una asociada.
- **Request Schema**:
  - `business_name` (string, required)
  - `tax_id` (string, required)
  - `is_active` (boolean, optional)
- **Response Schema**:
  - `company_id` (uuid)
  - `fecha_creacion` (datetime)
- **Validations**:
  - Prohibido borrado fisico en el ciclo de vida posterior.
  - Si el actor es Gerente de proyectos y ya tiene compania asociada, la creacion debe rechazarse.
- **Errors**:
  - `403` actor sin permiso para crear companias.
  - `409` gerente ya tiene compania asociada.
  - `409` identificador empresarial duplicado.

### Endpoint: PUT /api/v1/companies/{companyId}

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos
- **Company Ownership Rule**: Administrador con alcance habilitado para gestion de companias; Gerente de proyectos solo puede editar su propia compania.
- **Request Schema**: campos editables de compania.
- **Response Schema**: `company_id`, `fecha_edicion`.
- **Validations**: registro de auditoria obligatorio.
- **Errors**: `403` sin permiso sobre esa compania, `404` compania no encontrada, `409` conflicto de datos.

### Endpoint: PATCH /api/v1/companies/{companyId}/deactivate

- **Auth Required**: Yes
- **Allowed Roles**: Administrador
- **Company Ownership Rule**: Operacion de estado sobre compania objetivo permitida al Administrador.
- **Request Schema**:
  - `motivo` (string, required)
- **Response Schema**:
  - `company_id` (uuid)
  - `is_active` (boolean)
- **Validations**:
  - Sin borrado fisico.
- **Errors**:
  - `409` desactivacion conflictiva por reglas de continuidad.

### Endpoint: GET /api/v1/profile/me

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos, Presupuestador, Auditor
- **Company Ownership Rule**: Solo perfil del usuario autenticado.
- **Request Schema**: N/A
- **Response Schema**: datos de perfil propio.
- **Validations**: no exponer datos de terceros.
- **Errors**: `401` no autenticado.

### Endpoint: PUT /api/v1/profile/me

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos, Presupuestador, Auditor
- **Company Ownership Rule**: Solo perfil propio.
- **Request Schema**: campos permitidos de autoservicio.
- **Response Schema**: `user_id`, `fecha_edicion`.
- **Validations**: impedir elevacion de privilegios desde autoservicio.
- **Errors**: `400` validacion, `403` operacion fuera de perfil propio.

### Endpoint: POST /api/v1/profile/me/change-password

- **Auth Required**: Yes
- **Allowed Roles**: Administrador, Gerente de proyectos, Presupuestador, Auditor
- **Company Ownership Rule**: Solo cuenta propia.
- **Request Schema**:
  - `current_password` (string, required)
  - `new_password` (string, required)
- **Response Schema**:
  - `password_updated` (boolean)
  - `updated_at` (datetime)
- **Validations**:
  - Nueva contrasena cumple politica minima.
  - Almacenamiento de contrasena como hash Argon2id.
- **Errors**:
  - `400` nueva contrasena invalida.
  - `401` contrasena actual incorrecta.

### Endpoint: POST /api/v1/users/{userId}/password-recovery/enable

- **Auth Required**: Yes
- **Allowed Roles**: Administrador
- **Company Ownership Rule**: Usuario objetivo dentro de alcance permitido de compania.
- **Request Schema**:
  - `reason` (string, required)
- **Response Schema**:
  - `user_id` (uuid)
  - `recovery_enabled` (boolean)
- **Validations**:
  - Solo recuperacion asistida por Administrador en esta fase.
- **Errors**:
  - `403` actor sin permiso.

### Endpoint: GET /api/v1/audit/bootstrap

- **Auth Required**: Yes
- **Allowed Roles**: Administrador
- **Company Ownership Rule**: Solo lectura permitida para rol Administrador.
- **Request Schema**: filtros opcionales por fecha/resultado.
- **Response Schema**: lista de eventos de bootstrap con `actor`, `accion`, `objetivo`, `fecha_hora`, `resultado`.
- **Validations**:
  - Acceso prohibido para roles no Administrador.
- **Errors**:
  - `403` acceso no autorizado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST autenticar usuarios con credenciales por username o email y validar estado de cuenta antes de conceder acceso.
- **FR-002**: System MUST mostrar el mensaje exacto "Este no se encuentra habilitado, solicite al administrador su habilitacion" cuando una cuenta bloqueada o deshabilitada intente autenticarse.
- **FR-003**: System MUST detectar si existe al menos un usuario con rol Administrador antes de habilitar bootstrap inicial.
- **FR-004**: System MUST permitir el flujo de bootstrap del primer Administrador solo cuando no exista ninguno previo.
- **FR-005**: System MUST hacer idempotente el bootstrap para evitar creaciones repetidas de Administrador.
- **FR-006**: System MUST registrar auditoria de bootstrap con actor, accion, objetivo, fecha/hora y resultado.
- **FR-007**: System MUST restringir la visualizacion de auditoria de bootstrap al rol Administrador.
- **FR-008**: System MUST soportar dos vias de alta de usuario: registro autonomo publico y creacion administrativa exclusiva del rol Administrador.
- **FR-009**: System MUST permitir editar usuarios sin borrado fisico y mantener trazabilidad de cambios.
- **FR-010**: System MUST permitir desactivar usuarios (estado_cuenta = false) sin eliminarlos fisicamente.
- **FR-011**: System MUST permitir asignar y cambiar rol solo a Administrador y Gerente de proyectos, segun matriz de permisos.
- **FR-012**: System MUST permitir asignar compania a usuario a los roles Administrador y Gerente de proyectos, respetando pertenencia unica activa.
- **FR-013**: System MUST impedir cambiar de compania a un usuario que ya pertenece a una compania activa.
- **FR-014**: System MUST permitir gestion de companias sin borrado fisico segun rol: Administrador crea/edita/desactiva; Gerente de proyectos crea una sola compania propia y solo puede editarla.
- **FR-015**: System MUST aplicar matriz de permisos por rol para usuarios/companias.
- **FR-016**: System MUST permitir autoservicio de perfil y cambio de contrasena para todos los roles dentro de limites de seguridad.
- **FR-017**: System MUST almacenar contrasenas con hash Argon2id y nunca en texto plano.
- **FR-018**: System MUST exigir politica minima de contrasena: minimo 8 caracteres, una mayuscula, un numero y un caracter especial.
- **FR-019**: System MUST limitar la recuperacion de contrasena en esta fase a flujo asistido por Administrador.
- **FR-020**: System MUST permitir al Administrador habilitar cambio de contrasena para un usuario tras intentos fallidos de inicio de sesion.
- **FR-021**: System MUST conservar compatibilidad funcional para futura recuperacion por correo sin afectar el alcance actual.
- **FR-022**: System MUST aplicar restriccion de CompanyId en todos los endpoints privados del feature.
- **FR-023**: System MUST impedir que Gerente de proyectos administre otras companias.
- **FR-024**: System MUST rechazar cualquier intento de asignacion de usuario a proyecto dentro de los endpoints del feature usuarios/companias.
- **FR-025**: System MUST permitir que los roles operativos autenticados (Gerente de proyectos, Presupuestador y Auditor) consulten APUs publicos existentes en modo solo lectura.
- **FR-026**: System MUST mantener fuera de alcance la implementacion funcional de APU/EDS/insumos en esta iteracion.
- **FR-027**: System MUST soportar busqueda de usuarios por email, username, nombre/apellidos y numero de documento.
- **FR-028**: System MUST garantizar que cada usuario pertenezca como maximo a una compania activa al mismo tiempo.
- **FR-029**: System MUST impedir autoservicio fuera de los permisos del rol del usuario.
- **FR-030**: System MUST garantizar unicidad global de `username` en todo el sistema.
- **FR-031**: System MUST bloquear cuentas despues de 5 intentos fallidos consecutivos de autenticacion, con recuperacion asistida por Administrador.
- **FR-032**: System MUST restringir al rol Auditor a lectura sobre datos de terceros dentro de su propio CompanyId, manteniendo autoservicio de su propia cuenta.
- **FR-033**: System MUST denegar la creacion administrativa de usuarios cuando el actor sea Gerente de proyectos o Presupuestador, manteniendo el alta de no administradores por self-registration.
- **FR-034**: System MUST autorizar todos los endpoints privados del feature mediante verificacion de `permission_key` activo, derivado del rol vigente del actor autenticado, evitando listas fijas hardcodeadas como mecanismo de decision.
- **FR-035**: System MUST mantener un catalogo base de roles inmutable y cerrado en esta fase (Administrador, Gerente de proyectos, Presupuestador y Auditor), sin CRUD de roles base y permitiendo unicamente la gestion de asignaciones de permisos mediante `RolePermission`.
- **FR-036**: System MUST aplicar politica fail-closed en autorizacion: si el rol del actor esta inactivo o la asignacion `RolePermission` del `permission_key` requerido no existe/esta inactiva, la operacion privada se rechaza con `403` y se registra evento de seguridad/auditoria.
- **FR-037**: System MUST mantener baseline de seguridad en BD mediante seed obligatorio, idempotente y versionado para `Role`, `Permission` y `RolePermission`, prohibiendo autocreacion implicita de permisos/asignaciones en runtime.
- **FR-038**: System MUST forzar que todo usuario de self-registration se cree con rol por defecto Presupuestador.
- **FR-039**: System MUST impedir que self-registration acepte asignacion de `company_id`, `project_id` o rol manual en el alta inicial.
- **FR-040**: System MUST permitir que un usuario recien registrado por self-registration se autentique y consulte APUs publicos existentes sin ampliar el alcance funcional de APU en este feature.

### Non-Functional Requirements

- **NFR-001**: Seguridad de acceso: 100% de endpoints privados del alcance usuarios/companias requieren autenticacion valida y autorizacion por `permission_key` activo derivado del rol vigente.
- **NFR-002**: Privacidad multitenant: 100% de operaciones privadas deben validar aislamiento por CompanyId para lectura y escritura.
- **NFR-003**: Auditabilidad: 100% de operaciones de bootstrap y administracion de usuarios/companias dejan evidencia trazable de actor, accion, objetivo, fecha/hora y resultado.
- **NFR-004**: Usabilidad operativa: 95% de operaciones administrativas (alta/edicion/desactivacion) deben completarse por un usuario entrenado en menos de 3 minutos por operacion.
- **NFR-005**: Busqueda administrativa: 95% de consultas de usuarios por filtros deben completar en menos de 2 segundos con resultados dentro del alcance autorizado.
- **NFR-006**: Configuracion operativa: la conexion a base de datos del backend debe mantenerse en un unico punto de configuracion de la aplicacion (app config del proyecto de conexion BD).
- **NFR-007**: Consistencia de autorizacion por ambiente: 100% de despliegues deben conservar el mismo baseline de `Role`/`Permission`/`RolePermission` mediante seed versionado e idempotente.

### Business Rules

- **BR-001**: Borrado fisico de usuarios y companias esta prohibido; solo se permite desactivacion.
- **BR-002**: Un usuario no puede autoadministrarse fuera de los permisos de su rol.
- **BR-003**: Gerente de proyectos solo puede administrar su propia compania: puede crear una unica compania propia si no tiene una asociada y editarla; no puede administrar otras companias ni crear una segunda.
- **BR-004**: La asignacion o cambio de compania de un usuario solo puede ejecutarla un Administrador o un Gerente de proyectos dentro de su alcance autorizado.
- **BR-005**: En self-registration esta prohibido asignar compania, proyecto o rol manual durante el alta inicial.
- **BR-006**: La consulta de APUs publicos e insumos en este feature es solo consumo de capacidades existentes de solo lectura para roles operativos; no habilita implementacion funcional de APU/insumos y excluye acceso del Administrador.
- **BR-007**: Restriccion de CompanyId es obligatoria en endpoints privados.
- **BR-008**: Un usuario puede pertenecer solo a una compania al tiempo.
- **BR-009**: Un usuario solo puede cambiar de compania por accion de un Administrador o de un Gerente de proyectos y si actualmente no pertenece a ninguna compania.
- **BR-010**: Si una cuenta esta bloqueada o deshabilitada, el sistema debe mostrar el mensaje obligatorio definido en FR-002.
- **BR-011**: Flujo bootstrap solo se habilita cuando no existe ningun Administrador.
- **BR-012**: Reejecutar bootstrap no debe crear un nuevo Administrador.
- **BR-013**: Auditoria de bootstrap solo es visible para Administrador.
- **BR-014**: Contrasena siempre se almacena como hash Argon2id.
- **BR-015**: Politica minima de contrasena aplica para altas y cambios.
- **BR-016**: Recuperacion de contrasena en esta fase es asistida por Administrador.
- **BR-017**: `username` debe ser unico global en el sistema.
- **BR-018**: Una cuenta se bloquea automaticamente al completar 5 intentos fallidos consecutivos de inicio de sesion.
- **BR-019**: El rol Auditor solo tiene permisos de lectura sobre datos de terceros dentro de su propio CompanyId; mantiene permiso de autoservicio para su propio perfil y cambio de contrasena.
- **BR-020**: Gerente de proyectos y Presupuestador no pueden crear usuarios por endpoint administrativo.
- **BR-021**: El alta de usuarios para actores no Administrador se realiza por self-registration y aplica defaults de rol Presupuestador con `company_id` sin asignar.
- **BR-022**: Las decisiones de autorizacion en endpoints privados deben evaluarse por `permission_key` activo asociado al rol vigente, no por listas fijas hardcodeadas de roles.
- **BR-023**: Los roles base del sistema son inmutables y cerrados en esta fase (Administrador, Gerente de proyectos, Presupuestador y Auditor); no se permite crear, renombrar, eliminar ni desactivar roles base mediante operaciones de negocio del feature.
- **BR-024**: Ante rol inactivo o permiso faltante/inactivo para el `permission_key` requerido, la respuesta obligatoria del endpoint privado es `403` y se debe registrar el evento de denegacion para auditoria.
- **BR-025**: La provision de `Role`, `Permission` y `RolePermission` se realiza por seed versionado e idempotente; si falta una asignacion requerida, la operacion se deniega y no se compensa creando permisos en runtime.
- **BR-026**: Todo usuario creado por self-registration puede autenticarse inmediatamente y consumir APUs publicos existentes en modo solo lectura.

### In-Scope (explicito)

- Login por username o email.
- Registro autonomo de usuarios con rol por defecto Presupuestador.
- Bootstrap del primer Administrador con auditoria.
- Panel de administracion de usuarios y companias con gestion por rol.
- Asignacion/cambio de compania de usuarios por Administrador y Gerente de proyectos segun alcance autorizado.
- Matriz de permisos por rol para usuarios/companias.
- Autoservicio de perfil y cambio de contrasena.
- Restricciones multitenancy por CompanyId.
- Filtros administrativos de busqueda de usuarios.
- Consumo autenticado de APUs publicos existentes en modo solo lectura.
- Entrega exclusiva de backend sin implementacion de interfaces frontend.
- Configuracion centralizada de conexion a base de datos en la app de backend.

### Out-of-Scope (explicito)

- Implementacion de interfaces, rutas y estado de frontend para login, paneles y autoservicio.
- Automatizacion de pruebas UI/E2E frontend en esta iteracion.
- Modulo completo de proyectos.
- Modulo completo APU/EDS/insumos y cualquier operacion de escritura sobre APUs.
- Motor de calculo y reglas de cotizacion de presupuesto.
- Asignacion de usuarios a proyectos y gestion de membresias usuario-proyecto en este feature.
- CRUD de roles base del sistema (crear, renombrar, eliminar o desactivar roles base).
- Separacion de la estrategia de pruebas en multiples proyectos (ContractTests e IntegrationTests) para este feature en esta fase.
- Cualquier capacidad fuera de usuarios/autenticacion/usuarios-companias.

### Matriz de Permisos por Rol (explicito)

| Accion | Administrador | Gerente de proyectos | Presupuestador | Auditor |
|--------|---------------|----------------------|----------------|---------|
| Iniciar sesion | Permitido | Permitido | Permitido | Permitido |
| Crear usuario | Permitido (endpoint administrativo) | Denegado | Denegado | Denegado |
| Definir rol y compania al crear usuario | Permitido (rol y compania) | Denegado | Denegado | Denegado |
| Editar usuario | Permitido | Permitido (solo su compania) | Denegado | Denegado |
| Desactivar usuario | Permitido | Permitido (solo su compania) | Denegado | Denegado |
| Asignar/cambiar rol | Permitido | Permitido (sin elevar a Administrador) | Denegado | Denegado |
| Asignar/cambiar compania de usuario | Permitido | Permitido (solo su compania) | Denegado | Denegado |
| Crear compania | Permitido | Permitido (solo una propia) | Denegado | Denegado |
| Editar compania | Permitido | Permitido (solo su propia compania) | Denegado | Denegado |
| Desactivar compania | Permitido | Denegado | Denegado | Denegado |
| Ver auditoria bootstrap | Permitido | Denegado | Denegado | Denegado |
| Gestionar propio perfil/cuenta | Permitido | Permitido | Permitido | Permitido |
| Cambiar propia contrasena | Permitido | Permitido | Permitido | Permitido |
| Ver usuarios por filtros | Permitido | Permitido (solo su compania) | Denegado | Permitido (solo lectura, su CompanyId) |
| Consultar APUs publicos existentes (solo lectura) | Denegado | Permitido | Permitido | Permitido |
| Consultar insumos existentes (solo lectura) | Denegado | Permitido (solo su compania) | Permitido (solo su compania/proyectos asignados) | Permitido (solo su compania/proyectos asignados) |
| Crear/editar/eliminar APUs | Denegado en este feature | Denegado en este feature | Denegado en este feature | Denegado en este feature |
| Crear/editar/eliminar insumos | Denegado en este feature | Denegado en este feature | Denegado en este feature | Denegado en este feature |

### Reglas de Multitenancy y Privacidad (explicito)

- Todo endpoint privado del alcance debe derivar y validar CompanyId segun identidad autenticada y permisos de rol.
- Ningun actor puede leer o modificar usuarios/companias fuera de su alcance autorizado.
- Gerente de proyectos solo opera en su compania.
- Administrador y Gerente de proyectos pueden asignar o cambiar compania de usuarios segun alcance autorizado.
- Gerente de proyectos puede crear una sola compania propia y editar solo esa compania.
- Administrador no puede consultar APUs ni insumos de compania en este feature.
- La creacion administrativa de usuarios es exclusiva de Administrador; para los demas casos aplica self-registration.
- Auditor opera en modo lectura sobre datos de terceros segun alcance autorizado de compania.
- En self-registration, `rol_asignado`, `company_id` y `project_id` no son asignables y cualquier intento debe rechazarse.
- El rol Auditor no puede crear usuarios y permanece en solo lectura dentro de su CompanyId.
- Las operaciones de asignacion de usuarios a proyectos no forman parte del alcance de este feature y deben rechazarse en los endpoints de usuarios/companias.
- El cliente no puede forzar acceso cross-tenant por parametros no autorizados.

### Datos Funcionales de Usuario (explicito)

| Campo | Obligatorio | Regla funcional |
|-------|-------------|-----------------|
| primer_nombre | Si | Dato base de identificacion personal |
| segundo_nombre | No | Complemento de nombre |
| primer_apellido | Si | Dato base de identificacion personal |
| segundo_apellido | No | Complemento de apellido |
| tipo_documento | Si | Valor de catalogo de tipos de documento |
| numero_documento | Si | Identificador documental para gestion y busqueda |
| fecha_nacimiento | Si | Dato de perfil para validaciones de identidad |
| residencia.municipio | Si | Valor de catalogo territorial |
| residencia.departamento | Si | Valor de catalogo territorial |
| residencia.pais | Si | Valor de catalogo territorial |
| email | Si | Credencial de contacto y potencial autenticador |
| username | Si | Credencial de autenticacion alternativa |
| password | Si | Debe almacenarse como hash Argon2id |
| rol_asignado | Si | Rol funcional dentro del sistema; en self-registration y en creaciones realizadas por Presupuestador se fija por defecto en Presupuestador |
| tarjeta_profesional | No | Dato profesional segun perfil |
| fecha_expedicion_tarjeta_profesional | No | Fecha asociada a tarjeta profesional |
| archivo_tarjeta_profesional | No | Evidencia documental asociada |
| fecha_creacion | Si | Trazabilidad de alta |
| fecha_edicion | Si | Trazabilidad de cambios |
| fecha_ultimo_login | Si | Trazabilidad de acceso |
| estado_cuenta | Si | Control de habilitacion de cuenta |
| company_id | Condicional | Asociacion del usuario a una compania; solo Administrador o Gerente de proyectos pueden asignarlo manualmente y en self-registration inicia sin asignar |

### Key Entities *(include if feature involves data)*

- **UserAccount**: Representa credenciales de acceso, estado de cuenta, fechas de acceso y asociacion de rol.
- **UserProfile**: Representa datos personales y profesionales del usuario.
- **Role**: Representa categorias base de autorizacion funcional para acciones administrativas y de autoservicio; su catalogo se mantiene inmutable en esta fase.
- **Permission**: Representa permisos atomicos identificados por `permission_key` para control fino de acceso en endpoints privados.
- **RolePermission**: Representa la asignacion many-to-many entre roles y permisos, con estado activo para evaluacion de autorizacion.
- **Company**: Representa la organizacion/tenant con estado activo y datos administrativos.
- **UserCompanyMembership**: Representa pertenencia activa del usuario a una sola compania y su historial de asignacion.
- **AuthSession o LoginAudit**: Representa trazabilidad de inicios de sesion, intentos fallidos y eventos de bloqueo/habilitacion.

### NEEDS CLARIFICATION (Resueltas)

- **NC-001 (resuelta)**: `username` es unico global en el sistema.
- **NC-002 (resuelta)**: La cuenta se bloquea al completar 5 intentos fallidos consecutivos.
- **NC-003 (resuelta)**: El rol Auditor tiene alcance de solo lectura sobre datos de terceros dentro de su CompanyId, conservando autoservicio de su propia cuenta.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% de usuarios con cuenta activa y credenciales validas pueden completar login por email o username en menos de 30 segundos desde la pantalla de acceso.
- **SC-002**: 100% de intentos de autenticacion con cuenta bloqueada o deshabilitada muestran el mensaje obligatorio exacto definido en FR-002.
- **SC-003**: El flujo de bootstrap crea como maximo un primer Administrador por entorno y rechaza el 100% de reintentos posteriores.
- **SC-004**: 100% de operaciones de crear/editar/desactivar usuarios y companias dejan evidencia de auditoria con actor, accion, objetivo, fecha/hora y resultado.
- **SC-005**: 100% de operaciones privadas del feature respetan aislamiento por CompanyId y bloquean accesos cross-tenant no autorizados.
- **SC-006**: 95% de busquedas administrativas por filtros de usuarios retornan resultados en menos de 2 segundos dentro del alcance de permisos.
- **SC-007**: 95% de usuarios administrativos entrenados completan una operacion de alta o edicion de usuario en menos de 3 minutos.
- **SC-008**: 100% de cuentas creadas por self-registration quedan con rol Presupuestador y sin compania/proyecto asignados al alta.
- **SC-009**: 100% de usuarios de self-registration que se autentican correctamente pueden consultar APUs publicos existentes en modo solo lectura.

## Testing Strategy *(mandatory)*

- Este feature implementa solo backend en esta iteracion; la evidencia obligatoria se centra en pruebas de caja negra sobre API, autorizacion y reglas de negocio.
- La ejecucion de pruebas de esta fase se concentra en un unico proyecto `BlackBoxTests` para todo el alcance del feature.
- No se separan suites en proyectos `ContractTests` o `IntegrationTests` durante esta fase.
- No se requieren pruebas unitarias para este feature salvo que, por cambio emergente, se toque logica de calculo APU (fuera de alcance esperado).
- La validacion frontend queda en modo consumidor de contratos API para una iteracion posterior del equipo frontend.
- Toda historia de usuario debe mapearse a evidencia verificable de pruebas funcionales y de autorizacion del backend.

### Planned Test Evidence

- **US1**: Pruebas de caja negra de self-registration y autenticacion (registro autonomo, login por email/username, cuenta deshabilitada, credenciales invalidas, acceso de solo lectura a APUs publicos existentes).
- **US2**: Pruebas de caja negra de bootstrap idempotente y visibilidad de auditoria solo para Administrador.
- **US3**: Pruebas de caja negra de panel usuarios (crear/editar/desactivar/cambiar rol/asignar compania) con validacion por rol y CompanyId.
- **US4**: Pruebas de caja negra de panel companias (crear/editar/desactivar) sin borrado fisico.
- **US5**: Pruebas de caja negra de autoservicio de perfil y cambio de contrasena con politica minima.
- **US6**: Pruebas de caja negra de filtros de busqueda y aislamiento multitenant.

## Data Schema Change Control *(mandatory when structural data change exists)*

- **Driver Requirement (FR/UC/BR)**: FR-003..FR-040, UC-001..UC-006, BR-001..BR-026.
- **Change Type**: Alter / Add.
- **Migration Plan**:
  1. Incorporar/ajustar entidades funcionales de usuario, compania, membresias y auditoria de login/bootstrap.
  2. Incorporar baseline de seguridad para `Role`/`Permission`/`RolePermission` mediante seed idempotente y versionado.
  3. Aplicar cambios de forma incremental con validacion de integridad de datos activos.
  4. Ejecutar despliegue por etapas con control de compatibilidad de autenticacion.
- **Impact Analysis**:
  - Backend/API: contratos de autenticacion, usuarios y companias.
  - Frontend: impacto de consumo contractual (sin implementacion frontend en esta iteracion).
  - Pruebas: suites de caja negra para autorizacion, aislamiento tenant y auditoria.
- **Rollback Plan**:
  1. Revertir a version previa de contratos y estados de cuenta.
  2. Preservar auditoria historica como solo lectura.
  3. Reaplicar baseline anterior de reglas de acceso.
- **Explicit User Authorization Evidence**: Pendiente de registrar en PR/issue de implementacion si los cambios estructurales de datos exceden el esquema actual.

## Traceability Matrix *(mandatory)*

| Requirement | Use Case | Endpoint/Contract | Test Evidence | Planned Task IDs |
|-------------|----------|-------------------|---------------|------------------|
| FR-001, FR-002 | UC-001 | POST /api/v1/auth/login | Black-box login tests | T001, T002 |
| FR-003, FR-004, FR-005 | UC-002 | POST /api/v1/admin/bootstrap/first-administrator | Black-box bootstrap idempotency tests | T003, T004 |
| FR-006, FR-007 | UC-002 | GET /api/v1/audit/bootstrap | Black-box access-control and audit visibility tests | T005 |
| FR-008, FR-009, FR-033, FR-038, FR-039 | UC-003 | POST /api/v1/auth/register, POST /api/v1/users, PUT /api/v1/users/{userId} | Black-box registration and admin-only user creation tests | T006, T007, T021 |
| FR-010 | UC-003 | PATCH /api/v1/users/{userId}/deactivate | Black-box user deactivation tests | T008 |
| FR-011 | UC-003 | PATCH /api/v1/users/{userId}/role | Black-box role-assignment tests | T009 |
| FR-012, FR-013, FR-028 | UC-003 | PATCH /api/v1/users/{userId}/company | Black-box company-membership constraint tests | T010 |
| FR-014 | UC-004 | POST/PUT/PATCH /api/v1/companies | Black-box company administration tests (admin full + gerente una propia) | T011, T012 |
| FR-015, FR-023, FR-024 | UC-003, UC-004 | Users/Companies private endpoints | Black-box RBAC matrix and project-assignment rejection tests | T013 |
| FR-016, FR-017, FR-018 | UC-005 | PUT /api/v1/profile/me, POST /api/v1/profile/me/change-password | Black-box self-service and password-policy tests | T014 |
| FR-019, FR-020 | UC-005 | POST /api/v1/users/{userId}/password-recovery/enable | Black-box admin-assisted recovery tests | T015 |
| FR-022, FR-027, FR-029 | UC-006 | GET /api/v1/users (filtered) + restricted private endpoints | Black-box multitenancy/privacy/filter tests | T016, T017 |
| FR-025, FR-026, FR-040 | UC-001, UC-006 | GET /api/v1/apus/public (existente, solo consumo) | Black-box public-APU read-only access tests | T017 |
| FR-030 | UC-003 | POST /api/v1/users | Black-box uniqueness tests (username global) | T018 |
| FR-031 | UC-001, UC-005 | POST /api/v1/auth/login, POST /api/v1/users/{userId}/password-recovery/enable | Black-box lockout and assisted recovery tests | T019 |
| FR-032 | UC-006 | GET /api/v1/users | Black-box auditor scope tests (same CompanyId only) | T020 |
| FR-034 | UC-003, UC-004, UC-006 | Endpoints privados del feature (Users/Companies/Profile/Audit) | Black-box permission-key authorization tests | T013, T016, T017 |
| FR-035 | UC-003, UC-004 | Endpoints administrativos del feature (sin CRUD de roles base) | Black-box role-catalog immutability tests | T013 |
| FR-036 | UC-003, UC-004, UC-006 | Endpoints privados del feature (validacion fail-closed) | Black-box deny-by-default and security-audit tests | T013, T016, T017 |
| FR-037 | UC-003, UC-004, UC-006 | Provision de seguridad en infraestructura de datos (`Role`/`Permission`/`RolePermission`) | Black-box seeded-baseline consistency tests | T022 |

## Assumptions

- El flujo de login acepta un solo campo identificador para email o username sin duplicar formularios.
- La unicidad de email se mantiene global, consistente con requerimientos previos del proyecto.
- La unicidad de username se define como global en todo el sistema.
- El bloqueo de cuenta se activa al quinto intento fallido consecutivo.
- El self-registration es publico y siempre crea cuentas con rol Presupuestador.
- En self-registration no se permite asignar compania, proyecto ni rol manual.
- La autorizacion de endpoints privados se decide por `permission_key` activo derivado del rol vigente; la matriz de roles se usa como referencia de cobertura funcional.
- El catalogo base de roles (Administrador, Gerente de proyectos, Presupuestador, Auditor) es inmutable y cerrado en esta fase.
- La autorizacion opera en modo fail-closed: rol inactivo o `RolePermission` faltante/inactiva produce `403` y evento auditable.
- El baseline de `Role`/`Permission`/`RolePermission` se mantiene por seed idempotente y versionado; no hay autocreacion de permisos/asignaciones en runtime.
- La estrategia de pruebas del feature se implementa en un unico proyecto `BlackBoxTests` durante esta fase.
- El rol Auditor opera en modo solo lectura sobre datos de terceros dentro de su propio CompanyId y conserva autoservicio de su propia cuenta.
- El rol Auditor no puede crear usuarios.
- La creacion administrativa de usuarios es exclusiva de Administrador; Gerente de proyectos y Presupuestador deben usar self-registration para nuevas cuentas no administrativas.
- Los roles Administrador y Gerente de proyectos pueden asignar/cambiar compania de usuarios en operaciones administrativas segun alcance autorizado.
- El consumo de APUs publicos e insumos en esta iteracion usa capacidades existentes de solo lectura para roles operativos y no implica implementacion funcional de APU/insumos.
- Esta iteracion entrega solo backend; la implementacion frontend queda fuera de alcance.
- La conexion a base de datos del backend se configura en un unico punto de la app (app config del proyecto de conexion BD).
- Gerente de proyectos tiene capacidad administrativa solo dentro de su compania; puede crear una sola compania propia y editarla, sin facultad para elevar usuarios al rol Administrador.
- Los catalogos de tipo_documento, municipio, departamento y pais existen o seran provistos por configuracion administrativa.
- La entidad UserCompanyMembership se usa funcionalmente para validar pertenencia unica e historial, sin habilitar multiasignacion activa simultanea.
- El alcance de este feature no modifica reglas de negocio de calculo APU ni cierre de presupuesto.
