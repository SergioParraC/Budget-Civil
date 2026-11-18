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

- [x] 1.1 Reconciliar `REQ-INSUMOS-04` y `Resource.CalculateQuotationStatus` para exigir proveedor, y verificar que `Resource` y la cadena de migraciones están al día. Verificación: el delta de insumos incluye la nueva condición, `dotnet build` sin errores y `dotnet ef migrations list` con la cadena vigente; si falta, se detiene el cambio y se registra el bloqueo.
- [x] 1.2 Crear `Supplier` en `Domain/Suppliers` con `CompanyId`, `BusinessName` obligatorio; `ContactName`, `ContactPhone`, `Email`, `Address`, `Observations`, `CountryId`, `DepartmentId` y `MunicipalityId` opcionales; `IsActive`, `RowVersion` y autoría. Verificación: prueba unitaria que crea un proveedor solo con razón social y rechaza la razón social vacía (`SC-PROVEEDORES-04`, `SC-PROVEEDORES-06`).
- [x] 1.3 Implementar la validación del email, la coherencia país/departamento/municipio y la regla de no unicidad de razón social. Verificación: pruebas unitarias `SC-PROVEEDORES-07`…`SC-PROVEEDORES-12`, `SC-PROVEEDORES-20` y `SC-PROVEEDORES-21` en verde.
- [x] 1.4 Implementar en dominio la asociación 1:N, el proveedor opcional, el estado `FALTA_INFORMACION` sin proveedor y la copia que conserva `supplier_id`. Verificación: pruebas unitarias `SC-PROVEEDORES-13`…`SC-PROVEEDORES-19`, `SC-PROVEEDORES-47` y `SC-PROVEEDORES-48` en verde.
- [x] 1.5 Cubrir con pruebas unitarias los campos, la ubicación, el estado y la copia (`T-U-15`, `T-U-16`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y cobertura ≥ 80 % en `Domain/Suppliers` y `Domain/Resources`.

## 2. Persistencia

- [x] 2.1 Configurar `Supplier` en EF Core: tabla `suppliers`, longitudes, `rowversion`, `is_active`, FK `RESTRICT` a `companies` y FKs opcionales `RESTRICT` a `countries`, `departments` y `municipalities`. Verificación: el modelo genera el SQL esperado y la prueba de integración comprueba las FKs (`T-I-36`).
- [x] 2.2 Configurar `resources.supplier_id` nullable y su FK `RESTRICT` a `suppliers`, con índice `IX_resources_supplier_id`. Verificación: el script de migración contiene la columna, el índice y la FK, y no altera las columnas heredadas.
- [x] 2.3 Crear los índices de compañía, razón social y ubicación, sin índices únicos sobre razón social o email. Verificación: el script no contiene `UQ_` sobre `business_name` ni `email` y contiene los índices de ubicación.
- [x] 2.4 Generar la migración `YYYYMMDDHHMM_e3a_suppliers` y actualizar `docs/runbook-migraciones.md` con la aplicación y la reversión (`D-40`, `D-107`). Verificación: `dotnet ef migrations script` revisado y `Down` elimina FKs, índices, columna y tabla.
- [x] 2.5 Implementar `ISupplierRepository` en `Application/Suppliers/Repositories` y su versión EF: alta, actualización, baja lógica, reactivación, detalle completo, listado con filtros, selector y consulta de ubicación. Verificación: pruebas de integración `T-I-36` y `T-I-37` en verde contra SQL Server local. Hecho 13/11/2026: `SupplierRepository` implementado y `SupplierPersistenceTests` (8 casos) en verde contra SQL Server 2022 en Docker (`T-I-36`, `T-I-37`).
- [x] 2.6 Añadir las pruebas de integración de persistencia: FKs de ubicación, índices, `rowversion`, baja lógica, reutilización entre proyectos y aislamiento. Verificación: `T-I-36` y `T-I-37` documentados con evidencia y resultado por aserción. Hecho 13/11/2026: los 8 casos de `SupplierPersistenceTests` cubren ubicación y `rowversion`, reutilización entre dos proyectos, baja lógica que conserva el insumo, razón social duplicada, aislamiento entre compañías, FK de ubicación inexistente, concurrencia y FKs/índices del modelo; resultado y comando quedan en «Estado de verificación».
- [x] 2.7 Actualizar `05-modelo-datos.md` con `suppliers`, sus FKs de ubicación, `resources.supplier_id` y la relación 1:N. Verificación: el documento describe los campos opcionales, la ubicación, la cardinalidad y la ausencia de unicidad.

## 3. Aplicación

- [x] 3.1 Crear los DTOs y el mapeo manual: `SupplierDto` completo, `SupplierOptionDto` con solo `supplierId` y `businessName`, `SupplierSummary` dentro de `ResourceDto` y `LocationAlertDto`. Verificación: prueba de contrato que serializa los campos y comprueba que el selector no expone contacto ni ubicación (`D-41`, `D-55`).
- [x] 3.2 Implementar `CreateSupplierCommand` y `UpdateSupplierCommand` con validadores de razón social obligatoria, email opcional, ubicación opcional y `rowVersion`. Verificación: pruebas unitarias `SC-PROVEEDORES-04`…`SC-PROVEEDORES-12` y `SC-PROVEEDORES-44` en verde.
- [x] 3.3 Implementar `ChangeSupplierStatusCommand` para baja lógica y reactivación. Verificación: pruebas unitarias `SC-PROVEEDORES-40`…`SC-PROVEEDORES-43` en verde.
- [x] 3.4 Implementar `GetSuppliersQuery` completo con filtros por razón social, contacto y ubicación, y paginación con total. Verificación: pruebas unitarias `SC-PROVEEDORES-28`…`SC-PROVEEDORES-31` en verde.
- [x] 3.5 Implementar `GetSupplierOptionsQuery` con solo `supplierId` y `businessName`, filtros por razón social, contacto y ubicación, y exclusión de inactivos. Verificación: pruebas unitarias `SC-PROVEEDORES-32`…`SC-PROVEEDORES-34` en verde.
- [x] 3.6 Implementar `GetSupplierLocationAlertQuery` con la comparación por nivel más específico y los códigos `SUPPLIER_LOCATION_MATCH`, `SUPPLIER_LOCATION_MISMATCH` y `LOCATION_INCOMPLETE`. Verificación: pruebas unitarias `SC-PROVEEDORES-35`…`SC-PROVEEDORES-39` en verde.
- [x] 3.7 Integrar el proveedor en los casos de uso de insumos: `supplierId` opcional en alta y edición, recálculo de estado, resumen en la consulta, conservación en la copia y datos heredados. Verificación: pruebas unitarias `SC-PROVEEDORES-14`, `SC-PROVEEDORES-17`…`SC-PROVEEDORES-19`, `SC-PROVEEDORES-49` y `SC-PROVEEDORES-50` en verde.
- [x] 3.8 Implementar el alcance y la autorización: Administrador lee todo, Director y Presupuestador leen y escriben en su compañía, Auditor lee la propia; el proveedor del insumo debe ser de la compañía del proyecto. Verificación: pruebas unitarias `T-U-16` con `SC-PROVEEDORES-01`…`SC-PROVEEDORES-03`, `SC-PROVEEDORES-22`…`SC-PROVEEDORES-27` en verde.
- [x] 3.9 Cubrir con pruebas unitarias los validadores, los casos de uso, el selector y la alerta (`T-U-17`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.

## 4. API, contrato y permisos

- [x] 4.1 Exponer los siete endpoints de `/api/v1/suppliers` (listado, detalle, selector, alerta de ubicación, alta, edición y estado) con los atributos `SUPPLIERS_READ` y `SUPPLIERS_WRITE`. Verificación: Swagger muestra las rutas y una petición sin permiso responde `403`.
- [x] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` (`SUPPLIER_NOT_FOUND`, `SUPPLIER_INACTIVE`, `SUPPLIER_COMPANY_MISMATCH`, `SUPPLIER_CONCURRENCY_CONFLICT`, `SUPPLIER_WRITE_FORBIDDEN`, `LOCATION_INCOMPLETE`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`) y `409` en concurrencia. Verificación: `T-C-03` sin sobres propios ni `snake_case`.
- [x] 4.3 Ampliar `SecurityBaselineSeeder` con `SUPPLIERS_READ` y `SUPPLIERS_WRITE` en la versión siguiente a la vigente y la asignación por rol del diseño. Verificación: ejecutar el `seed` dos veces deja el mismo inventario y la matriz coincide con `REQ-PROVEEDORES-07`.
- [x] 4.4 Verificar el contrato v1: `camelCase`, paginación `page`/`pageSize`, `ProblemDetails`, selector con solo dos campos y alerta con `canCompare`/`code`. Verificación: `T-C-03` revisado contra Swagger y sin diferencias inesperadas.
- [x] 4.5 Actualizar `.github/docs/02-requerimientos.md` (validación y autorización de proveedores), `06-flujos-casos-de-uso.md` (UC-04) y `05-modelo-datos.md`, y proponer los identificadores nuevos a `PLAN §5.6.1` y `§5.6.5`. Verificación: los documentos describen los campos, la ubicación, el selector y la alerta, y los identificadores aparecen en la matriz funcionalidad → prueba.

## 5. Verificación de la etapa

- [x] 5.1 Ejecutar las pruebas unitarias, de arquitectura y de contrato en CI. Verificación: `dotnet test` en verde, `T-A-02` sin violaciones y cobertura ≥ 80 % en `Domain/Suppliers` y `Domain/Resources`. Hecho 13/11/2026: 272/272 unitarias y de arquitectura en verde (`T-U-15`…`T-U-17`, `T-C-03`, `T-A-02`), 29/29 de integración contra SQL Server 2022 en Docker y cobertura de líneas 94,9 % en `Domain/Suppliers` y 81,8 % en `Domain/Resources`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local: `T-I-36`, `T-I-37`, `T-I-38`, `T-S-15` y `T-C-03`, con dos compañías, dos proyectos, proveedores duplicados y ubicaciones distintas. Verificación: checklist con resultado y evidencia por identificador, incluida la reutilización, la alerta y el aislamiento.
- [x] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los catorce `REQ-PROVEEDORES-*` y anexarla a este archivo (`PLAN §6.1.4`, `§9`). Verificación: ningún requisito queda sin escenario ni sin prueba.
- [x] 5.4 Ejecutar `openspec validate e3a-rf05-proveedores --strict` y archivar solo con la revisión del estudiante. Verificación: validación sin errores, checklist firmado y `openspec archive` con la capacidad `proveedores` sincronizada. Hecho 13/11/2026: `openspec validate --strict` sin errores y `openspec archive` ejecutado por decisión del estudiante; `5.2` queda pendiente y su checklist se firma en la revisión posterior al archivo.

## Matriz de trazabilidad (`PLAN §6.1.4`, `§9`)

| Requisito | Escenarios | RF / BR / R | Decisiones | Prueba |
|---|---|---|---|---|
| `REQ-PROVEEDORES-01` | `SC-PROVEEDORES-01`, `02`, `03` | RF-05; D-02, D-27, D-36 | D1, D4, D7 | `T-U-16`, `T-I-37`, `T-S-15` |
| `REQ-PROVEEDORES-02` | `SC-PROVEEDORES-04`, `05`, `06`, `07`, `08` | RF-05; D-28, D-100 | D1 | `T-U-15`, `T-I-36`, `T-C-03` |
| `REQ-PROVEEDORES-03` | `SC-PROVEEDORES-09`, `10`, `11`, `12` | RF-05; D-96 | D2 | `T-U-15`, `T-I-36` |
| `REQ-PROVEEDORES-04` | `SC-PROVEEDORES-13`, `14`, `15`, `16` | RF-05, RF-06; D-27, D-29 | D2, D5 | `T-U-16`, `T-I-37` |
| `REQ-PROVEEDORES-05` | `SC-PROVEEDORES-17`, `18`, `19` | RF-05; BR-009; D-89 | D5 | `T-U-16`, `T-C-03` |
| `REQ-PROVEEDORES-06` | `SC-PROVEEDORES-20`, `21` | RF-05; Q-09 | D7 | `T-U-15`, `T-I-37` |
| `REQ-PROVEEDORES-07` | `SC-PROVEEDORES-22`, `23`, `24`, `25`, `26`, `27` | RF-05; D-34, D-83, D-84; matriz 1.3.2 | D4 | `T-U-16`, `T-S-15` |
| `REQ-PROVEEDORES-08` | `SC-PROVEEDORES-28`, `29`, `30`, `31` | RF-05; D-96, D-101 | D3 | `T-U-15`, `T-C-03` |
| `REQ-PROVEEDORES-09` | `SC-PROVEEDORES-32`, `33`, `34` | RF-05; D-101 | D3 | `T-U-17`, `T-I-38`, `T-C-03` |
| `REQ-PROVEEDORES-10` | `SC-PROVEEDORES-35`, `36`, `37`, `38`, `39` | RF-05; D-96 | D3, D6 | `T-U-17`, `T-I-38`, `T-C-03` |
| `REQ-PROVEEDORES-11` | `SC-PROVEEDORES-40`, `41`, `42`, `43` | RF-05; D-49 | D7 | `T-U-16`, `T-I-37` |
| `REQ-PROVEEDORES-12` | `SC-PROVEEDORES-44`, `45`, `46` | RF-05; D-08, D-50, D-97 | D1, D3 | `T-U-15`, `T-I-36` |
| `REQ-PROVEEDORES-13` | `SC-PROVEEDORES-47`, `48` | RF-06; D-27, D-29 | D5 | `T-U-16`, `T-I-37` |
| `REQ-PROVEEDORES-14` | `SC-PROVEEDORES-49`, `50` | RF-05; D-28 | D5 | `T-U-17`, `T-C-03` |

Cobertura: 14 requisitos, 50 escenarios y 8 identificadores de prueba, sin requisito ni escenario
huérfano. Las pruebas unitarias y de contrato (`T-U-15`, `T-U-16`, `T-U-17`, `T-C-03`, `T-A-02`) corren en
CI; las de integración y seguridad (`T-I-36`, `T-I-37`, `T-I-38`, `T-S-15`) son manuales con evidencia
(`D-66`).

## Estado de verificación (28/30)

Evidencia obtenida en esta sesión:

| Verificación | Resultado |
|---|---|
| `dotnet build backend/BudgetCivil.Backend.sln` | 0 errores |
| `dotnet test` unitarias (`BudgetCivil.Tests.Unit`) | 264/264 en verde |
| Arquitectura (`BudgetCivil.Tests.Architecture`, `T-A-02`) | 8/8 en verde |
| Contrato (`T-C-03`) | Incluido en las unitarias: `SupplierContractTests` |
| Cobertura (`T-U-15`, `T-U-16`) | `Domain/Suppliers` 94,9 % de líneas (112/118); `Domain/Resources` 81,8 % (162/198) con `coverlet.console` |
| Integración (`T-I-36`, `T-I-37`) | `SupplierPersistenceTests` 8/8 en verde contra SQL Server 2022 en Docker (`localhost,14330`, base `BudgetCivilDb_Integration`) |
| Suite de integración completa | 31/31 en verde en ejecuciones consecutivas (base recién creada y base existente) |
| `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes |
| `dotnet ef database update` sobre base vacía | Aplica las cinco migraciones, incluida `20261113040910_e3a_suppliers` |
| Esquema aplicado (`T-I-36`) | `suppliers` con `business_name` NOT NULL, contacto/ubicación nulos, `row_version` `timestamp`, FKs `NO_ACTION` a `companies`, `countries`, `departments`, `municipalities` y `users`, e índice de compañía/razón social/ubicación sin `UQ_`; `resources.supplier_id` nullable con `FK_dbo_resources_suppliers_supplier_id` e `IX_dbo_resources_supplier_id` |
| Swagger (`4.1`) | Cinco rutas de `/api/v1/suppliers` con las siete operaciones (`/`, `/options`, `/{id}`, `/{id}/location-alert`, `/{id}/status`) y `401` sin token (fail-closed) |
| `seed` v6 ejecutado dos veces (`4.3`) | Mismo inventario (33 permisos, 76 asignaciones activas); matriz `SUPPLIERS_*` coincide con `REQ-PROVEEDORES-07`; la reconciliación por diferencia no revoca nada en una base alineada (`REQ-IDENTIDAD-30`) |
| `openspec validate e3a-rf05-proveedores --strict` | Válido |

Pendiente:

- **5.2**: la persistencia (`T-I-36`, `T-I-37`) ya está cubierta por pruebas ejecutadas; el checklist manual de
  `T-I-38`, `T-S-15` y `T-C-03` contra Swagger sigue pendiente.
- **5.4**: `openspec validate --strict` pasa; el archivado espera la revisión del estudiante.

Conciliación de identidad (13/11/2026): el fallo de `T-I-23` era un desfase entre `REQ-IDENTIDAD-10` —que exigía
`USERS_READ` ausente en Presupuestador— y `D-85`/`D-112`, que le permiten buscar usuarios de su compañía para
asignar Presupuestadores y Auditores. Se actualizó `openspec/specs/identidad/spec.md` (`REQ-IDENTIDAD-10`,
`SC-IDENTIDAD-24` y `SC-IDENTIDAD-24b`) y el aserto de `SeedIdempotencyTests`; `MembershipPersistenceTests` pasó a
verificar la invariante persistida de `SC-IDENTIDAD-31` en lugar del número de llamadas que devuelven `true`, porque
la reactivación de la fila vigente es un desenlace válido de la carrera.

Reconciliación del `seed` (13/11/2026, `REQ-IDENTIDAD-30`): `SecurityBaselineSeeder` sube a `v6` y revoca por
diferencia lo que la base conserva fuera de la matriz —asignaciones rol-permiso (`is_active = 0`, `revoked_at_utc`) y
permisos retirados del catálogo—, en lugar de solo crear y reactivar. Dos pruebas nuevas en `SeedIdempotencyTests`
cubren `SC-IDENTIDAD-77` y `SC-IDENTIDAD-78`, y una ejecución real contra SQL Server en Docker confirmó la
reconciliación (`revoked 1 assignments and 0 permissions outside the matrix`) sobre una base sembrada con `v5`.

Entorno de verificación usado el 13/11/2026: SQL Server 2022 en Docker
(`docker run -d --name budgetcivil-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=… -p 14330:1433 mcr.microsoft.com/mssql/server:2022-latest`)
con `BUDGETCIVIL_TEST_CONNECTION` apuntando a `localhost,14330`; el arnés local de VSTest sigue sin poder abrir el
proceso padre (`Win32Exception 5`), así que las suites se ejecutan con un runner en proceso sobre el mismo ensamblado.

Bases locales creadas para la verificación manual: `BudgetCivilDb_MigrationTest`, `BudgetCivilDb_ApiTest`
y `BudgetCivilDb_IntegrationTest` (se pueden eliminar con `DROP DATABASE`).

## Workflow follow-up

- Archivar el cambio solo después de que `e3a-rf05-insumos-proyecto` esté aplicado y el estudiante apruebe los escenarios.
- Sincronizar `specs/proveedores/spec.md` a `openspec/specs/proveedores/` al archivar.
- Actualizar el índice de `openspec/project.md` con la capacidad `proveedores` (M-04, E-3a).
- Documentar en el backlog el retiro de `distributor`/`contact_number`, la publicación de proveedores en el catálogo (`E-5`) y el cálculo de distancias.
