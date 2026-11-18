# Cambio: e3a-rf05-proveedores

## Why

El cambio `e3a-rf05-insumos-proyecto` modela al proveedor como dos campos de texto libre en el insumo
(`distributor` y `contact_number`). Eso obliga a repetir los mismos datos en cada insumo y no permite
mantener un directorio de proveedores reutilizable, con ubicación y observaciones, para todos los proyectos
de una compañía.

`E-3a` necesita un directorio de proveedores por compañía, reutilizable entre proyectos, con datos de
contacto y ubicación, un selector ligero para asignar proveedores desde un insumo y una alerta no
bloqueante cuando el proveedor está en un lugar distinto al del proyecto. El ajuste es **solo de backend**:
la interfaz sigue fuera del alcance de esta etapa (`D-109`).

## What Changes

- Se crea la entidad `Supplier`, **propia de una compañía**, reutilizable por los insumos de **todos** los
  proyectos de esa compañía; no lleva `project_id`.
- Se exige únicamente la **razón social** (`business_name`). Son opcionales `contact_name`,
  `contact_phone`, `email`, `address`, `observations`, `country_id`, `department_id` y `municipality_id`.
- `country_id`, `department_id` y `municipality_id` referencian los catálogos existentes y deben ser
  coherentes entre sí cuando se informen (`D-96`).
- Se añade `Resource.supplier_id` como referencia **opcional**; un proveedor puede suministrar muchos
  insumos y `supplier_id` no es único.
- Un insumo **sin proveedor** es válido, pero queda en `FALTA_INFORMACION` aunque tenga descripción,
  unidad, precio y marca. Al asociar un proveedor y completar los datos, pasa a `POR_COTIZAR`; si se retira
  el proveedor, vuelve a `FALTA_INFORMACION`.
- Se exponen:
  - un endpoint de **consulta completa** de proveedores;
  - un endpoint **ligero de selección** que devuelve solo `supplierId` y `businessName`, filtrable por
    razón social, nombre de contacto y ubicación;
  - un endpoint de **alerta de ubicación** que compara el proveedor con el proyecto y devuelve una
    advertencia, sin impedir la asignación.
- Se añaden los permisos `SUPPLIERS_READ` y `SUPPLIERS_WRITE` al `seed` versionado; Director y
  Presupuestador escriben, Administrador y Auditor solo leen.
- Se conservan `distributor` y `contact_number` como campos heredados de solo lectura.
- Migración manual con `suppliers`, sus FKs de ubicación y `resources.supplier_id` (`D-40`, `D-107`).

## Capabilities

### New Capabilities

- `proveedores`: directorio de proveedores por compañía con datos de contacto, ubicación y observaciones;
  asociación opcional de uno a muchos con los insumos; selector ligero; alerta de ubicación no bloqueante;
  autorización y baja lógica.

### Modified Capabilities

- Ninguna en este delta. La integración con `insumos` se especifica dentro de `proveedores` y ajusta la
  regla de completitud de `REQ-INSUMOS-04`: el insumo sin proveedor queda en `FALTA_INFORMACION`. El
  cambio `e3a-rf05-insumos-proyecto` debe reconciliar esa regla antes de implementar ambos cambios juntos.

## Fuera de alcance

- Frontend, componentes, rutas y cliente Angular (`D-109`).
- Varios proveedores por insumo (N:M) o proveedor principal.
- Órdenes de compra, listas de precios, evaluación o comparación de proveedores.
- Catálogo abierto (`E-5`): publicará los datos del proveedor cuando se especifique.
- Borrado físico de proveedores; solo baja lógica.
- Retiro de `distributor` y `contact_number`; quedan obsoletos y se retiran en un cambio posterior.
- Cálculo de distancias, rutas o tiempos de transporte; la alerta compara ubicación administrativa, no
  kilómetros.

## Decisiones y supuestos

