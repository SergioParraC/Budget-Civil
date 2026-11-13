# Tareas: i-3b-apus-y-matrices

Convención: cada tarea declara su verificación. Las pruebas de integración y seguridad son manuales con
evidencia (`D-66`); las unitarias, de arquitectura y de contrato corren en CI. Los identificadores nuevos
(`T-U-18`…`T-U-20`, `T-I-39`…`T-I-41`, `T-S-16`, `T-S-17`, `T-C-04`, `T-A-03`) se registran en la tarea
5.3 y se proponen para `PLAN §5.6.7` (`INC-12`). Se conservan los ya asignados por `PLAN §5.6.1`:
`T-U-03`, `T-U-04`, `T-U-11`, `T-I-07`, `T-I-08`, `T-I-09`, `T-I-20`, `T-S-06` y `T-E-01`.

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-03` | Reconciliación de insumos y decisiones del usuario | Unitaria |
| `T-U-04` | Composición de Matriz y no anidamiento | Unitaria |
| `T-U-11` | Conteo de uso y expansión de un nivel | Unitaria |
| `T-U-18` | Completitud, estado y autoría del APU | Unitaria |
| `T-U-19` | Vínculo 1:1 y reglas de composición | Unitaria |
| `T-U-20` | Alcance por rol en lectura y escritura de APUs y Matrices | Unitaria |
| `T-I-07` | Alta, copia y detalle del APU con su ítem | Integración manual |
| `T-I-08` | Reconciliación y copia transaccional | Integración manual |
| `T-I-09` | Persistencia y copia de Matrices | Integración manual |
| `T-I-20` | Conteo de uso contra datos reales | Integración manual |
| `T-I-39` | Persistencia de `apus`, `apu_resources` y `matrix_components` | Integración manual |
| `T-I-40` | Copia de APU y de Matriz con independencia en ambos sentidos | Integración manual |
| `T-I-41` | Conteo de uso con Matriz y con insumo sin uso | Integración manual |
| `T-S-06` | Prohibición de copia y consulta de otra compañía | Seguridad manual |
| `T-S-16` | Aislamiento y solo lectura de Administrador y Auditor | Seguridad manual |
| `T-S-17` | Fail-closed y matriz de permisos de APUs | Seguridad manual |
| `T-C-04` | Contrato v1 de APUs, Matrices y conteo de uso | Contrato en CI |
| `T-A-03` | Reglas de capas y aislamiento de módulos de `M-05` | Arquitectura en CI |
| `T-E-01` | Flujo manual: ítem → APU → Matriz → copia → conteo | Extremo a extremo manual |

**Puerta de entrada:** no iniciar la implementación hasta que `E-2` esté archivado y `INC-02` esté
confirmado (`PLAN §4.2`, `§4.5.1`). La tarea 1.1 lo verifica.

## 1. Preparación y dominio

- [ ] 1.1 Verificar la precondición `E-2` y la cadena de migraciones. Verificación: `dotnet build` sin errores, `dotnet ef migrations list` con la cadena vigente, existencia de `WbsItem`/`WbsCode` con cantidad de obra y decisión registrada sobre `INC-02`; si `E-2` no está cerrado, se detiene el cambio y se documenta el bloqueo.
- [ ] 1.2 Crear `Apu` y `ApuResource` en `Domain/Apus` con los campos del diseño, `Status`, autoría, `RowVersion` y `CalculateStatus()`. Verificación: pruebas unitarias `SC-APUS-06`…`SC-APUS-14`, `SC-APUS-63` y `SC-APUS-64` en verde.
- [ ] 1.3 Crear `MatrixComponent` con las validaciones de consumo y desperdicio y las reglas de tipo `MATRIX`, no anidamiento y autorreferencia. Verificación: pruebas unitarias `SC-APUS-26`…`SC-APUS-34` en verde (`T-U-04`).
- [ ] 1.4 Implementar la creación y la edición del APU con vínculo obligatorio al ítem y el recálculo de estado; la composición solo admite insumos activos del mismo proyecto. Verificación: pruebas unitarias `SC-APUS-01`…`SC-APUS-05`, `SC-APUS-15`…`SC-APUS-20` en verde (`T-U-19`).
- [ ] 1.5 Implementar la lógica de dominio de la copia: origen independiente, sin aprobación, actor como último modificador y estado recalculado. Verificación: pruebas unitarias `SC-APUS-42`, `SC-APUS-46` y `SC-APUS-53`…`SC-APUS-55` en verde (`T-U-03`).
- [ ] 1.6 Cubrir la expansión de un nivel del conteo de uso en el dominio. Verificación: pruebas unitarias `SC-APUS-56`…`SC-APUS-62` en verde (`T-U-11`) y cobertura ≥ 80 % en `Domain/Apus`.

## 2. Persistencia

- [ ] 2.1 Configurar `Apu` en EF Core: tabla `apus`, longitudes, `CK_apus_yield`, `CK_apus_status`, FK de proyecto y usuario `RESTRICT`, `rowversion`, `is_active`/`deleted_at` y filtro global. Verificación: el script de migración contiene la tabla, los dos `CHECK` y el filtro; `T-I-39` lo comprueba contra SQL Server.
- [ ] 2.2 Configurar `ApuResource` y `MatrixComponent`: tablas `apu_resources` y `matrix_components`, `CHECK` de consumo y desperdicio, FK de composición en `CASCADE` y de insumo en `RESTRICT`, e índices del diseño. Verificación: el script contiene los cuatro `CHECK`, las FK con el borrado esperado y los cinco índices; sin índices únicos de líneas (`T-I-39`).
- [ ] 2.3 Aplicar el ajuste de `wbs_items.apu_id` (`nullable` + `UQ_wbs_items_apu_id` filtrado) según `INC-02`, en la migración de `E-2` o en la de este cambio. Verificación: el `Down` revierte el índice y la nulabilidad, y una prueba de integración comprueba que dos ítems no pueden apuntar al mismo APU (`T-I-39`).
- [ ] 2.4 Implementar `IWbsItemReader` en `Infrastructure`: ítem por proyecto, ítem por APU y cantidades de obra del proyecto. Verificación: la consulta respeta el proyecto y devuelve `null` para ítems de otra compañía (`T-I-07`, `T-S-06`).
- [ ] 2.5 Implementar `IApuResourceCopyWriter` en `Infrastructure` para crear insumos copiados y Matrices copiadas dentro del mismo `DbContext`. Verificación: una transacción fallida no deja insumos ni APU parciales (`T-I-08`).
- [ ] 2.6 Generar la migración `YYYYMMDDHHMM_apus_add_e3b` y actualizar `docs/runbook-migraciones.md` con respaldo, aplicación y reversión (`D-40`, `D-107`). Verificación: `dotnet ef migrations script` revisado, `Down` elimina índices, FKs y tablas, y `dotnet ef migrations list` lista la migración una sola vez.
- [ ] 2.7 Ampliar `SecurityBaselineSeeder` a `v7` con `APUS_READ` y `APUS_WRITE` y la asignación por rol del diseño. Verificación: ejecutar el `seed` dos veces deja 35 permisos, no duplica asignaciones y no reactiva permisos retirados (`T-S-17`).

## 3. Aplicación

- [ ] 3.1 Crear los DTOs y el mapeo manual de APU, líneas, Matriz, componentes y conteo de uso (sin AutoMapper ni Mapster) con `camelCase` en la frontera. Verificación: prueba de contrato que serializa `apuId`, `wbsItemId`, `wbsItemCode`, `status`, `yield`, `lines` y `rowVersion`, sin campos de otra compañía (`D-41`, `D-55`, `T-C-04`).
- [ ] 3.2 Implementar el alta y la edición del APU con validador: `wbsItemId` obligatorio, descripción y unidad no vacías, `yield > 0` y `rowVersion` en la edición. Verificación: pruebas unitarias `SC-APUS-06`…`SC-APUS-10` y `SC-APUS-67`…`SC-APUS-69` en verde.
- [ ] 3.3 Implementar los casos de uso de composición del APU con `rowVersion` del APU y recálculo del estado. Verificación: pruebas unitarias `SC-APUS-15`…`SC-APUS-20` y `SC-APUS-67`…`SC-APUS-69` en verde.
- [ ] 3.4 Implementar los casos de uso de composición de la Matriz con `rowVersion` del insumo y actualización de su autoría y estado. Verificación: pruebas unitarias `SC-APUS-26`…`SC-APUS-34`, `SC-APUS-66` y `SC-APUS-70` en verde (`T-U-04`).
- [ ] 3.5 Implementar la vista previa de reconciliación de APU y de Matriz con la clasificación `EXACT`/`SIMILAR`/`NONE` y hasta diez candidatos. Verificación: pruebas unitarias `SC-APUS-36`, `SC-APUS-48` y `SC-APUS-52` en verde (`T-U-03`).
- [ ] 3.6 Implementar la copia de APU: ítem destino libre, resoluciones obligatorias, `USE_EXISTING`/`KEEP_ORIGIN`, copia transaccional y origen de otra compañía tratado como inexistente. Verificación: pruebas unitarias `SC-APUS-42`…`SC-APUS-52` en verde (`T-U-03`, `T-I-08`).
- [ ] 3.7 Implementar la copia de Matriz con reconciliación de componentes y la creación de la Matriz destino. Verificación: pruebas unitarias `SC-APUS-35`…`SC-APUS-41` en verde (`T-U-03`, `T-I-09`).
- [ ] 3.8 Implementar el listado y el detalle del APU con búsqueda por descripción o código del ítem, filtro por estado y paginación; el detalle incluye composición, código y cantidad de obra. Verificación: pruebas unitarias `SC-APUS-21`…`SC-APUS-25` y `SC-APUS-05` en verde (`T-I-07`).
- [ ] 3.9 Implementar la consulta de conteo de uso en `Application/Calculation` con la proyección de consumo directo y la expansión de un nivel. Verificación: pruebas unitarias `SC-APUS-56`…`SC-APUS-62` en verde y sin duplicar la fórmula fuera de la consulta (`T-U-11`, `T-I-20`).
- [ ] 3.10 Implementar las reglas de alcance: Administrador lee todo, Director lee y escribe en su compañía, Presupuestador lee y escribe solo en proyectos asignados, Auditor lee solo en asignados. Verificación: pruebas unitarias `SC-APUS-71`…`SC-APUS-83` en verde (`T-U-20`, `T-S-16`).
- [ ] 3.11 Cubrir con pruebas unitarias los validadores, los casos de uso, el mapeo y los errores declarados. Verificación: `dotnet test backend/tests/BudgetCivil.Tests.Unit` en verde y sin pruebas omitidas.

## 4. API, contrato y permisos

- [ ] 4.1 Exponer los dieciséis endpoints del diseño bajo `/api/v1/projects/{projectId}` con los atributos `APUS_READ`, `APUS_WRITE` y `RESOURCES_READ`. Verificación: Swagger muestra las rutas y una petición sin permiso responde `403` (`T-S-17`, `T-C-04`).
- [ ] 4.2 Traducir los errores a `ProblemDetails` con `extensions.code` y `409` en concurrencia, con los códigos del diseño. Verificación: `T-C-04` sin sobres propios ni `snake_case`, y cada código observado en una prueba.
- [ ] 4.3 Rechazar el tipo `MATRIX` en `POST /projects/{projectId}/resources/copy` con `RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`. Verificación: prueba del caso de uso de copia de insumo y escenario `SC-INSUMOS-56` en verde (`REQ-INSUMOS-09`).
- [ ] 4.4 Actualizar la documentación del contrato v1 y el cliente generado de forma diferida (`D-56`, `D-109`). Verificación: `T-C-04` en CI y `IMPACTO_FRONTEND.md` sin cambios adicionales a los ya registrados.


## 5. Verificación de la etapa

- [ ] 5.1 Ejecutar `dotnet build` y `dotnet test` de las suites unitaria y de arquitectura. Verificación: compilación sin errores ni advertencias, `T-U-03`, `T-U-04`, `T-U-11`, `T-U-18`…`T-U-20`, `T-C-04` y `T-A-03` en verde, y cobertura ≥ 80 % en `Domain/Apus`.
- [ ] 5.2 Ejecutar la verificación manual contra SQL Server local con dos compañías y dos proyectos: `T-I-07`, `T-I-08`, `T-I-09`, `T-I-20`, `T-I-39`, `T-I-40`, `T-I-41`, `T-S-06`, `T-S-16` y `T-S-17`. Verificación: checklist con resultado y evidencia por identificador, incluida la independencia de la copia en ambos sentidos y el conteo con Matriz.
- [ ] 5.3 Completar la matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` para los dieciocho `REQ-APUS-*` y los dos `REQ-INSUMOS-*` modificados, y proponer los identificadores nuevos para `PLAN §5.6.7`. Verificación: ningún requisito queda sin escenario ni sin prueba.
- [ ] 5.4 Ejecutar `openspec validate i-3b-apus-y-matrices --strict` y archivar solo con la aprobación del estudiante y `E-2` cerrado. Verificación: validación sin errores, checklist de cierre firmado y `openspec archive` con las capacidades `apus` e `insumos` sincronizadas.

