# Cambio: unidades-medida-y-tipos-insumo

## Why

Hoy las unidades de APU e insumos son texto libre sin un catálogo que el usuario pueda administrar: el APU
limita la unidad a 5 caracteres, el insumo la limita a 20 y no existe una consulta de unidades para los
formularios. Además, el catálogo de tipos de insumo existe en `resource_types` y se usa para validar, pero no
hay un endpoint que lo exponga para que el usuario elija el tipo al crear un APU o un insumo. Esto obliga a
hardcodear valores en el frontend y no permite que el usuario cree sus propias unidades.

## What Changes

- Se crea la tabla global `measurement_units` con exactamente tres atributos: `id` numérico autoincremental,
  `short_unit` de hasta 10 caracteres y `unit` de hasta 100 caracteres. `short_unit` es único sin distinguir
  mayúsculas.
- Se agrega `GET /api/v1/catalogs/units` para listar el catálogo y `POST /api/v1/catalogs/units` para crear
  una unidad. `POST` valida longitud y duplicados, devuelve `201` con `{ id, shortUnit, unit }`, `400` ante
  datos inválidos y `409 UNIT_ALREADY_EXISTS` ante una sigla repetida.
- El APU y el insumo **siguen guardando la sigla textual** en su atributo `unit` (`apus.unit` y
  `resources.unit`); no se crea clave foránea hacia `measurement_units` ni se guarda el `id` del catálogo. El
  catálogo es una lista de sugerencias y el usuario puede crear unidades nuevas sin vincularse a un listado
  estricto.
- Se amplía `apus.unit` de 5 a 10 caracteres para que acepte las siglas del catálogo. `resources.unit`
  permanece en 20 caracteres y no requiere migración. **No es un cambio incompatible**: los valores
  existentes siguen siendo válidos.
- Se agrega `GET /api/v1/projects/{projectId}/resource-types` para devolver los tipos de insumo activos con
  `{ id, name }`, ordenados como el catálogo, de modo que los formularios de alta de APU e insumo puedan
  elegir el tipo. Devuelve todos los tipos activos, no solo los usados en el proyecto, para permitir crear
  el primer insumo de cualquier tipo.
- Se agrega el permiso `CATALOGS_WRITE` para crear unidades (Administrador, Director y Presupuestador); la
  consulta usa `CATALOGS_READ` y los tipos de insumo usan `RESOURCES_READ` con el alcance de lectura del
  proyecto.
- **No se crea una tabla de actividades**: las unidades quedan disponibles para APU e insumos y para una
  etapa futura de actividades; hoy no existe una entidad de actividad con unidad.

**Fuera de alcance:** crear/editar/eliminar tipos de insumo, editar o eliminar unidades, conversión de
unidades, validar que la unidad del APU o del insumo exista en el catálogo, cambiar `resources.unit` de 20 a
10, tabla de actividades, frontend y cliente Angular (`D-109`).

## Capabilities

### New Capabilities

- `unidades`: catálogo global de unidades de medida con alta y consulta; el APU y el insumo guardan la sigla
  textual sin clave foránea.

### Modified Capabilities

- `insumos`: se agrega la consulta de tipos de insumo activos para los formularios de alta de APU e insumo.
- `apus`: la unidad del APU pasa de 1–5 a 1–10 caracteres y sigue siendo texto libre.

## Impact

- **Contrato de API**: `GET`/`POST /api/v1/catalogs/units` con `{ id, shortUnit, unit }`; `GET
  /api/v1/projects/{projectId}/resource-types` con `[{ id, name }]`; `camelCase`, `ProblemDetails` y los
  códigos `UNIT_VALIDATION_ERROR`, `UNIT_ALREADY_EXISTS`, `PROJECT_NOT_FOUND` y `RESOURCE_READ_FORBIDDEN`.
- **Base de datos**: nueva tabla `measurement_units` (`id` `INT IDENTITY`, `short_unit` `VARCHAR(10)`,
  `unit` `VARCHAR(100)`, único por `short_unit`) y migración `ALTER TABLE apus ALTER COLUMN unit
  VARCHAR(10)`. Sin cambios en `resources.unit`.
- **Permisos**: nuevo `CATALOGS_WRITE` en el `seed` `v9` (38 permisos) para Administrador, Director y
  Presupuestador; `CATALOGS_READ` y `RESOURCES_READ` se conservan.
- **Código**: `Domain/Catalogs` (`MeasurementUnit`), `Application/Catalogs` (alta y consulta), configuración
  y repositorio en `Infrastructure`, `CatalogsController` y un controlador/consulta de tipos de insumo en
  `Api`, y el ajuste de `Apu.UnitMaxLength` con sus validadores.
- **Pruebas**: unitarias de validación y unicidad, consulta de tipos, contrato de ambos endpoints, seguridad
  de permisos y aislamiento, migración y arquitectura.
- **Documentación**: `PLAN_INGENIERIA.md` (`RF-05`, `RF-07`, `D-16`, `D-31`), `docs/02`, `docs/05`,
  `docs/runbook-migraciones.md` y la fila `Unidades` de `openspec/project.md §8` al archivar.

## Criterios de aceptación

1. `openspec validate unidades-medida-y-tipos-insumo --strict` pasa sin errores ni advertencias.
2. `POST /catalogs/units` crea una unidad con `shortUnit` y `unit`, rechaza duplicados con `409` y datos
   inválidos con `400`.
3. `GET /catalogs/units` devuelve las unidades creadas con `{ id, shortUnit, unit }`.
4. El APU guarda una unidad de hasta 10 caracteres y el insumo conserva su unidad de hasta 20; ninguno
   guarda el `id` del catálogo.
5. `GET /projects/{id}/resource-types` devuelve los siete tipos activos con `{ id, name }` y respeta el
   alcance por compañía y proyecto.
6. No se crea una tabla de actividades ni se modifica `resources.unit`.
7. La migración crea `measurement_units`, su índice único y amplía `apus.unit`, con `Down` reversible.
