# Design: unidades-medida-y-tipos-insumo

## Context

Hoy `Apu.UnitMaxLength` es 5 y `apus.unit` es `VARCHAR(5)`; `Resource.UnitMaxLength` es 20 y
`resources.unit` es `VARCHAR(20)`. Ambos guardan texto libre y no existe un catálogo de unidades.
`ResourceType` es un catálogo global sembrado con siete tipos (`Id` Guid, `Code`, `Name`, `IsActive`) en
`resource_types`; no hay configuración por proyecto ni endpoint que lo exponga. `CatalogsController` es
dueño de `/api/v1/catalogs` (`D-102`) y tiene `[AllowWithoutCompanyContext]`; `ResourcesController` vive en
`/api/v1/projects/{projectId}/resources` y usa `ResourceAccessService` para el alcance por proyecto. El
`seed` de seguridad está en `v8` con 37 permisos y existe `CATALOGS_READ`, pero no `CATALOGS_WRITE`.

No existe una entidad de “actividad” con unidad: `WbsProjectActivity` registra participantes y última
modificación del EDT. Los ítems del EDT heredan la unidad del APU. Ver `proposal.md - Why` para la
motivación.

Los insumos tienen baja lógica reservada (`Resource.IsActive`, `DeletedAtUtc` y el filtro global
`HasQueryFilter`) pero ningún endpoint de baja. Las referencias están configuradas con `RESTRICT`:
`apu_resources.resource_id`, `matrix_components.component_resource_id` y `resources.origin_resource_id`;
`matrix_components.matrix_resource_id` usa `CASCADE`. `D-49` reservaba la baja lógica de `Resource`.

## Goals / Non-Goals

**Goals:**

- Crear `measurement_units` con exactamente `id`, `short_unit` y `unit`.
- Exponer alta y consulta del catálogo de unidades.
- Exponer los tipos de insumo activos para los formularios de APU e insumo.
- Exponer la consulta de insumos por sigla de unidad sin depender del catálogo.
- Exponer la baja física de un insumo respetando las referencias existentes.
- Ampliar la unidad del APU de 5 a 10 caracteres sin cambiar el texto libre ni crear claves foráneas.
- Mantener el aislamiento por compañía en el endpoint de tipos y el modelo de permisos fail-closed.
- Cubrir migración, contrato, seguridad y arquitectura con pruebas.

**Non-Goals:**

- Tabla de actividades o unidad de actividad.
- Editar o eliminar unidades del catálogo, conversión de unidades o validación de que la unidad exista en el
  catálogo.
- Insertar automáticamente en `measurement_units` las unidades escritas en APU o insumo.
- Cambiar `resources.unit` de 20 a 10.
- Crear o modificar tipos de insumo.
- Paginar el catálogo de unidades en este incremento.
- Baja lógica de insumos, eliminación en cascada de líneas de APU o componentes de Matriz, y eliminación de
  APUs, Matrices o proyectos.
- Frontend y cliente Angular (`D-109`).

## Decisions

### D1. Tabla `measurement_units` con tres atributos

```text
measurement_units
  id          INT IDENTITY, PK
  short_unit  VARCHAR(10), NOT NULL
  unit        VARCHAR(100), NOT NULL
  UQ_dbo_measurement_units_short_unit (short_unit)   -- colación CI
  CK_measurement_units_short_unit_not_empty
  CK_measurement_units_unit_not_empty
```

Es un catálogo **global**, como `resource_types` y `base_currencies`: no lleva `company_id`, `is_active` ni
timestamps porque el requisito fija tres atributos. La unicidad de `short_unit` evita duplicados `M2`/`m2`.
Alternativa considerada: catálogo por compañía; se descarta porque exigiría un cuarto atributo y el usuario
pidió explícitamente tres.

### D2. La unidad es una sugerencia, no una clave foránea