| Tema | Decisión | Motivo |
|---|---|---|
| Alcance del proveedor | Por compañía; reutilizable en todos sus proyectos | Evita duplicar el directorio y permite usar el mismo proveedor en varios proyectos |
| Cardinalidad | 1 proveedor → N insumos; un insumo → 0..1 proveedor | Un insumo puede existir sin proveedor |
| Campos obligatorios | Solo `business_name` | El enunciado lo fija como único dato mínimo |
| Ubicación | `country_id`, `department_id`, `municipality_id` opcionales y coherentes | Permite la alerta sin volver obligatoria la ubicación |
| Observaciones | `observations` opcional, texto libre | Guarda notas operativas del proveedor |
| Unicidad | `supplier_id` no único; `business_name` sin restricción única | Un proveedor puede repetirse y dos proveedores pueden llamarse igual |
| Estado del insumo | Sin proveedor → `FALTA_INFORMACION`; con proveedor y cuatro campos → `POR_COTIZAR` | El proveedor es opcional para crear, pero necesario para cotizar |
| Alerta de ubicación | No bloqueante; solo advierte | La decisión de asignar sigue siendo del usuario |
| Selector ligero | Devuelve solo `supplierId` y `businessName` | Reduce el payload al elegir un proveedor |
| Autorización | Director y Presupuestador escriben; Administrador y Auditor leen | Coherente con la matriz `1.3.2` y `D-34` |
| Campos heredados | `distributor` y `contact_number` obsoletos, sin borrado | Evita romper el cambio de insumos en curso |
| Alcance técnico | Solo backend | La interfaz no se especifica en `E-3a` (`D-109`) |

## Criterios de aceptación

1. `openspec validate e3a-rf05-proveedores --strict` pasa sin errores.
2. Cada `REQ-PROVEEDORES-*` tiene al menos un `SC-PROVEEDORES-*` en formato `Dado / Cuando / Entonces` y
   cita sus `RF`, `BR`, `R` y `D`.
3. Solo `business_name` es obligatorio; contacto, teléfono, email, dirección, observaciones y ubicación son
   opcionales y el email se valida cuando se informa.
4. Un proveedor se reutiliza en insumos de proyectos distintos de la misma compañía sin duplicar el
   registro.
5. Un insumo sin proveedor queda en `FALTA_INFORMACION` y puede guardarse; al asociar un proveedor pasa a
   `POR_COTIZAR` si los demás datos están completos.
6. El endpoint de selección devuelve solo `supplierId` y `businessName` y filtra por razón social,
   contacto y ubicación.
7. La alerta de ubicación devuelve `SUPPLIER_LOCATION_MISMATCH` cuando corresponde, no bloquea la
   asignación y devuelve `LOCATION_INCOMPLETE` cuando no hay datos suficientes.
8. Administrador y Auditor no crean ni editan proveedores.
9. La copia de un insumo dentro de la misma compañía conserva su proveedor.
10. Ningún requisito depende de un `X` abierto.

## Impact

- **Base de datos**: tabla `suppliers` (`company_id`, `business_name` obligatorio; `contact_name`,
  `contact_phone`, `email`, `address`, `observations`; FKs `country_id`, `department_id`,
  `municipality_id`; `is_active`, `rowversion`, autoría); columna `resources.supplier_id` nullable con FK
  `RESTRICT`; índices por compañía, razón social y ubicación. Migración manual (`D-40`, `D-107`).
- **Contrato de API**: `/api/v1/suppliers` (listado y detalle completos, alta, edición, baja lógica),
  `/api/v1/suppliers/options` (selector ligero) y
  `/api/v1/suppliers/{supplierId}/location-alert?projectId=` (alerta no bloqueante); `supplierId` en las
  peticiones y respuestas de insumos; `camelCase`, `ProblemDetails` y `page`/`pageSize`.
- **Código**: `M-04` en las cuatro capas; sin dependencias nuevas.
- **Permisos**: `SUPPLIERS_READ` y `SUPPLIERS_WRITE` con `seed` versionado e idempotente.
- **Pruebas**: unitarias de campos opcionales, ubicación, estado del insumo, selector y comparación de
  ubicación; integración manual de FKs, reutilización y alerta; arquitectura de módulos.
- **Catálogo**: `E-5` consumirá los datos del proveedor; queda fuera de este cambio.
- **Frontend**: fuera de alcance (`D-109`).
