# Cambio: unidades-medida-y-tipos-insumo

## Why

Hoy las unidades de APU e insumos son texto libre sin un catálogo que el usuario pueda administrar: el APU
limita la unidad a 5 caracteres, el insumo la limita a 20 y no existe una consulta de unidades para los
formularios. Además, el catálogo de tipos de insumo existe en `resource_types` y se usa para validar, pero no
hay un endpoint que lo exponga para que el usuario elija el tipo al crear un APU o un insumo. Esto obliga a
hardcodear valores en el frontend y no permite que el usuario cree sus propias unidades. Tampoco existe una
consulta de insumos por sigla de unidad ni una forma de eliminar un insumo que ya no se usa, y una unidad
escrita en un APU o insumo no debe contaminar el catálogo global.

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
- Si la unidad usada en el APU o el insumo no existe en `measurement_units`, el sistema NO la inserta ni
  actualiza en el catálogo: el valor queda únicamente en `apus.unit` o `resources.unit`. El catálogo solo se
  modifica con `POST /api/v1/catalogs/units`.
- Se amplía `apus.unit` de 5 a 10 caracteres para que acepte las siglas del catálogo. `resources.unit`
  permanece en 20 caracteres y no requiere migración. **No es un cambio incompatible**: los valores
  existentes siguen siendo válidos.
- Se agrega `GET /api/v1/projects/{projectId}/resource-types` para devolver los tipos de insumo activos con
  `{ id, name }`, ordenados como el catálogo, de modo que los formularios de alta de APU e insumo puedan
  elegir el tipo. Devuelve todos los tipos activos, no solo los usados en el proyecto, para permitir crear
  el primer insumo de cualquier tipo.
- Se agrega `GET /api/v1/projects/{projectId}/resources/by-unit/{shortUnit}` para devolver los insumos del
  proyecto cuya unidad coincide exactamente con la sigla, sin distinguir mayúsculas y sin exigir que la sigla
  exista en el catálogo. Devuelve una colección vacía si no hay coincidencias y usa `RESOURCES_READ` con el
  alcance del proyecto.
- Se agrega `DELETE /api/v1/projects/{projectId}/resources/{resourceId}` con **borrado físico** del insumo y
  `204`; exige `RESOURCES_WRITE`, lo permiten Director y Presupuestador y lo rechazan Administrador y
  Auditor. Si el insumo está usado por una línea de APU, un componente de Matriz o es origen de una copia,
  responde `409 RESOURCE_IN_USE` y no elimina nada. Revisa `D-49`, que reservaba baja lógica para `Resource`.
- Se agrega el permiso `CATALOGS_WRITE` para crear unidades (Administrador, Director y Presupuestador); la
  consulta usa `CATALOGS_READ` y los tipos de insumo usan `RESOURCES_READ` con el alcance de lectura del
  proyecto.
- **No se crea una tabla de actividades**: las unidades quedan disponibles para APU e insumos y para una
  etapa futura de actividades; hoy no existe una entidad de actividad con unidad.

**Fuera de alcance:** crear/editar/eliminar tipos de insumo, editar o eliminar unidades del catálogo,
conversión de unidades, validar que la unidad del APU o del insumo exista en el catálogo, insertar
automáticamente en el catálogo las unidades escritas en APU o insumo, cambiar `resources.unit` de 20 a 10,
baja lógica de insumos, eliminar APUs, Matrices o proyectos, tabla de actividades, frontend y cliente Angular
(`D-109`).

## Capabilities

### New Capabilities

- `unidades`: catálogo global de unidades de medida con alta y consulta; el APU y el insumo guardan la sigla
  textual sin clave foránea y sin insertarla automáticamente en el catálogo.

### Modified Capabilities

- `insumos`: se agrega la consulta de tipos de insumo activos para los formularios de alta de APU e insumo,
  la consulta de insumos por sigla de unidad y la baja física de un insumo.
- `apus`: la unidad del APU pasa de 1–5 a 1–10 caracteres y sigue siendo texto libre sin alta implícita en el
  catálogo.

## Impact

- **Contrato de API**: `GET`/`POST /api/v1/catalogs/units` con `{ id, shortUnit, unit }`; `GET
  /api/v1/projects/{projectId}/resource-types` con `[{ id, name }]`; `GET
  /api/v1/projects/{projectId}/resources/by-unit/{shortUnit}` con los insumos del contrato `ResourceDto`; y
  `DELETE /api/v1/projects/{projectId}/resources/{resourceId}` con `204`. `camelCase`, `ProblemDetails` y los
  códigos `UNIT_VALIDATION_ERROR`, `UNIT_ALREADY_EXISTS`, `PROJECT_NOT_FOUND`, `RESOURCE_READ_FORBIDDEN`,
  `RESOURCE_WRITE_FORBIDDEN`, `RESOURCE_NOT_FOUND`, `RESOURCE_IN_USE` y `VALIDATION_ERROR`.
- **Base de datos**: nueva tabla `measurement_units` (`id` `INT IDENTITY`, `short_unit` `VARCHAR(10)`,
  `unit` `VARCHAR(100)`, único por `short_unit`) y migración `ALTER TABLE apus ALTER COLUMN unit
  VARCHAR(10)`. Sin cambios en `resources.unit` ni migración para la baja física: se reutilizan los `RESTRICT`
  de `apu_resources.resource_id`, `matrix_components.component_resource_id` y `resources.origin_resource_id`,
  y el `CASCADE` de `matrix_components.matrix_resource_id`. `D-49` se revisa para permitir la baja física de
  `Resource`; `is_active` y `deleted_at` se conservan sin uso en este flujo.
- **Permisos**: nuevo `CATALOGS_WRITE` en el `seed` `v9` (38 permisos) para Administrador, Director y
  Presupuestador; `CATALOGS_READ`, `RESOURCES_READ` y `RESOURCES_WRITE` se conservan.
- **Código**: `Domain/Catalogs` (`MeasurementUnit`), `Application/Catalogs` (alta y consulta), configuración
  y repositorio en `Infrastructure`, `CatalogsController` y un controlador/consulta de tipos de insumo en
  `Api`, el ajuste de `Apu.UnitMaxLength` con sus validadores, y en `Application/Resources` la consulta por
  unidad y la baja física con sus acciones en `ResourcesController`.
- **Pruebas**: unitarias de validación y unicidad, consulta de tipos, consulta por sigla, baja física y
  bloqueo por uso, contrato de los endpoints, seguridad de permisos y aislamiento, migración y arquitectura.
- **Documentación**: `PLAN_INGENIERIA.md` (`RF-05`, `RF-07`, `D-16`, `D-31` y la revisión de `D-49`),
  `docs/02`, `docs/05`, `docs/runbook-migraciones.md` y la fila `Unidades` de `openspec/project.md §8` al
  archivar.

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
8. `GET /projects/{id}/resources/by-unit/{shortUnit}` devuelve los insumos coincidentes sin distinguir
   mayúsculas, devuelve una colección vacía sin coincidencias y no exige que la sigla esté en el catálogo.
9. Un APU o un insumo con una unidad que no está en `measurement_units` se guarda con esa unidad y no crea ni
   modifica filas del catálogo.
10. `DELETE /projects/{id}/resources/{resourceId}` elimina físicamente el insumo con `204`; responde `409
    RESOURCE_IN_USE` si está usado por un APU, una Matriz o una copia, `404` en una segunda baja y `403` al
    Administrador y al Auditor.
