# Requerimientos

> Documento actualizado el 27/10/2026. Usa los identificadores `RF`, `NFR` y `BR` del plan. La verificación detallada está en el capítulo 5 de `PLAN_INGENIERIA.md`.

## 1. Requerimientos funcionales

| ID | Funcionalidad | Prioridad | MVP | Criterio de aceptación |
|---|---|---|---|---|
| RF-01 | Usuarios, roles y asignación a compañías y proyectos | Alta | Sí | El Administrador crea y cambia roles globalmente; el Director asigna usuarios de su compañía a proyectos |
| RF-02 | Compañías: creación y datos de registro | Alta | Sí | Se crea la compañía con razón social y NIT únicos; el Administrador solo edita datos de registro |
| RF-03 | Configuración del proyecto | Alta | Sí | Se guardan divisa ISO, AIU, tope, modo de firma, requisito de auditor y visibilidad |
| RF-04 | EDT recursiva con códigos | Alta | Sí | Capítulos e ítems sin límite de profundidad; códigos únicos por proyecto; búsqueda parcial por código o nombre |
| RF-05 | Insumos del proyecto | Alta | Sí | Siete tipos, estados de cotización y sugerencia de unicidad; los cuatro campos obligatorios definen `FALTA_INFORMACION` |
| RF-06 | Copia de insumos entre proyectos | Media | Sí | Se copia un insumo con su precio a otro proyecto de la compañía; queda independiente |
| RF-07 | APUs por proyecto | Alta | Sí | APU 1:1 con el ítem; creación y copia desde proyectos de la compañía o del catálogo público |
| RF-08 | Reconciliación de insumos al copiar | Alta | Sí | El sistema sugiere insumos similares; el usuario decide usar el existente o agregar el de origen |
| RF-09 | Matrices | Alta | Sí | Insumo compuesto reutilizable en varios APUs; no pertenece a la EDT y no se anida |
| RF-10 | Motor de cálculo | Alta | Sí | Costo directo, AIU y redondeo con una sola implementación; la fórmula exacta cierra `X-15` |
| RF-11 | Cotización y cambio masivo | Alta | Sí | Director y Presupuestador marcan insumos como `COTIZADO` por selección, filtro o totalidad |
| RF-12 | Aprobación por APU según modo | Alta | Sí | Modos sin firma, firma de presupuestador o firma de presupuestador con auditor |
| RF-13 | Registro único de aprobación | Alta | Sí | Un registro por APU con último aprobador, rol, fecha y modo |
| RF-14 | Autoría de última modificación | Media | Sí | APU e insumo guardan usuario y fecha de la última modificación |
| RF-15 | Catálogo público | Media | Sí | Consulta anónima de APUs e insumos de proyectos públicos; copia independiente |
| RF-16 | Búsqueda y similitud de insumos | Media | Sí | Búsqueda parcial y sugerencia de similitud; la decisión es del usuario |
| RF-17 | Alertas de tope presupuestal | Media | Sí | Se muestra alerta cuando el costo supera el tope configurado |
| RF-18 | Correos transaccionales | Media | Sí | Se envían correos de creación de cuenta y recuperación de contraseña |
| RF-19 | Importación y exportación | Baja | No | Excluida del MVP (`D-18`) |
| RF-20 | Listado y filtrado de insumos | Alta | Sí | Filtros por tipo, estado, marca, descripción, ciudad y precio |
| RF-21 | Transición a `FINALIZADO` | Alta | Sí | Solo el Director; exige 100 % cotizado y la aprobación requerida |
| RF-22 | Conteo de uso de insumos | Media | Sí | Suma por ítem de `consumo × cantidad de obra`, para todos los tipos |

## 2. Requerimientos no funcionales