## Trazabilidad

| Requisito | Escenarios | RF/BR/R | Decisiones | Prueba |
|---|---|---|---|---|
| `REQ-APUS-01` | `SC-APUS-01`…`05` | `RF-07`; `BR-016`; `R-13` | `D-16`, `D-31` | `T-U-19`, `T-I-07`, `T-I-39` |
| `REQ-APUS-02` | `SC-APUS-06`…`10` | `RF-07`; `BR-016` | `D-31` | `T-U-18`, `T-I-39` |
| `REQ-APUS-03` | `SC-APUS-11`…`14` | `RF-07` | `05 §3` | `T-U-18`, `T-I-39` |
| `REQ-APUS-04` | `SC-APUS-15`…`20` | `RF-07`; `R-01` | `D-05`, `D-49` | `T-U-19`, `T-I-39` |
| `REQ-APUS-05` | `SC-APUS-21`…`25` | `RF-07`; `R-18` | `D-30`, `D-31`, `D-101` | `T-I-07`, `T-C-04` |
| `REQ-APUS-06` | `SC-APUS-26`…`30` | `RF-09`; `BR-008` | `D-48` | `T-U-04`, `T-I-09` |
| `REQ-APUS-07` | `SC-APUS-31`…`34` | `RF-09`; `BR-008`; `R-11` | `D-16`, `D-48` | `T-U-04`, `T-I-09` |
| `REQ-APUS-08` | `SC-APUS-35`…`41` | `RF-09`; `BR-021` | `D-16`, `D-25`, `D-29` | `T-U-03`, `T-I-09`, `T-I-40` |
| `REQ-APUS-09` | `SC-APUS-42`…`46` | `RF-07`; `BR-018` | `D-13`, `D-16`, `D-24` | `T-I-07`, `T-I-08` |
| `REQ-APUS-10` | `SC-APUS-47`…`52` | `RF-08`; `BR-021` | `D-25` | `T-U-03`, `T-I-08` |
| `REQ-APUS-11` | `SC-APUS-53`…`55` | `RF-07`, `RF-08`; `R-17` | `D-29` | `T-I-40` |
| `REQ-APUS-12` | `SC-APUS-56`…`59` | `RF-22`; `R-21` | `D-33` | `T-U-11`, `T-I-20`, `T-I-41` |
| `REQ-APUS-13` | `SC-APUS-60`…`62` | `RF-22`; `R-21`; `BR-008` | `D-33` | `T-U-11`, `T-I-41` |
| `REQ-APUS-14` | `SC-APUS-63`…`66` | `RF-14`; `R-07` | `D-08` | `T-U-18`, `T-I-39` |
| `REQ-APUS-15` | `SC-APUS-67`…`70` | —; `R-14` | `D-50`, `D-97` | `T-I-39` |
| `REQ-APUS-16` | `SC-APUS-71`…`75` | `RF-07`; `R-08` | `D-34`, `D-82`, `D-83`, `D-84` | `T-U-20`, `T-S-16`, `T-S-17` |
| `REQ-APUS-17` | `SC-APUS-76`…`80` | `RF-07`, `RF-22` | `D-34`, `D-83`, `D-84` | `T-U-20`, `T-S-16` |
| `REQ-APUS-18` | `SC-APUS-81`…`83` | —; `R-10`, `R-14` | `D-02`, `D-36` | `T-S-06`, `T-S-16` |
| `REQ-INSUMOS-04` | `SC-INSUMOS-11`…`14`, `52`…`55` | `RF-05`, `RF-09`; `BR-009` | `D-89`, `D-48` | `T-U-18`, `T-I-33` |
| `REQ-INSUMOS-09` | `SC-INSUMOS-28`…`30`, `56` | `RF-06`, `RF-09`; `BR-021` | `D-27`, `D-29` | `T-I-34`, `T-I-40` |

