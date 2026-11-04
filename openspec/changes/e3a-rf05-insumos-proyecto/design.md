# Design: e3a-rf05-insumos-proyecto

## Context

`E-1b` dejó la línea base de identidad, compañías y proyectos (`openspec/specs/identidad`,
`openspec/specs/proyectos`). El catálogo `Municipality` ya existe en
`BudgetCivil.Domain/Catalogs`; **no** existen `Resource` ni `ResourceType`, y el módulo `M-04` está
vacío. La autorización se resuelve por `permission_key` con `SecurityBaselineSeeder` (versión `v3`), el
alcance por proyecto se resuelve con el rol del actor y `ProjectMember`, y el listado reutiliza
`ListingQueryPipeline` para `page`/`pageSize`.

`E-3a` depende de `E-2` (proyecto y EDT). En el código actual todavía no hay entidades de EDT, así que
este diseño asume que `Project` y `ProjectMember` estarán disponibles y que la migración se genera sobre
la cadena vigente, no sobre la baseline de `E-1b`. Ver `proposal.md` para la motivación.

## Goals / Non-Goals

**Goals:**

- Modelar `Resource` con los siete tipos, el estado de cotización por completitud, autoría y `rowversion`.
- Exponer listado, detalle, alta, edición, copia y sugerencia de similitud bajo
  `/api/v1/projects/{projectId}/resources`.
- Resolver el alcance por compañía y proyecto sin que `M-04` dependa de la infraestructura de `M-02`.
- Añadir permisos y sembrar los siete `ResourceType` de forma idempotente.
- Cubrir con pruebas unitarias, de integración manual y de arquitectura.

**Non-Goals:**

- Transición a `COTIZADO` y cambio masivo (`RF-11`, `E-4a`).
- Composición de Matriz, `ApuResource`, copia de APU y reconciliación al incorporar un APU (`E-3b`).
- Copia desde el catálogo público y publicación (`E-5`).
- Conteo de uso (`RF-22`), cálculo (`RF-10`) y cierre (`RF-21`).
- Endpoint de baja lógica y frontend (`D-49`, `D-109`).

## Decisions

### D1. Modelo de dominio

`Resource` vive en `BudgetCivil.Domain/Resources` y se crea solo a través de métodos que fijan autoría y
estado; no se exponen setters públicos para `ProjectId`, `QuotationStatus`, `RowVersion`,
`LastModifiedByUserId` ni `LastModifiedAtUtc`.

```text
Resource
  Id, ProjectId, ResourceTypeId, MunicipalityId?
  Description, Unit?, UnitPrice?, Brand?, Distributor?, UserKey?, Details?, ContactNumber?
  QuotationStatus, OriginResourceId?
  LastModifiedByUserId, LastModifiedAtUtc, RowVersion
  IsActive (baja lógica reservada, sin endpoint en E-3a)
```

`QuotationStatus` es una enumeración de cadena: `FALTA_INFORMACION`, `POR_COTIZAR`, `COTIZADO`.
`COTIZADO` se declara para que el contrato y la base no cambien en `E-4a`, pero **E-3a no lo produce**.

`Resource.CalculateQuotationStatus()`:

```text
completo = Description no vacía && Unit no vacía && UnitPrice.HasValue && Brand no vacía
estado   = completo ? POR_COTIZAR : FALTA_INFORMACION
```

Se recalcula en el alta, en la edición y en la copia. La interacción con un insumo ya `COTIZADO` se
decide en `E-4a` (ver Open Questions).

### D2. Persistencia

- Tabla `resources` con nombres `snake_case` y esquema único (`D-95`).
- `resource_types` como catálogo de referencia: `id`, `code`, `name`, `is_active`.
- `unit_price` `decimal(18,2)` con `CHECK (unit_price >= 0)`; `description` 255, `unit` 20, `brand` 100,
  `distributor` 150, `user_key` 50, `contact_number` 50, `details` `nvarchar(max)`.
- `row_version` con `IsRowVersion()` (`D-50`, `D-97`).
- `is_active` con valor por defecto `true` y `deleted_at` nullable; filtro global de consulta. No hay
  endpoint de baja en este cambio (`D-49`).
- FKs `RESTRICT`: `project_id` → `projects`, `resource_type_id` → `resource_types`,
  `municipality_id` → `municipalities`, `origin_resource_id` → `resources`, y la propiedad
  `LastModifiedByUserId` mapeada a `last_modified_by` → `users`. `LastModifiedAtUtc` se mapea a
  `last_modified_at` (`PLAN §3.3.2`).
- Índices: `IX_resources_project_id_resource_type_id`,
  `IX_resources_project_id_quotation_status`, `IX_resources_project_id_description` (`PLAN §3.3.5`).