| ID | Categoría | Requisito | Medición | Estado |
|---|---|---|---|---|
| NFR-001 | Seguridad | JWT, RBAC y aislamiento por compañía y proyecto en todo endpoint privado | 100 % de endpoints privados validan token, rol y pertenencia | Vigente |
| NFR-002 | Exposición pública | Las respuestas del catálogo no incluyen autoría, empresa ni firma; los precios y contactos sí se publican (`D-28`) | 0 campos prohibidos en la respuesta | Modificado |
| NFR-003 | Rendimiento | p95 ≤ 1,5 s en WBS y catálogo | — | **Eliminado** (`D-67`) |
| NFR-004 | Auditoría | Bitácora inmutable de cada cambio | — | **Obsoleto**: se reemplaza por último modificador y aprobación única (`D-08`, `D-52`) |
| NFR-005 | Mantenibilidad | Cobertura de pruebas | ≥ 80 % en dominio y motor de cálculo | Vigente (`D-61`) |

## 3. Reglas de negocio

| ID | Regla |
|---|---|
| BR-001 | Un usuario pertenece a una sola compañía. Excepción: el Administrador no tiene compañía (`X-25`) |
| BR-002 | Un proyecto pertenece a una compañía; sus APUs e insumos pertenecen a ese proyecto |
| BR-003 | Cambiar precio, consumo o rendimiento recalcula ítem, capítulo y proyecto |
| BR-004 | Se registra el último usuario que modificó cada APU o insumo y una aprobación por APU |
| BR-005 | Un proyecto o APU privado solo es accesible por la compañía propietaria |
| BR-006 | El catálogo público expone composición, precios y contactos; no expone autoría ni empresa (`D-28`) |
| BR-007 | No se finaliza un presupuesto con insumos en `FALTA_INFORMACION` o sin cotizar |
| BR-008 | La Matriz es un insumo compuesto, reutilizable en varios APUs, no pertenece a la EDT y no se anida |
| BR-009 | Un insumo está completo con descripción, precio, unidad y marca; si falta uno, pasa a `FALTA_INFORMACION` |
| BR-010 | Distribuidor, contacto, ciudad, clave de usuario y detalles son opcionales |
| BR-011 | El paso a `FINALIZADO` exige la aprobación definida por el modo de firma del proyecto |
| BR-012 | La firma se captura en el frontend; el backend no guarda imagen, token ni hash |
| BR-013 | Un APU se publica solo si su proyecto es público; un proyecto público expone todos sus APUs e insumos |
| BR-014 | La cascada de estados se ejecuta en una sola transacción |
| BR-015 | Los códigos del EDT son únicos por proyecto, sin distinguir mayúsculas: 1–10 caracteres en capítulos y 1–5 en ítems |
| BR-016 | Un APU se vincula a un único ítem de la EDT |
| BR-017 | Editar un APU aprobado elimina su aprobación y exige una nueva |
| BR-018 | Se copia un APU solo desde un proyecto de la misma compañía o desde el catálogo público; nunca desde otra compañía |
| BR-019 | El AIU se define únicamente por proyecto |
| BR-020 | No hay conversión automática de divisas; cada proyecto usa una sola moneda |
| BR-021 | La copia de una Matriz es independiente y exige reconciliar sus insumos en el proyecto destino |

## 4. Reglas de autorización
| Módulo | Acción | Administrador | Director | Presupuestador | Auditor |
|---|---|---|---|---|---|
| Usuarios | Crear o cambiar rol global | Sí | No | No | No |
| Usuarios | Buscar y asignar a compañía o proyecto | Sí | Sí (su compañía) | No | No |
| Compañía | Crear | Sí (ilimitadas) | Sí (una) | No | No |
| Compañía | Editar datos de registro | Sí | Sí (la propia) | No | No |
| Proyectos | Crear y configurar | No | Sí | No | No |
| Proyectos | Leer | Sí | Sí | Sí (asignados) | Sí (asignados) |
| EDT | Crear y editar | No (lectura) | Sí | Sí | No (lectura) |
| Insumos | Crear y editar | No (lectura) | Sí | Sí | No (lectura) |
| APUs y matrices | Crear, editar y copiar | No (lectura) | Sí | Sí | No (lectura) |
| Cotización | Cambio masivo a `COTIZADO` | No | Sí | Sí | No |
| Aprobación | Firmar APU | No | No | Sí | No |
| Aprobación | Visto bueno | No | No | No | Sí |
| Cierre | Transición a `FINALIZADO` | No | Sí | No | No |
| Catálogo | Consultar | Sí | Sí | Sí | Sí (también anónimo) |
| Autoría | Ver último modificador | Sí (lectura) | Sí | Sí | Sí (lectura) |

