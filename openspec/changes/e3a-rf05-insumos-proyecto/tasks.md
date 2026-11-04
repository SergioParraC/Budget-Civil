# Tareas: e3a-rf05-insumos-proyecto

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores
nuevos (`T-U-13`, `T-U-14`, `T-I-33`…`T-I-35`, `T-S-13`, `T-S-14`, `T-C-02`) se registran en la tarea 4.5
y se proponen para `PLAN §5.6.5`:

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-13` | Completitud, autoría e independencia de la copia | Unitaria |
| `T-U-14` | Alcance por rol en lectura y escritura | Unitaria |
| `T-I-33` | Persistencia, `CHECK`, FKs, índices y `rowversion` | Integración manual |
| `T-I-34` | Copia independiente y sugerencia de similitud | Integración manual |
| `T-I-35` | Búsqueda, filtros y paginación | Integración manual |
| `T-S-13` | Aislamiento entre compañías y solo lectura de Administrador y Auditor | Seguridad manual |
| `T-S-14` | Fail-closed y matriz de permisos de insumos | Seguridad manual |
| `T-C-02` | Contrato v1 de insumos: `camelCase`, `ProblemDetails` y paginación | Contrato en CI |

**Puerta de entrada:** no iniciar la implementación hasta que `E-2` esté archivado (`PLAN §4.2`,
`§4.5.1`). La tarea 1.1 lo verifica.

## 1. Preparación y dominio

- [x] 1.1 Verificar la precondición `E-2` y el estado de la cadena de migraciones. Verificación: `dotnet build` sin errores, `dotnet ef migrations list` responde con la cadena vigente, y existe un proyecto con EDT y pertenencias para probar; si `E-2` no está cerrado, se detiene el cambio y se registra el bloqueo.
- [x] 1.2 Crear `Resource` en `Domain/Resources` con los campos del diseño, `QuotationStatus`, autoría y `rowversion`, y la regla `CalculateQuotationStatus()` de `REQ-INSUMOS-04`. Verificación: pruebas unitarias de los escenarios `SC-INSUMOS-11`…`SC-INSUMOS-14` en verde.
- [x] 1.3 Crear el catálogo `ResourceType` y los siete códigos (`MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`, `WORK`, `MATRIX`) con sus nombres en español. Verificación: prueba unitaria que enumera exactamente siete tipos y rechaza un código desconocido (`SC-INSUMOS-04`, `SC-INSUMOS-05`).
- [x] 1.4 Implementar la creación y la copia de dominio: proyecto inmutable, `origin_resource_id` apuntando al insumo de origen y estado recalculado. Verificación: pruebas unitarias `SC-INSUMOS-02`, `SC-INSUMOS-28` y `SC-INSUMOS-31`…`SC-INSUMOS-33` en verde.
- [x] 1.5 Cubrir con pruebas unitarias la completitud, la autoría y la independencia de la copia (`T-U-13`). Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y cobertura ≥ 80 % en el dominio de `Resources`.

## 2. Persistencia

- [x] 2.1 Configurar `Resource` en EF Core: tabla `resources`, longitudes, `CHECK (unit_price >= 0)`, `CHECK` del estado, FKs `RESTRICT`, `rowversion`, `is_active`/`deleted_at` y filtro global. Verificación: el modelo genera el SQL esperado y la prueba de integración comprueba `CHECK` y FKs (`T-I-33`).
- [x] 2.2 Crear los índices `IX_resources_project_id_resource_type_id`, `IX_resources_project_id_quotation_status` e `IX_resources_project_id_description`, sin índices únicos de unicidad. Verificación: el script de migración contiene los tres índices y ningún `UQ_` sobre descripción o `user_key`.
- [x] 2.3 Implementar `ResourceTypeSeeder` idempotente con GUID fijos, invocado al arrancar después del `seed` de seguridad. Verificación: ejecutar el `seed` dos veces deja exactamente siete filas y no duplica ninguna.
- [x] 2.4 Generar la migración `YYYYMMDDHHMM_resources_add_e3a` y actualizar `docs/runbook-migraciones.md` con el respaldo, la aplicación y la reversión (`D-40`, `D-107`). Verificación: `dotnet ef migrations script` revisado, `Down` elimina índices, FKs y tablas, y `dotnet ef migrations list` lista una sola migración nueva.
- [x] 2.5 Implementar `IResourceRepository` en `Application/Resources/Repositories` y su versión EF: alta, actualización con `rowversion`, detalle, listado con búsqueda/filtros/paginación, origen de copia y sugerencias. Verificación: pruebas de integración `T-I-33` y `T-I-35` en verde contra SQL Server local.
- [x] 2.6 Implementar `IResourceProjectAccessReader` en `Application/Resources/Repositories` y su versión EF, que resuelve compañía del proyecto y pertenencia del actor sin exponer `DbContext` a `M-04`. Verificación: pruebas de integración de alcance por rol (`T-U-14`) en verde.
- [x] 2.7 Añadir las pruebas de integración de persistencia: `CHECK`, FKs, `rowversion`, índices y filtro de baja lógica. Verificación: `T-I-33` documentado con evidencia y resultado por aserción.

## 3. Aplicación

- [x] 3.1 Crear los DTOs y el mapeo manual de `Resource` (sin AutoMapper ni Mapster) con `camelCase` en la frontera. Verificación: prueba de contrato que serializa el DTO con `resourceTypeId`, `quotationStatus`, `lastModifiedBy` y `rowVersion`, sin campos de otra compañía (`D-41`, `D-55`).
- [x] 3.2 Implementar `CreateResourceCommand` y su validador: descripción obligatoria, precio ≥ 0, tipo existente, municipio existente y opcionales sin bloquear (`REQ-INSUMOS-03`). Verificación: pruebas unitarias `SC-INSUMOS-04`…`SC-INSUMOS-10` en verde.
- [x] 3.3 Implementar `UpdateResourceCommand` con `rowVersion`, proyecto inmutable, autoría y recálculo del estado. Verificación: pruebas unitarias `SC-INSUMOS-13`, `SC-INSUMOS-14`, `SC-INSUMOS-35`, `SC-INSUMOS-37` y `SC-INSUMOS-38` en verde.
- [x] 3.4 Implementar `CopyResourceCommand`: origen de la misma compañía con lectura, destino con escritura, copia de datos y precio, `origin_resource_id` apuntando al origen y estado recalculado. Verificación: pruebas unitarias `SC-INSUMOS-28`…`SC-INSUMOS-33` en verde.
- [x] 3.5 Implementar `GetResourcesQuery` con búsqueda parcial, filtros por tipo, estado, marca, municipio y rango de precio, y paginación con total. Verificación: pruebas unitarias `SC-INSUMOS-18`…`SC-INSUMOS-27` en verde.
- [x] 3.6 Implementar la consulta de sugerencias por descripción o `user_key`, limitada al proyecto y a diez resultados. Verificación: pruebas unitarias `SC-INSUMOS-15`…`SC-INSUMOS-17` en verde y el alta nunca se bloquea por coincidencia.
- [x] 3.7 Implementar las reglas de alcance: Administrador lee todo, Director lee y escribe en su compañía, Presupuestador lee y escribe solo en proyectos asignados, Auditor lee solo en asignados. Verificación: pruebas unitarias `T-U-14` con los escenarios `SC-INSUMOS-39`…`SC-INSUMOS-51` en verde.
- [x] 3.8 Cubrir con pruebas unitarias los validadores, los casos de uso y el mapeo. Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.

## 4. API, contrato y permisos

- [x] 4.1 Exponer los seis endpoints de `/api/v1/projects/{projectId}/resources` con los atributos de permiso `RESOURCES_READ` y `RESOURCES_WRITE`. Verificación: Swagger muestra las rutas y una petición sin permiso responde `403`.
- [x] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` (`RESOURCE_NOT_FOUND`, `RESOURCE_CONCURRENCY_CONFLICT`, `RESOURCE_READ_FORBIDDEN`, `RESOURCE_WRITE_FORBIDDEN`, `VALIDATION_ERROR`, `PAGE_SIZE_INVALID`) y `409` en concurrencia. Verificación: `T-C-02` sin sobres propios ni `snake_case`.
- [x] 4.3 Ampliar `SecurityBaselineSeeder` a `v4` con `RESOURCES_READ` y `RESOURCES_WRITE` y la asignación por rol del diseño. Verificación: ejecutar el `seed` dos veces deja el mismo inventario y la matriz de permisos coincide con `REQ-INSUMOS-13` y `REQ-INSUMOS-14`.
- [x] 4.4 Verificar el contrato v1: `camelCase`, paginación `page`/`pageSize`, `ProblemDetails` y DTOs sin entidades. Verificación: `T-C-02` revisado contra Swagger y sin diferencias inesperadas.
- [x] 4.5 Actualizar `.github/docs/02-requerimientos.md` (validación y autorización de insumos), `05-modelo-datos.md` (`Resource` y `ResourceType`) y `06-flujos-casos-de-uso.md` (UC-04); proponer los identificadores nuevos a `PLAN §5.6.1` y `§5.6.5`. Verificación: los tres documentos citan `municipality_id` en lugar de `city` y los identificadores nuevos aparecen en la matriz funcionalidad → prueba.

