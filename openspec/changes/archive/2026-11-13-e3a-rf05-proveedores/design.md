# Design: e3a-rf05-proveedores

## Context

`e3a-rf05-insumos-proyecto` ya define `Resource` con `distributor` y `contact_number` como texto libre y
un `quotation_status` basado en descripción, unidad, precio y marca. Este cambio añade el directorio de
proveedores, la ubicación administrativa y la relación opcional con los insumos; se implementa junto con el
cambio de insumos y modifica su regla de completitud.

`Project` ya tiene `CountryId`, `DepartmentId` y `MunicipalityId` obligatorios, y los catálogos
`countries`, `departments` y `municipalities` existen. `SecurityBaselineSeeder` está en `v4`; este cambio
usa la versión siguiente. El alcance es backend: no hay frontend (`D-109`).

## Goals / Non-Goals

**Goals:**

- Modelar `Supplier` por compañía, reutilizable en todos sus proyectos.
- Admitir observaciones y ubicación opcional con FKs coherentes.
- Asociar `Resource.supplier_id` de forma opcional y derivar `FALTA_INFORMACION` cuando falta.
- Exponer consulta completa, selector ligero y alerta de ubicación no bloqueante.
- Respetar autorización, aislamiento por compañía y concurrencia optimista.

**Non-Goals:**

- Varios proveedores por insumo (N:M) o proveedor principal.
- Órdenes de compra, tarifas, evaluación o comparación de precios.
- Catálogo público (`E-5`).
- Interfaz de usuario.
- Cálculo de distancias, rutas o tiempos de transporte.
- Borrado físico de proveedores.

## Decisions

### D1. Entidad `Supplier` por compañía

```text
Supplier
  Id, CompanyId
  BusinessName
  ContactName?, ContactPhone?, Email?, Address?, Observations?
  CountryId?, DepartmentId?, MunicipalityId?
  IsActive, RowVersion
  CreatedAtUtc, UpdatedAtUtc, LastModifiedByUserId, LastModifiedAtUtc
```

- `CompanyId` obligatorio: el directorio es de la compañía y se reutiliza en todos sus proyectos.
- **Alternativa descartada:** proveedor por proyecto. Duplicaría el directorio y contradiría la
  reutilización entre proyectos (`D-27`).
- Solo `BusinessName` es obligatorio. `ContactName`, `ContactPhone`, `Email`, `Address` y `Observations`
  son opcionales; `Email` se valida con formato cuando se informa.
- `BusinessName` no tiene restricción única; dos proveedores pueden llamarse igual.
- Longitudes: `business_name` 200, `contact_name` 150, `contact_phone` 30, `email` 254, `address` 300,
  `observations` 1000.

### D2. Ubicación y persistencia

- `suppliers` con FKs opcionales `country_id` → `countries`, `department_id` → `departments`,
  `municipality_id` → `municipalities`, todas `RESTRICT`.
- Coherencia jerárquica validada en aplicación: si hay departamento, su país debe coincidir; si hay
  municipio, su departamento debe coincidir. La ubicación parcial se permite.
- Índices `IX_suppliers_company_id_business_name`, `IX_suppliers_company_id_country_id`,
  `IX_suppliers_company_id_department_id`, `IX_suppliers_company_id_municipality_id` e
  `IX_suppliers_company_id_is_active`; **sin** índice único sobre `business_name` ni `email`.
- `resources.supplier_id` nullable, FK `RESTRICT` a `suppliers.id`; índice `IX_resources_supplier_id`.
- La FK no garantiza que el proveedor sea de la misma compañía que el proyecto: esa regla se valida en
  el caso de uso, porque `resources` no lleva `company_id` y la compañía se resuelve por `project_id`.
- **Alternativa descartada:** tabla puente `ResourceSupplier` (N:M). El requisito fija un proveedor por
  insumo; N:M añadiría proveedor principal, precio por proveedor y reglas de desempate fuera de alcance.

