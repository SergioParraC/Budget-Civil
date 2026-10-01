# Quickstart: Validacion Backend del Feature Companias y Proyectos

Date: 2026-09-30
Feature: specs/002-gestion-companias-proyectos/spec.md
Scope: Backend only

## 1. Purpose

Validar de extremo a extremo que el backend del feature 002 cumple:
- ciclo de vida de companias/proyectos,
- matriz de permisos por rol,
- transiciones de estado explicitas,
- trazabilidad separada,
- listados paginados,
- y reglas de persistencia por esquemas (FR-048/049/050).

Referencias:
- Contracts: specs/002-gestion-companias-proyectos/contracts/companies-projects-api.md
- Data model: specs/002-gestion-companias-proyectos/data-model.md
- Research: specs/002-gestion-companias-proyectos/research.md
- Plan: specs/002-gestion-companias-proyectos/plan.md

## 2. Prerequisites

- .NET SDK 10 instalado
- SQL Server 2022 accesible
- Solucion restaurable en `backend/BudgetCivil.Backend.sln`
- Conexion configurada para API/Infrastructure
- Permisos y catalogos base cargados

## 3. Setup

1. Restaurar paquetes:

```bash
dotnet restore backend/BudgetCivil.Backend.sln
```

2. Aplicar migraciones:

```bash
dotnet ef database update --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api
```

3. Ejecutar pruebas black-box:

```bash
dotnet test backend/tests/BudgetCivil.Backend.BlackBoxTests
```

4. Iniciar API para validaciones manuales:

```bash
dotnet run --project backend/src/BudgetCivil.Api
```

## 4. Validation Scenarios

## Scenario A: Cumplimiento de esquemas (FR-048)

Pasos:
1. Confirmar que existen tablas en esquemas `param`, `iam`, `dbo`, `audit` y `log`.
2. Ejecutar consulta en SQL Server:

```sql
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name IN ('param','iam','dbo','audit','log')
ORDER BY s.name, t.name;
```

Resultado esperado:
- Las entidades del feature aparecen en el esquema correcto segun `data-model.md`.

## Scenario B: Cumplimiento de direccion de FK (FR-049)

Pasos:
1. Ejecutar consulta de validacion de FK no permitidas:

```sql
SELECT
    fs.name AS from_schema,
    ft.name AS from_table,
    ts.name AS to_schema,
    tt.name AS to_table,
    fk.name AS fk_name
FROM sys.foreign_keys fk
JOIN sys.tables ft ON fk.parent_object_id = ft.object_id
JOIN sys.schemas fs ON ft.schema_id = fs.schema_id
JOIN sys.tables tt ON fk.referenced_object_id = tt.object_id
JOIN sys.schemas ts ON tt.schema_id = ts.schema_id
WHERE
    (fs.name = 'param' AND ts.name IN ('dbo','audit','log'))
    OR (fs.name = 'iam' AND ts.name IN ('dbo','audit','log'))
    OR (ts.name = 'log')
    OR (fs.name = 'log' AND ts.name IN ('param','iam','dbo','audit'));
```

Resultado esperado:
- Cero filas retornadas.

## Scenario C: Cadena y nomenclatura de migraciones (FR-050)

Pasos:
1. Listar archivos de migracion de Infrastructure.
2. Verificar orden temporal y formato de nombre.

PowerShell de apoyo:

```powershell
Get-ChildItem backend/src/BudgetCivil.Infrastructure -Recurse -Filter *.cs |
Where-Object { $_.Name -match '^[0-9]{12}_.+_.+\.cs$' } |
Select-Object -ExpandProperty Name
```

Resultado esperado:
- Nombres de migracion con formato `YYYYMMDDHHMM_<scope>_<change>`.
- Orden de cambios consistente con `param -> iam -> dbo -> audit -> log`.
- Constraints/indexes siguiendo prefijos `PK_`, `FK_`, `IX_`, `UQ_`, `CK_`.

## Scenario D: Catalogos reutilizables y paginacion

Pasos:
1. Consultar catalogos (`countries`, `departments`, `municipalities`, `currencies`, `document-types`) con `pageSize=10`, `50`, `100`.
2. Repetir con `pageSize=25`.

Resultado esperado:
- `10/50/100` responden 200.
- `25` responde 400 con `PAGE_SIZE_INVALID`.

## Scenario E: Ciclo de vida de compania

Pasos:
1. Crear compania.
2. Editar compania.
3. Inhabilitar compania.
4. Intentar operacion con usuario no autorizado de compania inhabilitada.
5. Rehabilitar compania.

Resultado esperado:
- Sin borrado fisico.
- Trazabilidad de creacion/modificacion preservada.
- Usuario no autorizado recibe `COMPANY_INACTIVE` o error funcional equivalente.
- Rehabilitacion solo restituye memberships activas.

## Scenario F: Creacion de proyecto y precondiciones

Pasos:
1. Intentar crear proyecto sin Director de proyectos activo en la compania.
2. Crear proyecto con Director activo.
3. Crear proyecto con actor Presupuestador y verificar autoasignacion de Director encargado.

Resultado esperado:
- Paso 1 rechazado con conflicto funcional.
- Paso 2 exitoso con estado inicial `POR_COTIZAR`.
- Paso 3 exitoso con asignacion automatica correcta.

## Scenario G: Maquina de estados y permisos por rol