- **Sin** índice único sobre `description`, `user_key` ni (`project_id`, `description`): la unicidad es
  una sugerencia (`Q-09`, `D-16`).

### D3. Estado de cotización

El estado se deriva en el dominio y se persiste como `VARCHAR(30)` con
`CHECK (quotation_status IN ('FALTA_INFORMACION','POR_COTIZAR','COTIZADO'))`. No hay disparador ni cálculo
en SQL; la base solo garantiza el conjunto cerrado. Esto mantiene una sola fuente de verdad y permite
que `E-4a` añada la transición sin migrar datos.

### D4. Autorización y alcance

- Permisos nuevos en `SecurityBaselineSeeder` (subir `SeedVersion` a `v4`):
  `RESOURCES_READ` y `RESOURCES_WRITE`.
- Asignación: Administrador → `RESOURCES_READ`; Director → `RESOURCES_READ`, `RESOURCES_WRITE`;
  Presupuestador → `RESOURCES_READ`, `RESOURCES_WRITE`; Auditor → `RESOURCES_READ`.
- El permiso se exige en el endpoint (`PermissionAuthorizationHandler`); el **alcance** se resuelve en
  el caso de uso con `IActorAccessReader` y el rol:
  - Administrador: lectura de cualquier proyecto.
  - Director: lectura y escritura en proyectos de su compañía.
  - Presupuestador: lectura y escritura solo en proyectos con `ProjectMember` activo.
  - Auditor: lectura solo en proyectos con `ProjectMember` activo.
- `M-04` no consulta `DbContext` ni repositorios de `M-02`: define
  `IResourceProjectAccessReader` en `Application/Resources/Repositories`, implementado en
  Infrastructure, que responde compañía del proyecto y pertenencia del actor. Así se respeta la regla de
  módulos (`PLAN §3.4.1`) y la prueba de arquitectura.

