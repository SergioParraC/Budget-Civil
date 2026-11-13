# Tareas: e3a-rf05-proveedores

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores nuevos
(`T-U-15`, `T-U-16`, `T-U-17`, `T-I-36`, `T-I-37`, `T-I-38`, `T-S-15`, `T-C-03`) se registran en la tarea
4.5 y se proponen para `PLAN §5.6.5`:

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-15` | Campos opcionales, email, jerarquía de ubicación y duplicados | Unitaria |
| `T-U-16` | Asociación 1:N, proveedor opcional, estado del insumo y copia | Unitaria |
| `T-U-17` | Selector ligero y comparación de ubicación | Unitaria |
| `T-I-36` | Persistencia, FKs de ubicación, índices y `rowversion` | Integración manual |
| `T-I-37` | Reutilización entre proyectos, baja lógica y duplicados | Integración manual |
| `T-I-38` | Selector, alerta de ubicación y asignación no bloqueante | Integración manual |
| `T-S-15` | Aislamiento entre compañías y solo lectura de Administrador y Auditor | Seguridad manual |
| `T-C-03` | Contrato v1 de proveedores | Contrato en CI |

**Puerta de entrada:** este cambio se implementa junto con `e3a-rf05-insumos-proyecto`, porque necesita
`Resource` y modifica su regla de completitud. No se inicia hasta que el cambio de insumos esté aplicado y
`E-2` cerrado.

## 1. Dominio

- [ ] 1.1 Reconciliar `REQ-INSUMOS-04` y `Resource.CalculateQuotationStatus` para exigir proveedor, y verificar que `Resource` y la cadena de migraciones están al día. Verificación: el delta de insumos incluye la nueva condición, `dotnet build` sin errores y `dotnet ef migrations list` con la cadena vigente; si falta, se detiene el cambio y se registra el bloqueo.
- [ ] 1.2 Crear `Supplier` en `Domain/Suppliers` con `CompanyId`, `BusinessName` obligatorio; `ContactName`, `ContactPhone`, `Email`, `Address`, `Observations`, `CountryId`, `DepartmentId` y `MunicipalityId` opcionales; `IsActive`, `RowVersion` y autoría. Verificación: prueba unitaria que crea un proveedor solo con razón social y rechaza la razón social vacía (`SC-PROVEEDORES-04`, `SC-PROVEEDORES-06`).
- [ ] 1.3 Implementar la validación del email, la coherencia país/departamento/municipio y la regla de no unicidad de razón social. Verificación: pruebas unitarias `SC-PROVEEDORES-07`…`SC-PROVEEDORES-12`, `SC-PROVEEDORES-20` y `SC-PROVEEDORES-21` en verde.
- [ ] 1.4 Implementar en dominio la asociación 1:N, el proveedor opcional, el estado `FALTA_INFORMACION` sin proveedor y la copia que conserva `supplier_id`. Verificación: pruebas unitarias `SC-PROVEEDORES-13`…`SC-PROVEEDORES-19`, `SC-PROVEEDORES-47` y `SC-PROVEEDORES-48` en verde.
- [ ] 1.5 Cubrir con pruebas unitarias los campos, la ubicación, el estado y la copia (`T-U-15`, `T-U-16`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y cobertura ≥ 80 % en `Domain/Suppliers` y `Domain/Resources`.

## 2. Persistencia

- [ ] 2.1 Configurar `Supplier` en EF Core: tabla `suppliers`, longitudes, `rowversion`, `is_active`, FK `RESTRICT` a `companies` y FKs opcionales `RESTRICT` a `countries`, `departments` y `municipalities`. Verificación: el modelo genera el SQL esperado y la prueba de integración comprueba las FKs (`T-I-36`).
- [ ] 2.2 Configurar `resources.supplier_id` nullable y su FK `RESTRICT` a `suppliers`, con índice `IX_resources_supplier_id`. Verificación: el script de migración contiene la columna, el índice y la FK, y no altera las columnas heredadas.
- [ ] 2.3 Crear los índices de compañía, razón social y ubicación, sin índices únicos sobre razón social o email. Verificación: el script no contiene `UQ_` sobre `business_name` ni `email` y contiene los índices de ubicación.
- [ ] 2.4 Generar la migración `YYYYMMDDHHMM_e3a_suppliers` y actualizar `docs/runbook-migraciones.md` con la aplicación y la reversión (`D-40`, `D-107`). Verificación: `dotnet ef migrations script` revisado y `Down` elimina FKs, índices, columna y tabla.
- [ ] 2.5 Implementar `ISupplierRepository` en `Application/Suppliers/Repositories` y su versión EF: alta, actualización, baja lógica, reactivación, detalle completo, listado con filtros, selector y consulta de ubicación. Verificación: pruebas de integración `T-I-36` y `T-I-37` en verde contra SQL Server local.
- [ ] 2.6 Añadir las pruebas de integración de persistencia: FKs de ubicación, índices, `rowversion`, baja lógica, reutilización entre proyectos y aislamiento. Verificación: `T-I-36` y `T-I-37` documentados con evidencia y resultado por aserción.
- [ ] 2.7 Actualizar `05-modelo-datos.md` con `suppliers`, sus FKs de ubicación, `resources.supplier_id` y la relación 1:N. Verificación: el documento describe los campos opcionales, la ubicación, la cardinalidad y la ausencia de unicidad.

## 3. Aplicación

- [ ] 3.1 Crear los DTOs y el mapeo manual: `SupplierDto` completo, `SupplierOptionDto` con solo `supplierId` y `businessName`, `SupplierSummary` dentro de `ResourceDto` y `LocationAlertDto`. Verificación: prueba de contrato que serializa los campos y comprueba que el selector no expone contacto ni ubicación (`D-41`, `D-55`).
- [ ] 3.2 Implementar `CreateSupplierCommand` y `UpdateSupplierCommand` con validadores de razón social obligatoria, email opcional, ubicación opcional y `rowVersion`. Verificación: pruebas unitarias `SC-PROVEEDORES-04`…`SC-PROVEEDORES-12` y `SC-PROVEEDORES-44` en verde.
- [ ] 3.3 Implementar `ChangeSupplierStatusCommand` para baja lógica y reactivación. Verificación: pruebas unitarias `SC-PROVEEDORES-40`…`SC-PROVEEDORES-43` en verde.
- [ ] 3.4 Implementar `GetSuppliersQuery` completo con filtros por razón social, contacto y ubicación, y paginación con total. Verificación: pruebas unitarias `SC-PROVEEDORES-28`…`SC-PROVEEDORES-31` en verde.
- [ ] 3.5 Implementar `GetSupplierOptionsQuery` con solo `supplierId` y `businessName`, filtros por razón social, contacto y ubicación, y exclusión de inactivos. Verificación: pruebas unitarias `SC-PROVEEDORES-32`…`SC-PROVEEDORES-34` en verde.
- [ ] 3.6 Implementar `GetSupplierLocationAlertQuery` con la comparación por nivel más específico y los códigos `SUPPLIER_LOCATION_MATCH`, `SUPPLIER_LOCATION_MISMATCH` y `LOCATION_INCOMPLETE`. Verificación: pruebas unitarias `SC-PROVEEDORES-35`…`SC-PROVEEDORES-39` en verde.
- [ ] 3.7 Integrar el proveedor en los casos de uso de insumos: `supplierId` opcional en alta y edición, recálculo de estado, resumen en la consulta, conservación en la copia y datos heredados. Verificación: pruebas unitarias `SC-PROVEEDORES-14`, `SC-PROVEEDORES-17`…`SC-PROVEEDORES-19`, `SC-PROVEEDORES-49` y `SC-PROVEEDORES-50` en verde.
- [ ] 3.8 Implementar el alcance y la autorización: Administrador lee todo, Director y Presupuestador leen y escriben en su compañía, Auditor lee la propia; el proveedor del insumo debe ser de la compañía del proyecto. Verificación: pruebas unitarias `T-U-16` con `SC-PROVEEDORES-01`…`SC-PROVEEDORES-03`, `SC-PROVEEDORES-22`…`SC-PROVEEDORES-27` en verde.
- [ ] 3.9 Cubrir con pruebas unitarias los validadores, los casos de uso, el selector y la alerta (`T-U-17`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.

## 4. API, contrato y permisos

- [ ] 4.1 Exponer los siete endpoints de `/api/v1/suppliers` (listado, detalle, selector, alerta de ubicación, alta, edición y estado) con los atributos `SUPPLIERS_READ` y `SUPPLIERS_WRITE`. Verificación: Swagger muestra las rutas y una petición sin permiso responde `403`.
- [ ] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` (`SUPPLIER_NOT_FOUND`, `SUPPLIER_INACTIVE`, `SUPPLIER_COMPANY_MISMATCH`, `SUPPLIER_CONCURRENCY_CONFLICT`, `SUPPLIER_WRITE_FORBIDDEN`, `LOCATION_INCOMPLETE`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`) y `409` en concurrencia. Verificación: `T-C-03` sin sobres propios ni `snake_case`.
- [ ] 4.3 Ampliar `SecurityBaselineSeeder` con `SUPPLIERS_READ` y `SUPPLIERS_WRITE` en la versión siguiente a la vigente y la asignación por rol del diseño. Verificación: ejecutar el `seed` dos veces deja el mismo inventario y la matriz coincide con `REQ-PROVEEDORES-07`.
- [ ] 4.4 Verificar el contrato v1: `camelCase`, paginación `page`/`pageSize`, `ProblemDetails`, selector con solo dos campos y alerta con `canCompare`/`code`. Verificación: `T-C-03` revisado contra Swagger y sin diferencias inesperadas.
- [ ] 4.5 Actualizar `.github/docs/02-requerimientos.md` (validación y autorización de proveedores), `06-flujos-casos-de-uso.md` (UC-04) y `05-modelo-datos.md`, y proponer los identificadores nuevos a `PLAN §5.6.1` y `§5.6.5`. Verificación: los documentos describen los campos, la ubicación, el selector y la alerta, y los identificadores aparecen en la matriz funcionalidad → prueba.

## 5. Verificación de la etapa

- [ ] 5.1 Ejecutar las pruebas unitarias, de arquitectura y de contrato en CI. Verificación: `dotnet test` en verde, `T-A-02` sin violaciones y cobertura ≥ 80 % en `Domain/Suppliers` y `Domain/Resources`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local: `T-I-36`, `T-I-37`, `T-I-38`, `T-S-15` y `T-C-03`, con dos compañías, dos proyectos, proveedores duplicados y ubicaciones distintas. Verificación: checklist con resultado y evidencia por identificador, incluida la reutilización, la alerta y el aislamiento.
- [ ] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los catorce `REQ-PROVEEDORES-*` y anexarla a este archivo (`PLAN §6.1.4`, `§9`). Verificación: ningún requisito queda sin escenario ni sin prueba.
- [ ] 5.4 Ejecutar `openspec validate e3a-rf05-proveedores --strict` y archivar solo con la revisión del estudiante. Verificación: validación sin errores, checklist firmado y `openspec archive` con la capacidad `proveedores` sincronizada.

## Workflow follow-up

- Archivar el cambio solo después de que `e3a-rf05-insumos-proyecto` esté aplicado y el estudiante apruebe los escenarios.
- Sincronizar `specs/proveedores/spec.md` a `openspec/specs/proveedores/` al archivar.
- Actualizar el índice de `openspec/project.md` con la capacidad `proveedores` (M-04, E-3a).
- Documentar en el backlog el retiro de `distributor`/`contact_number`, la publicación de proveedores en el catálogo (`E-5`) y el cálculo de distancias.