## Dependencias y bloqueos

1. **`E-2` (proyecto y EDT) es bloqueante**: no existen `WbsItem`, `WbsCode` ni su migración. Sin el ítem
   no se puede crear ni copiar un APU, ni calcular el conteo de uso (`INC-01`).
2. **`INC-02` bloquea la migración**: la nulabilidad y el índice único filtrado de `wbs_items.apu_id`
   deben confirmarse antes de generar `apus_add_e3b`.
3. **`X-15` no bloquea `E-3b`** porque el cálculo (`RF-10`) está fuera de alcance; sí condiciona el
   significado de `yield` y la unidad de esfuerzo (`INC-11`).
4. **`E-4b` consume los puntos de invalidación**: `ApuApproval` y `D-53` se implementan allí sobre los
   endpoints de composición definidos aquí.
5. **`E-5`** añadirá el origen público a la copia; no cambia el contrato de reconciliación.

## Estado de implementación

- **No iniciada.** El cambio solo contiene la propuesta, el diseño, los deltas de especificación y las
  tareas. `E-2` no está implementado en el código y `INC-02` está pendiente de confirmación, por lo que la
  puerta de la tarea `1.1` permanece cerrada.
- **Verificado en el repositorio:** `openspec validate i-3b-apus-y-matrices --strict` pasa; no existen
  entidades `Apu`, `ApuResource`, `MatrixComponent` ni tablas `wbs*`; el `SecurityBaselineSeeder` está en
  `v6` con 33 permisos y `RESOURCES_READ`/`RESOURCES_WRITE` ya asignados.

## Workflow follow-up

- Confirmar `INC-02`, `INC-04` y `INC-06` con el estudiante antes de iniciar la implementación.
- Sincronizar `specs/apus/spec.md` a `openspec/specs/apus/` y los cambios de `specs/insumos/spec.md` a
  `openspec/specs/insumos/spec.md` al archivar.
- Actualizar el índice de `openspec/project.md` (fila `APUs` → Vigente) y la tabla de rutas de
  `PLAN §3.4.3` cuando el cambio se archive.
- Registrar `T-U-18`…`T-U-20`, `T-I-39`…`T-I-41`, `T-S-16`, `T-S-17`, `T-C-04` y `T-A-03` en una sección
  `PLAN §5.6.7` de verificación de `E-3b`.