Pasos:
1. `POR_COTIZAR -> FALTA_INFORMACION` con Director.
2. `FALTA_INFORMACION -> COTIZADO` con Presupuestador.
3. Intentar `COTIZADO -> FINALIZADO` con Presupuestador.
4. `COTIZADO -> FINALIZADO` con Director y firma activa.
5. `ANY -> INHABILITADO` con Director.
6. Intentar `ANY -> INHABILITADO` con Presupuestador.
7. Reapertura `FINALIZADO -> FALTA_INFORMACION` con Director.

Resultado esperado:
- Pasos permitidos responden 200.
- Pasos no permitidos responden 403 o error funcional de transicion invalida.

## Scenario H: Reactivacion desde INHABILITADO

Pasos:
1. Ejecutar `/projects/{id}/status/reactivate` con Director.
2. Repetir con Administrador.
3. Intentar salida de `INHABILITADO` hacia estado diferente de `FALTA_INFORMACION`.

Resultado esperado:
- Director y Administrador pueden reactivar.
- Estado destino siempre `FALTA_INFORMACION`.
- Destino distinto se rechaza.

## Scenario I: Trazabilidad separada

Pasos:
1. Cambiar estado de proyecto.
2. Cambiar visibilidad del proyecto.
3. Consultar evidencias en `audit.ProjectStatusTransition` y `audit.ProjectVisibilityTransition`.

Resultado esperado:
- Se registran eventos independientes con actor y timestamp.

## Scenario J: Asignaciones en compania/proyecto

Pasos:
1. Director asigna usuario permitido a compania y proyecto.
2. Presupuestador intenta asignar usuario en compania.
3. Presupuestador asigna Presupuestador/Auditor en proyecto.
4. Auditor intenta asignar en compania/proyecto.
5. Administrador intenta asignar rol no Director en proyecto.

Resultado esperado:
- Solo operaciones permitidas por matriz de rol se ejecutan.
- Violaciones de regla responden con denegacion/validacion.

## Scenario K: Listados por rol y alcance

Pasos:
1. Administrador lista usuarios globales (`GET /users`).
2. Administrador/Director lista usuarios por compania.
3. Director/Presupuestador lista usuarios de su compania.
4. Presupuestador/Auditor lista solo Presupuestador/Auditor de su compania.
5. Administrador lista companias globales.
6. Administrador lista proyectos globales.
7. Director lista proyectos por su compania autenticada.

Resultado esperado:
- Se respeta visibilidad por rol/compania y paginacion valida.

## Scenario L: Fail-closed de permisos

Pasos:
1. Desactivar permiso requerido para endpoint privado.
2. Ejecutar endpoint con JWT valido.
3. Reactivar permiso y repetir.

Resultado esperado:
- Con permiso inactivo: denegacion.
- Con permiso activo: operacion normal.

## 5. Implemented BlackBox Test Inventory

Rutas reales en `backend/tests/BudgetCivil.Backend.BlackBoxTests/Features`:

- `Features/Companies/CompanyLifecycleContractTests.cs`
- `Features/Companies/CompanyInactiveAccessTests.cs`
- `Features/Projects/ProjectLifecycleContractTests.cs`
- `Features/Projects/ProjectStatusAuthorizationTests.cs`
- `Features/Projects/ProjectStatusReactivationTests.cs`
- `Features/Memberships/CompanyMembershipAuthorizationTests.cs`
- `Features/Memberships/ProjectMembershipAuthorizationTests.cs`
- `Features/Memberships/UserCompanyConstraintTests.cs`
- `Features/Listings/CatalogListingsContractTests.cs`
- `Features/Listings/RoleScopedListingsContractTests.cs`
- `Features/Listings/PaginationValidationTests.cs`
- `Features/SchemaGovernance/SchemaAllocationSmokeTests.cs`
- `Features/SchemaGovernance/ForeignKeyDirectionRulesTests.cs`
- `Features/SchemaGovernance/MigrationNamingConventionTests.cs`
- `Features/Security/FailClosedAuthorizationTests.cs`
- `Features/Security/CompanyIsolationTests.cs`

Comandos utiles:

```bash
dotnet test backend/tests/BudgetCivil.Backend.BlackBoxTests/BudgetCivil.Backend.BlackBoxTests.csproj --filter "FullyQualifiedName~Listings"
dotnet test backend/tests/BudgetCivil.Backend.BlackBoxTests/BudgetCivil.Backend.BlackBoxTests.csproj --filter "FullyQualifiedName~SchemaGovernance"
dotnet test backend/tests/BudgetCivil.Backend.BlackBoxTests/BudgetCivil.Backend.BlackBoxTests.csproj
```

## 6. Exit Criteria

La validacion del feature se considera completa cuando:
- Escenarios A-L pasan con resultados esperados.
- No existen cambios automaticos de estado por editar proyecto.
- `INHABILITADO` puede aplicarse desde cualquier fase solo por Director.
- Salida de `INHABILITADO` solo por Director o Administrador y siempre a `FALTA_INFORMACION`.
- Reapertura `FINALIZADO -> FALTA_INFORMACION` solo por Director.
- Todos los listados validan `pageSize` en {10, 50, 100}.
- Trazabilidad de estado/visibilidad se registra por separado.
- Se cumple la segregacion por esquema y reglas de FK/migraciones de FR-048/049/050.
