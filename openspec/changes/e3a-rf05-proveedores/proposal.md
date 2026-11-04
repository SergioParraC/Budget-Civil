# Cambio: e3a-rf05-proveedores

## Why

El cambio `e3a-rf05-insumos-proyecto` modela al proveedor como dos campos de texto libre en el insumo
(`distributor` y `contact_number`). Eso obliga a repetir los mismos datos en cada insumo, impide mantener
un directorio de contactos y no garantiza que dos insumos del mismo proveedor apunten a la misma
información.

`E-3a` necesita un directorio de proveedores por compañía, con datos de contacto reutilizables y una
relación de uno a muchos con los insumos. El ajuste es **solo de backend**: la interfaz sigue fuera del
alcance de esta etapa (`D-109`).

## What Changes

- Se crea la entidad `Supplier`, **propia de una compañía**, con los datos mínimos: razón social, nombre de
  contacto, teléfono del contacto, correo electrónico y dirección.
- Se añade `Resource.supplier_id` como referencia **opcional** a un proveedor de la misma compañía; un
  proveedor puede tener **muchos insumos asociados** y `supplier_id` **no es único**.
- Se permite que existan proveedores con la misma razón social: no hay restricción única sobre
  `business_name`; el identificador del proveedor es su clave.
- Se exponen listado, consulta, alta, edición y baja lógica de proveedores, y la asociación se refleja en
  el alta, la edición, la consulta y la copia de insumos.
- Se añaden los permisos `SUPPLIERS_READ` y `SUPPLIERS_WRITE` al `seed` versionado; Director y
  Presupuestador escriben, Administrador y Auditor solo leen.
- Se conservan `distributor` y `contact_number` como campos heredados de solo lectura; cuando un insumo
  tiene proveedor, los datos de contacto se obtienen del proveedor.
- Migración manual con `suppliers` y `resources.supplier_id`, sin migración de datos (`D-40`, `D-107`).

## Capabilities

### New Capabilities

- `proveedores`: directorio de proveedores por compañía, datos mínimos de contacto, asociación de uno a
  muchos con los insumos del proyecto y reglas de autorización y baja lógica.

### Modified Capabilities

- Ninguna en este delta. La integración con `insumos` se especifica del lado del proveedor y se implementa
  junto con el cambio en curso `e3a-rf05-insumos-proyecto`, que incorpora `supplier_id` en su modelo de
  insumo. No se duplica el delta de `insumos` para no crear dos fuentes de verdad.

## Fuera de alcance

- Frontend, componentes, rutas y cliente Angular (`D-109`).
- Varios proveedores por insumo (N:M); este cambio fija **un proveedor por insumo** y muchos insumos por
  proveedor.
- Órdenes de compra, listas de precios por proveedor, evaluación de proveedores y comparación de precios.
- Catálogo abierto (`E-5`): publicará los datos del proveedor cuando se especifique; aquí no se modifica.
- Borrado físico de proveedores; solo baja lógica.
- Retiro de `distributor` y `contact_number`; quedan obsoletos y se retiran en un cambio posterior.

## Decisiones y supuestos

| Tema | Decisión | Motivo |
|---|---|---|
| Alcance del proveedor | Por compañía | Los insumos de todos los proyectos de la compañía pueden reutilizarlo |
| Cardinalidad | 1 proveedor → N insumos; un insumo → 0..1 proveedor | El enunciado pide «múltiples insumos asociados» |
| Unicidad | `supplier_id` no único; `business_name` sin restricción única | El proveedor puede repetirse y dos proveedores pueden llamarse igual |
| Campos obligatorios | Razón social, contacto, teléfono, email y dirección | Son los datos mínimos solicitados |
| Autorización | Director y Presupuestador escriben; Administrador y Auditor leen | Coherente con la matriz `1.3.2` y `D-34` |
| Campos heredados | `distributor` y `contact_number` quedan obsoletos, sin borrado | Evita romper el cambio de insumos en curso |
| Alcance técnico | Solo backend | La interfaz no se especifica en `E-3a` (`D-109`) |

## Criterios de aceptación

1. `openspec validate e3a-rf05-proveedores --strict` pasa sin errores.
2. Cada `REQ-PROVEEDORES-*` tiene al menos un `SC-PROVEEDORES-*` en formato `Dado / Cuando / Entonces` y
   cita sus `RF`, `BR`, `R` y `D`.
3. Los cinco datos mínimos son obligatorios y el email se valida con formato.
4. Un proveedor puede tener varios insumos y el mismo proveedor se repite en `resources.supplier_id` sin
   restricción única.
5. Un insumo no puede asociarse a un proveedor de otra compañía.
6. Dos proveedores con la misma razón social coexisten.
7. Administrador y Auditor no crean ni editan proveedores.
8. La copia de un insumo dentro de la misma compañía conserva su proveedor.
9. Ningún requisito depende de un `X` abierto.

## Impact

- **Base de datos**: tabla `suppliers` (`company_id`, `business_name`, `contact_name`, `contact_phone`,
  `email`, `address`, `is_active`, `rowversion`, autoría); columna `resources.supplier_id` nullable con
  FK `RESTRICT`; índices `suppliers(company_id, business_name)` y `resources(supplier_id)`.
- **Contrato de API**: endpoints `/api/v1/suppliers` (listado, detalle, alta, edición y baja lógica) y
  `supplierId` en las peticiones y respuestas de insumos; `camelCase`, `ProblemDetails` y paginación
  `page`/`pageSize`.
- **Código**: `M-04` en las cuatro capas; sin dependencias nuevas.
- **Permisos**: `SUPPLIERS_READ` y `SUPPLIERS_WRITE` con `seed` versionado e idempotente.
- **Pruebas**: unitarias de validación y asociación; integración manual de compañía, duplicados y copia;
  arquitectura de módulos.
- **Catálogo**: `E-5` consumirá los datos del proveedor; queda fuera de este cambio.
- **Frontend**: fuera de alcance (`D-109`).
