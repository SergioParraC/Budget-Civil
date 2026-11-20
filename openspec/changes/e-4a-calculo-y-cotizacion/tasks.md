# Tareas: e-4a-calculo-y-cotizacion

Convención: cada tarea declara su verificación. Las pruebas unitarias, de contrato y de arquitectura corren
en CI; la integración y la seguridad se verifican manualmente con evidencia (`D-66`). `T-U-05`, `T-U-09` y
`T-I-16` ya están asignados por `PLAN §5.6.1` a `RF-10` y `RF-17`, y `T-U-06` y `T-I-10` a `RF-11`; los
identificadores nuevos (`T-U-25`…`T-U-29`, `T-I-46`, `T-I-47`, `T-S-20`, `T-S-21`, `T-C-06`, `T-C-07`,
`T-A-05`) se registran en la tarea 6.3 y se proponen para `PLAN §5.6.8` (`INC-12`).

| Identificador | Qué comprueba | Nivel |
|---|---|---|
| `T-U-05` | Costo directo, AIU aplicado una vez, redondeo y nulos del presupuesto | Unitaria |
| `T-U-06` | Completitud, resumen por insumo, idempotencia y autoría de la cotización | Unitaria |
| `T-U-09` | Alerta de tope con y sin tope, total nulo y total igual al tope | Unitaria |
| `T-U-25` | Agregación B de capítulos, ítems raíz, capítulo vacío y propagación de nulos | Unitaria |
| `T-U-26` | Valores y contrato del árbol: `unitCost`, `directCost` y `apu.unitPrice` | Unitaria y contrato |
| `T-U-27` | Alcance por rol del presupuesto | Unitaria |
| `T-U-28` | Selección, filtro y totalidad con filtros combinados y conteo de pendientes | Unitaria |
| `T-U-29` | Precondición `REQ-PROYECTOS-05` en la transición a `COTIZADO` | Unitaria |
| `T-I-10` | Cotización masiva contra datos reales, incluidos los omitidos | Integración manual |
| `T-I-16` | Alerta de tope contra datos reales | Integración manual |
| `T-I-46` | Árbol y presupuesto contra SQL Server, incluido el movimiento | Integración manual |
| `T-I-47` | Cotización de 300 insumos y transición a `COTIZADO` con pendientes | Integración manual |
| `T-S-20` | Aislamiento entre compañías y solo lectura del presupuesto | Seguridad manual |
| `T-S-21` | Permisos y alcance de la cotización masiva | Seguridad manual |
| `T-C-06` | Contrato v1 del árbol y del presupuesto | Contrato en CI |
| `T-C-07` | Contrato del cambio masivo de cotización | Contrato en CI |
| `T-A-05` | Fronteras de módulos: `Wbs` no depende de `Apus` ni de su infraestructura | Arquitectura en CI |

**Puerta de entrada:** `E-3b` está archivado (`2026-11-17-i-3b-apus-y-matrices`), `openspec/specs/apus/` está
vigente y `ApuCostCalculator` ya cubre `D-06`, `D-07` y `D-121`. No hay migración pendiente. En `M-04`,
`QuotationStatus.Cotizado` existe en el contrato pero ningún comando lo produce y `REQ-PROYECTOS-05` está
especificado sin implementar; `RESOURCES_WRITE`, `ResourceAccessService` y `Resource.IsComplete` ya existen.

## 1. Cálculo compartido y contrato del presupuesto

- [x] 1.1 Crear `IApuCostReader` y los modelos del snapshot (ítems con `quantityWork` y `apuId`, líneas de
  APU con Matrices expandidas, `CostSettings`, `budgetLimit` y divisa) y verificar con `dotnet build` y una
  prueba de mapeo de dos ítems y un APU.
- [x] 1.2 Implementar `ApuCostService.GetProjectSnapshotAsync` reutilizando `ApuCostCalculator.CalculateApu`
  y verificar con `T-U-05` que el costo directo por APU coincide con el detalle del APU.
- [x] 1.3 Implementar `ProjectBudgetCalculator` con costo directo, `CalculateIndirectCosts`, total,
  `isComplete`, contadores y `isOverBudget`, y verificar con `T-U-05` y `T-U-09`.
- [x] 1.4 Crear `CalculationAccessService` e `ICalculationProjectAccessReader` con las reglas de lectura del
  árbol y verificar con `T-U-27` (Administrador, Director, Presupuestador y Auditor).
- [x] 1.5 Crear `GetProjectBudgetQueryHandler` y `ProjectBudgetDtos` con `camelCase`, y verificar con
  `T-U-05`, `T-U-09` y `T-U-27`.
