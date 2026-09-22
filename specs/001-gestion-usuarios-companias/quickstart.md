# Quickstart: Validacion Backend del Feature Usuarios y Companias

Date: 2026-09-22
Feature: specs/001-gestion-usuarios-companias/spec.md
Scope: Backend only

## 1. Purpose

Validar end-to-end que el backend del feature cumple autenticacion, autorizacion, multitenancy y auditoria bajo la arquitectura de 4 capas.

Referencias:
- Contracts: specs/001-gestion-usuarios-companias/contracts/users-companies-api.md
- Data model: specs/001-gestion-usuarios-companias/data-model.md
- Research decisions: specs/001-gestion-usuarios-companias/research.md

## 2. Prerequisites

- .NET SDK 10 instalado
- SQL Server 2022 disponible
- Proyecto backend con capas:
  - BudgetCivil.Api
  - BudgetCivil.Application
  - BudgetCivil.Domain
  - BudgetCivil.Infrastructure
- Conexion a BD configurada en un unico punto de app config backend

## 3. Setup

1. Restaurar paquetes:

```bash
dotnet restore
```

2. Aplicar migraciones:

```bash
dotnet ef database update --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api
```

3. Iniciar API:

```bash
dotnet run --project backend/src/BudgetCivil.Api
```

4. Ejecutar pruebas del feature (suite unica):

```bash
dotnet test backend/tests/BudgetCivil.Backend.BlackBoxTests
```

## 4. Validation Scenarios

### Scenario A: Seed baseline idempotente

Pasos:
1. Ejecutar migraciones y seed baseline una vez.
2. Reejecutar migraciones y seed sin limpiar BD.

Resultado esperado:
- No se duplican `Role`, `Permission` ni `RolePermission`.
- No falla la inicializacion del baseline.

### Scenario B: Bootstrap del primer Administrador (idempotente)

Pasos:
1. Ejecutar `POST /api/v1/admin/bootstrap/first-administrator` sin administradores previos.
2. Repetir la misma solicitud.

Resultado esperado:
- Primera ejecucion crea exactamente un Administrador y evento de auditoria.
- Segunda ejecucion devuelve conflicto idempotente (`409`) sin crear otro Administrador.

### Scenario C: Login, intentos fallidos y lockout

Pasos:
1. Login exitoso con email.
2. Login exitoso con username.
3. Ejecutar 5 intentos fallidos consecutivos.
4. Reintentar login.

Resultado esperado:
- Login valido devuelve token.
- Al quinto fallo, la cuenta queda bloqueada.
- Respuesta muestra mensaje obligatorio de cuenta no habilitada.

### Scenario D: Creacion de usuarios por rol

Pasos:
1. Crear usuario con actor Administrador.
2. Intentar crear usuario administrativo con actor Gerente de proyectos.
3. Intentar crear usuario administrativo con actor Presupuestador.
4. Intentar crear usuario administrativo con actor Auditor.
5. Ejecutar self-registration publico (`POST /api/v1/auth/register`) con payload valido.
6. Repetir self-registration incluyendo `rol_asignado` o `company_id` en payload.

Resultado esperado:
- Solo Administrador crea usuarios por endpoint administrativo.
- Gerente, Presupuestador y Auditor reciben `403` en endpoint administrativo.
- Self-registration crea usuario con rol Presupuestador y `company_id = null`.
- Self-registration con campos restringidos (`rol_asignado`, `company_id`, `project_id`) se rechaza con `400`.

### Scenario E: Fail-closed por rol/permiso inactivo

Pasos:
1. Inactivar un rol que tenga acceso a endpoint privado.
2. Ejecutar endpoint privado con usuario de ese rol.
3. Reactivar rol, pero inactivar el `RolePermission` requerido.
4. Reejecutar endpoint privado.

Resultado esperado:
- En ambos casos, respuesta `403`.
- Se registra evento de seguridad/auditoria de denegacion.

### Scenario F: Privacidad por CompanyId

Pasos:
1. Preparar datos en al menos dos companias.
2. Consultar `GET /api/v1/users` autenticado como actor de la compania A.
3. Intentar leer/modificar recursos de compania B.

Resultado esperado:
- Solo se exponen datos dentro del alcance permitido.
- Accesos cross-tenant se deniegan (`403`) o retornan vacio segun politica del endpoint.

### Scenario G: Administracion de companias

Pasos:
1. Crear compania con Administrador.
2. Crear compania propia con Gerente de proyectos sin compania asociada.
3. Reintentar creacion de segunda compania con el mismo Gerente.
4. Editar compania con Administrador.
5. Editar compania propia con Gerente.
6. Intentar editar compania ajena con Gerente.
7. Desactivar compania con Administrador.
8. Repetir desactivacion con Gerente y otros roles no permitidos.

Resultado esperado:
- Administrador tiene gestion completa (alta/edicion/desactivacion) sin borrado fisico.
- Gerente puede crear exactamente una compania propia y editar solo su propia compania.
- Segunda creacion de compania por el mismo Gerente falla con `409`.
- Desactivacion por Gerente se rechaza con `403`.

### Scenario H: Autoservicio y cambio de password

Pasos:
1. Ejecutar `GET /api/v1/profile/me` y `PUT /api/v1/profile/me` con cada rol.
2. Ejecutar `POST /api/v1/profile/me/change-password` con password invalida y valida.
3. Verificar recuperacion asistida via `POST /api/v1/users/{userId}/password-recovery/enable` por Administrador.

Resultado esperado:
- Todos los roles pueden gestionar su cuenta propia.
- Password invalida se rechaza por politica minima.
- Recuperacion asistida solo por Administrador.

### Scenario I: Auditoria de bootstrap

Pasos:
1. Consultar `GET /api/v1/audit/bootstrap` con Administrador.
2. Repetir consulta con otros roles.

Resultado esperado:
- Administrador puede ver eventos.
- Otros roles reciben `403`.

### Scenario J: Consulta de APUs/insumos segun rol

Pasos:
1. Autenticar Gerente de proyectos, Presupuestador y Auditor.
2. Ejecutar consulta de lectura sobre capacidad existente de APUs publicos/insumos.
3. Autenticar Administrador.
4. Intentar la misma consulta con Administrador.

Resultado esperado:
- Roles operativos autorizados pueden consultar en modo solo lectura segun su alcance.
- Administrador recibe denegacion de acceso segun matriz funcional del feature.

## 5. Test Organization (Team Decision)

- Toda evidencia de prueba del feature vive en un unico proyecto:
  - `backend/tests/BudgetCivil.Backend.BlackBoxTests`
- Organizacion sugerida por carpetas:
  - `Authentication`
  - `Users`
  - `Companies`
  - `Security`
  - `Profile`
- No se crean proyectos separados `ContractTests` o `IntegrationTests` en esta fase.

Justificacion:
- Menor complejidad operativa.
- Menor duplicidad entre escenarios de contrato e integracion.
- Mejor trazabilidad FR -> endpoint -> evidencia en una sola suite.

## 6. Exit Criteria

La validacion del feature se considera completa cuando:

- Escenarios A-J ejecutados con resultado esperado.
- Endpoints privados validan `permission_key` activo y `CompanyId`.
- Politica fail-closed comprobada con denegaciones `403` auditadas.
- Lockout al quinto intento confirmado.
- No existe borrado fisico de usuarios/companias.
- Baseline seed de seguridad validado como idempotente.