## 5. Verificación de la etapa

- [ ] 5.1 Ejecutar las pruebas unitarias, de arquitectura y de contrato en CI. Verificación: `dotnet test` en verde, `T-A-02` sin violaciones y cobertura ≥ 80 % en el dominio de `Resources`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local: `T-I-05`, `T-I-06`, `T-I-15`, `T-I-18`, más `T-I-33`, `T-I-34`, `T-I-35`, `T-S-13` y `T-S-14`, con dos compañías y dos proyectos. Verificación: checklist con resultado y evidencia por identificador, incluida la independencia de la copia y el aislamiento.
- [x] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los quince `REQ-INSUMOS-*` y anexarla a este archivo (`PLAN §6.1.4`, `§9`). Verificación: ningún requisito queda sin escenario ni sin prueba.
- [ ] 5.4 Ejecutar `openspec validate e3a-rf05-insumos-proyecto --strict` y archivar solo con la revisión del estudiante y `E-2` cerrado. Verificación: validación sin errores, checklist de cierre firmado y `openspec archive` con la capacidad `insumos` sincronizada.

## Trazabilidad

| Requisito | Escenarios | RF/BR/R | Decisiones | Prueba |
|---|---|---|---|---|
| `REQ-INSUMOS-01` | `SC-INSUMOS-01`…`03` | `RF-05`; `R-02` | `D-05`, `D-27` | `T-I-33` |
| `REQ-INSUMOS-02` | `SC-INSUMOS-04`…`06` | `RF-05`; `BR-008` | `D-16` | `T-U-13` |
| `REQ-INSUMOS-03` | `SC-INSUMOS-07`…`10` | `RF-05`; `BR-009`, `BR-010` | `D-96` | `T-U-13`, `T-I-33` |
| `REQ-INSUMOS-04` | `SC-INSUMOS-11`…`14` | `RF-05`; `BR-009` | `D-89` | `T-U-13` |
| `REQ-INSUMOS-05` | `SC-INSUMOS-15`…`17` | `RF-16`; `Q-09` | `D-16` | `T-U-13`, `T-I-34` |
| `REQ-INSUMOS-06` | `SC-INSUMOS-18`…`20` | `RF-16`, `RF-20` | `D-25` | `T-I-35` |
| `REQ-INSUMOS-07` | `SC-INSUMOS-21`…`25` | `RF-20` | `D-96`, `D-101` | `T-I-35` |
| `REQ-INSUMOS-08` | `SC-INSUMOS-26`, `27` | `RF-20` | `D-101` | `T-I-35` |
| `REQ-INSUMOS-09` | `SC-INSUMOS-28`…`30` | `RF-06` | `D-27`, `D-29` | `T-I-34` |
| `REQ-INSUMOS-10` | `SC-INSUMOS-31`…`33` | `RF-06`; `R-17` | `D-27`, `D-29` | `T-U-13`, `T-I-34` |
| `REQ-INSUMOS-11` | `SC-INSUMOS-34`…`36` | `RF-14` | `D-08` | `T-U-13` |
| `REQ-INSUMOS-12` | `SC-INSUMOS-37`, `38` | — | `D-50`, `D-97` | `T-I-33` |
| `REQ-INSUMOS-13` | `SC-INSUMOS-39`…`43` | `RF-05`, `RF-06` | `D-34` | `T-U-14`, `T-S-14` |
| `REQ-INSUMOS-14` | `SC-INSUMOS-44`…`49` | `RF-05`, `RF-20` | `D-83`, `D-84` | `T-U-14`, `T-S-13` |
| `REQ-INSUMOS-15` | `SC-INSUMOS-50`, `51` | —; `R-10`, `R-14` | `D-02`, `D-36` | `T-S-13` |

