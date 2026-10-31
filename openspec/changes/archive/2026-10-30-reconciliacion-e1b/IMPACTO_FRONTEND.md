# Impacto en el frontend (fuera de alcance, `D-109`)

Este documento **registra** el impacto del cambio `reconciliacion-e1b` sobre el cliente Angular
existente. No especifica funcionalidad de interfaz: el frontend queda fuera del alcance de `E-1b` y de
`E-2` (`D-109`), no aparecen requisitos de interfaz en `openspec/specs` y el cliente `v0.0.1` no se
mantiene compatible (`A-10`). El cliente se regenera desde el contrato OpenAPI al abrir la etapa de
interfaz (`D-56`, `D-46`).

## Registro de impactos

| ID | Cambio | Rompe | Mitigación | Estado |
|---|---|---|---|---|
| IF-01 | `snake_case` → `camelCase` en todos los DTO (`D-99`) | Todos los DTO que consume el cliente | Regenerar el cliente desde el contrato (`D-56`) | Pendiente |
| IF-02 | Formato de error → `ProblemDetails` con `extensions.code` (`D-100`) | Manejo de errores del cliente | Interceptor único actualizado en la misma ventana | Pendiente |
| IF-03 | Rol "Gerente de proyectos" → "Director de proyectos" con `role_code = DIRECTOR` (`D-74`) | Guardias, menús y claims | Regenerar el cliente y ajustar las constantes de rol | Pendiente |
| IF-04 | Dueño único de `/companies` y `/users` (`D-102`) | Servicios duplicados en `M-01` y `M-02` | Eliminar el servicio redundante al regenerar | Pendiente |
| IF-05 | `page` y `pageSize` unificados (`D-101`) | Paginación de todos los listados | Ajuste puntual en los servicios de listado | Pendiente |
| IF-06 | Hash provisto por Identity, bloqueo por intentos, verificación de correo y autorregistro con identidad completa (`D-81`, `D-80`, `D-76` modificada) | Flujo de inicio de sesión y formulario de autorregistro | Regenerar el cliente, crear el formulario de registro con los campos nuevos y probar el flujo completo | Pendiente |
| IF-07 | La pertenencia deja las tablas de membresía: compañía en `User.company_id` y proyecto en `ProjectMember` (`D-98`) | Servicios de membresías: la baja de compañía pasa de `{membershipId}` a `{userId}` y la respuesta de asignación ya no expone `membershipId` | Regenerar el cliente y usar el `userId` en la baja; la respuesta de asociación solo confirma usuario y compañía | Pendiente |
| IF-08 | La configuración del proyecto pasa a entidad propia 1:1 (`D-93`): nueva ruta `GET /projects/{id}/configuration`, `budgetLimit` y `requiresAuditor` en crear/editar, y `SIN_FIRMA` como tercer modo | Formularios de proyecto: campos nuevos, selector de tres modos y lectura de la configuración por su ruta | Regenerar el cliente, añadir el tope y el requisito de auditor, y leer la configuración desde la ruta nueva | Pendiente |

## Detalle

### IF-01 `camelCase`

- **Qué cambia:** los nombres de campo JSON de todos los endpoints de identidad, compañía y proyecto
  pasan a `camelCase`.
- **Qué rompe:** cualquier acceso por nombre de propiedad del cliente actual, incluidas las interfaces
  TypeScript escritas a mano.
- **Mitigación:** el cliente se genera desde el contrato OpenAPI; no se mantienen modelos escritos a
  mano. Verificación: `T-C-01` sobre el documento y compilación del cliente regenerado.
- **Fuera de alcance:** ninguna corrección manual del cliente `v0.0.1`.

### IF-02 `ProblemDetails`

- **Qué cambia:** todos los errores dejan de usar el sobre propio `{error, meta}` y pasan a
  `ProblemDetails` con el código de negocio en `extensions.code`.
- **Qué rompe:** lectura del mensaje de error, traducciones y mensajes por campo.
- **Mitigación:** un interceptor único que lea `extensions.code` y traduzca; se actualiza en la misma
  ventana en que se regenera el cliente, no antes.
- **Verificación:** `T-C-01` y prueba manual de los tres casos base (validación, `403`, `409`).

### IF-03 Rol Director de proyectos

- **Qué cambia:** el código de rol es `DIRECTOR` y el nombre visible es Director de proyectos.
- **Qué rompe:** guardias de ruta, menús por rol y cualquier comparación con el término anterior.
- **Mitigación:** regenerar el cliente y sustituir las constantes de rol en un solo punto del código
  del cliente.
- **Verificación:** recorrido manual de las rutas protegidas por rol después de la regeneración.

### IF-04 Dueño único de `/companies` y `/users`

- **Qué cambia:** esas rutas solo existen en `M-01`; el duplicado de `M-02` desaparece.
- **Qué rompe:** los servicios del cliente que apuntaban a la copia de `M-02`, con permisos distintos.
- **Mitigación:** eliminar el servicio redundante al regenerar; el cliente queda con un único servicio
  por recurso.
- **Verificación:** el documento OpenAPI no declara rutas duplicadas (`T-C-01`).

### IF-05 Paginación `page`/`pageSize`

- **Qué cambia:** los parámetros de paginación son `page` y `pageSize`, con el conjunto `{10, 50, 100}`
  y un máximo configurable.
