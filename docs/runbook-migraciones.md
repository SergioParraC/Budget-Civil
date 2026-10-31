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
   - `project_configurations` con clave `project_id` y `projects` sin `base_currency_id` ni AIU (`D-93`).
5. **Aplicar** en la ventana de mantenimiento:
   `sqlcmd -S .\SQLEXPRESS -d BudgetCivilDb -i artifacts/migracion.sql`
   o, contra la cadena configurada,
   `dotnet ef database update --project backend/src/BudgetCivil.Infrastructure --startup-project backend/src/BudgetCivil.Api`.
6. **Verificar** después de aplicar:
   - `SELECT * FROM __EFMigrationsHistory;` contiene la migración aplicada;
   - existen `users`, `projects`, `project_members`, `project_configurations` y los catálogos;
   - `dotnet ef migrations list` no reporta migraciones pendientes.
7. **Registrar** la versión aplicada (fecha, migración, responsable) en el control de cambios del
   proyecto.

Si el paso 6 falla, se restaura el respaldo del paso 1 y no se continúa con el despliegue.

## 3. Baseline vigente

| Migración | Contenido | Fecha |
|---|---|---|
| `202610300438_e1b_reconciliacion_baseline` | Esquema completo del modelo reconciliado de `E-1b`, sin migración de datos (base vacía, `D-98`/`D-95`/`D-107`) | 29/10/2026 |
| `202610300527_e1b_email_verification` | Tabla `email_verification_tokens` para la verificación de correo del autorregistro (`D-80`, `REQ-IDENTIDAD-07`) | 30/10/2026 |

La baseline se aplica sobre una base **vacía**. No existen migraciones intermedias: la cadena anterior se
eliminó porque la base fue vaciada por el autor (`DEC-19`).

## 4. Reversión

- Al no haber datos, la reversión es **recrear la base vacía** y volver a aplicar la baseline vigente.
- Si la baseline ya está aplicada en local o demo y se necesita volver a una versión anterior del código,
  se restaura el respaldo del paso 1 y se aplica el script de esa versión.
- El método `Down` de la baseline no se usa para conservar datos: borra el esquema.

## 5. Crear la base vacía (desarrollo)

```sql
CREATE DATABASE BudgetCivilDb;
```

Después de crearla, aplicar la baseline con el procedimiento de la sección 2. El `seed` de seguridad
(roles, permisos, administrador inicial y catálogos) se ejecuta en el siguiente arranque de la API.