## 5. Reglas de validación
**General:** JSON en `camelCase` (`D-55`), errores con `ProblemDetails`, paginación `page`/`pageSize`, `companyId` y `projectId` tomados del token o de la ruta, nunca del cuerpo.

| Endpoint | Validaciones |
|---|---|
| `POST /users` | `email` válido y único; `fullName` no vacío; `role` válido; `companyId` inyectado salvo Administrador |
| `POST /companies` | `businessName` no vacío; `taxId` único |
| `POST /projects` | `name` no vacío; `currency` ISO 4217; `aiuAdmin`, `aiuContingency` y `aiuProfit` entre 0 y 100; `budgetLimit` ≥ 0; `signatureMode` válido; `requiresAuditor` y `isPublic` booleanos |
| `POST /projects/{id}/wbs` | `code` único por proyecto; 1–10 caracteres en capítulos y 1–5 en ítems; `parentId` del mismo proyecto; `quantityWork` > 0 en ítems |
| `POST /projects/{id}/resources` | `description` obligatoria; `unitPrice` ≥ 0; `unit` y `brand` requeridos para salir de `FALTA_INFORMACION`; `resourceTypeId` válido; opcionales sin bloquear |
| `POST /apus` | `description` no vacía; `unit` no vacía; `yield` > 0; `projectId` con pertenencia validada |
| `POST /apus/{id}/resources` | `resourceId` del mismo proyecto; `consumption` > 0; `wastePercentage` entre 0 y 100 |
| `POST /apus/{id}/approval` | APU del actor; `signatureCapture` como evidencia temporal; frase de confirmación no vacía |
| `GET /public/apus` | Sin autenticación; solo proyectos públicos; sin autoría, empresa ni firma; precios permitidos |

## 6. Matriz de trazabilidad
`T-U` = unitaria automatizada; `T-I` = integración manual; `T-S` = seguridad manual; `T-E` = flujo manual (`D-65`, `D-66`).

| RF | Caso de uso | Verificación |
|---|---|---|
| RF-01 | UC-01 | T-I-01, T-S-01, T-S-04 |
| RF-02 | UC-01 | T-I-02, T-S-05 |
| RF-03 | UC-02 | T-I-03 |
| RF-04 | UC-03 | T-U-01, T-I-04 |
| RF-05 | UC-04 | T-U-02, T-I-05 |
| RF-06 | UC-04 | T-I-06 |
| RF-07 | UC-05 | T-I-07, T-E-01 |
| RF-08 | UC-05 | T-U-03, T-I-08 |
| RF-09 | UC-05 | T-U-04, T-I-09 |
| RF-10 | UC-06 | T-U-05 |
| RF-11 | UC-06 | T-U-06, T-I-10 |
| RF-12 | UC-07 | T-U-07, T-I-11 |
| RF-13 | UC-07 | T-I-12 |
| RF-14 | UC-07 | T-I-13 |
| RF-15 | UC-08 | T-I-14, T-S-02, T-S-06 |
| RF-16 | UC-04 | T-U-08, T-I-15 |
| RF-17 | UC-06 | T-U-09, T-I-16 |
| RF-18 | UC-01 | T-I-17 |
| RF-19 | — | Fuera del MVP (`D-18`) |
| RF-20 | UC-04 | T-I-18 |
| RF-21 | UC-07 | T-U-10, T-I-19 |
| RF-22 | UC-05 | T-U-11, T-I-20 |





