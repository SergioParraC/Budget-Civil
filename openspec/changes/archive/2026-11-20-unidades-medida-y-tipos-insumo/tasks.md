# Tareas: unidades-medida-y-tipos-insumo

Convención: cada tarea declara su verificación. Las pruebas unitarias, de contrato y de arquitectura corren
en CI; la integración y la seguridad se verifican manualmente con evidencia (`D-66`). Los identificadores
nuevos (`T-U-28`…`T-U-32`, `T-I-47`, `T-I-48`, `T-S-21`, `T-S-22`, `T-C-07`, `T-C-08`, `T-A-06`) se registran
en la tarea 5.3 y se proponen para `PLAN §5.6.8`.

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-28` | Validación, recorte, unicidad y consulta del catálogo de unidades | Unitaria |
| `T-U-29` | Tipos activos, orden del catálogo y exclusión de inactivos | Unitaria |
| `T-U-30` | APU acepta unidad de 10, rechaza 11 y no inserta la unidad en el catálogo | Unitaria |
| `T-U-31` | Consulta de insumos por `shortUnit`: exacta, sin distinguir mayúsculas, vacía y sin catálogo | Unitaria |
| `T-U-32` | Baja física: elimina la fila, `404` repetido, `409` por uso y Matriz con componentes | Unitaria |
| `T-I-47` | Migración `measurement_units`, índice único y `apus.unit` de 10 | Integración manual |
| `T-I-48` | FKs `RESTRICT`/`CASCADE` que sostienen la baja física | Integración manual |
| `T-S-21` | Permisos de unidades, aislamiento y alcance de tipos | Seguridad manual |
| `T-S-22` | Permisos y aislamiento de la consulta por unidad y de la baja física | Seguridad manual |
| `T-C-07` | Contrato de `GET/POST /catalogs/units` y `GET /resource-types` | Contrato en CI |
| `T-C-08` | Contrato de `GET /resources/by-unit/{shortUnit}` y `DELETE /resources/{resourceId}` | Contrato en CI |
| `T-A-06` | `Application/Catalogs` no depende de APU/insumos; APU e insumos no dependen del catálogo; `Resources` no depende de APU | Arquitectura en CI |

**Puerta de entrada:** `resource_types` ya está sembrado con siete tipos y `ResourceAccessService` existe.
No hay una tabla de actividades ni un catálogo de unidades previo.

## 1. Catálogo, migración y APU

- [x] 1.1 Crear `MeasurementUnit` en `Domain/Catalogs` con `Id` `int`, `ShortUnit` y `Unit`, su configuración
  EF (`measurement_units`, longitudes 10/100, índice único y checks) y el `DbSet`; verificar con
  `dotnet build` y que la configuración no exponga setters públicos.
- [x] 1.2 Crear la migración `units_add_catalog` con la tabla, el índice único, los checks y
  `ALTER TABLE apus ALTER COLUMN unit VARCHAR(10)`; verificar con `dotnet ef migrations script` y `T-I-47`.
- [x] 1.3 Cambiar `Apu.UnitMaxLength` de 5 a 10 y verificar con `T-U-30` que el alta y la edición aceptan una
  unidad de 10, rechazan 11 y no consultan ni insertan en el catálogo.
- [x] 1.4 Implementar `IMeasurementUnitRepository` y `MeasurementUnitRepository` (listar, verificar sigla
  única sin distinguir mayúsculas, agregar); verificar con `T-I-47` el alta, el duplicado y la lectura.
- [x] 1.5 Actualizar `docs/runbook-migraciones.md` y `docs/05` con `measurement_units`, el índice único y
  `apus.unit VARCHAR(10)`; verificar que el script documentado coincide con la migración.

## 2. Aplicación y API de unidades

- [x] 2.1 Crear `CreateUnitCommandHandler`, su validador y `UnitRules` con `201`, `400
  UNIT_VALIDATION_ERROR` y `409 UNIT_ALREADY_EXISTS`; verificar con `T-U-28`.
- [x] 2.2 Crear `GetUnitsQueryHandler` que devuelva `{ id, shortUnit, unit }` ordenado por `shortUnit`;
  verificar con `T-U-28` el catálogo con datos y vacío.
- [x] 2.3 Agregar `GET` y `POST /api/v1/catalogs/units` a `CatalogsController`, registrar los handlers en DI
  y verificar con `T-C-07` el contrato y los códigos de error.
- [x] 2.4 Agregar `CATALOGS_WRITE` al `SecurityBaselineSeeder` (`v9`, 38 permisos), la política
  `CatalogsWrite` y su registro en `Program.cs`; verificar con `T-S-21` que solo Administrador, Director y
  Presupuestador crean y que el Auditor recibe `403`.
- [x] 2.5 Extender `LayerDependencyTests` con `T-A-06` y actualizar `docs/02` con las rutas, campos y
  permisos de unidades; verificar con `dotnet test` de arquitectura y que el contrato documentado coincide
  con `T-C-07`.

## 3. Tipos de insumo

- [x] 3.1 Crear `IResourceTypeReader` y `ResourceTypeReader` para devolver los tipos activos ordenados según
  `ResourceTypeCatalog.Definitions`; verificar con `T-U-29`.
- [x] 3.2 Crear `GetResourceTypesQueryHandler` con `ResourceAccessService.EnsureReadAsync` y verificar con
  `T-U-29` y `T-S-21` el orden, la exclusión de inactivos y el aislamiento por compañía.
- [x] 3.3 Crear `ResourceTypesController` con `GET /api/v1/projects/{projectId}/resource-types` y
  `RESOURCES_READ`, registrarlo en DI y verificar con `T-C-07`.
- [x] 3.4 Actualizar `docs/02` con la ruta y el contrato `[{ id, name }]` de tipos de insumo; verificar que
  el ejemplo documentado coincide con la respuesta y con `T-C-07`.

## 4. Insumos: consulta por unidad y baja física

- [x] 4.1 Crear `GetResourcesByUnitQueryHandler` y el método de repositorio que proyecta los insumos del
  proyecto con `unit` igual a la sigla sin distinguir mayúsculas, ordenados por descripción y sin consultar
  `measurement_units`; verificar con `T-U-31` la coincidencia, la sigla en otra capitalización, la colección
  vacía y la sigla inválida.
- [x] 4.2 Exponer `GET /api/v1/projects/{projectId}/resources/by-unit/{shortUnit}` en `ResourcesController`
  con `ResourcesRead`, registrar el handler en DI y verificar con `T-C-08` el contrato, `400` y `404`.
- [x] 4.3 Crear `DeleteResourceCommandHandler` con `ResourceAccessService.EnsureWriteAsync`, verificación de
  referencias en `apu_resources.resource_id`, `matrix_components.component_resource_id` y
  `resources.origin_resource_id`, y eliminación física; verificar con `T-U-32` el `204`, el `409
  RESOURCE_IN_USE`, la Matriz con componentes y la segunda baja `404 RESOURCE_NOT_FOUND`.
- [x] 4.4 Exponer `DELETE /api/v1/projects/{projectId}/resources/{resourceId}` en `ResourcesController` con
  `ResourcesWrite` y `204` sin cuerpo; verificar con `T-C-08` y `T-S-22` que Director y Presupuestador
  asignado eliminan, y que Administrador, Auditor y Presupuestador no asignado reciben `403`.
- [x] 4.5 Confirmar que el alta de insumo y de APU no inserta ni actualiza `measurement_units` cuando la
  unidad no está en el catálogo; verificar con `T-U-30` y `T-U-31` que el catálogo queda intacto y que la
  consulta por sigla sigue devolviendo el insumo.
- [x] 4.6 Actualizar `docs/02` con `GET /resources/by-unit/{shortUnit}`, `DELETE /resources/{resourceId}`, el
  código `RESOURCE_IN_USE` y el ejemplo de la unidad no catalogada; verificar contra `T-C-08`.
- [x] 4.7 Actualizar `PLAN_INGENIERIA.md` con la revisión de `D-49` (baja física del insumo, sin baja lógica)
  y `docs/05` con las FKs que sostienen la baja; verificar que la decisión y el esquema documentado coinciden
  con `T-I-48`.

## 5. Integración y cierre

- [x] 5.1 Ejecutar `dotnet build` y `dotnet test` de las suites unitaria, de contrato y de arquitectura;
  verificar que `T-U-28`…`T-U-32`, `T-C-07`, `T-C-08` y `T-A-06` pasan.
- [x] 5.2 Ejecutar la verificación manual contra SQL Server local: crear `M2`, intentar `m2`, crear un APU
  y un insumo con una unidad de 10 caracteres y con una unidad fuera del catálogo, consultar por sigla,
  eliminar un insumo libre, comprobar `409` en uno usado y en una copia, eliminar una Matriz con componentes,
  y comprobar permisos y aislamiento (`T-I-47`, `T-I-48`, `T-S-21`, `T-S-22`); registrar la evidencia.
- [x] 5.3 Registrar `T-U-28`…`T-U-32`, `T-I-47`, `T-I-48`, `T-S-21`, `T-S-22`, `T-C-07`, `T-C-08` y
  `T-A-06` en `PLAN §5.6.8` y dejar preparada la fila `Unidades` de `openspec/project.md §8` para el archive;
  verificar con `openspec list --specs` y `openspec validate --all --strict`.
- [x] 5.4 Revisar el diff completo y ejecutar `openspec validate --all --strict`; confirmar que no se creó
  una tabla de actividades, que no se modificó `resources.unit` y que el alta de APU o insumo no agrega
  filas a `measurement_units`.

## Workflow follow-up

- Archivar el change con la aprobación final del estudiante; al archivar se incorpora `unidades` a
  `openspec/specs/` y se fusionan los deltas de `insumos` y `apus`.
- Revisar `PLAN §3.4.3` con las rutas `GET/POST /catalogs/units`, `GET /projects/{id}/resource-types`, `GET
  /projects/{id}/resources/by-unit/{shortUnit}` y `DELETE /projects/{id}/resources/{resourceId}`.
- Registrar en `PLAN_INGENIERIA.md` la revisión de `D-49`: la baja del insumo es física mediante el endpoint
  nuevo, no lógica.
- Regenerar el cliente Angular cuando el contrato esté estable (`D-109`).
- Evaluar en otra etapa la edición/baja de unidades del catálogo, la paginación del catálogo, el alcance por
  compañía y, si la baja física es la única vía, el retiro de `is_active`/`deleted_at` de `resources`.