- [x] 1.6 Crear `BudgetController` con `GET /api/v1/projects/{projectId}/budget`, `WBS_READ` y
  `ProblemDetails`; verificar con `T-C-06` y la operación visible en `/swagger`.
- [x] 1.7 Extender `LayerDependencyTests` con `T-A-05` para comprobar que `Application/Calculation` no dependa
  de `Application/Apus` ni de `Application/Wbs` y que `Application/Wbs` no dependa de `Application/Apus`;
  verificar con `dotnet test` de arquitectura.

## 2. Persistencia de lectura y registro

- [ ] 2.1 Implementar `ProjectCostReader.GetProjectAsync` con consultas set-based por `project_id` para
  ítems, APUs, líneas, componentes de Matriz, unidad de Matriz, configuración, divisa y tope; verificar con
  `T-I-46` que no hay N+1 y que los totales coinciden con los del detalle del APU.
- [ ] 2.2 Implementar `CalculationProjectAccessReader` sobre `projects` y `project_members`; verificar con
  `T-S-20` que otra compañía responde `403` y un proyecto inexistente `404`.
- [x] 2.3 Registrar los lectores, el servicio y el handler en `Infrastructure/DependencyInjection.cs` y
  `Application/DependencyInjection.cs`; verificar con `dotnet build` y un arranque local de la API.

## 3. Árbol EDT con costos

- [x] 3.1 Extender `WbsNodeDto` con `unitCost` y `directCost` por ítem y `directCost` por capítulo, y
  actualizar `WbsMapper.ToItemNode` para recibir el cálculo y dejar de fijar `apu.unitPrice = 0`; verificar
  con `T-U-26` y `T-C-06`.
- [x] 3.2 Inyectar `ApuCostService` en `GetWbsTreeQueryHandler` y resolver `apu.unitPrice`, `unitCost` y
  `directCost` por ítem; verificar con `T-U-26` que un ítem con APU completo devuelve los tres valores y que
  un ítem sin APU devuelve `apu = null` y costos nulos.
- [x] 3.3 Implementar la agregación B del capítulo: acumular costos de ítems del subárbol una sola vez, no
  sumar el `directCost` publicado de un capítulo hijo como ítem adicional, devolver `0` en un capítulo vacío
  y `null` si algún ítem es nulo; verificar con `T-U-25`.
- [x] 3.4 Agregar pruebas unitarias de ítem raíz, capítulo con subcapítulos, capítulo vacío, capítulo con
  ítem incompleto y movimiento seguido de una nueva consulta del árbol; verificar con `T-U-25` y `T-I-46`.
- [x] 3.5 Documentar en `IMPACTO_FRONTEND.md`, `docs/02` y `docs/05` los campos nuevos, la semántica B, los
  nulos y la necesidad de refrescar `GET /wbs` después de un movimiento; verificar que los ejemplos
  documentados coinciden con los JSON de `design.md` y con `T-C-06`.

## 4. Endpoint de presupuesto y alerta

- [ ] 4.1 Conectar `GetProjectBudgetQueryHandler` al `ProjectCostReader` y al `ApuCostService` y verificar
  con `T-I-46` que el `directCost` del presupuesto coincide con la suma de los costos directos del árbol,
  incluidos los ítems raíz.
- [x] 4.2 Verificar la alerta de tope con `T-U-09` y `T-I-16`: total mayor, igual y menor que el tope; sin
  tope; total nulo por presupuesto incompleto; `budgetLimit` y `isOverBudget` en la respuesta.
- [ ] 4.3 Verificar con `T-S-20` que el Administrador consulta cualquier proyecto, el Director su compañía,
  el Presupuestador y el Auditor solo los asignados, y que otra compañía responde `403
  WBS_READ_FORBIDDEN`.
- [x] 4.4 Actualizar `docs/02` (ruta, campos y permisos), `docs/05` (fórmulas y orden de redondeo) y
  `PLAN_INGENIERIA.md` (`RF-10`, `RF-17`, `D-06`, `D-07`, `D-121`); verificar que la ruta documentada
  responde y que `openspec validate --all --strict` sigue en verde.

## 5. Cotización masiva y precondición de cierre

- [x] 5.1 Crear `ChangeResourceQuotationStatusCommand` con `scope` discriminado (`SELECTION`, `FILTER`,
  `ALL`), su validador y la respuesta con `requested`, `updated`, `alreadyQuoted`, `skipped`,
  `pendingResources` y `allQuoted`; verificar con `T-U-06` y `T-U-28`.
