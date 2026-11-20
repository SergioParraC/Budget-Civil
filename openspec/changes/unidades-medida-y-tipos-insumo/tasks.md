# Tareas: unidades-medida-y-tipos-insumo

Convención: cada tarea declara su verificación. Las pruebas unitarias, de contrato y de arquitectura corren
en CI; la integración y la seguridad se verifican manualmente con evidencia (`D-66`). Los identificadores
nuevos (`T-U-28`…`T-U-30`, `T-I-47`, `T-S-21`, `T-C-07`, `T-A-06`) se registran en la tarea 4.3 y se
proponen para `PLAN §5.6.8`.

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-28` | Validación, recorte, unicidad y consulta del catálogo de unidades | Unitaria |
| `T-U-29` | Tipos activos, orden del catálogo y exclusión de inactivos | Unitaria |
| `T-U-30` | APU acepta unidad de 10, rechaza 11 y no exige catálogo | Unitaria |
| `T-I-47` | Migración `measurement_units`, índice único y `apus.unit` de 10 | Integración manual |
| `T-S-21` | Permisos de unidades, aislamiento y alcance de tipos | Seguridad manual |
| `T-C-07` | Contrato de `GET/POST /catalogs/units` y `GET /resource-types` | Contrato en CI |
| `T-A-06` | `Application/Catalogs` no depende de APU/insumos; `Resources` no depende de APU | Arquitectura en CI |

**Puerta de entrada:** `resource_types` ya está sembrado con siete tipos y `ResourceAccessService` existe.
No hay una tabla de actividades ni un catálogo de unidades previo.

## 1. Catálogo, migración y APU

- [ ] 1.1 Crear `MeasurementUnit` en `Domain/Catalogs` con `Id` `int`, `ShortUnit` y `Unit`, su configuración
  EF (`measurement_units`, longitudes 10/100, índice único y checks) y el `DbSet`; verificar con
  `dotnet build` y que la configuración no exponga setters públicos.
- [ ] 1.2 Crear la migración `units_add_catalog` con la tabla, el índice único, los checks y
  `ALTER TABLE apus ALTER COLUMN unit VARCHAR(10)`; verificar con `dotnet ef migrations script` y `T-I-47`.
- [ ] 1.3 Cambiar `Apu.UnitMaxLength` de 5 a 10 y verificar con `T-U-30` que el alta y la edición aceptan una
  unidad de 10, rechazan 11 y no consultan el catálogo.
- [ ] 1.4 Implementar `IMeasurementUnitRepository` y `MeasurementUnitRepository` (listar, verificar sigla
  única sin distinguir mayúsculas, agregar); verificar con `T-I-47` el alta, el duplicado y la lectura.
- [ ] 1.5 Actualizar `docs/runbook-migraciones.md` y `docs/05` con `measurement_units`, el índice único y
  `apus.unit VARCHAR(10)`; verificar que el script documentado coincide con la migración.

## 2. Aplicación y API de unidades

- [ ] 2.1 Crear `CreateUnitCommandHandler`, su validador y `UnitRules` con `201`, `400
  UNIT_VALIDATION_ERROR` y `409 UNIT_ALREADY_EXISTS`; verificar con `T-U-28`.
- [ ] 2.2 Crear `GetUnitsQueryHandler` que devuelva `{ id, shortUnit, unit }` ordenado por `shortUnit`;
  verificar con `T-U-28` el catálogo con datos y vacío.
- [ ] 2.3 Agregar `GET` y `POST /api/v1/catalogs/units` a `CatalogsController`, registrar los handlers en DI
  y verificar con `T-C-07` el contrato y los códigos de error.
- [ ] 2.4 Agregar `CATALOGS_WRITE` al `SecurityBaselineSeeder` (`v9`, 38 permisos), la política
  `CatalogsWrite` y su registro en `Program.cs`; verificar con `T-S-21` que solo Administrador, Director y
  Presupuestador crean y que el Auditor recibe `403`.
- [ ] 2.5 Extender `LayerDependencyTests` con `T-A-06` y actualizar `docs/02` con las rutas, campos y
  permisos de unidades; verificar con `dotnet test` de arquitectura y que el contrato documentado coincide
  con `T-C-07`.

## 3. Tipos de insumo

- [ ] 3.1 Crear `IResourceTypeReader` y `ResourceTypeReader` para devolver los tipos activos ordenados según
  `ResourceTypeCatalog.Definitions`; verificar con `T-U-29`.
- [ ] 3.2 Crear `GetResourceTypesQueryHandler` con `ResourceAccessService.EnsureReadAsync` y verificar con
  `T-U-29` y `T-S-21` el orden, la exclusión de inactivos y el aislamiento por compañía.
- [ ] 3.3 Crear `ResourceTypesController` con `GET /api/v1/projects/{projectId}/resource-types` y
  `RESOURCES_READ`, registrarlo en DI y verificar con `T-C-07`.
- [ ] 3.4 Actualizar `docs/02` con la ruta y el contrato `[{ id, name }]` de tipos de insumo; verificar que
  el ejemplo documentado coincide con la respuesta y con `T-C-07`.

## 4. Integración y cierre

- [ ] 4.1 Ejecutar `dotnet build` y `dotnet test` de las suites unitaria, de contrato y de arquitectura;
  verificar que `T-U-28`, `T-U-29`, `T-U-30`, `T-C-07` y `T-A-06` pasan.
- [ ] 4.2 Ejecutar la verificación manual contra SQL Server local: crear `M2`, intentar `m2`, crear un APU
  y un insumo con una unidad de 10 caracteres, consultar los tipos y comprobar permisos y aislamiento
  (`T-I-47`, `T-S-21`); registrar la evidencia.
- [ ] 4.3 Registrar `T-U-28`…`T-U-30`, `T-I-47`, `T-S-21`, `T-C-07` y `T-A-06` en `PLAN §5.6.8` y dejar
  preparada la fila `Unidades` de `openspec/project.md §8` para el archive; verificar con `openspec list
  --specs` y `openspec validate --all --strict`.
- [ ] 4.4 Revisar el diff completo y ejecutar `openspec validate --all --strict`; confirmar que no se creó
  una tabla de actividades ni se modificó `resources.unit`.

## Workflow follow-up

- Archivar el change con la aprobación final del estudiante; al archivar se incorpora `unidades` a
  `openspec/specs/` y se fusionan los deltas de `insumos` y `apus`.
- Revisar `PLAN §3.4.3` con las rutas `GET/POST /catalogs/units` y `GET /projects/{id}/resource-types`.
- Regenerar el cliente Angular cuando el contrato esté estable (`D-109`).
- Evaluar en otra etapa la edición/baja de unidades, la paginación del catálogo y, si se requiere, el
  alcance por compañía.