`apus.unit` y `resources.unit` siguen guardando la sigla textual. No se agrega `unit_id` ni FK a
`measurement_units`, y el alta de APU/insumo no valida que la unidad exista en el catálogo. Esto cumple
“NO se debe guardar el ID” y permite que el usuario cree unidades nuevas sin un listado estricto. El alta de
APU o insumo nunca hace *upsert* en `measurement_units`: si la sigla no existe, solo se persiste en
`apus.unit` o `resources.unit`, y el catálogo se modifica únicamente con `POST /catalogs/units`
(`REQ-UNIDADES-05`). Alternativa considerada: FK a `measurement_units`; se descarta porque rompería el texto
libre y obligaría a migrar datos existentes que no están en el catálogo.

### D3. Superficie del catálogo de unidades

Se agrega al `CatalogsController` existente, que ya es dueño de `/api/v1/catalogs` y tiene
`[AllowWithoutCompanyContext]`:

```text
GET  /api/v1/catalogs/units        CATALOGS_READ
POST /api/v1/catalogs/units        CATALOGS_WRITE
```

```json
// POST request
{ "shortUnit": "M2", "unit": "Metro cuadrado" }

// POST 201 y elemento de GET
{ "id": 1, "shortUnit": "M2", "unit": "Metro cuadrado" }
```

`GET` devuelve un arreglo de `{ id, shortUnit, unit }` ordenado por `shortUnit`; no se pagina porque el
catálogo está acotado por siglas de 10 caracteres y es una lista de apoyo para un selector. Alternativa
considerada: ruta separada `/api/v1/units`; se descarta para conservar la propiedad de `/catalogs` (`D-102`).

### D4. Validación, normalización y errores del alta

El comando `CreateUnit` recibe `shortUnit` y `unit`, recorta espacios y valida:

| Regla | Error |
|---|---|
| `shortUnit` no vacío y de 1–10 caracteres | `400 UNIT_VALIDATION_ERROR` |
| `unit` no vacío y de 1–100 caracteres | `400 UNIT_VALIDATION_ERROR` |
| `shortUnit` único sin distinguir mayúsculas | `409 UNIT_ALREADY_EXISTS` |
| Alta correcta | `201` con `{ id, shortUnit, unit }` |

La unicidad se verifica en aplicación (para el mensaje de negocio) y en base con
`UQ_dbo_measurement_units_short_unit`; una violación concurrente se traduce a `409 UNIT_ALREADY_EXISTS`. El
`id` es `INT IDENTITY` y no lo envía el cliente. Se conserva la capitalización ingresada; solo se recortan
los extremos.

### D5. APU de 5 a 10 caracteres

`Apu.UnitMaxLength` pasa de 5 a 10 y `apus.unit` pasa a `VARCHAR(10)`. Los validadores de
`CreateWbsItemCommand` y `UpdateWbsItemCommand` usan la constante, así que heredan el cambio. `resources.unit`
permanece en 20: la sigla del catálogo cabe y no se rompe el texto libre existente. La unidad del APU sigue
siendo texto y no se valida contra `measurement_units`.

La migración debe ser reversible: `Up` amplía la columna y `Down` intenta volver a `VARCHAR(5)`. El `Down`
puede fallar si ya existen unidades de 6–10 caracteres; el runbook debe advertir que se haga respaldo y que
el rollback requiere limpiar o acortar esos datos antes. No se trunca automáticamente.

### D6. Endpoint de tipos de insumo

```text
GET /api/v1/projects/{projectId}/resource-types     RESOURCES_READ + alcance del proyecto
```

```json
[
  { "id": "11111111-1111-1111-1111-111111111101", "name": "Materiales" },
  { "id": "11111111-1111-1111-1111-111111111102", "name": "Mano de obra" }
]
```

- Devuelve **todos los tipos activos** de `resource_types`, no solo los usados en el proyecto, porque los
  formularios de alta necesitan poder crear el primer insumo de cualquier tipo.
- Ordena según `ResourceTypeCatalog.Definitions` (Materiales, Mano de obra, Herramienta, Equipos,
  Auxiliares, Trabajos, Matriz); si apareciera un tipo fuera de esa lista, se ordena al final por nombre.