- **Qué rompe:** los listados que enviaban `page_size` o `pageNumber`.
- **Mitigación:** ajuste puntual en los servicios de listado al regenerar.
- **Verificación:** `T-C-01` y recorrido manual de un listado por recurso.

### IF-06 Credenciales, bloqueo, verificación de correo y datos del registro

- **Qué cambia:** el hash lo gestiona Identity; la cuenta se bloquea por intentos fallidos; el
  autorregistro exige verificación de correo y pide identidad completa (tipo y número de documento,
  primer y segundo nombre, primer y segundo apellido, fecha de nacimiento, correo, nombre de usuario y
  contraseña); la residencia se completa después en el perfil.
- **Qué rompe:** hoy **no existe formulario de registro en el cliente** —`AuthService` solo consume
  `/auth/login`—, así que este impacto es de construcción, no de corrección: hay que crear la pantalla
  con los campos nuevos. También cambian el mensaje de cuenta deshabilitada y el flujo de recuperación
  asistida.
- **Mitigación:** regenerar el cliente, crear el formulario de registro con los campos nuevos y probar
  el flujo completo después de `E-1b`.
- **Verificación:** `T-S-07`, `T-S-11`, `T-S-12` y prueba manual del flujo completo.

### IF-07 Pertenencia a compañía y proyecto

- **Qué cambia:** la pertenencia a compañía vive en `User.company_id` y la de proyecto en
  `ProjectMember` (`D-98`); desaparecen `user_company_memberships` y `project_user_memberships`, con su
  historial de altas y bajas.
- **Qué rompe:** `PATCH /companies/{companyId}/memberships/{membershipId}/deactivate` pasa a
  `PATCH /companies/{companyId}/memberships/{userId}/deactivate`; la respuesta de
  `POST /companies/{companyId}/memberships` deja de traer `membershipId` (solo `companyId`, `userId` e
  `isActive`); la baja de proyecto elimina la fila de `ProjectMember`, de modo que volver a asignar al
  mismo usuario es una asignación nueva y no un alta sobre un historial.
- **Mitigación:** regenerar el cliente y sustituir el identificador de membresía por el `userId` en la
  baja de compañía; ajustar los tipos de respuesta de las dos asignaciones.
- **Verificación:** `T-I-29`, `SC-IDENTIDAD-28`…`SC-IDENTIDAD-31` y `SC-PROYECTOS-26`…`SC-PROYECTOS-28`.

### IF-08 Configuración del proyecto como entidad propia

- **Qué cambia:** la configuración es una entidad separada 1:1 con el proyecto (`D-93`) y se consulta en
  `GET /api/v1/projects/{projectId}/configuration` (permiso `PROJECTS_READ`). Devuelve `baseCurrencyId`,
  `budgetLimit`, `aiuAdminPercent`, `aiuUnforeseenPercent`, `aiuProfitPercent`, `signatureMode`,
  `signerUserId` y `requiresAuditor`.
- **Campos nuevos en crear y editar proyecto:** `budgetLimit` (decimal opcional, mayor que cero, 18,2) y
  `requiresAuditor` (booleano opcional); `signingMode` admite ahora tres valores: `SIN_FIRMA` (nuevo),
  `UNICA_POR_PRESUPUESTADOR_ASIGNADO` e `INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE`.
- **Qué rompe:** los formularios que ofrecían solo dos modos de firma y no enviaban tope; las vistas que
  deducían la divisa, el AIU y la firma desde el proyecto en lugar de la configuración; el detalle del
  proyecto (`GET /projects/{id}`) ahora incluye `budgetLimit`, `signatureMode`, `signerUserId` y
  `requiresAuditor`, y su origen es la configuración.
- **Mitigación:** regenerar el cliente, añadir tope presupuestal y requisito de auditor al formulario,
  incorporar `SIN_FIRMA` al selector y leer la configuración por su ruta cuando la pantalla sea de
  configuración. `budgetLimit` es opcional: el proyecto se puede crear sin tope.
- **Verificación:** `T-I-30`, `SC-PROYECTOS-22`, `SC-PROYECTOS-23` y `T-C-01` sobre el documento OpenAPI.

## Regla de ventana única

1. Todo el cambio incompatible de `E-1b` ocurre en **una sola ventana**; no se publican dos contratos en
   paralelo y no se mantiene compatibilidad con el cliente `v0.0.1` (`D-109`, `A-10`).
2. Mientras la ventana está abierta, el cliente Angular no es un criterio de aceptación de `E-1b`; el
   criterio es el contrato documentado y validado (`T-C-01`).
3. La regeneración del cliente y las correcciones de `IF-01`…`IF-06` pertenecen a la etapa de interfaz,
   posterior a `E-2`; hasta entonces el sistema se verifica con Swagger y con las pruebas de API
   (`D-42`, `D-66`).
4. Si la etapa de interfaz no se abre, el impacto queda aceptado como deuda documentada: el producto se
   opera por API en entorno local o demo (`D-39`).

## Aceptación

El estudiante acepta el corte de compatibilidad del cliente Angular `v0.0.1` (`A-10`) y firma este
registro como parte del checklist de `PLAN §4.8.7` (punto 9) y de la tarea T030.

| Rol | Nombre | Fecha | Firma |
|---|---|---|---|
| Estudiante responsable | Sergio Parra | 30/10/2026 | Sergio Parra |
