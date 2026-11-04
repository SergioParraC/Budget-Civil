# Tareas: e3a-rf05-proveedores

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores nuevos
(`T-U-15`, `T-U-16`, `T-I-36`, `T-I-37`, `T-S-15`, `T-C-03`) se registran en la tarea 4.5 y se proponen
para `PLAN §5.6.5`:

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-15` | Datos mínimos, email, duplicados y reglas de baja lógica | Unitaria |
| `T-U-16` | Asociación 1:N, misma compañía, copia y datos heredados | Unitaria |
| `T-I-36` | Persistencia, FK, índices y `rowversion` | Integración manual |
| `T-I-37` | Un proveedor con varios insumos, baja lógica y duplicados | Integración manual |
| `T-S-15` | Aislamiento entre compañías y solo lectura de Administrador y Auditor | Seguridad manual |
| `T-C-03` | Contrato v1 de proveedores | Contrato en CI |

**Puerta de entrada:** este cambio se implementa junto con `e3a-rf05-insumos-proyecto`, porque necesita el
modelo `Resource`. No se inicia hasta que ese cambio esté aplicado y `E-2` cerrado.

## 1. Dominio

- [ ] 1.1 Verificar que `Resource` existe y que la cadena de migraciones está al día. Verificación: `dotnet build` sin errores, `Resource` y su migración presentes, y `dotnet ef migrations list` con la cadena vigente; si falta, se detiene el cambio y se registra el bloqueo.
- [ ] 1.2 Crear `Supplier` en `Domain/Suppliers` con `CompanyId`, `BusinessName`, `ContactName`, `ContactPhone`, `Email`, `Address`, `IsActive`, `RowVersion` y autoría. Verificación: prueba unitaria que construye un proveedor válido y rechaza un campo obligatorio vacío (`SC-PROVEEDORES-04`, `SC-PROVEEDORES-05`, `SC-PROVEEDORES-07`).
- [ ] 1.3 Implementar la validación del email y las reglas de dominio: sin unicidad de razón social, asociación 1:N y baja lógica. Verificación: pruebas unitarias `SC-PROVEEDORES-06`, `SC-PROVEEDORES-12`, `SC-PROVEEDORES-13`, `SC-PROVEEDORES-23` y `SC-PROVEEDORES-26` en verde.
- [ ] 1.4 Cubrir con pruebas unitarias los datos mínimos, el email, los duplicados y la baja lógica (`T-U-15`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y cobertura ≥ 80 % en `Domain/Suppliers`.

## 2. Persistencia

- [ ] 2.1 Configurar `Supplier` en EF Core: tabla `suppliers`, longitudes, `CHECK` de email no vacío, `rowversion`, `is_active` y FK `RESTRICT` a `companies`. Verificación: el modelo genera el SQL esperado y la prueba de integración comprueba la FK (`T-I-36`).
- [ ] 2.2 Configurar `resources.supplier_id` nullable y su FK `RESTRICT` a `suppliers`, con índice `IX_resources_supplier_id`. Verificación: el script de migración contiene la columna, el índice y la FK, y no altera las columnas heredadas.
- [ ] 2.3 Crear los índices `IX_suppliers_company_id_business_name` e `IX_suppliers_company_id_is_active`, sin índices únicos sobre razón social o email. Verificación: el script no contiene `UQ_` sobre `business_name` ni `email`.
- [ ] 2.4 Generar la migración `YYYYMMDDHHMM_e3a_suppliers` y actualizar `docs/runbook-migraciones.md` con la aplicación y la reversión (`D-40`, `D-107`). Verificación: `dotnet ef migrations script` revisado y `Down` elimina FK, índice, columna y tabla.
- [ ] 2.5 Implementar `ISupplierRepository` en `Application/Suppliers/Repositories` y su versión EF: alta, actualización, baja lógica, reactivación, detalle y listado con búsqueda y paginación. Verificación: pruebas de integración `T-I-36` y `T-I-37` en verde contra SQL Server local.
- [ ] 2.6 Añadir las pruebas de integración de persistencia: FK, índices, `rowversion`, baja lógica y aislamiento. Verificación: `T-I-36` documentado con evidencia y resultado por aserción.
- [ ] 2.7 Actualizar `05-modelo-datos.md` con `suppliers`, `resources.supplier_id` y la relación 1:N. Verificación: el documento describe los cinco campos, la cardinalidad y la ausencia de unicidad.

## 3. Aplicación

- [ ] 3.1 Crear los DTOs y el mapeo manual de `Supplier` (`SupplierDto`, `SupplierListItem`, `SupplierSummary` dentro de `ResourceDto`), sin AutoMapper ni Mapster. Verificación: prueba de contrato que serializa `businessName`, `contactName`, `contactPhone`, `email`, `address`, `isActive` y `rowVersion` (`D-41`, `D-55`).
- [ ] 3.2 Implementar `CreateSupplierCommand` y `UpdateSupplierCommand` con validadores de los cinco campos, email y `rowVersion`. Verificación: pruebas unitarias `SC-PROVEEDORES-04`…`SC-PROVEEDORES-07` y `SC-PROVEEDORES-27` en verde.
- [ ] 3.3 Implementar `ChangeSupplierStatusCommand` para baja lógica y reactivación. Verificación: pruebas unitarias `SC-PROVEEDORES-23`…`SC-PROVEEDORES-26` en verde.
- [ ] 3.4 Implementar `GetSuppliersQuery` con búsqueda por razón social, contacto o email, y paginación con total. Verificación: pruebas unitarias `SC-PROVEEDORES-20`…`SC-PROVEEDORES-22` en verde.
- [ ] 3.5 Integrar el proveedor en los casos de uso de insumos: `supplierId` en alta y edición, resumen en la consulta y conservación en la copia. Verificación: pruebas unitarias `SC-PROVEEDORES-08`…`SC-PROVEEDORES-11`, `SC-PROVEEDORES-30` y `SC-PROVEEDORES-31` en verde.
- [ ] 3.6 Implementar el alcance y la autorización: Administrador lee todo, Director y Presupuestador leen y escriben en su compañía, Auditor lee la propia; el proveedor del insumo debe ser de la compañía del proyecto. Verificación: pruebas unitarias `T-U-16` con `SC-PROVEEDORES-01`…`SC-PROVEEDORES-03`, `SC-PROVEEDORES-14`…`SC-PROVEEDORES-19` en verde.
- [ ] 3.7 Cubrir con pruebas unitarias los validadores, los casos de uso, la asociación y la copia (`T-U-16`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.

## 4. API, contrato y permisos

- [ ] 4.1 Exponer los cinco endpoints de `/api/v1/suppliers` con los atributos `SUPPLIERS_READ` y `SUPPLIERS_WRITE`. Verificación: Swagger muestra las rutas y una petición sin permiso responde `403`.
- [ ] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` (`SUPPLIER_NOT_FOUND`, `SUPPLIER_INACTIVE`, `SUPPLIER_COMPANY_MISMATCH`, `SUPPLIER_CONCURRENCY_CONFLICT`, `SUPPLIER_WRITE_FORBIDDEN`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`) y `409` en concurrencia. Verificación: `T-C-03` sin sobres propios ni `snake_case`.
- [ ] 4.3 Ampliar `SecurityBaselineSeeder` con `SUPPLIERS_READ` y `SUPPLIERS_WRITE` en la versión siguiente a la vigente y la asignación por rol del diseño. Verificación: ejecutar el `seed` dos veces deja el mismo inventario y la matriz coincide con `REQ-PROVEEDORES-05`.
- [ ] 4.4 Verificar el contrato v1: `camelCase`, paginación `page`/`pageSize`, `ProblemDetails` y DTOs sin entidades. Verificación: `T-C-03` revisado contra Swagger y sin diferencias inesperadas.
- [ ] 4.5 Actualizar `.github/docs/02-requerimientos.md` (validación y autorización de proveedores) y `06-flujos-casos-de-uso.md` (UC-04), y proponer los identificadores nuevos a `PLAN §5.6.1` y `§5.6.5`. Verificación: los documentos describen los cinco campos, la relación 1:N y los identificadores nuevos aparecen en la matriz funcionalidad → prueba.

## 5. Verificación de la etapa

- [ ] 5.1 Ejecutar las pruebas unitarias, de arquitectura y de contrato en CI. Verificación: `dotnet test` en verde, `T-A-02` sin violaciones y cobertura ≥ 80 % en `Domain/Suppliers`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local: `T-I-36`, `T-I-37`, `T-S-15`, `T-C-03`, con dos compañías, dos proyectos y proveedores duplicados. Verificación: checklist con resultado y evidencia por identificador, incluida la baja lógica y el aislamiento.
- [ ] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los diez `REQ-PROVEEDORES-*` y anexarla a este archivo (`PLAN §6.1.4`, `§9`). Verificación: ningún requisito queda sin escenario ni sin prueba.
- [ ] 5.4 Ejecutar `openspec validate e3a-rf05-proveedores --strict` y archivar solo con la revisión del estudiante. Verificación: validación sin errores, checklist firmado y `openspec archive` con la capacidad `proveedores` sincronizada.

## Workflow follow-up

- Archivar el cambio solo después de que `e3a-rf05-insumos-proyecto` esté aplicado y el estudiante apruebe los escenarios.
- Sincronizar `specs/proveedores/spec.md` a `openspec/specs/proveedores/` al archivar.
- Actualizar el índice de `openspec/project.md` con la capacidad `proveedores` (M-04, E-3a).
- Documentar en el backlog el retiro de `distributor`/`contact_number` y la publicación de proveedores en el catálogo (`E-5`).