- Solo expone `id` y `name`; `code` e `is_active` quedan internos.
- La consulta usa `ResourceAccessService.EnsureReadAsync`: Administrador cualquiera, Director su compañía,
  Presupuestador y Auditor asignados. Otra compañía `403 RESOURCE_READ_FORBIDDEN`; proyecto inexistente
  `404 PROJECT_NOT_FOUND`.
- Alternativa considerada: `GET /api/v1/catalogs/resource-types`; se descarta porque el requisito pide el
  alcance del proyecto y el formulario se abre dentro de un proyecto.

### D7. Permisos y seed

Se agrega `CATALOGS_WRITE` al `SecurityBaselineSeeder` (`v9`, 38 permisos) y se concede a Administrador,
Director y Presupuestador. El Auditor conserva solo `CATALOGS_READ`. Se agrega
`AuthorizationPolicies.CatalogsWrite` y su política en `Program.cs`.

| Endpoint | Permiso | Roles |
|---|---|---|
| `GET /catalogs/units` | `CATALOGS_READ` | Administrador, Director, Presupuestador, Auditor |
| `POST /catalogs/units` | `CATALOGS_WRITE` | Administrador, Director, Presupuestador |
| `GET /projects/{id}/resource-types` | `RESOURCES_READ` + proyecto | Administrador cualquiera; Director su compañía; Presupuestador/Auditor asignados |

Alternativa considerada: reutilizar `RESOURCES_WRITE` para crear unidades; se descarta porque el catálogo es
global y el Administrador debe poder administrarlo, y porque `CATALOGS_WRITE` comunica mejor la intención.

### D8. Organización del código

| Capa | Piezas previstas |
|---|---|
| Domain/Catalogs | `MeasurementUnit` con `Id` `int`, `ShortUnit` y `Unit`; sin setters públicos |
| Application/Catalogs | `Commands/CreateUnit/CreateUnitCommandHandler`, `Queries/GetUnitsQueryHandler`, `Repositories/IMeasurementUnitRepository`, `Services/UnitRules` |
| Application/Resources | `Queries/GetResourceTypesQueryHandler`, `Repositories/IResourceTypeReader` |
| Infrastructure | `Configurations/Catalogs/MeasurementUnitConfiguration`, `Repositories/MeasurementUnitRepository`, `Repositories/ResourceTypeReader`, `BudgetCivilDbContext`, `DependencyInjection` |
| Api | `CatalogsController` con `GET/POST units`, `ResourceTypesController`, `AuthorizationPolicies.CatalogsWrite`, política en `Program.cs` |

`Application/Catalogs` no depende de `Application/Apus` ni de `Application/Resources`; la consulta de tipos
vive en `Application/Resources` y usa su propio `ResourceAccessService`. `T-A-06` verifica esas fronteras.
El catálogo se registra en `BudgetCivilDbContext` como `DbSet<MeasurementUnit>` y su configuración fija
`ToTable("measurement_units")`, `id` `ValueGeneratedOnAdd`, `short_unit` `HasMaxLength(10)`,
`unit` `HasMaxLength(100)`, índice único y checks de no vacío.

### D9. Migración y datos

Migración `20261120125801_units_add_catalog`:

```text
Up:
  CREATE TABLE measurement_units (
    id INT IDENTITY PRIMARY KEY,
    short_unit VARCHAR(10) NOT NULL,
    unit VARCHAR(100) NOT NULL,
    CONSTRAINT CK_measurement_units_short_unit_not_empty CHECK (LEN(LTRIM(RTRIM(short_unit))) > 0),
    CONSTRAINT CK_measurement_units_unit_not_empty CHECK (LEN(LTRIM(RTRIM(unit))) > 0)
  );
  CREATE UNIQUE INDEX UQ_dbo_measurement_units_short_unit ON measurement_units (short_unit);
  ALTER TABLE apus ALTER COLUMN unit VARCHAR(10) NOT NULL;

Down:
  ALTER TABLE apus ALTER COLUMN unit VARCHAR(5) NOT NULL;   -- falla si hay datos de 6–10
  DROP TABLE measurement_units;
```