### D5. Superficie de API

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/projects/{projectId}/resources` | `RESOURCES_READ` | Listado con búsqueda, filtros y paginación |
| `GET` | `/api/v1/projects/{projectId}/resources/{resourceId}` | `RESOURCES_READ` | Detalle |
| `POST` | `/api/v1/projects/{projectId}/resources` | `RESOURCES_WRITE` | Alta |
| `PUT` | `/api/v1/projects/{projectId}/resources/{resourceId}` | `RESOURCES_WRITE` | Edición con `rowVersion` |
| `POST` | `/api/v1/projects/{projectId}/resources/copy` | `RESOURCES_WRITE` | Copia desde otro proyecto |
| `GET` | `/api/v1/projects/{projectId}/resources/suggestions` | `RESOURCES_WRITE` | Sugerencia de similitud |

- `CreateResourceRequest` no incluye `projectId`, `quotationStatus`, `originResourceId`, autoría ni
  `rowVersion`; el proyecto sale de la ruta y el resto lo fija el servidor.
- `UpdateResourceRequest` exige `rowVersion`; `projectId` no se puede cambiar en una edición (mover un
  insumo entre proyectos se resuelve por copia).
- `CopyResourceRequest` solo lleva `sourceResourceId`; el destino sale de la ruta.
- Respuesta de listado: `items`, `page`, `pageSize`, `totalItems`; `ResourceDto` expone autoría y
  `rowVersion` en Base64, nunca datos de otra compañía.
- Errores con `ProblemDetails` y `extensions.code`: `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`,
  `RESOURCE_NOT_FOUND`, `RESOURCE_READ_FORBIDDEN`, `RESOURCE_WRITE_FORBIDDEN`,
  `RESOURCE_CONCURRENCY_CONFLICT`, `PROJECT_SCOPE_FORBIDDEN` (`D-100`).

### D6. Búsqueda, filtros y paginación

- Se reutiliza `ListingQueryPipeline` para normalizar `page`/`pageSize`; el conjunto admitido se toma de
  configuración con `{10, 50, 100}` por defecto.
- La búsqueda usa coincidencia parcial sobre `description` sin distinguir mayúsculas; los filtros se
  combinan con `AND` y se traducen a predicados EF sobre `resources`.
- Los filtros se validan con FluentValidation antes de consultar: tipo existente, estado dentro del
  conjunto cerrado, `municipalityId` existente y rango de precio con mínimo ≤ máximo y mínimo ≥ 0.
- El total se calcula con la misma consulta filtrada antes de paginar.

### D7. Sugerencia de similitud

- Consulta independiente `GET .../suggestions?description=&userKey=` que devuelve hasta diez insumos
  del mismo proyecto, activos, con coincidencia parcial de descripción o igual `user_key`.
- La UI la invoca **antes** de enviar el alta o la edición; el alta y la edición nunca consultan
  similitudes ni fallan por duplicado. Así la sugerencia es observable y no acopla la escritura.
- `E-3b` reutilizará esta consulta para la reconciliación de `D-25`, sin cambiar el contrato.

### D8. Copia independiente

`CopyResourceCommandHandler`:

1. Resuelve el proyecto destino y exige escritura.
2. Carga el insumo de origen y exige lectura sobre su proyecto; si es de otra compañía, responde como
   inexistente.
3. Crea una entidad nueva con `ProjectId` destino, los mismos datos y precio, `OriginResourceId = null`,
   `LastModifiedByUserId = actor`, `LastModifiedAtUtc = now` y estado recalculado por completitud.
4. Guarda en una sola transacción. No copia vínculos, aprobaciones ni componentes de Matriz.

La independencia se garantiza por construcción: no hay referencia compartida ni actualización en
cascada (`R-17`, `D-29`). La copia de componentes de Matriz se añade en `E-3b`.

### D9. Concurrencia y errores

- La edición compara `rowVersion`; si no coincide, lanza `ConflictAppException` con
  `RESOURCE_CONCURRENCY_CONFLICT` y la API responde `409`.
- El alta y la copia no aceptan `rowVersion`.
- El middleware de errores existente traduce las excepciones a `ProblemDetails`; no se añade un formato
  nuevo (`D-100`).

### D10. Migración y datos iniciales

- Una migración nueva `YYYYMMDDHHMM_e3a_resources` con `resources`, `resource_types`, FKs, `CHECK` e
  índices (`D-107`). No migra datos: la tabla nace vacía.
- `ResourceTypeSeeder` idempotente, con GUID fijos y clave `code`:
  `MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`, `WORK`, `MATRIX`, con los nombres en español del
  `PLAN §3.3.7`. Se ejecuta al arrancar, después del `seed` de seguridad.
- No se toca la carga de `Municipality`; el catálogo ya existe.
- Reversión: `Down` elimina los índices, las FKs y las tablas; el `seed` es idempotente y no requiere
  reversión.

### D11. Pruebas

- **Unitarias** (`Tests.Unit`): completitud y estado, copia independiente, autoría, alcance por rol y
  validadores.
- **Integración manual** (`Tests.Integration`, SQL Server local, `D-66`): FKs, `CHECK`, `rowversion`,
  índices, copia y aislamiento entre dos compañías.
- **Arquitectura** (`Tests.Architecture`): `Domain` sin EF, `Application` sin `DbContext`, `Api` sin
  persistencia y `M-04` sin infraestructura de `M-02`.

## Risks / Trade-offs

| Riesgo | Mitigación |
|---|---|
| Duplicados por ausencia de restricción única | La sugerencia y la advertencia son la única defensa; se acepta porque `Q-09` lo exige y el usuario decide |
| Búsqueda `LIKE` sobre descripción en tablas grandes | Índice `(project_id, description)` y alcance por proyecto; se revisa en `E-6` si el volumen crece |
| Copia de una Matriz sin componentes en `E-3a` | Se copia como insumo simple; `E-3b` extiende la copia a componentes y lo verifica |
| Estado `COTIZADO` declarado pero no producido | `E-4a` define la transición y la edición de un insumo cotizado; el contrato y la base ya lo admiten |
| Cadena de migraciones con objetos ajenos a `E-3a` | Antes de generar, `dotnet ef migrations list` y revisión del snapshot; no se elimina ninguna migración en este cambio |
| Permisos compartidos con otros cambios en curso | El `seed` es versionado e idempotente; se sube a `v4` y se revisa el inventario antes de aplicar |

## Migration Plan

1. Respaldo de la base y revisión de la cadena (`dotnet ef migrations list`).
2. Generar `YYYYMMDDHHMM_e3a_resources` y revisar el script (`dotnet ef migrations script`).
3. Aplicar en ventana de mantenimiento; ejecutar el `seed` y verificar los siete tipos.
4. Verificar `CHECK`, FKs, índices y `rowversion`; ejecutar la suite unitaria y la integración manual.
5. Reversión: `Down` de la migración y restauración del respaldo si hubiera datos.

## Open Questions

- El comportamiento de editar un insumo ya `COTIZADO` (¿vuelve a `FALTA_INFORMACION`, conserva el estado
  o exige recotizar?) pertenece a `E-4a`; no cambia el alcance de `E-3a` porque este cambio no produce
  `COTIZADO`.
- La copia de componentes de Matriz se especifica en `E-3b`; `E-3a` no crea componentes.
