# Design: e3a-rf05-proveedores

## Context

`e3a-rf05-insumos-proyecto` define `Resource` con `distributor` y `contact_number` como texto libre. Este
cambio añade una entidad de proveedor y la relación con esos insumos; depende del modelo de `Resource` y se
implementa junto con el cambio de insumos. El alcance es backend: no hay componentes, rutas ni cliente
Angular (`D-109`).

El proyecto ya tiene `Company`, `Project`, `ProjectMember`, autorización por `permission_key`, `rowversion`
y el patrón de listados con `ListingQueryPipeline`. `SecurityBaselineSeeder` está en `v3`; el cambio de
insumos prevé `v4`, así que este cambio usa la versión siguiente disponible.

## Goals / Non-Goals

**Goals:**

- Modelar `Supplier` por compañía con los cinco datos mínimos de contacto.
- Asociar `Resource.supplier_id` con cardinalidad 1:N y sin unicidad.
- Exponer CRUD de proveedores y reflejar el proveedor en el ciclo del insumo.
- Respetar la autorización, el aislamiento por compañía y la concurrencia optimista.
- Resolver la convivencia con `distributor`/`contact_number` sin romper el cambio de insumos.

**Non-Goals:**

- Varios proveedores por insumo (N:M) o proveedor principal.
- Órdenes de compra, tarifas, evaluación o comparación de proveedores.
- Catálogo público (`E-5`).
- Interfaz de usuario.
- Borrado físico de proveedores.

## Decisions

### D1. Entidad `Supplier` por compañía

```text
Supplier
  Id, CompanyId
  BusinessName, ContactName, ContactPhone, Email, Address
  IsActive, RowVersion
  CreatedAtUtc, UpdatedAtUtc, LastModifiedByUserId, LastModifiedAtUtc
```

- `CompanyId` obligatorio: el directorio es de la compañía y se reutiliza en todos sus proyectos.
- **Alternativa descartada:** proveedor por proyecto. Duplicaría el directorio y contradiría la
  reutilización entre proyectos de la misma compañía (`D-27`).
- Todos los campos de contacto son obligatorios, según el enunciado; el email se valida con formato.
- `BusinessName` no tiene restricción única; dos proveedores pueden llamarse igual.

### D2. Persistencia y relación 1:N

- Tabla `suppliers` con `id`, `company_id`, `business_name` (200), `contact_name` (150),
  `contact_phone` (30), `email` (254), `address` (300), `is_active`, `row_version`, autoría y fechas UTC.
- Índices `IX_suppliers_company_id_business_name` y `IX_suppliers_company_id_is_active`; **sin** índice
  único sobre `business_name` ni `email`.
- `resources.supplier_id` nullable, FK `RESTRICT` a `suppliers.id`; índice `IX_resources_supplier_id`.
- La FK no garantiza que el proveedor sea de la misma compañía que el proyecto: esa regla se valida en
  el caso de uso, porque `resources` no lleva `company_id` y la compañía se resuelve por `project_id`.
- **Alternativa descartada:** tabla puente `ResourceSupplier` (N:M). El enunciado pide que un proveedor
  tenga múltiples insumos, no que un insumo tenga múltiples proveedores; N:M añadiría Proveedor principal,
  precio por proveedor y reglas de desempate que no están en el alcance.

### D3. API de proveedores

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/suppliers` | `SUPPLIERS_READ` | Listado con búsqueda y paginación |
| `GET` | `/api/v1/suppliers/{supplierId}` | `SUPPLIERS_READ` | Detalle y contacto |
| `POST` | `/api/v1/suppliers` | `SUPPLIERS_WRITE` | Alta |
| `PUT` | `/api/v1/suppliers/{supplierId}` | `SUPPLIERS_WRITE` | Edición con `rowVersion` |
| `PATCH` | `/api/v1/suppliers/{supplierId}/status` | `SUPPLIERS_WRITE` | Baja lógica o reactivación |

- `CreateSupplierRequest`: `businessName`, `contactName`, `contactPhone`, `email`, `address`. La compañía
  sale del actor o, para el Administrador, del contexto de consulta; nunca del cuerpo.
- `SupplierDto`: los cinco datos, `isActive`, `rowVersion` y autoría. No expone datos de otra compañía.
- Búsqueda por `businessName`, `contactName` o `email`, parcial y sin distinguir mayúsculas; orden estable
  por `businessName` y luego `id`.
- Errores `ProblemDetails`: `SUPPLIER_NOT_FOUND`, `SUPPLIER_INACTIVE`, `SUPPLIER_COMPANY_MISMATCH`,
  `SUPPLIER_CONCURRENCY_CONFLICT`, `SUPPLIER_WRITE_FORBIDDEN`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`.

