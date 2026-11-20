# Runbook de migraciones manuales

Procedimiento operativo para crear y aplicar el esquema de `BudgetCivilDb` **manualmente** (`D-40`, `D-107`).
La API **no** crea ni modifica el esquema al arrancar: solo ejecuta el `seed` de seguridad, que es
idempotente y versionado (`D-82`).

## 1. Requisitos

- .NET SDK 10 y la herramienta `dotnet-ef` 10 (`dotnet tool install --global dotnet-ef`).
- SQL Server 2022 local (en desarrollo, la instancia `SQLEXPRESS` de la máquina).
- Cadena de conexión `BudgetCivilDatabase` en `backend/src/BudgetCivil.Api/appsettings.json`
  (o `appsettings.Development.json` / variable de entorno `ConnectionStrings__BudgetCivilDatabase`).

## 2. Procedimiento

1. **Respaldar** la base antes de tocar el esquema (`BACKUP DATABASE BudgetCivilDb TO DISK = ...` desde
   SSMS o `sqlcmd`). Sin respaldo no se aplica nada.
2. **Revisar** el estado de migraciones:
   `dotnet ef migrations list --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api`
3. **Generar el script** idempotente y guardarlo para revisión:
   `dotnet ef migrations script --idempotent --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api --output artifacts/migracion.sql`
4. **Revisar el SQL** antes de aplicarlo:
   - un solo esquema (`dbo`), sin `param`, `iam`, `audit` ni `log` (`D-95`);
   - tablas en `snake_case` y restricciones con prefijo `PK_`/`FK_`/`IX_`/`UQ_`/`CK_` (`D-107`);
   - sin `user_company_memberships`, `project_user_memberships` ni `user_profiles` (`D-98`, `D-78`);
   - `project_configurations` con clave `project_id` y `projects` sin `base_currency_id` ni AIU (`D-93`);
   - `resources` con `project_id`, `resource_type_id`, `municipality_id`, `row_version`, autoría y baja
     lógica; `resource_types` con exactamente siete tipos (`D-49`, `D-50`, `D-96`);
   - `suppliers` con `company_id`, `business_name` obligatorio, contacto y ubicación opcionales,
     `is_active`, `row_version` y autoría; `resources.supplier_id` opcional con FK `RESTRICT` e índice;
     sin índice único sobre `business_name` ni `email` (`REQ-PROVEEDORES-01`…`06`, `D-27`, `D-96`).
5. **Aplicar** en la ventana de mantenimiento:
   `sqlcmd -S .\SQLEXPRESS -d BudgetCivilDb -i artifacts/migracion.sql`
   o, contra la cadena configurada,
   `dotnet ef database update --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api`.
6. **Verificar** después de aplicar:
   - `SELECT * FROM __EFMigrationsHistory;` contiene la migración aplicada;
   - existen `users`, `projects`, `project_members`, `project_configurations`, `resources`,
     `resource_types`, `measurement_units` y los catálogos;
   - `dotnet ef migrations list` no reporta migraciones pendientes.
7. **Registrar** la versión aplicada (fecha, migración, responsable) en el control de cambios del
   proyecto.

Si el paso 6 falla, se restaura el respaldo del paso 1 y no se continúa con el despliegue.

La API **no aplica migraciones** al arrancar (`D-40`). Antes del `seed` verifica si hay migraciones
pendientes y, si las hay, se detiene con un mensaje que indica el comando. Desde Visual Studio puedes
aplicarlas con la Consola del Administrador de paquetes:

```powershell
Update-Database -ProjectName BudgetCivil.Infrastructure -StartupProjectName BudgetCivil.Api
```

o desde la terminal con el comando `dotnet ef database update` mostrado en el mensaje.

## 3. Baseline vigente

| Migración | Contenido | Fecha |
|---|---|---|
| `202610300438_e1b_reconciliacion_baseline` | Esquema completo del modelo reconciliado de `E-1b`, sin migración de datos (base vacía, `D-98`/`D-95`/`D-107`) | 29/10/2026 |
| `202610300527_e1b_email_verification` | Tabla `email_verification_tokens` para la verificación de correo del autorregistro (`D-80`, `REQ-IDENTIDAD-07`) | 30/10/2026 |
| `20261031201251_e3b_users_company` | Historial de pertenencia `users_company` para la asignación a compañías (`D-98`) | 31/10/2026 |
| `202611040532_resources_add_e3a` | Tablas `resources` y `resource_types`, con índices, FKs, `CHECK` y `rowversion` (`REQ-INSUMOS-01`…`04`, `D-49`, `D-50`, `D-96`, `D-107`) | 04/11/2026 |
| `20261113040910_e3a_suppliers` | Tabla `suppliers` (compañía, razón social obligatoria, contacto y ubicación opcionales, baja lógica) y `resources.supplier_id` con FK `RESTRICT` e índice; sin migración de datos (`REQ-PROVEEDORES-01`…`06`, `D-27`, `D-40`, `D-96`, `D-107`) | 13/11/2026 |
| `20261114065800_wbs_add_i2b` | Tablas `wbs_codes`, `wbs_chapters`, `wbs_items` y `wbs_project_activity` con `CHECK`, FKs `RESTRICT`, índices, `rowversion`, `wbs_items.wbs_chapter_id` nullable e índice único filtrado de `apu_id`; sin migración de datos (`REQ-EDT-*`, `D-113`…`D-120`) | 14/11/2026 |
| `20261115092848_apus_add_e3b` | Tablas `apus`, `apu_resources` y `matrix_components` con `CHECK`, FKs e índices; FK `wbs_items.apu_id → apus` (`RESTRICT`); cantidades a `DECIMAL(18,10)`; `rounding_decimals` y `quantity_decimals` (0–10, default 2); sin migración de datos (`D-113`, `D-121`) | 15/11/2026 |
| `20261115230025_apus_remove_yield` | Elimina `CK_apus_yield` y la columna `apus.yield`; `Down` la recrea con default `1` (`D-123`, `REQ-APUS-22`) | 15/11/2026 |
| `20261120125801_units_add_catalog` | Crea `measurement_units` (`id` `INT IDENTITY`, `short_unit` `VARCHAR(10)`, `unit` `VARCHAR(100)`) con índice único sin distinguir mayúsculas y `CHECK` de no vacío; amplía `apus.unit` de `VARCHAR(5)` a `VARCHAR(10)`; sin migración de datos (`REQ-UNIDADES-01`, `REQ-APUS-02`, `D-16`, `D-107`) | 20/11/2026 |