- [ ] 5.2 Implementar `ResourceQuotationRepository` con la resolución set-based de candidatos por filtro, la
  relectura de `Resource.IsComplete` dentro de la transacción y el `UPDATE` que solo toca las filas que
  cambian de estado; verificar con `T-I-10` que el conteo coincide con el listado filtrado.
- [x] 5.3 Omitir los insumos en `FALTA_INFORMACION`, incluidas las Matrices sin componentes, sin escribir
  precio ni estado y devolviendo `RESOURCE_QUOTATION_INCOMPLETE` en `skipped`; verificar con `T-U-06`,
  `T-U-28` y `T-I-10`.
- [x] 5.4 Escribir `last_modified_by`/`last_modified_at` solo en las filas actualizadas y un evento de
  auditoría con `RESOURCE_QUOTATION_CHANGED` y los conteos; verificar con `T-U-06` y `T-I-10`.
- [x] 5.5 Exponer `PATCH /api/v1/projects/{projectId}/resources/quotation-status` en `ResourcesController`
  con `AuthorizationPolicies.ResourcesWrite`, `ProblemDetails` y validación de `scope`; verificar con
  `T-C-07`, `T-S-21` y la operación visible en `/swagger`.
- [x] 5.6 Validar `REQ-PROYECTOS-05` en `ChangeProjectStatusCommandHandler`: contar los insumos distintos de
  `COTIZADO` y responder `409 PROJECT_QUOTATION_INCOMPLETE` con `pendingResources` cuando el destino sea
  `COTIZADO`; verificar con `T-U-29` y `T-I-47`.
- [x] 5.7 Documentar en `IMPACTO_FRONTEND.md` los tres alcances, el refresco del listado y del presupuesto
  tras cotizar, el `pendingResources` como habilitador de la transición y el `409` de la transición.
- [ ] 5.8 Verificar con `T-S-21` que el Administrador y el Auditor reciben `403`, que el Presupuestador sin
  membresía recibe `403 RESOURCES_WRITE_FORBIDDEN` y que otra compañía no revela insumos.

## 6. Integración, documentación del plan y cierre

- [x] 6.1 Ejecutar `dotnet build` y `dotnet test` de las suites unitaria, de contrato y de arquitectura;
  verificar que `T-U-05`, `T-U-06`, `T-U-09`, `T-U-25`…`T-U-29`, `T-C-06`, `T-C-07` y `T-A-05` pasan.
- [ ] 6.2 Ejecutar la verificación manual contra SQL Server local (`T-I-46` y `T-I-47`): crear proyecto con
  AIU, capítulos anidados, ítems con APU y precios, consultar `GET /wbs` y `GET /budget`, mover un capítulo,
  volver a consultar y comprobar que ningún ítem se cuenta dos veces y que el total no cambió por el
  movimiento; cotizar trescientos insumos con al menos seis incompletos, comprobar el resumen y que la
  transición responde `409` hasta cotizar los pendientes. Registrar la evidencia.
- [x] 6.3 Registrar `T-U-25`…`T-U-29`, `T-I-46`, `T-I-47`, `T-S-20`, `T-S-21`, `T-C-06` y `T-C-07` en
  `PLAN §5.6.8`, añadir `PATCH /projects/{id}/resources/quotation-status` a `PLAN §3.4.3` y dejar preparada
  la actualización de las filas `Cálculo` y `Cotización` de `openspec/project.md §8` para el archive, con la
  fila `RF-11` movida de `aprobaciones`/`E-4b` a `cotizacion`/`E-4a`; verificar con `openspec list --specs` y
  `openspec validate --all --strict`.
- [x] 6.4 Revisar el diff completo, ejecutar `openspec validate --all --strict` y confirmar que no se creó
  una spec durable nueva ni un change adicional; los únicos cambios de especificación son los deltas de
  `e-4a-calculo-y-cotizacion` (`cotizacion` nueva y `edt` modificada).

## Workflow follow-up

- Archivar el change con la aprobación final del estudiante; al archivar se incorporan `calculo` y
  `cotizacion` a `openspec/specs/` y se fusiona el delta de `edt`.
- Revisar `PLAN §5.6.8` y la tabla de rutas de `PLAN §3.4.3` con las operaciones `GET /projects/{id}/budget`
  y `PATCH /projects/{id}/resources/quotation-status`.
- Regenerar el cliente Angular cuando el contrato v8 esté estable (`D-109`).
- `RF-12`, `RF-13` y `RF-21` (aprobación por APU y cierre) siguen en `E-4b`; la aprobación no es masiva.