### D4. Autorización y alcance

- Permisos nuevos: `SUPPLIERS_READ` y `SUPPLIERS_WRITE`, con `seed` versionado e idempotente.
- Asignación: Administrador → `SUPPLIERS_READ`; Director → `SUPPLIERS_READ` + `SUPPLIERS_WRITE`;
  Presupuestador → `SUPPLIERS_READ` + `SUPPLIERS_WRITE`; Auditor → `SUPPLIERS_READ`.
- El Administrador lee cualquier compañía; Director, Presupuestador y Auditor leen la propia.
- El Administrador no escribe contenido comercial; mantiene el criterio de solo lectura de `D-83`.
- El alcance se resuelve con `IActorAccessReader` y el `company_id` del actor; no se acepta `companyId`
  del cuerpo.

### D5. Integración con el insumo

- `CreateResourceRequest` y `UpdateResourceRequest` aceptan `supplierId` opcional.
- Al asociar un proveedor se valida: existe, pertenece a la compañía del proyecto y está activo. Si el
  insumo ya tenía ese proveedor, una edición de otros campos no falla porque el proveedor esté inactivo.
- `ResourceDto` incluye un resumen `supplier` con razón social, contacto, teléfono, email y dirección; si
  no hay proveedor, devuelve `null`.
- La copia de un insumo conserva `supplier_id`; si el proveedor no pertenece a la compañía del destino, la
  copia se rechaza sin crearla.
- `distributor` y `contact_number` siguen aceptándose y devolviéndose como campos heredados de solo
  lectura; cuando hay proveedor, el contacto se toma del proveedor. Se retiran en un cambio posterior.

### D6. Baja lógica y duplicados

- `PATCH /status` cambia `is_active`; no hay borrado físico. Un proveedor inactivo conserva sus insumos y
  no se ofrece para nuevas asociaciones.
- La reactivación vuelve a habilitarlo.
- No hay deduplicación automática ni restricción única: dos proveedores con la misma razón social
  coexisten. La búsqueda y el contacto permiten distinguirlos.

### D7. Migración y datos iniciales

- Migración `YYYYMMDDHHMM_e3a_suppliers`: crea `suppliers`, añade `resources.supplier_id`, su índice y la
  FK, y no migra datos. Las columnas heredadas no se tocan.
- Inserción de permisos en el `seed`; si `e3a-rf05-insumos-proyecto` ya subió a `v4`, este cambio usa
  `v5`; si no, usa `v4`. El `seed` debe ser idempotente en ambos casos.
- Reversión: `Down` elimina la FK, el índice, la columna y la tabla; los permisos sembrados se dejan
  inactivos, no se borran.

### D8. Pruebas

- **Unitarias**: validación de los cinco campos, email, duplicados, 1:N, proveedor inactivo, misma
  compañía, copia y autoría.
- **Integración manual** (`D-66`): FK, índice, `rowversion`, aislamiento entre compañías, baja lógica y
  copia.
- **Arquitectura**: `Domain` sin EF, `Application` sin `DbContext`, `Api` sin persistencia y `M-04` sin
  infraestructura de `M-02`.

## Risks / Trade-offs

| Riesgo | Mitigación |
|---|---|
| Proveedores duplicados por nombre | No hay unicidad por requisito; la búsqueda por contacto y email ayuda a elegir; se puede añadir un código en un cambio posterior |
| `supplier_id` apunta a un proveedor de otra compañía | La FK no lo impide; se valida en el caso de uso y se cubre con prueba de aislamiento |
| Convivencia con `distributor`/`contact_number` | Se marcan obsoletos y se mantienen de solo lectura; se retiran cuando el cambio de insumos esté archivado |
| Conflicto de versión del `seed` con el cambio de insumos | Se usa la versión siguiente a la vigente y se revisa el inventario antes de aplicar |
| Catálogo público expone datos de proveedor | `E-5` decidirá el DTO público; aquí solo se documenta el impacto (`D-28`) |
| El Administrador necesita filtrar por compañía | El listado acepta `companyId` solo para el Administrador; el resto lo deriva del actor |

## Migration Plan

1. Respaldo y revisión de la cadena (`dotnet ef migrations list`).
2. Generar `YYYYMMDDHHMM_e3a_suppliers` y revisar el script.
3. Aplicar; ejecutar el `seed` y verificar los permisos.
4. Verificar FK, índice, `rowversion` y aislamiento con dos compañías.
5. Reversión: `Down` y restauración del respaldo si hubiera datos.

## Open Questions

- Ninguna que afecte el alcance de este cambio. El retiro de `distributor`/`contact_number` y la
  publicación en el catálogo se deciden en cambios posteriores.