### D3. API de proveedores

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| `GET` | `/api/v1/suppliers` | `SUPPLIERS_READ` | Listado completo con filtros y paginación |
| `GET` | `/api/v1/suppliers/{supplierId}` | `SUPPLIERS_READ` | Detalle completo, incluida ubicación y observaciones |
| `GET` | `/api/v1/suppliers/options` | `SUPPLIERS_READ` | Selector ligero: solo `supplierId` y `businessName` |
| `GET` | `/api/v1/suppliers/{supplierId}/location-alert?projectId=` | `SUPPLIERS_READ` | Alerta de ubicación no bloqueante |
| `POST` | `/api/v1/suppliers` | `SUPPLIERS_WRITE` | Alta |
| `PUT` | `/api/v1/suppliers/{supplierId}` | `SUPPLIERS_WRITE` | Edición con `rowVersion` |
| `PATCH` | `/api/v1/suppliers/{supplierId}/status` | `SUPPLIERS_WRITE` | Baja lógica o reactivación |

- `CreateSupplierRequest`: `businessName` obligatorio; `contactName`, `contactPhone`, `email`, `address`,
  `observations`, `countryId`, `departmentId`, `municipalityId` opcionales. La compañía sale del actor;
  nunca del cuerpo.
- `SupplierDto` completo: los campos anteriores, `isActive`, `rowVersion` y autoría.
- `SupplierOptionDto`: solo `supplierId` y `businessName`. Filtros `businessName`, `contactName`,
  `countryId`, `departmentId`, `municipalityId`; solo proveedores activos.
- Listado completo con los mismos filtros, más `page`/`pageSize` y orden por `businessName`, `id`.
- `LocationAlertDto`: `supplierId`, `projectId`, `canCompare`, `isSameLocation`, `code`, `message`,
  `supplierLocation` y `projectLocation`.