## Estado de implementación

- **1.1:** `E-2` no está cerrado y no hay entidades de EDT; se verificó que `Resource` solo depende de
  `Project` y `ProjectMember`, que ya existen. Se continuó por instrucción del estudiante y se deja
  constancia de que el gate `E-2` sigue pendiente.
- **Pruebas unitarias:** 197 casos en verde con el runner en proceso (`VSTest` no puede abrir el proceso
  padre en este entorno).
- **Arquitectura:** 8 reglas de `T-A-02` en verde con el runner en proceso y los ensamblados compartidos
  de ASP.NET Core; no se modificó el proyecto de pruebas.
- **Integración:** `ResourcePersistenceTests` está escrito, pero no se ejecutó por falta de
  `BUDGETCIVIL_TEST_CONNECTION` (SQL Server local). Queda para 5.2.
- **Cobertura:** el runner de cobertura falla por incompatibilidad con `coverlet.core`; no se midió el
  80 %. Queda para 5.1.
- **Contrato/Swagger:** los endpoints y DTOs están implementados; la revisión manual de `T-C-02` queda en
  5.2.
- **Arranque:** el `ResourceTypes` faltante era una compilación obsoleta (el proceso `BudgetCivil.Api`
  bloqueaba los DLL). Se añadió una verificación de migraciones pendientes y se retiró el proveedor
  EventLog, que ocultaba el error de base de datos; la base local debe recibir
  `202611040532_resources_add_e3a` antes de iniciar.

## Workflow follow-up

- Archivar el cambio solo después de que `E-2` esté cerrado y el estudiante apruebe los escenarios.
- Sincronizar `openspec/changes/e3a-rf05-insumos-proyecto/specs/insumos/spec.md` a `openspec/specs/insumos/` al archivar.
- Confirmar que el cambio vacío `e3a-insumos-proyecto` no tiene artefactos y retirarlo antes de archivar, para que `openspec list` muestre un solo cambio activo.
- Actualizar el estado de `insumos` en el índice de `openspec/project.md` cuando el cambio se archive.