No hay backfill: el catálogo nace vacío y el usuario lo llena. `resources.unit` no se toca. El `Down` de
`apus.unit` debe ejecutarse solo después de revisar que no existan unidades de 6–10 caracteres; el runbook
documenta el respaldo y la limpieza previa.

### D10. Pruebas y documentación

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-28` | Validación, recorte y unicidad del alta de unidades | Unitaria |
| `T-U-29` | Tipos activos, orden del catálogo y exclusión de inactivos | Unitaria |
| `T-U-30` | APU acepta unidad de 10, rechaza 11 y no inserta la unidad en el catálogo | Unitaria |
| `T-U-31` | Consulta de insumos por `shortUnit`: coincidencia exacta, sin distinguir mayúsculas, vacía y sin catálogo | Unitaria |
| `T-U-32` | Baja física: elimina la fila, `404` en la repetición, `409` por uso y Matriz con componentes | Unitaria |
| `T-I-47` | Migración `measurement_units`, índice único y `apus.unit` de 10 | Integración manual |
| `T-I-48` | FKs `RESTRICT`/`CASCADE` que sostienen la baja física | Integración manual |
| `T-S-21` | Permisos de unidades, aislamiento y alcance de tipos | Seguridad manual |
| `T-S-22` | Permisos y aislamiento de la consulta por unidad y de la baja física | Seguridad manual |
| `T-C-07` | Contrato de `GET/POST /catalogs/units` y `GET /resource-types` | Contrato en CI |
| `T-C-08` | Contrato de `GET /resources/by-unit/{shortUnit}` y `DELETE /resources/{resourceId}` | Contrato en CI |
| `T-A-06` | `Application/Catalogs` no depende de APU/insumos; APU e insumos no dependen de `Application/Catalogs`; `Application/Resources` no depende de APU | Arquitectura en CI |

Se actualizan `docs/02` (rutas y campos), `docs/05` (tabla `measurement_units`, `apus.unit` 10,
`resources.unit` 20 y FKs de la baja física), `docs/runbook-migraciones.md`, `PLAN_INGENIERIA.md` (`RF-05`,
`RF-07`, `D-16`, `D-31` y la revisión de `D-49`) y la fila `Unidades` de `openspec/project.md §8` al
archivar. Los IDs nuevos se proponen para `PLAN §5.6.8`.

### D11. Consulta de insumos por sigla de unidad

```text
GET /api/v1/projects/{projectId}/resources/by-unit/{shortUnit}     RESOURCES_READ + alcance del proyecto
```

```json
[
  {
    "resourceId": "11111111-1111-1111-1111-111111111101",
    "description": "Cemento gris",
    "unit": "M2",
    "unitPrice": 25000.00,
    "quotationStatus": "POR_COTIZAR"
  }
]
```

- Devuelve los insumos del proyecto cuyo `unit` coincide exactamente con `shortUnit`, sin distinguir
  mayúsculas. La sigla recibida se recorta y se rechaza vacía o de más de 20 caracteres
  (`400 VALIDATION_ERROR`, que es el máximo de `resources.unit`).
- No exige que la sigla exista en `measurement_units`, porque una unidad puede vivir solo en el APU o el
  insumo (`REQ-UNIDADES-05`); por eso no consulta el catálogo.
- Devuelve una colección vacía si no hay coincidencias, ordenada por `description` y sin paginar: es una
  consulta de apoyo para el formulario. Reutiliza el contrato `ResourceDto` del listado.
- La comparación se hace con `ToUpper()` en ambos lados después de recortar la entrada, para no depender de
  la colación de la base; los insumos sin `unit` no aparecen.
- Usa `ResourceAccessService.EnsureReadAsync`: proyecto inexistente o no visible `404 PROJECT_NOT_FOUND`;
  actor sin alcance de lectura `403 RESOURCE_READ_FORBIDDEN`.
- La ruta estática `by-unit` no colisiona con `{resourceId:guid}` ni con `suggestions`. Alternativa
  considerada: parámetro `shortUnit` en `GET /resources`; se descarta porque el usuario pidió una consulta
  dedicada y el listado general mantiene paginación y filtros combinados.

### D12. Baja física del insumo

```text
DELETE /api/v1/projects/{projectId}/resources/{resourceId}     RESOURCES_WRITE + alcance de escritura
```

- El endpoint elimina la fila con `Remove` y `SaveChanges`; no escribe `is_active = 0` ni `deleted_at`.
  Responde `204 No Content`, sin cuerpo.
- Antes de eliminar verifica dependencias con consultas `EXISTS` sobre `apu_resources.resource_id`,
  `matrix_components.component_resource_id` y `resources.origin_resource_id` (esta última sin el filtro
  global, para no ignorar copias inactivas). Si existe cualquiera, responde `409 RESOURCE_IN_USE` y no
  elimina nada.
- Si el insumo es una Matriz, sus propios `matrix_components` caen en cascada (`CASCADE` ya configurado); la
  Matriz no se puede eliminar si está usada por un APU, como componente de otra Matriz o es origen de una
  copia.
- `Resource.IsActive` y `DeletedAtUtc` dejan de usarse en este flujo; se conservan en el modelo para no
  introducir una migración destructiva. Revisa `D-49`, que asignaba baja lógica a `Resource`: el autor pidió
  explícitamente baja física para el insumo.
- Segunda baja del mismo recurso: `404 RESOURCE_NOT_FOUND`. Proyecto de otra compañía: `404
  PROJECT_NOT_FOUND` (sin revelar existencia). Administrador y Auditor: `403 RESOURCE_WRITE_FORBIDDEN`.
- No se agrega token de concurrencia al `DELETE`: la operación elimina la fila y una repetición termina en
  `404`, sin riesgo de sobrescribir datos.
- Alternativa considerada: baja lógica con `is_active` (`D-49`); se descarta por pedido explícito del autor y
  porque las FKs `RESTRICT` ya impiden dejar referencias huérfanas.

## Risks / Trade-offs

- **[Catálogo global visible entre compañías]** -> Las unidades personalizadas creadas por una compañía son
  visibles para las demás. Se acepta porque una unidad de medida no es un dato sensible y el requisito fija
  tres atributos. Si se vuelve un problema, se agrega `company_id` en otra etapa.
- **[El APU admite 10 y el insumo 20]** -> Es intencional: el APU se alinea con la sigla del catálogo y el
  insumo conserva su texto libre existente. Se documenta para evitar conclusiones erróneas.
- **[Sin edición ni borrado de unidades]** -> Un error de digitación permanece en el catálogo. Se puede
  agregar un endpoint de edición/baja en otra etapa; el alcance actual es alta y consulta.
- **[`Down` de `apus.unit` puede fallar]** -> Hay datos de 6–10 caracteres. El runbook exige respaldo y
  revisión previa; no se trunca automáticamente.
- **[Seed `v9` revoca permisos fuera de la matriz]** -> Es el comportamiento esperado del seeder; se revisa
  que `CATALOGS_WRITE` quede solo en Administrador, Director y Presupuestador.
- **[Catálogo sin paginación]** -> Si crece mucho, el `GET` se vuelve pesado. Se mitiga con la unicidad y el
  límite de 10 caracteres; se puede paginar en otra etapa.
- **[Tipos de insumo fuera de `ResourceTypeCatalog.Definitions`]** -> Se ordenan al final por nombre; los
  siete tipos sembrados mantienen el orden esperado.
- **[Baja física irreversible]** -> El insumo eliminado no se recupera. Se mitiga bloqueando la baja con
  `409 RESOURCE_IN_USE` cuando hay referencias y documentando la operación en `docs/02`; no se ofrece papelera
  en este incremento.
- **[Revisión de `D-49`]** -> `Resource` conserva `is_active`/`deleted_at`, pero este endpoint ya no los usa.
  Se registra la revisión en `PLAN_INGENIERIA.md` para que la decisión y el código no diverjan; una limpieza
  de columnas queda para otra etapa.
- **[La consulta por sigla no valida el catálogo]** -> Una sigla escrita solo en un insumo no aparece en
  `GET /catalogs/units`; el formulario debe combinar la consulta por unidad con el catálogo y el usuario puede
  crearla explícitamente. Se documenta en `docs/02`.
- **[Las copias bloquean la baja del origen]** -> El origen de una copia no se elimina mientras la copia
  exista. Se informa con `409 RESOURCE_IN_USE`; el usuario debe eliminar primero la copia.

## Migration Plan

1. Respaldar la base local/demo y revisar el script generado por `dotnet ef migrations script`.
2. Aplicar la migración `units_add_catalog`; verificar la tabla, el índice único, los checks y
   `apus.unit VARCHAR(10)`.
3. Desplegar la API; el `seed` `v9` se ejecuta al arrancar y agrega `CATALOGS_WRITE` a los tres roles.
4. Verificar `GET/POST /catalogs/units`, `GET /projects/{id}/resource-types`, `GET
   /projects/{id}/resources/by-unit/{shortUnit}` y `DELETE /projects/{id}/resources/{resourceId}` con datos
   reales.
5. Rollback: revertir el despliegue, revisar que no haya unidades de 6–10 caracteres y ejecutar el `Down`;
   si las hay, limpiarlas o acortarlas antes. No hay datos de negocio que reconstruir.
6. La consulta y la baja física no agregan migraciones: confirmar en SQL Server que
   `apu_resources.resource_id`, `matrix_components.component_resource_id` y `resources.origin_resource_id`
   siguen en `RESTRICT`, y que `matrix_components.matrix_resource_id` sigue en `CASCADE` (`T-I-48`).

## Open Questions

Ninguna que cambie las specs, el enfoque o las tareas. El catálogo global, la ausencia de FK, el límite de
10 en el APU, el permiso `CATALOGS_WRITE`, los tipos activos sin filtro por uso, la consulta por sigla y la
baja física con `409 RESOURCE_IN_USE` quedaron cerrados en `specs/` y en este diseño.

## Traceability

| Requisito | Escenarios | RF / D | Prueba |
|---|---|---|---|
| `REQ-UNIDADES-01` | `SC-UNIDADES-01`…`05` | RF-05, RF-07; D-16, D-31 | `T-U-28`, `T-I-47` |
| `REQ-UNIDADES-02` | `SC-UNIDADES-06`…`09` | RF-05, RF-07; D-16 | `T-U-28`, `T-C-07` |
| `REQ-UNIDADES-03` | `SC-UNIDADES-10`…`12` | RF-05, RF-07; D-16 | `T-U-28`, `T-C-07` |
| `REQ-UNIDADES-04` | `SC-UNIDADES-13`…`15` | RF-05; matriz `1.3.2` | `T-S-21` |
| `REQ-UNIDADES-05` | `SC-UNIDADES-16`…`18` | RF-05, RF-07; D-16, D-31 | `T-U-30` |
| `REQ-INSUMOS-16` | `SC-INSUMOS-57`…`63` | RF-05; D-16 | `T-U-29`, `T-S-21`, `T-C-07` |
| `REQ-INSUMOS-17` | `SC-INSUMOS-64`…`70` | RF-05, RF-20; D-16 | `T-U-31`, `T-S-22`, `T-C-08` |
| `REQ-INSUMOS-18` | `SC-INSUMOS-71`, `76` | RF-05; D-49 revisada, D-50 | `T-U-32`, `T-I-48` |
| `REQ-INSUMOS-19` | `SC-INSUMOS-72`…`74` | RF-05; D-49 revisada | `T-U-32`, `T-I-48` |
| `REQ-INSUMOS-20` | `SC-INSUMOS-75` | RF-09; D-48 | `T-U-32`, `T-I-48` |
| `REQ-INSUMOS-21` | `SC-INSUMOS-77`…`81` | RF-05; matriz `1.3.2` | `T-S-22` |
| `REQ-INSUMOS-22` | `SC-INSUMOS-82`…`83` | RF-05, RF-20; D-02, D-36 | `T-S-22` |
| `REQ-APUS-02` | `SC-APUS-06`…`10`, `109`, `110` | RF-07; D-31 | `T-U-30`, `T-I-47` |