- Códigos de alerta: `SUPPLIER_LOCATION_MATCH`, `SUPPLIER_LOCATION_MISMATCH`, `LOCATION_INCOMPLETE`.
- Errores `ProblemDetails`: `SUPPLIER_NOT_FOUND`, `SUPPLIER_INACTIVE`, `SUPPLIER_COMPANY_MISMATCH`,
  `SUPPLIER_CONCURRENCY_CONFLICT`, `SUPPLIER_WRITE_FORBIDDEN`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`.

### D4. Autorización y alcance

- Permisos nuevos `SUPPLIERS_READ` y `SUPPLIERS_WRITE`, con `seed` versionado e idempotente.
- Administrador → `SUPPLIERS_READ`; Director y Presupuestador → lectura y escritura; Auditor → lectura.
- El Administrador lee cualquier compañía; Director, Presupuestador y Auditor leen la propia.
- El Administrador no escribe contenido comercial (`D-83`).
- El alcance se resuelve con `IActorAccessReader`; no se acepta `companyId` del cuerpo.
- La alerta de ubicación exige `SUPPLIERS_READ` y lectura del proyecto con el mismo alcance de `M-04`;
  un proyecto de otra compañía responde como inexistente.

### D5. Integración con el insumo y estado

- `CreateResourceRequest` y `UpdateResourceRequest` aceptan `supplierId` opcional.
- Al asociar un proveedor se valida: existe, pertenece a la compañía del proyecto y está activo. Si el
  insumo ya tenía ese proveedor, una edición de otros campos no falla porque el proveedor esté inactivo.
- `Resource.CalculateQuotationStatus` pasa a exigir **cinco** condiciones: descripción, unidad, precio,
  marca y proveedor. Sin proveedor → `FALTA_INFORMACION`; con proveedor y los cuatro campos → `POR_COTIZAR`.
  Si se retira el proveedor, vuelve a `FALTA_INFORMACION`.
- `ResourceDto` incluye un resumen `supplier` con razón social, contacto, teléfono, email, dirección y
  ubicación; si no hay proveedor, devuelve `null`. Las observaciones quedan en el detalle completo.
- La copia de un insumo conserva `supplier_id`; si el proveedor no pertenece a la compañía del destino, la
  copia se rechaza sin crearla.
- `distributor` y `contact_number` siguen aceptándose y devolviéndose como campos heredados de solo
  lectura; cuando hay proveedor, el contacto se toma del proveedor.

### D6. Comparación de ubicación

Se compara el nivel más específico que ambos tengan informado:

1. Si ambos tienen `municipality_id`: iguales → coincidencia; distintos → discrepancia.
2. Si no, si ambos tienen `department_id`: iguales → coincidencia; distintos → discrepancia.
3. Si no, si ambos tienen `country_id`: iguales → coincidencia; distintos → discrepancia.
4. Si no hay un nivel común → `LOCATION_INCOMPLETE` con `canCompare = false`.

Un municipio distinto en el mismo departamento también es discrepancia: la alerta es una advertencia de
distancia, no una validación de jerarquía. La alerta nunca devuelve `409` ni impide la asignación.

### D7. Baja lógica y duplicados

- `PATCH /status` cambia `is_active`; no hay borrado físico. Un proveedor inactivo conserva sus insumos y
  no se ofrece para nuevas asociaciones.
- La reactivación vuelve a habilitarlo.
- No hay deduplicación automática ni restricción única: dos proveedores con la misma razón social
  coexisten. La búsqueda por contacto y ubicación permite distinguirlos.

### D8. Migración y datos iniciales

- Migración `YYYYMMDDHHMM_e3a_suppliers`: crea `suppliers`, añade `resources.supplier_id`, sus índices y
  FKs, y no migra datos. Las columnas heredadas no se tocan.
- Inserción de permisos en el `seed`; si el cambio de insumos ya subió a `v4`, este usa `v5`. El `seed`
  debe ser idempotente.
- Reversión: `Down` elimina la FK, el índice, la columna y la tabla; los permisos sembrados se dejan
  inactivos, no se borran.

### D9. Pruebas

- **Unitarias**: validación de campos opcionales, email, jerarquía de ubicación, duplicados, 1:N,
  proveedor opcional, estado del insumo, selector y comparación de ubicación.
- **Integración manual** (`D-66`): FKs, índices, `rowversion`, aislamiento, baja lógica, reutilización
  entre proyectos, alerta y copia.
- **Arquitectura**: `Domain` sin EF, `Application` sin `DbContext`, `Api` sin persistencia y `M-04` sin
  infraestructura de `M-02`.

## Riesgos / Trade-offs

| Riesgo | Mitigación |
|---|---|
| Alerta de ubicación con falsos positivos | Es solo advertencia; el usuario decide y puede corregir la ubicación del proveedor o del proyecto |
| Ubicación incompleta | La alerta devuelve `LOCATION_INCOMPLETE`; no bloquea la asignación |
| La regla de estado contradice `REQ-INSUMOS-04` | Se documenta en `proveedores`; el cambio de insumos debe reconciliar `REQ-INSUMOS-04` y `Resource.CalculateQuotationStatus` antes de implementar ambos |
| Proveedores duplicados por nombre | No hay unicidad por requisito; la búsqueda por contacto y ubicación ayuda a elegir |
| `supplier_id` de otra compañía | La FK no lo impide; se valida en el caso de uso y se cubre con prueba de aislamiento |
| Convivencia con `distributor`/`contact_number` | Se marcan obsoletos y se mantienen de solo lectura; se retiran cuando el cambio de insumos esté archivado |
| Conflicto de versión del `seed` | Se usa la versión siguiente a la vigente y se revisa el inventario antes de aplicar |

## Migration Plan

1. Respaldo y revisión de la cadena (`dotnet ef migrations list`).
2. Generar `YYYYMMDDHHMM_e3a_suppliers` y revisar el script.
3. Aplicar; ejecutar el `seed` y verificar los permisos.
4. Verificar FKs de ubicación, índice, `rowversion` y aislamiento con dos compañías.
5. Reversión: `Down` y restauración del respaldo si hubiera datos.

## Open Questions

- Ninguna que afecte el alcance de este cambio. El retiro de `distributor`/`contact_number`, la
  publicación en el catálogo y el cálculo de distancias se deciden en cambios posteriores.