La baseline se aplica sobre una base **vacía**. No existen migraciones intermedias: la cadena anterior se
eliminó porque la base fue vaciada por el autor (`DEC-19`).

La migración `20261113080043_wbs_add_i2b` no forma parte de esta cadena: pertenecía a una implementación
anterior de la EDT y se retiró de la base local el 14/11/2026. La migración vigente es
`20261114065800_wbs_add_i2b`, generada sobre la cadena actual y aplicada localmente el 14/11/2026. El
respaldo previo al reinicio quedó en
`H:\Program Files\MSSQL17.SQLEXPRESS\MSSQL\Backup\BudgetCivilDb_pre_reset_20261114.bak`.

## 4. Reversión

- Al no haber datos, la reversión es **recrear la base vacía** y volver a aplicar la baseline vigente.
- Si la baseline ya está aplicada en local o demo y se necesita volver a una versión anterior del código,
  se restaura el respaldo del paso 1 y se aplica el script de esa versión.
- El método `Down` de la baseline no se usa para conservar datos: borra el esquema.

### 4.1 Reversión de `e3a_suppliers`

`Down` de `20261113040910_e3a_suppliers` elimina, en este orden: la FK
`FK_dbo_resources_suppliers_supplier_id`, la tabla `suppliers`, el índice `IX_dbo_resources_supplier_id` y
la columna `resources.supplier_id`. No se puede revertir con datos asociados: exige respaldo previo
(paso 1) porque se pierden las asociaciones insumo → proveedor. Los permisos `SUPPLIERS_READ` y
`SUPPLIERS_WRITE` del `seed` se dejan inactivos, no se borran (`D-82`).

### 4.2 Reversión de `wbs_add_i2b`

`Down` de `20261114065800_wbs_add_i2b` elimina en orden inverso las tablas `wbs_project_activity`,
`wbs_items`, `wbs_chapters` y `wbs_codes`, junto con sus índices, `CHECK` y FKs `RESTRICT`. No se puede
revertir con datos asociados: exige respaldo previo (paso 1) porque se pierden los códigos y el árbol
completos. Los permisos `WBS_READ` y `WBS_WRITE` del `seed` se dejan inactivos, no se borran (`D-82`,
`D-119`).

### 4.3 Reversión de `apus_add_e3b`

`Down` de `20261115092848_apus_add_e3b` elimina, en orden inverso: la FK
`FK_dbo_wbs_items_apus_apu_id`, las tablas `apu_resources`, `matrix_components` y `apus`, los `CHECK`
`CK_project_configurations_rounding_decimals` y `CK_project_configurations_quantity_decimals`, las
columnas `rounding_decimals` y `quantity_decimals`, y devuelve `wbs_items.quantity_work` a
`DECIMAL(18,4)`. Exige respaldo previo (paso 1) porque se pierden APUs, Matrices y composiciones. Los
permisos `APUS_READ` y `APUS_WRITE` del `seed` `v8` se dejan inactivos, no se borran (`D-82`).

### 4.4 Reversión de `apus_remove_yield`

`Down` de `20261115230025_apus_remove_yield` agrega `apus.yield` como `DECIMAL(18,4)` no nulo con default
`1` y recrea `CK_apus_yield` (`yield > 0`). La base de desarrollo está vacía, así que no hay pérdida de
datos; en un entorno con filas, el valor histórico de rendimiento no se puede recuperar y se debe restaurar
el respaldo del paso 1 (`D-123`).

### 4.5 Reversión de `units_add_catalog`

`Down` de `20261120125801_units_add_catalog` devuelve `apus.unit` a `VARCHAR(5)` y elimina la tabla
`measurement_units` con su índice y `CHECK`. El `ALTER` puede fallar si existen unidades de 6–10 caracteres:
el runbook exige respaldo, revisión previa y limpieza o acortamiento manual; no se trunca automáticamente.
El permiso `CATALOGS_WRITE` del `seed` `v9` se deja inactivo, no se borra (`D-82`).


## 5. Crear la base vacía (desarrollo)

```sql
CREATE DATABASE BudgetCivilDb;
```

Después de crearla, aplicar la baseline con el procedimiento de la sección 2. El `seed` de seguridad
(roles, permisos y administrador inicial), el `seed` de los siete tipos de insumo y los catálogos se
ejecutan en el siguiente arranque de la API.
