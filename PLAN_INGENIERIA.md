# PLAN_INGENIERIA.md

**Proyecto:** Budget Civil — Gestión de presupuestos de obra civil (APU / EDT)
**Tipo de documento:** Especificaciones iniciales de ingeniería (problema, requisitos, arquitectura y plan)
**Metodología:** Spec-Driven Development (SDD) con OpenSpec
**Versión:** 0.28 — Autorregistro público incorporado al alcance
**Fecha:** 29/10/2026
**Autor:** [POR COMPLETAR]
**Estado:** Documento completo (v0.28). Los seis capítulos están redactados y los documentos base `00`–`06` y `09` actualizados. La v0.28 incorpora el autorregistro público de usuarios (`D-72`, `D-73`).

> **Regla de trazabilidad.** Todo enunciado debe poder rastrearse a una fuente: los documentos base `.github/docs/00`–`06` y `09`, o una decisión explícita `D-01`…`D-73`. Lo que no tenga fuente se marca `POR CONFIRMAR` o `DERIVADO` y **no se convierte en requisito** hasta ser validado. No se recicla texto ambiguo de los documentos base.

## 0. Control del documento

### 0.1 Propósito
Producir, de forma incremental y verificable, el documento único de ingeniería que sirva de insumo para generar especificaciones y cambios en OpenSpec. Este documento **no** reemplaza a las especificaciones de OpenSpec: las alimenta y las hace verificables.

### 0.2 Alcance de esta versión
| Elemento | Estado |
|---|---|
| Capítulo 1 — Análisis del problema | Cerrado (v0.5); registros en 0.5 y 0.6 |
| Capítulo 2 — Propuesta de solución | Cerrado (v0.28): 23 funcionalidades, 9 módulos y 8 incrementos |
| Capítulo 3 — Arquitectura y tecnología | Cerrado (v0.19): 3.1–3.5 completos |
| Capítulo 4 — Estrategia de desarrollo | Cerrado (v0.21) |
| Capítulo 5 — Estrategia de calidad | Cerrado (v0.24) |
| Capítulo 6 — SDD e inteligencia artificial | Cerrado (v0.26) |
| Actualización de `.github/docs/00`–`09` | Completada el 27/10/2026: `00`–`06` y `09`; `07` y `08` no existen |
| Autorregistro público de usuarios | Incorporado al alcance el 29/10/2026 (`D-72`, `D-73`): registro anónimo, sin sesión y con nombre de usuario obligatorio |
| Especificaciones y cambios en OpenSpec | Listos para iniciar; el plan está completo |

### 0.3 Fuentes utilizadas
- `.github/docs/00-idea-problema.md` … `06-flujos-casos-de-uso.md`
- `.github/docs/09-operacion-copilot-spec-kit.md`
- Aclaraciones del autor: `D-01` a `D-10` (registradas en 0.5) y `D-72`–`D-73` (registradas el 29/10/2026)
- **Excluidos por instrucción:** código implementado, `.github/spec/`, `specs/`, `docs/`.

### 0.4 Convenciones
| Marca | Significado |
|---|---|
| `CONFIRMADO` | Respaldado por una fuente documental o por una decisión `D-xx`. |
| `PARCIAL` | Existe decisión, pero queda un detalle abierto que impide especificarlo. |
| `DERIVADO` | Inferencia razonable a partir de las fuentes; requiere validación explícita. |
| `POR CONFIRMAR` | Sin información suficiente; **todavía no es requisito**. |
| `OBSOLETO` | Contenido de los documentos base que queda contradicho por una decisión `D-xx`. |

Prefijos de identificadores: `P` problema, `H` hipótesis, `N` necesidad, `ACT` actor, `S` alcance incluido, `E` exclusión, `R` restricción, `A` supuesto, `X` contradicción o vacío, `Q` pregunta, `RF` funcionalidad, `M` módulo, `I` incremento.

### 0.5 Decisiones registradas
| ID | Decisión | Impacto principal | Estado |
|---|---|---|---|
| D-01 | Plataforma SaaS multiempresa. El Administrador designa directores de proyecto; el Administrador o un Director sin compañía crean compañías; un Director no puede tener más de una compañía. | Tenancy, onboarding, RBAC | CONFIRMADO |
| D-02 | Cuatro roles: Administrador (plataforma, sin compañía), Director de proyectos, Presupuestador y Auditor. Reemplazan `PROJECT_MANAGER`, `ESTIMATOR` y `READ_ONLY`. | Actores, autorización, modelo de datos | CONFIRMADO (`D-15`, `D-21`) |
| D-03 | Firma configurable **por proyecto**, editable solo por el Director: (1) sin firma; (2) firma de presupuestador (firmante único o firma por cada APU modificado/creado); (3) firma de presupuestador + aprobación de auditor por APU. | Flujo de aprobación, estados, modelo de datos | MODIFICADO por `D-52` (aprobación única por APU) |
| D-04 | Un presupuesto solo pasa a `FINALIZADO` si todos los insumos están `COTIZADO`; el cambio de estado puede ser masivo. | Regla de cierre | CONFIRMADO |
| D-05 | Un APU pertenece a un solo proyecto. Se crea desde cero, o se copia desde un APU de la empresa o desde el catálogo abierto. Los insumos son por proyecto y al copiar se detectan similitudes para que el usuario decida. La Matriz es un insumo compuesto reutilizable en varios APUs. | Modelo APU/insumo, copia, catálogo | CONFIRMADO (`D-12`, `D-13`, `D-16`) |
| D-06 | Fórmulas: insumo → cantidad requerida = cantidad unitaria × rendimiento en APU; mano de obra → valor total = unidad de esfuerzo (hora-hombre o día-hombre, configurable) × rendimiento en APUs. | Motor de cálculo | PARCIAL (X-15) |
| D-07 | El AIU se parametriza únicamente por proyecto. | Configuración de proyecto | CONFIRMADO |
| D-08 | Control de cambios reducido: solo se guarda el último usuario que modificó un APU o un insumo. Sin historial de valores ni de acciones. | Alcance de auditoría | CONFIRMADO (X-01) |
| D-09 | OpenSpec reemplaza por completo a Spec Kit. Los documentos `00`–`09` siguen vigentes como documentación y se actualizan gradualmente. | Proceso SDD y documentación | CONFIRMADO |
| D-10 | El proyecto es académico. | Contexto, plan y evaluación | CONFIRMADO (`D-20`) |
| D-11 | Autoría y aprobación (deriva de Q-01 = A): último usuario modificador por APU e insumo; un registro de aprobación por APU con usuario, rol, fecha y modo; estado del presupuesto. Sin historial de valores ni congelamiento de versión. | Modelo de datos, flujo de aprobación | MODIFICADO por `D-52` (registro único de aprobación) |
| D-12 | Catálogo abierto global, alimentado automáticamente por APUs e insumos que las empresas publiquen y sin curaduría. La condición de público/privado se configura por proyecto y por defecto es privada. El contenido publicado se define en `D-23` y `X-21`. | Catálogo, visibilidad, modelo de datos | CONFIRMADO (Q-08, Q-18; `X-21`) |
| D-13 | Copia de APUs: solo desde APUs públicos o desde proyectos de la misma compañía del usuario. Está prohibido consultar o copiar APUs privados o de otras compañías. | Copia, aislamiento multitenant | CONFIRMADO (Q-10; X-18) |
| D-14 | La transición a `FINALIZADO` la ejecuta únicamente el Director y exige que todos los APUs estén aprobados por el profesional correspondiente, según el modo configurado en el proyecto. | Flujo de cierre | CONFIRMADO (Q-04; `D-52`) |
| D-15 | El rol Auditor es requisito para auditar y puede ser interno o externo. El Presupuestador administra todas las características del EDT. Director y Presupuestador cambian insumos a `COTIZADO` de forma masiva. El Administrador solo edita datos de registro de la compañía. | Actores, autorización | CONFIRMADO (Q-03, Q-05, Q-06, Q-07; X-16) |
| D-16 | Relación APU–ítem 1:1. La Matriz se reutiliza dentro del proyecto y puede copiarse a otro. La copia conserva vínculo solo cuando proviene del catálogo abierto. La unicidad de insumos es una sugerencia, no una restricción. | Modelo, copia, deduplicación | CONFIRMADO (Q-09, Q-11, Q-15, Q-16; X-19) |
| D-17 | Notificaciones por correo limitadas a creación de cuenta y recuperación de contraseña. | Alcance, infraestructura | CONFIRMADO (Q-13) |
| D-18 | MVP: alertas de tope sí; importación y exportación no. | Frontera del MVP | CONFIRMADO (Q-12) |
| D-19 | No existen datos históricos del proceso: los criterios se apoyan en supuestos declarados. | Problema, métricas | CONFIRMADO (Q-14) |
| D-20 | Entrega académica: no se exige un documento formal de evaluación; se exige documentación completa del aplicativo, implementación correcta de SDD y resultado funcional. | Propósito del documento, capítulo 4 | CONFIRMADO (Q-02) |
| D-21 | Cambio de roles: el Director lo hace dentro de su compañía; el Administrador a nivel global. | Autorización | CONFIRMADO (Q-17) |
| D-22 | La publicación es por proyecto: si el proyecto es público, todos sus APUs e insumos lo son. Los flags `Is_Public` por APU e insumo quedan sin uso. | Visibilidad, modelo de datos | CONFIRMADO (Q-18) |
| D-23 | Al publicar un insumo se exponen todos sus campos, incluidos distribuidor y contacto. | Visibilidad, privacidad | CONFIRMADO y ampliado por `D-28` (`Q-22 = A`) |
| D-24 | La copia de APUs no está restringida por el estado del proyecto de origen o destino. | Copia | CONFIRMADO (Q-19) |
| D-25 | Al incorporar un APU se comparan sus insumos con los del proyecto destino: los inexistentes se agregan y los similares se sugieren. El usuario decide entre usar el insumo existente (con su precio unitario) o conservar el valor de origen y agregar el insumo. No hay actualización automática. | Copia, deduplicación, costos | CONFIRMADO (Q-20) |
| D-26 | El firmante único del modo 2 puede ser cualquier Presupuestador asignado y su firma cubre todos los APUs. | Flujo de aprobación | CONFIRMADO (Q-21) |
| D-27 | Los insumos y APUs de otros proyectos de la misma compañía pueden copiarse a un proyecto nuevo o existente. No existe un registro propio de insumos de compañía. | Modelo de insumos | CONFIRMADO (Q-24) |
| D-28 | El catálogo abierto publica todos los campos del insumo, incluidos precio unitario, distribuidor y contacto. Se mantiene la anonimización de autoría y de empresa; queda sin efecto la restricción de "sin precios". | Visibilidad, privacidad, DTO público | CONFIRMADO (Q-22 = A) |
| D-29 | La reutilización de un insumo de la compañía en otro proyecto es una copia con su precio, independiente del original: los cambios posteriores no se propagan. | Modelo de insumos, costos | CONFIRMADO (Q-23 = A) |
| D-30 | EDT recursiva sin límite de profundidad ni de hijos. Numeración automática y jerárquica (`1`, `1.1`, `1.1.1`), editable por el usuario, única en el proyecto y compartida entre capítulos e ítems; al reordenar o eliminar se renumera solo lo no personalizado. Los hijos de un capítulo con código personalizado heredan ese código como prefijo (`CIM.1`, `CIM.1.1`). El límite de longitud se aplica al segmento ingresado, no al código efectivo. | EDT, modelo de datos | CONFIRMADO (definición EDT; Q-26 = A) |
| D-31 | El código del ítem vive en la EDT (`WbsItem`). El APU no almacena código propio, aunque la vista de edición del APU muestra el código del ítem. | Modelo de datos | CONFIRMADO (definición EDT) |
| D-32 | Base de cálculo por tipo: materiales por unidad de obra (`consumo × cantidad del ítem`); mano de obra por jornada o cuadrilla con unidad de esfuerzo configurable. La fórmula exacta se cierra en el capítulo 3. | Motor de cálculo | CONFIRMADO (base de cálculo; `X-15`) |
| D-33 | El listado de insumos muestra el conteo de uso para todos los tipos de insumo: suma por ítem de `consumo del insumo × cantidad de obra`. La consulta pertenece a `M-06` y se implementa en `I-3b`, cuando ya existen APUs y cantidades. | EDT, APUs, plan | CONFIRMADO (reparto de incrementos) |
| D-34 | Precisión de autorización: el Director puede buscar usuarios para agregarlos o retirarlos de la compañía y de los proyectos; el Presupuestador solo lee los datos de la compañía y de sus proyectos asignados; el Administrador no edita parámetros de proyectos, APUs ni insumos (solo usuarios, roles y compañías); el Director no firma APUs, solo los usuarios que elaboraron el presupuesto; Auditores y Administradores solo leen información (el Auditor además aprueba APUs); todos editan su perfil. | Autorización, actores | CONFIRMADO (revisión de capítulos 1 y 2) |
| D-35 | El catálogo público permite copiar APUs e insumos de forma independiente. | Catálogo, copia | CONFIRMADO (revisión de capítulos 1 y 2) |
| D-36 | Aislamiento multitenant mediante filtro global por compañía y un único bypass controlado para el catálogo público. | Persistencia, seguridad | CONFIRMADO (Q-27 = A) |
| D-37 | El cálculo se implementa como servicio de dominio puro; la capa de aplicación orquesta y persiste. | Arquitectura, pruebas | CONFIRMADO (Q-28 = A) |
| D-38 | El catálogo público es anónimo. Al exponer precios y contactos (`D-28`) se acepta el riesgo de scraping; exige límite por IP y prohibición de exportación masiva. | Seguridad, catálogo | CONFIRMADO (Q-30 = B) |
| D-39 | TLS se posterga: solo se permite despliegue local o demo controlada. Queda prohibido exponer la aplicación a Internet sin HTTPS. | Despliegue, seguridad | CONFIRMADO (Q-31 = C) |
| D-40 | Las migraciones se aplican manualmente con un runbook documentado antes de cada despliegue; se elimina la expectativa de inicialización automática de `05 §8`. | Operación | CONFIRMADO (Q-32 = C) |
| D-41 | El mapeo entidad → DTO es manual, sin AutoMapper. | Dependencias, mantenibilidad | CONFIRMADO (Q-33 = A) |
| D-42 | OpenAPI se documenta con Swashbuckle (documento y Swagger UI). | Contrato de API | CONFIRMADO (Q-34 = B) |
| D-43 | La biblioteca de UI es Angular Material. | Frontend | CONFIRMADO (Q-35 = A) |
| D-44 | La captura de firma usa `signature_pad`. | Frontend, aprobación | CONFIRMADO (Q-36 = A) |
| D-45 | El correo se envía mediante proveedor transaccional; SendGrid, Mailgun o SES se elige al implementar. | Infraestructura, notificaciones | CONFIRMADO (Q-37 = B) |
| D-46 | La integración continua se ejecuta con GitHub Actions. | Calidad, despliegue | CONFIRMADO (Q-38 = A) |
| D-47 | Las consultas de conteo de uso y de catálogo público se implementan como proyecciones de EF Core en `Application`, sin duplicar la fórmula del APU. | Consultas, rendimiento | CONFIRMADO (Q-29 = A) |
| D-48 | La composición de una Matriz se almacena en `MatrixComponent` (matriz, insumo, consumo, desperdicio), sin FK polimórfica. | Persistencia, matrices | CONFIRMADO (Q-40 = A) |
| D-49 | Borrado lógico (`is_active`/`deleted_at`) en `Project`, `Apu` y `Resource`, con `RESTRICT` en registros referenciados. | Persistencia, integridad | CONFIRMADO (Q-42 = A) |
| D-50 | Control de concurrencia optimista con `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource`; la API responde `409 Conflict`. | Persistencia, concurrencia | CONFIRMADO (Q-43 = A) |
| D-51 | El EDT se modela con `WbsChapter` y `WbsItem` separados y una tabla `WbsCode` (`project_id`, `code`, `node_type`) con índice único (`project_id`, `code`) que garantiza la unicidad entre capítulos e ítems. | EDT, persistencia | CONFIRMADO (Q-39 = B) |
| D-52 | Aprobación única por APU: un registro por APU con el último usuario que aprobó, su rol, fecha, modo y evidencia capturada. No se conserva historial por usuario ni se valida la aprobación de múltiples auditores; el modo 3 se limita a una aprobación por APU. | Aprobaciones, alcance | CONFIRMADO (Q-41 = C) |
| D-53 | Toda edición de un APU aprobado elimina su registro de aprobación y exige una nueva aprobación. | Aprobaciones, integridad | CONFIRMADO (Q-44 = A) |
| D-54 | El backend se organiza en proyectos por capa (`Domain`, `Application`, `Infrastructure`, `Api`) con carpetas por módulo dentro de cada capa. | Organización de componentes | CONFIRMADO (Q-45 = A) |
| D-55 | Los contratos JSON usan `camelCase`; los ejemplos con `snake_case` de `02 §5` quedan obsoletos. | Contratos de API | CONFIRMADO (Q-47 = B) |
| D-56 | El cliente Angular se genera desde el contrato OpenAPI y se regenera en la tubería de CI. | Frontend, contrato | CONFIRMADO (Q-48 = A) |
| D-57 | Estado del frontend híbrido: sesión y proyecto activo en servicios con signals; datos de servidor en servicios por feature; estado efímero en componentes. Regla: si un estado lo usan dos features, se promueve a servicio. | Frontend | CONFIRMADO (Q-46 = D) |
| D-58 | La cadencia es una iteración por etapa, sin fechas fijas; cada etapa cierra con su flujo demostrable. | Proceso | CONFIRMADO (Q-49 = A) |
| D-59 | Se adopta GitFlow: `main`, `develop`, `feature/*`, `release/*` y `hotfix/*`. | Proceso, control de versiones | CONFIRMADO (Q-50 = B) |
| D-60 | Los commits siguen Conventional Commits. | Proceso | CONFIRMADO (Q-51 = A) |
| D-61 | La cobertura mínima es 80 % en dominio y motor de cálculo; dominio y aplicación es la meta. | Calidad | CONFIRMADO (Q-52 = A) |
| D-62 | La revisión de cambios es autoevaluación con checklist y CI; revisión externa opcional al cerrar etapa. | Calidad, proceso | CONFIRMADO (Q-53 = A) |
| D-63 | Las reglas de arquitectura se verifican con NetArchTest en la tubería de CI. | Calidad, arquitectura | CONFIRMADO (Q-54 = A) |
| D-64 | No se implementa prueba de rendimiento automatizada. | Calidad | CONFIRMADO (Q-55 = D) |
| D-65 | Las pruebas extremo a extremo son manuales y se ejecutan como checklist al cerrar cada etapa. | Calidad, interfaz | CONFIRMADO (Q-56 = C) |
| D-66 | Las pruebas de integración con base de datos son manuales: no se usa Testcontainers ni un servicio SQL en CI; se ejecutan sobre SQL Server local con checklist y evidencia. | Calidad, persistencia | CONFIRMADO (Q-57 = manual) |
| D-67 | Se elimina `NFR-003` (p95 ≤ 1.5 s) del alcance; no habrá requisito de rendimiento medible. | Alcance, calidad | CONFIRMADO (Q-58 = B) |
| D-68 | El identificador de cada cambio de OpenSpec es libre, elegido por el estudiante; debe ser único y descriptivo. | Proceso, OpenSpec | CONFIRMADO (Q-59 = C) |
| D-69 | Las especificaciones se escriben en español; el código y los identificadores técnicos en inglés. | Documentación | CONFIRMADO (Q-60 = A) |
| D-70 | No se exige registro del uso de IA; la evidencia es el cambio revisado y el checklist. | Proceso, IA | CONFIRMADO (Q-61 = C) |
| D-71 | La IA puede redactar especificaciones y código; el estudiante revisa y aprueba cada cambio. | Proceso, IA | CONFIRMADO (Q-62 = A) |
| D-72 | El autorregistro de usuarios **sí existe**: cualquier persona crea su propia cuenta desde un endpoint público, **sin sesión iniciada** y sin pertenecer previamente a ninguna compañía. La cuenta nace sin compañía y sin proyecto, con el rol inicial que fije el sistema, y su asignación posterior se rige por `D-02`, `D-21` y `D-34`. Sustituye a `E-11` y a `A-03`. | Alcance, identidad, seguridad | CONFIRMADO (Q-63 = A; aclaración del autor, 29/10/2026) |
| D-73 | El **nombre de usuario** (`username`) es un atributo obligatorio y único de la cuenta, además del correo electrónico; sirve para iniciar sesión y se declara en el autorregistro. | Identidad, modelo de datos, contratos | CONFIRMADO (aclaración del autor, 29/10/2026) |

### 0.6 Contradicciones y vacíos vigentes
Este registro es la lista de trabajo para los siguientes capítulos. Ningún punto marcado como `PENDIENTE` puede convertirse en requisito.

| ID | Contradicción o vacío | Fuentes en conflicto | Estado tras D-01…D-73 | Afecta a |
|---|---|---|---|---|
| X-01 | Trazabilidad: "log inmutable de cada acción y de los cambios de costo" frente a "solo último modificador, sin valores ni historial". | `00 §5`, `NFR-004`, `BR-004` ↔ `D-08`, `D-11` | **RESUELTO por D-11 (Q-01 = A).** Último modificador + aprobaciones por APU + estado; sin historial de valores ni congelamiento de versión | P-01, S-11, E-05 |
| X-02 | Firma: "no se persiste token/hash", "aprobación única" y "solo ADMIN/ESTIMATOR firman" frente a tres modos de firma con firmas por APU y aprobación de auditores. | `00 §9`, `05 §4`, `BR-011`, `02 §4` ↔ `D-03`, `D-11`, `D-14` | **RESUELTO:** se persisten aprobaciones por APU, el Director ejecuta el cierre y el firmante único quedó definido en `D-26` | S-10, ACT-03, ACT-04 |
| X-03 | Cierre: bastaba con ausencia de `FALTA_INFORMACION`; ahora se exige el 100 % de insumos `COTIZADO`. | `05 §4`, `BR-007`, `BR-011` ↔ `D-04` | **Resuelto por D-04.** Los documentos `02` y `05` quedan `OBSOLETO` en ese punto | S-10, criterios de aceptación |
| X-04 | APU por proyecto: el modelo lo define a nivel compañía y con relación 1:1 con el ítem; la decisión lo hace pertenecer a un proyecto y ser copiable. | `05 §2/§3` ↔ `D-05`, `D-16` | **Resuelto por Q-15 = B:** relación 1:1 entre APU e ítem; para reutilizar hay que copiar | S-06 |
| X-05 | Insumos por proyecto: el modelo los define a nivel compañía y `FR-007` habla de una tabla unificada por empresa. | `05 §3`, `FR-007` ↔ `D-05`, `D-12` | **Resuelto por `D-05`, `D-28` y `D-29`:** insumos propios de cada proyecto, copiables, y catálogo global con todos los campos | S-05, S-14 |
| X-06 | Catálogo abierto: no existe en ningún documento base; la decisión lo introduce como fuente de APUs. | `00`–`06` ↔ `D-05`, `D-12` | **Resuelto por `D-12`, `D-22` y `D-28`:** global, automático, sin curaduría, publicación por proyecto y todos los campos del insumo | S-08 |
| X-07 | RBAC: el Administrador tenía permisos totales; ahora solo lee proyectos e insumos. | `02 §4` ↔ `D-02` | **Resuelto por D-02.** La matriz de `02 §4` queda `OBSOLETO` | 1.3 |
| X-08 | Administrador y compañías: "puede crear, buscar y editar compañías" frente a "no puede alterar información de las compañías". | `D-02` (interno) | **Resuelto por Q-07 = B:** solo edita datos de registro (razón social, NIT) | 1.3, S-02 |
| X-09 | Auditor: se define como "ente regulador" pero es asignado por el Director y pertenece a la compañía. | `D-02` (interno), `05 §4` | **Resuelto por Q-03 = C:** puede ser interno o externo; se exige el rol Auditor | 1.3, E-10 |
| X-10 | Unicidad de insumos: "debe existir insumos únicos para cada proyecto" frente a "si se repite el mismo insumo… el responsable es el usuario". | `D-05` (interno) | **Resuelto por Q-09:** la unicidad es una sugerencia; decide el Presupuestador | S-05, S-14 |
| X-11 | Copia entre proyectos: la frase "que no se encuentren" está incompleta. | `D-05` | **Resuelto por `D-13` y `D-24`:** solo APUs públicos o de proyectos de la misma compañía, sin restricción por estado | S-06 |
| X-12 | Matriz reutilizable "en más de un APU", sin precisar si cruza proyectos, cuando los insumos son por proyecto. | `D-05` (interno) | **Resuelto por Q-11 + D-16:** reutilización dentro del proyecto y copia a otro proyecto | S-07 |
| X-13 | EDT: el Director "crea y edita elementos"; el Presupuestador solo "edita la EDS". | `D-02` (interno) | **Resuelto por Q-06 = A:** el Presupuestador administra todas las características del EDT | 1.3, 2 |
| X-14 | Requisitos de la institución académica: entregables, fechas, criterios de evaluación. | `D-10` ↔ ausencia de documento | **Resuelto por Q-02 = B / D-20:** no hay documento formal; se exige aplicación funcional y documentación del desarrollo | 1.2, capítulo 4 |
| X-15 | Fórmulas de D-06 dimensionalmente ambiguas (multiplicar por el rendimiento y expresar la mano de obra en unidad de esfuerzo). | `D-06` ↔ `05 §5-B` | PARCIAL: `D-32` fija la base por tipo (material por unidad de obra; mano de obra por jornada). La fórmula exacta se cierra en el capítulo 3 | P-03, S-09 |
| X-16 | Autoridad sobre el rol: Q-03 indicaba que el rol del Auditor solo lo modificaba el Director, mientras `D-02` permite al Administrador cambiar cualquier rol. | `Q-03` ↔ `D-02` | **Resuelto por D-21:** el Director dentro de su compañía y el Administrador a nivel global | 1.3 |
| X-17 | Granularidad de publicación: `D-12` configura público/privado por proyecto, pero `D-05` y `FR-005` hablan de `Is_Public` por APU e insumo; tampoco se definía qué datos del insumo se publican. | `D-12` ↔ `D-05`, `FR-005` | **Resuelto por Q-18 = A:** la publicación es por proyecto. Los flags por APU e insumo quedan `OBSOLETO` (DERIVADO). El contenido publicado pasa a `X-21` | S-08, S-05 |
| X-18 | Copia y estado del proyecto: Q-10 define el origen permitido, pero no si se puede copiar hacia o desde proyectos `FINALIZADO`. | `Q-10` ↔ `D-04` | **Resuelto por Q-19 = A:** sin restricción de estado | S-06 |
| X-19 | Vínculo de procedencia: Q-16 = D conserva vínculo solo desde el catálogo, sin definir su efecto. | `Q-16` ↔ `D-11` | **Resuelto por D-25:** el vínculo sirve para comparar insumos y decidir; no hay actualización automática | S-08, S-14 |
| X-20 | Firmante único del modo 2: Q-04 exige la firma de todos los APUs según configuración, pero no define quién puede ser el firmante único ni el alcance de su firma. | `Q-04` ↔ `D-03` | **Resuelto por Q-21 = A:** cualquier Presupuestador asignado; su firma cubre todos los APUs | S-10 |
| X-21 | Contenido publicado del insumo: Q-18b = D exponía todos los campos, mientras `NFR-002`, `BR-006` y `00 §8` prohíben publicar precio, distribuidor y contacto. | `D-23` ↔ `NFR-002`, `BR-006` | **Resuelto por D-28:** se publican todos los campos, incluido el precio. `NFR-002`, `BR-006` y el `PublicApuDTO` por whitelist quedan `OBSOLETO` en su restricción de precios | S-08, NFR-002 |
| X-22 | Reutilización de insumos de la compañía en proyectos nuevos: no estaba definido si se copia el insumo o se referencia el registro de la compañía. | `D-27` ↔ `D-05` | **Resuelto por D-29:** copia con precio e independiente del original | S-05, S-09 |
| X-23 | Registro de insumos a nivel de compañía: `D-27` permite reutilizarlos, pero `D-05` los define por proyecto. No se sabía si existe una entidad propia de insumos de compañía o si se reutilizan desde los proyectos existentes. | `D-27` ↔ `D-05` | **Resuelto por Q-24:** no existe registro propio; se copian insumos y APUs desde otros proyectos de la misma compañía | S-05, M-04 |
| X-24 | Numeración de hijos bajo un código personalizado: si un capítulo se renombra a `CIM`, no estaba definido si sus hijos se numeran `CIM.1` o `1.1`. | `D-30` (interno) | **Resuelto por Q-26 = A:** los hijos heredan el código del padre como prefijo | S-04, M-03 |
| X-25 | `BR-001` exige que todo usuario tenga compañía; `D-02` define al Administrador sin compañía y `D-72` admite usuarios autorregistrados sin compañía. | `BR-001` ↔ `D-02`, `D-72` | **Resuelto por `D-02`, `D-34` y `D-72`:** `company_id` es nullable para el rol Administrador y para el usuario autorregistrado pendiente de asignación; `02 §3` debe actualizarse | 3.3, `05` |
| X-26 | El modo 3 exigía que uno o más auditores aprobaran cada APU; con `D-52` solo se conserva la última aprobación. | `D-03` ↔ `D-52` | **Resuelto por D-52:** el modo 3 se limita a una aprobación por APU; no se valida la aprobación de varios auditores | S-10, RF-12 |
| X-27 | Autorregistro: `E-11` lo excluía del alcance y `A-03` suponía que solo el Administrador creaba usuarios, mientras la aclaración del autor lo exige como capacidad del sistema. | `E-11`, `A-03` ↔ `D-72` | **Resuelto por `D-72` y `D-73`:** el autorregistro entra en alcance; `E-11` queda `OBSOLETO` y `A-03` se reescribe como confirmado | S-01, RF-23, capítulo 3 |

### 0.7 Impacto sobre los documentos base
Actualización realizada el 27/10/2026 (`D-09`). El apartado de Spec Kit de `04 §5` y el antiguo documento `09` quedaron reemplazados por `09-operacion-openspec.md`.

| Documento | Sección | Cambio requerido | Prioridad |
|---|---|---|---|
| `00` | §3, §5, §7, §9 | Roles reales, alcance reducido de trazabilidad, criterios de éxito y firma configurable | Alta |
| `02` | §1, §4, §5, §6 | FR-001, FR-005, FR-006, FR-011, FR-012 y FR-013; matriz de autorización; validaciones; matriz de trazabilidad; **NFR-002 y BR-006 (publicación de precios)**; **BR-001 (usuario sin compañía)**; nombres de campos `camelCase` (`D-55`); **NFR-003 eliminado** (`D-67`) | Alta |
| `03` | §2, §5 | Configuración de firma, catálogo abierto y aislamiento por proyecto | Media |
| `04` | §1, §5 | Versión de EF Core frente a .NET 10; sustitución de Spec Kit por OpenSpec (D-09) | Alta |
| `05` | §1–§8 | `project_id` en APU e insumo, eliminación de `Apu.code` (el código pertenece al ítem), eliminación de los flags `Is_Public` por APU e insumo, eliminación de `AuditLog` y `BudgetVersion`, reglas de unicidad del EDT, registro de aprobaciones, AIU por proyecto, autoría mínima y migraciones manuales (`D-40`) | Alta |
| `06` | §1–§5 | Actores y casos de uso reescritos con cuatro roles y tres modos de firma | Alta |
| `09` | Todo | Reemplazo por la guía operativa de OpenSpec | Alta |

> **v0.28 (29/10/2026):** el autorregistro (`D-72`, `D-73`) obliga a revisar `00 §3`, `02 §1/§4` (RF-01 y matriz de autorización), `05 §3` (agregar `User.username` y `company_id` nullable) y `06 §1` (caso de uso de autorregistro). Queda `PENDIENTE` de aplicar en los documentos narrativos.

### 0.8 Estructura del documento
| Capítulo | Contenido | Estado |
|---|---|---|
| 1 | Análisis del problema | Cerrado (v0.5) |
| 2 | Propuesta de solución | Cerrado (v0.8) |
| 3 | Decisiones de arquitectura y tecnología | Cerrado (v0.19) |
| 4 | Estrategia de desarrollo | Cerrado (v0.21) |
| 5 | Estrategia de calidad | Cerrado (v0.24) |
| 6 | Uso de SDD e inteligencia artificial (OpenSpec) | Cerrado (v0.26) |
| Anexos | A) Registro de decisiones · B) Preguntas abiertas · C) Trazabilidad requisito↔prueba · D) Glosario | Pendiente |

---

# 1. Análisis del problema

## 1.1 Identificación de la problemática

### 1.1.1 Situación actual
La gestión de presupuestos de obra civil se realiza con hojas de cálculo (`00 §2`). De los documentos se desprende el siguiente proceso implícito:

1. Cada proyecto se arma en un archivo de Excel independiente (`00 §2`).
2. Los APUs, insumos y rendimientos se reconstruyen o copian manualmente entre archivos (`00 §2`, `D-05`).
3. El cierre del presupuesto se valida de forma manual, sin control automático de integridad (`00 §7`, `D-04`).
4. No existe un registro de autoría ni de aprobación asociado a los cambios (`00 §2`).
5. La aprobación del presupuesto no tiene un mecanismo formal ni configurable (`D-03`).

**Estado:** los puntos 1 y 2 están `CONFIRMADO` por `00 §2`; los puntos 3, 4 y 5 son `DERIVADO`, porque los documentos describen el "deber ser" del sistema y no documentan el proceso actual con evidencia.

### 1.1.2 Problemas confirmados
| ID | Problema | Evidencia | Estado |
|---|---|---|---|
| P-01 | No es posible determinar quién es responsable del contenido de un APU ni quién aprobó un presupuesto. | `00 §2` ("no hay asociación de firma ni responsabilidad de usuario") | CONFIRMADO como problema. La solución queda acotada por `D-08` y `D-11`: último modificador y aprobaciones por APU, sin historial de valores |
| P-02 | Los APUs, insumos y rendimientos se reconstruyen por proyecto; el conocimiento técnico no se reutiliza de forma estructurada. | `00 §2` ("aislamiento de datos de rendimiento"), `D-05` | CONFIRMADO |
| P-03 | El cálculo depende de fórmulas manuales; hay riesgo de fórmulas rotas y redondeos inconsistentes. | `00 §7`, `01 §2` | DERIVADO: se declara como objetivo del sistema, no hay registro de incidentes |
| P-04 | Insumos equivalentes con nombres, unidades o precios distintos impiden consolidar y comparar información. | `D-05` (detección de similitud), `FR-007` (deduplicación) | CONFIRMADO como necesidad; sin cuantificación |
| P-05 | Un presupuesto puede cerrarse sin que la información esté completa ni exista una aprobación formal. | `00 §7`, `BR-011`, `D-04` | CONFIRMADO como requisito de solución; el proceso actual no está documentado (`DERIVADO`) |

### 1.1.3 Hipótesis por validar
No se incorporan como problemas hasta ser confirmadas.

| ID | Hipótesis | Cómo se validaría | Estado |
|---|---|---|---|
| H-01 | Cualquier persona con acceso al archivo puede modificar valores sin dejar control de autoría. | Entrevista con la empresa usuaria / revisión de un archivo real | POR CONFIRMAR |
| H-02 | El armado de un presupuesto consume horas de retrabajo por copia manual de APUs e insumos. | Medición de tiempos en un proyecto real | POR CONFIRMAR |
| H-03 | La aprobación del presupuesto se gestiona por correo o firma física, sin evidencia digital asociada. | Entrevista / revisión del proceso de aprobación vigente | POR CONFIRMAR |

### 1.1.4 Impacto
| Dimensión | Efecto | Evidencia | Dato disponible |
|---|---|---|---|
| Económico | Retrabajo por reconstrucción de APUs y riesgo de sobrecosto por errores de cotización | `00 §2`, `D-05` | `[SIN DATO]` |
| Técnico | Información dispersa y fórmulas no auditables | `00 §2`, `00 §7` | `[SIN DATO]` |
| Contractual | Aprobación sin evidencia formal de quién la emitió | `D-03` | `[POR CONFIRMAR requisito legal]` |
| Operativo | Dependencia de personas y ausencia de historial de autoría | `D-08` | CONFIRMADO como limitación aceptada |

### 1.1.5 Delimitación negativa
No forman parte de este problema:

1. La inteligencia de precios de mercado o el scraping de proveedores (`00 §6`).
2. La ejecución de obra, inventarios, compras y facturación (`00 §6`).
3. La interoperabilidad CAD/BIM (`00 §6`).
4. La reconstrucción histórica de cambios y valores: queda explícitamente descartada por `D-08`.

### 1.1.6 Evidencia faltante para cerrar el punto
No existen datos disponibles (`Q-14 = C`, `D-19`). Se trabajará con supuestos declarados y cada criterio de éxito deberá indicar que no tiene línea base. Queda como información deseable, no bloqueante:

- Número de empresas, proyectos, APUs e insumos gestionados hoy.
- Tiempo medio de armado y de corrección de un presupuesto.
- Frecuencia y costo de errores de cálculo.
- Proceso de aprobación actual y su validez contractual.

---

## 1.2 Necesidades de la institución

### 1.2.1 Definición de "institución" en este proyecto
El término no está definido en los documentos base. Con `D-01` y `D-10` se identifican **tres instancias** con necesidades distintas:

| Instancia | Descripción | Rol en el sistema | Estado |
|---|---|---|---|
| Institución académica | Universidad o programa que evalúa el proyecto | Ninguno (contexto y evaluación) | CONFIRMADO (`D-10`); sin documento formal de evaluación (`D-20`) |
| Operador de la plataforma | Quien administra el SaaS y da de alta a las empresas | `Administrador` | DERIVADO de `D-01` y `D-02` |
| Empresas clientes (tenants) | Constructoras que usan la plataforma | `Director de proyectos`, `Presupuestador`, `Auditor` | CONFIRMADO (`D-01`, `D-02`) |

> **Nota crítica:** el proyecto es un SaaS multiempresa (`D-01`), por lo que "la institución" en singular no existe. Cualquier requisito que hable de "la empresa" debe especificar si aplica al operador o a un tenant.

### 1.2.2 Necesidades del operador de la plataforma
| ID | Necesidad | Fuente | Requisito candidato | Estado |
|---|---|---|---|---|
| N-01 | Aislar la información entre empresas y entre proyectos de una misma empresa. | `00 §8`, `D-05` | Multitenancy + alcance por proyecto | CONFIRMADO |
| N-02 | Controlar el alta de empresas y de usuarios sin autoservicio. | `D-01`, `D-02` | Gestión de usuarios por Administrador | DERIVADO |
| N-03 | Operar con bajo costo de infraestructura en la fase MVP. | `01 §3` | Despliegue contenedorizado | CONFIRMADO |

### 1.2.3 Necesidades de las empresas clientes
| ID | Necesidad | Fuente | Requisito candidato | Estado |
|---|---|---|---|---|
| N-04 | Separar funciones y responsabilidades por rol. | `D-02` | RBAC de cuatro roles | CONFIRMADO |
| N-05 | Saber quién modificó por última vez un APU o un insumo. | `D-08` | Campo de autoría por entidad | CONFIRMADO |
| N-06 | Aprobar el presupuesto según la política de cada proyecto. | `D-03` | Configuración de firma por proyecto | CONFIRMADO |
| N-07 | Cerrar el presupuesto solo con información completa y cotizada. | `D-04` | Regla de transición a `FINALIZADO` | CONFIRMADO |
| N-08 | Reutilizar APUs sin reconstruirlos. | `00 §1`, `D-05` | Copia de APU desde empresa y catálogo | CONFIRMADO |
| N-09 | Evitar duplicidad de insumos al copiar entre proyectos. | `D-05` | Detección de similitud con confirmación | CONFIRMADO |
| N-10 | Reutilizar composiciones complejas dentro de un proyecto. | `D-05`, `FR-004` | Insumo tipo Matriz | CONFIRMADO |
| N-11 | Acceder a composiciones técnicas de referencia. | `00 §1`, `D-05`, `D-12` | Catálogo abierto global | CONFIRMADO |
| N-12 | Calcular automáticamente consumos, costos y AIU. | `00 §7`, `D-06`, `D-07` | Motor de cálculo | PARCIAL (`X-15`) |
| N-13 | Configurar cada proyecto (divisa, AIU, tope, firma, requisito de auditor). | `00 §5`, `D-03`, `D-07`, `D-14` | Configuración de proyecto | CONFIRMADO |
| N-14 | Alertar sobre desviaciones del tope presupuestal. | `FR-009`, `D-18` | Alertas | CONFIRMADO |
| N-15 | Intercambiar información con Excel. | `FR-010`, `D-18` | Importación/exportación | EXCLUIDO DEL MVP |
| N-16 | Operar con una agilidad comparable a la hoja de cálculo. | `00 §8` | Criterio de usabilidad | CONFIRMADO como necesidad, no medible aún |
| N-17 | Auditar solo con el rol Auditor, sin importar si es interno o externo. | `Q-03`, `D-15` | Rol Auditor | CONFIRMADO |

### 1.2.4 Necesidades de la institución académica
| ID | Necesidad | Fuente | Requisito candidato | Estado |
|---|---|---|---|---|
| N-18 | Entregar un aplicativo funcional, con documentación completa del producto e implementación correcta de SDD. | `Q-02 = B`, `D-20` | Documento único + OpenSpec + producto operativo | CONFIRMADO |
| N-19 | Dejar evidencia del proceso de ingeniería (decisiones, especificaciones, pruebas). | `D-09`, `D-10` | Documentación viva + OpenSpec | CONFIRMADO como intención |
| N-20 | Ser demostrable y reproducible en un entorno controlado. | `01 §3`, `03 §6` | Despliegue con Docker Compose | CONFIRMADO |

**Cierre del punto:** `Q-02` y `Q-14` están respondidas. No existen métricas históricas del proceso (`D-19`), por lo que los criterios se apoyan en supuestos declarados.

---

## 1.3 Actores involucrados

### 1.3.1 Actores del sistema
| ID | Actor | Ámbito | Pertenencia | Estado |
|---|---|---|---|---|
| ACT-01 | Administrador | Plataforma (global) | Ninguna compañía, ningún proyecto | CONFIRMADO (`D-02`, `D-01`) |
| ACT-02 | Director de proyectos | Una compañía y sus proyectos | Máximo una compañía (`D-01`) | CONFIRMADO |
| ACT-03 | Presupuestador | Compañía y proyectos asignados | 0..1 compañía; 0..N proyectos | CONFIRMADO |
| ACT-04 | Auditor | Compañía y proyectos asignados | 0..N proyectos; puede ser interno o externo (`Q-03`) | CONFIRMADO |
| ACT-05 | Sistema | Automatizaciones: cálculo, copia de APUs, detección de similitud, reglas de estado y validación de cierre | No aplica | DERIVADO (necesario para `D-04`, `D-05`, `D-06`) |
| ACT-06 | Usuario sin asignación | Usuario registrado sin compañía o sin proyecto | Sin compañía | CONFIRMADO (`D-02` admite "ninguna compañía"); es el estado natural del usuario autorregistrado (`D-72`) |
| ACT-07 | Visitante (solicitante de registro) | Acceso público al autorregistro | Sin cuenta y sin sesión | CONFIRMADO (`D-72`) |

### 1.3.2 Matriz de capacidades
`sí` = permitido explícitamente · `no` = denegado explícitamente · `lectura` = solo consulta.

Las celdas se cerraron con la revisión de capítulos 1 y 2 (`D-34`); ya no quedan valores indeterminados. El **autorregistro no es una capacidad por rol**: es una acción **anónima**, disponible **sin sesión** (`D-72`, `ACT-07`), y por eso no figura en la matriz.

| Capacidad | ACT-01 Administrador | ACT-02 Director | ACT-03 Presupuestador | ACT-04 Auditor |
|---|---|---|---|---|
| Crear usuarios (alta administrativa) | sí (global) | no | no | no |
| Buscar usuarios | sí | sí (para gestionar su compañía y sus proyectos) | no | no |
| Cambiar rol de un usuario | sí (global) | sí, solo en su compañía y entre Director / Presupuestador / Auditor | no | no |
| Crear compañía | sí (ilimitadas) | sí (una sola, solo si no tiene) | no | no |
| Editar datos de la compañía | sí (solo registro: razón social, NIT) | sí (solo la propia) | no | no |
| Leer compañía | sí | sí (la propia) | sí (solo lectura) | sí (la de sus proyectos) |
| Crear / editar proyecto | no (lectura) | sí | no | no |
| Configurar proyecto (divisa, AIU, topes, firma, requisito de auditor) | no | sí | no | no |
| Crear / editar EDT | no | sí | sí (todas las características) | no (lectura) |
| Crear / editar APU | no (lectura) | sí | sí | no (lectura) |
| Crear / editar insumos | no (lectura) | sí | sí | no (lectura) |
| Añadir insumos a un APU | no | sí | sí | no |
| Asignar usuarios a proyectos | no | sí (solo usuarios de su compañía) | no | no |
| Añadir / eliminar usuarios de una compañía | sí (asignación a compañías, salvo rol Administrador) | sí (su compañía, si el usuario no está en otra) | no | no |
| Configurar el modo de firma | no | sí | no | no |
| Firmar APU | no | no | sí en modos 2b y 3 (`D-03`, `D-34`) | no |
| Aprobar APU (visto bueno) | no | no | no | sí (modo 3, sin editar contenido) |
| Cambiar insumos a `COTIZADO` | no | sí (masivo) | sí (masivo) | no |
| Ejecutar la transición a `FINALIZADO` | no | sí (único habilitado) | no | no |
| Consultar el catálogo abierto | sí (lectura) | sí | sí | sí (lectura) |
| Editar el perfil propio | sí | sí | sí | sí |

### 1.3.3 Relaciones entre actores
1. El Administrador designa a un usuario como Director de proyectos y puede cambiar el rol de cualquier usuario (`D-01`, `D-02`).
2. El Administrador crea compañías sin límite; un Director puede crear una única compañía y solo si no tiene una (`D-01`).
3. El Administrador asigna usuarios a compañías para todos los roles **excepto** Administrador (`D-02`).
4. El Director añade o elimina usuarios de su compañía, siempre que no estén asignados a otra (`D-02`).
5. El Director asigna usuarios de su compañía a sus proyectos (`D-02`).
6. Presupuestador y Auditor acceden únicamente a los proyectos a los que fueron asignados (`D-02`).
7. El Administrador no pertenece a ninguna compañía ni proyecto (`D-02`).
8. Cualquier persona puede **autorregistrarse sin sesión**; la cuenta nace sin compañía ni proyecto y queda a la espera de asignación (`D-72`).

### 1.3.4 Restricciones de pertenencia
- Un Director no puede pertenecer a más de una compañía (`D-01`).
- Un usuario no puede estar asignado a dos compañías (`D-02`).
- Un usuario puede existir sin compañía y sin proyecto (`D-02`, ACT-06).
- El autorregistro no exige sesión ni pertenencia previa; el rol inicial y la ausencia de compañía los fija el sistema (`D-72`).
- Presupuestador y Auditor pueden estar en ninguno o en varios proyectos (`D-02`).
- El Administrador nunca pertenece a una compañía ni a un proyecto (`D-02`).

### 1.3.5 Actores renombrados o descartados
| Documento base | Actor o rol previo | Tratamiento |
|---|---|---|
| `00 §3`, `02 §4` | `ADMIN` | Reemplazado por ACT-01, con permisos reducidos a lectura sobre el contenido técnico (`D-02`) |
| `00 §3` | `PROJECT_MANAGER` / Gerente de proyectos | Reemplazado por ACT-02 Director de proyectos |
| `02 §4`, `06 §1` | `ESTIMATOR` | Reemplazado por ACT-03 Presupuestador |
| `00 §3` | `READ_ONLY` | Reemplazado por ACT-04 Auditor |
| `00 §3` | "usuario firmador configurable" | Reemplazado por la configuración de firma por proyecto (`D-03`) |
| `06 §1` | Auditor "Interventor / Director" | Reemplazado; puede ser interno o externo, siempre con el rol Auditor (`D-15`) |

### 1.3.6 Vacíos del punto
No quedan vacíos abiertos de actores. Permanecen `X-21` (contenido publicado del insumo) y `X-22` (reutilización de insumos), que afectan al alcance y no a los roles.

## 1.4 Alcance y limitaciones

### 1.4.1 Capacidades incluidas
| ID | Capacidad | Fuente | Estado |
|---|---|---|---|
| S-01 | Identidad y acceso: autorregistro público, usuarios, roles y asignación a compañías y proyectos. | `D-01`, `D-02`, `D-72` | CONFIRMADO |
| S-02 | Compañías: creación y configuración; el Administrador solo edita datos de registro. | `D-01`, `D-02`, `D-15` | CONFIRMADO |
| S-03 | Proyectos y su configuración: divisa, AIU, tope presupuestal, modo de firma y requisito de visto bueno del auditor. | `D-02`, `D-03`, `D-07`, `D-14` | CONFIRMADO |
| S-04 | EDT recursiva: capítulos y subcapítulos sin límite de profundidad, ítems por nodo, numeración automática editable con herencia de prefijo, código único por proyecto y búsqueda parcial por código de ítem o nombre de capítulo. | `D-30`, `D-31` | CONFIRMADO |
| S-05 | Insumos por proyecto: siete tipos, incluida la Matriz, con estados de cotización, sugerencia de unicidad, reutilización desde otros proyectos de la compañía mediante copia independiente y listado con filtros. | `D-05`, `FR-004`, `Q-09`, `D-27`, `D-29` | CONFIRMADO |
| S-06 | APUs por proyecto: creación, edición, relación 1:1 con el ítem, copia desde APUs públicos o de la misma compañía sin restricción de estado y reconciliación de insumos al copiar. | `D-05`, `D-13`, `D-16`, `D-24`, `D-25` | CONFIRMADO |
| S-07 | Matrices: reutilizables dentro del proyecto y copiables a otro proyecto. | `D-05`, `D-16` | CONFIRMADO |
| S-08 | Catálogo abierto global de APUs e insumos, con publicación automática por proyecto y exposición de todos los campos del insumo, incluidos precio unitario, distribuidor y contacto. | `D-12`, `D-22`, `D-28` | CONFIRMADO |
| S-09 | Motor de cálculo: consumos, rendimientos, costos y AIU. | `D-06`, `D-07`, `05 §5` | PARCIAL (`X-15`) |
| S-10 | Aprobaciones y cierre: tres modos de firma, aprobación por APU y transición a `FINALIZADO`. | `D-03`, `D-04` | CONFIRMADO (`X-02`) |
| S-11 | Autoría y aprobación: último usuario que modificó un APU o un insumo, más un registro de aprobación por APU (último aprobador, rol y fecha) y el estado del presupuesto. Sin historial de valores ni de aprobaciones. | `D-08`, `D-52` | CONFIRMADO |
| S-12 | Alertas por superación del tope presupuestal. | `FR-009`, `D-18` | CONFIRMADO |
| S-13 | Importación y exportación de presupuestos (fuera del MVP; evolución posterior). | `FR-010`, `D-18` | EXCLUIDO DEL MVP |
| S-14 | Búsqueda, sugerencia de similitud y reconciliación de insumos al incorporar un APU. | `FR-007`, `Q-09`, `D-25` | CONFIRMADO |
| S-15 | Notificaciones transaccionales por correo: creación de cuenta y recuperación de contraseña. | `D-17` | CONFIRMADO |
| S-16 | Conteo de uso de cada insumo en el proyecto: suma de `consumo × cantidad de obra` por ítem. | `D-33` | CONFIRMADO |

### 1.4.2 Fuera de alcance
| ID | Exclusión | Fuente | Estado |
|---|---|---|---|
| E-01 | Asignación automática de precios, cantidades o rendimientos por IA. | `00 §6` | CONFIRMADO |
| E-02 | Scraping o comparación de precios de proveedores en tiempo real. | `00 §6` | CONFIRMADO |
| E-03 | Ejecución de obra: inventarios, compras, facturación y control de materiales en obra. | `00 §6` | CONFIRMADO |
| E-04 | Visores 3D e integración CAD/BIM. | `00 §6` | CONFIRMADO |
| E-05 | Historial de cambios y de valores anteriores (auditoría detallada). | `D-08`, `D-11` | CONFIRMADO |
| E-06 | Conversión automática de divisas. | `05 §4` | CONFIRMADO |
| E-07 | Aplicación móvil y operación sin conexión. Fuera de alcance por defecto. | No documentado | DERIVADO |
| E-08 | Integraciones con ERP o contabilidad. Fuera de alcance por defecto. | No documentado | DERIVADO |
| E-09 | Notificaciones por correo distintas de la creación de cuenta y la recuperación de contraseña. | `D-17` | CONFIRMADO |
| E-10 | Auditoría ejercida por usuarios sin el rol Auditor. | `Q-03` | CONFIRMADO |
| E-11 | Autoregistro de usuarios. | DERIVADO de `D-01`, `D-02` | **OBSOLETO por `D-72`:** el autorregistro entra en alcance |
| E-12 | Congelamiento o versionado del presupuesto al finalizar. | `Q-01 = A` (no incluye snapshot) | DERIVADO |
| E-13 | Importación y exportación de presupuestos en el MVP. | `D-18` | CONFIRMADO |

### 1.4.3 Restricciones del producto
| ID | Restricción | Fuente | Estado |
|---|---|---|---|
| R-01 | Un APU pertenece a un único proyecto. | `D-05` | CONFIRMADO |
| R-02 | Los insumos son propios de cada proyecto, pueden reutilizarse desde otros proyectos de la compañía mediante copia independiente y su unicidad es una sugerencia, no una restricción. | `D-05`, `Q-09`, `D-27`, `D-29` | CONFIRMADO |
| R-03 | La copia de un APU es independiente de su origen, salvo cuando proviene del catálogo abierto, donde conserva un vínculo de procedencia. | `D-16`, `Q-16` | CONFIRMADO (`X-19`) |
| R-04 | El AIU se define únicamente a nivel de proyecto. | `D-07` | CONFIRMADO |
| R-05 | Una sola divisa por proyecto, sin conversión de valores. | `05 §4` | CONFIRMADO |
| R-06 | La transición a `FINALIZADO` exige el 100 % de insumos en `COTIZADO`. | `D-04` | CONFIRMADO |
| R-07 | Solo se conserva el último usuario modificador; no hay historial de acciones ni de valores. | `D-08` | CONFIRMADO |
| R-08 | El Administrador no modifica contenido técnico (proyectos, APUs, insumos). | `D-02` | CONFIRMADO |
| R-09 | Un Director de proyectos no puede tener más de una compañía. | `D-01` | CONFIRMADO |
| R-10 | Un usuario no puede estar asignado a dos compañías. | `D-02` | CONFIRMADO |
| R-11 | La Matriz no puede anidarse dentro de otra Matriz. | `FR-004`, `BR-008` | DERIVADO |
| R-12 | El despliegue objetivo es un único servidor Linux con Docker Compose. | `03 §6` | CONFIRMADO |
| R-13 | Un APU se vincula a un único ítem de la EDT. | `D-16`, `Q-15` | CONFIRMADO |
| R-14 | No se pueden consultar ni copiar APUs privados de otras compañías. | `D-13` | CONFIRMADO |
| R-15 | La publicación es por proyecto: un proyecto público expone todos sus APUs e insumos. | `D-22` | CONFIRMADO |
| R-16 | La copia de APUs no está restringida por el estado del proyecto. | `D-24` | CONFIRMADO |
| R-17 | La copia de un insumo de la compañía es independiente: los cambios de precio no se propagan a otros proyectos. | `D-29` | CONFIRMADO |
| R-18 | La EDT no tiene límite de profundidad ni de cantidad de hijos por nodo. | `D-30` | CONFIRMADO |
| R-19 | Los códigos del EDT son únicos por proyecto entre capítulos e ítems, sin distinguir mayúsculas. Conjunto cerrado permitido: `A-Z`, `0-9`, `.`, `-`, `_`, `/`, `*`, `=`, `(`, `)`, `%`, `&`, `#`, `¡`, `'`, `?`, `¿`; 1–10 caracteres en capítulos y 1–5 en ítems, medidos sobre el segmento ingresado. Se permite `.` porque la jerarquía se determina por la relación padre-hijo, no por el texto. | `D-30`, `D-34` | CONFIRMADO |
| R-20 | El APU no almacena código; el código pertenece al ítem de la EDT y se muestra en la vista del APU. | `D-31` | CONFIRMADO |
| R-21 | El conteo de uso de un insumo es la suma de `consumo × cantidad de obra` de todos los ítems que lo usan. | `D-33` | CONFIRMADO |
| R-22 | El despliegue público exige HTTPS; el MVP se opera en entorno local o demo controlada. | `D-39` | CONFIRMADO |
| R-23 | El catálogo público es anónimo y no permite exportación masiva. | `D-38` | CONFIRMADO |
| R-24 | El autorregistro es público y anónimo: no requiere sesión ni pertenencia previa y crea la cuenta sin compañía y sin proyecto. | `D-72` | CONFIRMADO |
| R-25 | El nombre de usuario (`username`) es obligatorio y único en toda la plataforma. | `D-73` | CONFIRMADO |

### 1.4.4 Supuestos
| ID | Supuesto | Fuente | Estado |
|---|---|---|---|
| A-01 | Las empresas aceptan publicar composiciones técnicas sin precios. | `00 §8` | CONFIRMADO como supuesto |
| A-02 | El catálogo abierto se puebla automáticamente con lo que las empresas publican; no hay curaduría. | `D-12` | CONFIRMADO |
| A-03 | Los usuarios se crean de dos formas: por autorregistro público y anónimo, y por alta administrativa del Administrador. | `D-01`, `D-02`, `D-72` | CONFIRMADO |
| A-04 | El desarrollo lo realiza una sola persona. | DERIVADO de `D-10` | POR CONFIRMAR |
| A-05 | Se dispone de un servidor Linux con Docker. | `03 §6` | CONFIRMADO |
| A-06 | No hay integraciones externas obligatorias en el MVP. | DERIVADO | POR CONFIRMAR |
| A-07 | Cada proyecto opera con una única moneda. | `05 §4` | CONFIRMADO |
| A-08 | No existen requisitos legales de firma electrónica certificada. | `X-02` | POR CONFIRMAR |
| A-09 | El volumen inicial es bajo (una empresa piloto o pocas). | DERIVADO | POR CONFIRMAR |

### 1.4.5 Frontera del MVP (provisional)
| Capacidad | ¿MVP? | Fundamento | Estado |
|---|---|---|---|
| Identidad, tenancy y roles | Sí | Núcleo de `D-01`, `D-02` | CONFIRMADO |
| Proyectos, EDT, insumos, APUs y matrices | Sí | Núcleo de `D-05` | CONFIRMADO |
| Motor de cálculo | Sí | `00 §7`, `D-06`, `D-07` | CONFIRMADO como necesidad |
| Autoría y aprobaciones por APU | Sí | `D-08`, `D-11` | CONFIRMADO |
| Cierre con el 100 % de insumos cotizados | Sí | `D-04` | CONFIRMADO |
| Los tres modos de firma | Sí | `D-03`, `D-14` | CONFIRMADO |
| Catálogo abierto | Sí | `D-12` | CONFIRMADO |
| Alertas de tope presupuestal | Sí | `D-18` | CONFIRMADO |
| Notificaciones por correo (dos casos) | Sí | `D-17` | CONFIRMADO |
| Autorregistro público de usuarios | Sí | `D-72` | CONFIRMADO |
| Importación y exportación | No | `D-18` | EXCLUIDO |

> **Frontera cerrada** por `D-18` (alertas sí; importación y exportación no) y por `D-03`/`D-14` (los tres modos de firma no se fraccionan).

### 1.4.6 Criterios de aceptación del capítulo 1
El capítulo se considera cerrado cuando:

1. Cada problema (`P`) tiene fuente y estado; las hipótesis (`H`) están separadas y no se usan como requisitos.
2. Cada necesidad (`N`) se mapea a una capacidad incluida (`S`), a una exclusión (`E`) o queda marcada como no requisito.
3. Cada actor (`ACT`) tiene ámbito, pertenencia, capacidades y restricciones, **sin contradicciones abiertas de autorización**.
4. El alcance tiene capacidades incluidas, exclusiones y frontera de MVP decidida.
5. `X-01` a `X-27` están resueltos o aceptados explícitamente como riesgo.
6. `Q-01` a `Q-63` están respondidas o diferidas con impacto declarado.
7. Los requisitos académicos están incorporados (`N-18`, `D-20`).

## 1.5 Riesgos del capítulo
| ID | Riesgo | Origen | Impacto |
|---|---|---|---|
| RT-01 | La ausencia de historial de valores puede debilitar el problema original y la evaluación del proyecto; se mitiga conservando las aprobaciones por APU. | `D-08`, `D-11` ↔ `00 §2` | Medio |
| RT-02 | Las fórmulas de `D-06` no son dimensionalmente verificables, lo que impide criterios de aceptación del motor. | `X-15` | Alto |
| RT-03 | El modelo de permisos (4 roles × 3 ámbitos × asignaciones) es costoso de implementar y de probar. | `D-01`, `D-02` | Medio |
| RT-04 | La duplicación de APUs e insumos por proyecto incrementa el volumen de datos y la divergencia entre proyectos. | `D-05` | Medio |
| RT-05 | El catálogo abierto no tiene curaduría ni criterios de calidad: puede llenarse de composiciones incompletas o duplicadas. | `D-12` | Medio |
| RT-06 | El auditor interno puede tener conflicto de interés; el sistema no lo impide. | `Q-03` | Medio |
| RT-07 | El alcance (tres modos de firma + matrices + catálogo + siete tipos de insumo) es amplio para un solo desarrollador. | `D-03`, `D-05`, `D-10` | Alto |
| RT-08 | Sin métricas del problema, los criterios de éxito no serán medibles. | 1.1.6 | Medio |
| RT-09 | Las funcionalidades excluidas del MVP (importación/exportación) pueden reingresar sin control de alcance. | `D-18` | Bajo |
| RT-10 | La publicación automática y sin curaduría puede exponer información comercial si un usuario marca público un proyecto con datos sensibles. | `D-12` | Alto |
| RT-11 | Publicar todos los campos del insumo expone precios, distribuidores y contactos. Riesgo aceptado en `D-28`; exige advertir al usuario al publicar y registrar su aceptación. | `D-28` | Alto |
| RT-12 | Si la reutilización de insumos de la compañía se resuelve como referencia compartida, un cambio de precio afectaría varios proyectos sin control. | `D-27` | Medio |

## 1.6 Preguntas y respuestas
| ID | Pregunta | Bloquea | Estado |
|---|---|---|---|
| Q-01 | ¿Qué debe conservar exactamente la autoría: solo el último modificador, o también los registros de aprobación y el congelamiento del presupuesto? | P-01, S-11, `X-01` | **Respondida (A) → `D-11`** |
| Q-02 | ¿Cuáles son los entregables, fechas y criterios de evaluación de la institución académica? | N-18, `X-14` | **Respondida (B) → `D-20`** |
| Q-03 | ¿El Auditor es interno de la compañía o externo a ella? | `X-09`, E-10 | **Respondida (C) → `D-15`; abre `X-16`** |
| Q-04 | ¿Qué rol ejecuta la transición a `FINALIZADO` y quién puede ser el firmante único del modo 2? | `X-02` | **Respondida → `D-14`; el firmante único pasa a `Q-21`** |
| Q-05 | ¿Qué rol cambia los insumos a `COTIZADO` de forma masiva? | `D-04` | **Respondida (C) → `D-15`** |
| Q-06 | ¿El Presupuestador crea capítulos e ítems o solo modifica los existentes? | `X-13` | **Respondida (A) → `D-15`** |
| Q-07 | ¿Qué datos de la compañía puede editar el Administrador? | `X-08` | **Respondida (B) → `D-15`** |
| Q-08 | ¿Cómo funciona el catálogo abierto: quién lo cura, incluye precios, cómo se relaciona con `Is_Public`? | `X-06`, S-08 | **Respondida (B) → `D-12`; abre `X-17`** |
| Q-09 | ¿La unicidad de insumos por proyecto es una restricción técnica o un criterio de interfaz? | `X-10` | **Respondida (sugerencia) → `D-16`** |
| Q-10 | ¿Bajo qué condiciones se copia un APU entre proyectos? | `X-11` | **Respondida → `D-13`; abre `X-18`** |
| Q-11 | ¿Una Matriz puede reutilizarse entre proyectos o solo dentro del proyecto? | `X-12`, S-07 | **Respondida (A + copia) → `D-16`** |
| Q-12 | ¿Las alertas de tope y la importación/exportación pertenecen al MVP? | S-12, S-13 | **Respondida (solo alertas) → `D-18`** |
| Q-13 | ¿Las notificaciones por correo están en alcance? | E-09 | **Respondida → `D-17`** |
| Q-14 | ¿Existen datos reales del proceso actual (tiempos, errores, volúmenes)? | 1.1.6 | **Respondida (C) → `D-19`** |
| Q-15 | ¿Un APU puede alimentar más de un ítem del mismo proyecto? | `X-04`, S-06 | **Respondida (B) → `D-16`** |
| Q-16 | ¿La copia de un APU conserva relación con su origen? | R-03 | **Respondida (D) → `D-16`; abre `X-19`** |
| Q-17 | ¿Quién puede cambiar el rol de un usuario? | `X-16` | **Respondida (B) → `D-21`** |
| Q-18 | ¿La publicación es por proyecto, por APU, por insumo o por combinación? | `X-17` | **Respondida (A) → `D-22`** |
| Q-18b | ¿Qué datos del insumo se publican? | `X-21` | **Respondida (D) → `D-23`; precisión en `Q-22`** |
| Q-19 | ¿Se puede copiar APUs hacia o desde proyectos en estado `FINALIZADO`? | `X-18` | **Respondida (A) → `D-24`** |
| Q-20 | ¿Qué efecto tiene el vínculo de procedencia del catálogo? | `X-19` | **Respondida → `D-25`** |
| Q-21 | En el modo 2 con firmante único, ¿quién puede ser designado y qué cubre su firma? | `X-20` | **Respondida (A) → `D-26`** |
| Q-22 | ¿"Todos los campos del insumo" incluye el precio unitario y los datos de distribuidor y contacto? | `X-21` | **Respondida (A) → `D-28`** |
| Q-23 | ¿La reutilización de insumos de la compañía en un proyecto nuevo copia el insumo o referencia el registro de la compañía? | `X-22` | **Respondida (A) → `D-29`** |
| Q-63 | ¿El autorregistro de usuarios forma parte del sistema? ¿Se exige un nombre de usuario? ¿Se requiere sesión previa para registrarse? | S-01, `X-27` | **Respondida (A, 29/10/2026) → `D-72` y `D-73`:** el autorregistro existe, es anónimo y exige nombre de usuario |

> Las preguntas `Q-24` a `Q-26` se registran y responden en 2.5.

## 1.7 Registro de cambios
| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 27/10/2026 | Creación del documento. Capítulo 1 en borrador. Se registran `D-01`…`D-10`, `X-01`…`X-15` y `Q-01`…`Q-16`. |
| 0.2 | 27/10/2026 | Q-01 respondida (A). Se agrega `D-11`, se resuelve `X-01` y se ajustan P-01, S-11, E-05, E-12 y RT-01. |
| 0.3 | 27/10/2026 | Q-02 a Q-16 respondidas. Se agregan `D-12`…`D-20`, se resuelven `X-04`…`X-14` y se abren `X-16`…`X-20` y `Q-17`…`Q-21`. |
| 0.4 | 27/10/2026 | Q-17 a Q-21 respondidas y nota sobre reutilización de insumos. Se agregan `D-21`…`D-27`, se resuelven `X-16`…`X-20` y se abren `X-21`, `X-22`, `Q-22` y `Q-23`. |
| 0.5 | 27/10/2026 | Q-22 y Q-23 respondidas. Se agregan `D-28` y `D-29`, se resuelven `X-21` y `X-22`, y se cierra el capítulo 1. |
| 0.6 | 27/10/2026 | Capítulo 2 en borrador: `RF-01`…`RF-19`, módulos `M-01`…`M-09` e incrementos `I-1`…`I-6`. Se abre `X-23` y las preguntas `Q-24` y `Q-25`. |
| 0.7 | 27/10/2026 | EDT definida (`D-30`, `D-31`), base de cálculo por tipo (`D-32`) y conteo de uso (`D-33`). Se agregan `S-16`, `R-18`…`R-21`, `RF-20`…`RF-22`; se dividen `I-3` e `I-4`. `X-23`, `Q-24` y `Q-25` cerradas; se abre `X-24` y `Q-26`. |
| 0.8 | 27/10/2026 | Q-26 respondida (A): los hijos heredan el código del padre como prefijo. `X-24` resuelto y capítulo 2 cerrado. |
| 0.9 | 27/10/2026 | Revisión de capítulos 1 y 2 aplicada: `D-34` y `D-35`; conteo de uso a `M-06`; copia pública en `I-5`; reglas de código cerradas; matriz de actores sin celdas indeterminadas; registro de preguntas consolidado. |
| 0.10 | 27/10/2026 | Capítulo 3 iniciado: 3.1 arquitectura y 3.2 stack, con correcciones a `04` (EF Core 10, FluentValidation, Spec Kit obsoleto) y apertura de `Q-27`…`Q-38`. |
| 0.11 | 27/10/2026 | Q-27, Q-28 y Q-30 a Q-38 respondidas (`D-36`…`D-46`); 3.1 y 3.2 actualizados con sus decisiones; `Q-29` queda abierta. |
| 0.12 | 27/10/2026 | Q-29 respondida (A) → `D-47` (proyecciones EF). 3.1 y 3.2 cerrados. |
| 0.13 | 27/10/2026 | 3.3 persistencia redactada: modelo actualizado (`User.company_id` nullable, `WbsNode`, `ApuApproval`, `MatrixComponent`, `ProjectMember`), eliminación de `AuditLog` y `BudgetVersion`, índices y runbook de migraciones. Se abre `Q-39`…`Q-43` y `X-25`. |
| 0.14 | 27/10/2026 | Q-40, Q-42 y Q-43 respondidas (`D-48`…`D-50`). Quedan abiertas `Q-39` (modelo del EDT) y `Q-41` (aprobaciones). |
| 0.15 | 27/10/2026 | Q-39 = B (`D-51`: `WbsCode` + tablas separadas) y Q-41 = C (`D-52`: aprobación única por APU). Se ajustan `D-03`, `D-11`, `D-14`, S-11, RF-13 y RF-21; se abre `Q-44`. |
| 0.16 | 27/10/2026 | Q-44 = A (`D-53`: editar un APU aprobado elimina su aprobación). 3.3 cerrada. |
| 0.17 | 27/10/2026 | 3.4 organización de componentes y 3.5 comparación de seis decisiones técnicas con alternativas. Se abren `Q-45`…`Q-48`. |
| 0.18 | 27/10/2026 | Q-45 = A (`D-54`), Q-47 = B (`D-55`, JSON `camelCase`) y Q-48 = A (`D-56`, cliente generado). Queda `Q-46`. |
| 0.19 | 27/10/2026 | Q-46 = D (`D-57`: estado híbrido con signals y estado local). Capítulo 3 cerrado. |
| 0.20 | 27/10/2026 | Capítulo 4: metodología SDD/OpenSpec, etapas E-0…E-6, orden de funcionalidades, dependencias y criterios de terminado. Se abren `Q-49`…`Q-53`. |
| 0.21 | 27/10/2026 | Q-49 = A (`D-58`), Q-50 = B (`D-59`, GitFlow), Q-51 = A (`D-60`), Q-52 = A (`D-61`), Q-53 = A (`D-62`). Capítulo 4 cerrado. |
| 0.22 | 27/10/2026 | Capítulo 5: tipos de prueba, pirámide, riesgos `RT-13`…`RT-21`, mitigaciones, matriz RF→prueba y verificación de NFR/BR. Se abren `Q-54`…`Q-57`. |
| 0.23 | 27/10/2026 | Q-54 = A (`D-63`, NetArchTest), Q-55 = D (`D-64`, sin prueba de rendimiento) y Q-56 = C (`D-65`, E2E manual). Se abre `Q-58` sobre `NFR-003`. |
| 0.24 | 27/10/2026 | Q-57 manual (`D-66`: integración manual con SQL local) y Q-58 = B (`D-67`: `NFR-003` eliminado). Capítulo 5 cerrado. |
| 0.25 | 27/10/2026 | Capítulo 6: OpenSpec como fuente de verdad, derivación de especificaciones, delegación a la IA, verificación del código y decisiones del estudiante. Se abren `Q-59`…`Q-62`. |
| 0.26 | 27/10/2026 | Q-59 = C (`D-68`), Q-60 = A (`D-69`), Q-61 = C (`D-70`) y Q-62 = A (`D-71`). Capítulo 6 cerrado; documento completo. |
| 0.27 | 27/10/2026 | Documentos base actualizados: `00`–`06` reescritos y `09` reemplazado por `09-operacion-openspec.md`. |
| 0.28 | 29/10/2026 | Autorregistro incorporado al alcance: se agregan `D-72` y `D-73`, se resuelve `X-27`, se registra `Q-63`, `E-11` queda `OBSOLETO` y `A-03` se confirma en su forma nueva. Se añaden `S-01` ampliada, `R-24`, `R-25`, `ACT-07`, `RF-23` y su trazabilidad en capítulos 2 a 6. |

---

**Capítulo 1 cerrado** (v0.5). Las preguntas `Q-01` a `Q-26` están respondidas y las contradicciones `X-01` a `X-24` están resueltas o aceptadas como riesgo. Ver capítulo 2.

---

# 2. Propuesta de solución

> **Estado del capítulo:** cerrado (v0.8). Recoge la solución derivada del capítulo 1; las prioridades y los incrementos fueron aprobados (`Q-25`). Todo lo marcado `DERIVADO` requiere validación antes de pasar a OpenSpec.

## 2.1 Descripción general de la solución

### 2.1.1 Enunciado
Budget Civil es una plataforma web multiempresa que sustituye la gestión de presupuestos de obra civil en hojas de cálculo por un flujo estructurado y verificable: la empresa organiza su EDT, administra insumos propios y APUs vinculados uno a uno con las actividades, calcula el presupuesto automáticamente, gestiona la cotización y las firmas de aprobación, y puede publicar sus composiciones en un catálogo abierto (`D-01`, `D-05`, `D-06`, `D-11`, `D-12`, `D-22`).

La solución no incorpora inteligencia que decida precios ni cantidades: automatiza el cálculo, la integridad y la trazabilidad de lo que define el usuario (`E-01`).

### 2.1.2 Flujo operativo de extremo a extremo
| # | Paso | Actor | Base |
|---|---|---|---|
| 1 | Autorregistro del usuario en un endpoint público (sin sesión) y, por separado, alta administrativa de usuarios y compañías y designación de Directores | Visitante y Administrador | `D-01`, `D-02`, `D-21`, `D-72` |
| 2 | Configuración de la compañía y creación del proyecto: divisa, AIU, tope, modo de firma, requisito de auditor y visibilidad | Director | `D-03`, `D-07`, `D-18`, `D-22` |
| 3 | Asignación de Presupuestadores y Auditores de la misma compañía | Director | `D-15` |
| 4 | Construcción de la EDT: capítulos, subcapítulos e ítems | Presupuestador o Director | `Q-06`, `S-04` |
| 5 | Registro de insumos del proyecto o copia desde otro proyecto de la compañía | Presupuestador o Director | `D-05`, `D-27`, `D-29` |
| 6 | Creación del APU o copia desde un APU público o de un proyecto de la compañía, con reconciliación de insumos | Presupuestador o Director | `D-13`, `D-16`, `D-25` |
| 7 | Cálculo automático de consumos, costo directo y AIU | Sistema | `D-06`, `D-07` |
| 8 | Cotización de insumos, incluido el cambio masivo a `COTIZADO` | Director y Presupuestador | `D-04`, `D-15` |
| 9 | Firma por APU según el modo configurado y aprobación del Auditor cuando aplique | Presupuestador y Auditor | `D-03`, `D-26` |
| 10 | Cierre a `FINALIZADO` cuando todo está cotizado y firmado | Director | `D-04`, `D-14` |
| 11 | Exposición en el catálogo abierto si el proyecto es público | Sistema | `D-12`, `D-22`, `D-28` |

### 2.1.3 Aporte frente al proceso actual
| Necesidad | Cómo la resuelve la solución |
|---|---|
| N-04 Separación de funciones | Cuatro roles con asignación por compañía y por proyecto (`D-15`, `D-21`) |
| N-05 Autoría | Último usuario modificador por APU e insumo (`D-08`) |
| N-06 Aprobación según política | Configuración de firma por proyecto con tres modos (`D-03`, `D-26`) |
| N-07 Cierre con información completa | Transición a `FINALIZADO` solo con el 100 % de insumos `COTIZADO` (`D-04`) |
| N-08 Reutilización de APUs | Copia desde APUs públicos o de la compañía, con reconciliación de insumos (`D-13`, `D-25`) |
| N-09 Duplicidad de insumos | Sugerencia de similitud y decisión del usuario (`D-25`) |
| N-10 Composiciones complejas | Insumo tipo Matriz reutilizable en varios APUs (`D-05`, `D-16`) |
| N-11 Referencias del sector | Catálogo abierto con publicación automática por proyecto (`D-12`, `D-22`) |
| N-12 Cálculo automático | Motor único de cálculo con AIU por proyecto (`D-06`, `D-07`) |

### 2.1.4 Límites de la solución
No forman parte de la solución: precios de mercado o scraping (`E-02`), ejecución de obra (`E-03`), CAD/BIM (`E-04`), historial de valores (`E-05`), conversión de divisas (`E-06`), importación/exportación en el MVP (`E-13`) y asignación automática por IA (`E-01`).

> **Precisión importante:** el sistema **publica** precios registrados por el usuario (`D-28`), pero **no los obtiene, compara ni actualiza** desde fuentes externas.

### 2.1.5 Flujo de autorregistro (`D-72`, `D-73`)

| # | Paso | Quién | Regla |
|---|---|---|---|
| 1 | Abrir el formulario público de registro | Visitante (`ACT-07`) | El endpoint es anónimo: no exige token, sesión ni pertenencia previa (`D-72`) |
| 2 | Declarar identidad y credenciales | Visitante | Obligatorios: **nombre de usuario** (`D-73`), correo electrónico y contraseña |
| 3 | Validar unicidad | Sistema | `username` y `email` son únicos, sin distinguir mayúsculas (`3.3.3`) |
| 4 | Crear la cuenta | Sistema | Sin compañía y sin proyecto; el rol inicial lo fija el sistema (`DERIVADO`, pendiente de validación explícita) |
| 5 | Enviar el correo de creación de cuenta | Sistema | Es uno de los dos únicos correos del sistema (`D-17`, `RF-18`) |
| 6 | Asignar compañía y proyectos | Administrador o Director | Pasos posteriores al registro (`D-34`, `1.3.3`) |

**Reglas del flujo:**
1. El autorregistro **no crea compañía**, no asigna proyectos y no permite elegir rol, compañía ni proyecto desde el cliente (`D-72`).
2. Un usuario autorregistrado y aún no asignado no puede consultar ni modificar datos de ninguna compañía (`R-10`, `R-14`).
3. El **alta administrativa** del Administrador (`RF-01`) sigue existiendo en paralelo: el autorregistro no la reemplaza (`D-02`, `D-34`).
4. Toda cuenta creada por autorregistro requiere después una asignación explícita para operar dentro de un tenant (`1.3.4`, `D-34`).

## 2.2 Funcionalidades prioritarias

### 2.2.1 Criterio de priorización
| Prioridad | Definición |
|---|---|
| Alta | Sin la funcionalidad no se puede completar el flujo proyecto → EDT → APU → cálculo → cotización → cierre. |
| Media | Necesaria para operar en condiciones reales, pero no bloquea el primer flujo completo. |
| Baja | Evolución posterior al MVP. |

La frontera del MVP está fijada por `D-18` (alertas sí; importación/exportación no), `D-17` (solo dos correos) y `D-14` (cierre por el Director). La prioridad y la secuencia fueron aprobadas en `Q-25`.

### 2.2.2 Catálogo de funcionalidades
| ID | Funcionalidad | Módulo | Prioridad | MVP | Fuente |
|---|---|---|---|---|---|
| RF-01 | Usuarios, roles y asignación a compañías y proyectos | M-01 | Alta | Sí | `S-01`, `D-15`, `D-21` |
| RF-02 | Compañías: creación y datos de registro | M-01 | Alta | Sí | `S-02` |
| RF-03 | Configuración del proyecto: divisa, AIU, tope, modo de firma, requisito de auditor y visibilidad | M-02 | Alta | Sí | `S-03`, `D-03`, `D-07`, `D-18` |
| RF-04 | EDT recursiva con numeración automática editable, códigos únicos y búsqueda parcial por código de ítem o nombre de capítulo | M-03 | Alta | Sí | `S-04`, `D-30`, `D-31` |
| RF-05 | Insumos del proyecto: siete tipos, estados de cotización y sugerencia de unicidad | M-04 | Alta | Sí | `S-05`, `D-05`, `Q-09` |
| RF-06 | Reutilización de insumos de la compañía mediante copia independiente | M-04 | Media | Sí | `S-05`, `D-27`, `D-29` |
| RF-07 | APUs por proyecto con relación 1:1 con el ítem; creación y copia desde proyectos de la compañía (I-3b) y desde el catálogo público (I-5) | M-05 | Alta | Sí | `S-06`, `D-13`, `D-16`, `D-24` |
| RF-08 | Reconciliación de insumos al incorporar un APU (agregar, sugerir, decidir) | M-05 | Alta | Sí | `S-06`, `D-25` |
| RF-09 | Matrices reutilizables dentro del proyecto y copiables | M-05 | Alta | Sí | `S-07`, `D-16` |
| RF-10 | Motor de cálculo: consumos, costo directo, AIU y redondeo | M-06 | Alta | Sí | `S-09`, `D-06`, `D-07` |
| RF-11 | Cotización de insumos y cambio masivo a `COTIZADO` | M-07 | Alta | Sí | `S-10`, `D-04`, `D-15` |
| RF-12 | Firma y aprobación por APU según los tres modos configurados | M-07 | Alta | Sí | `S-10`, `D-03`, `D-26` |
| RF-13 | Registro único de aprobación por APU (último aprobador, rol y fecha) | M-07 | Alta | Sí | `S-11`, `D-52` |
| RF-14 | Autoría de la última modificación en APU e insumo | M-07 | Media | Sí | `S-11`, `D-08` |
| RF-15 | Catálogo abierto: consulta y copia de APUs e insumos publicados, de forma independiente | M-08 | Media | Sí | `S-08`, `D-12`, `D-22`, `D-28`, `D-35` |
| RF-16 | Búsqueda y sugerencia de similitud de insumos | M-04 | Media | Sí | `S-14`, `D-25` |
| RF-17 | Alertas por superación del tope presupuestal | M-09 | Media | Sí | `S-12`, `D-18` |
| RF-18 | Notificaciones transaccionales: creación de cuenta y recuperación de contraseña | M-01 | Media | Sí | `S-15`, `D-17` |
| RF-19 | Importación y exportación de presupuestos | — | Baja | No | `S-13`, `D-18` |
| RF-20 | Listado y filtrado de insumos del proyecto | M-04 | Alta | Sí | `S-05`, `FR-007` |
| RF-21 | Transición a `FINALIZADO` con el 100 % cotizado y la aprobación exigida | M-07 | Alta | Sí | `S-10`, `D-04`, `D-14` |
| RF-22 | Conteo de uso de insumos en el proyecto | M-06 | Media | Sí | `S-16`, `D-33` |
| RF-23 | Autorregistro público de usuarios con nombre de usuario y sin sesión previa | M-01 | Alta | Sí | `S-01`, `S-15`, `D-72`, `D-73` |

### 2.2.3 Resumen
| Prioridad | Cantidad | Funcionalidades |
|---|---|---|
| Alta | 15 | RF-01, RF-02, RF-03, RF-04, RF-05, RF-07, RF-08, RF-09, RF-10, RF-11, RF-12, RF-13, RF-20, RF-21, RF-23 |
| Media | 7 | RF-06, RF-14, RF-15, RF-16, RF-17, RF-18, RF-22 |
| Baja | 1 | RF-19 |
| Total | 23 | 22 en el MVP; 1 excluida (`RF-19`) |

> **Trazabilidad:** toda capacidad `S-01`…`S-16` queda cubierta por al menos una `RF`; `S-13` queda fuera del MVP (`E-13`).

## 2.3 Organización del sistema en módulos

### 2.3.1 Criterio de organización
Los módulos se definen por **responsabilidad funcional y dato principal**, no por capa técnica (la organización en capas se trata en el capítulo 3). Cada módulo agrupa funcionalidades que comparten un mismo núcleo de datos y reglas.

### 2.3.2 Módulos funcionales
| ID | Módulo | Responsabilidad | Capacidades | Funcionalidades |
|---|---|---|---|---|
| M-01 | Acceso y organización | Autorregistro, identidad, roles, asignación a compañías y proyectos, datos de registro de la compañía y correos transaccionales | `S-01`, `S-02`, `S-15` | RF-01, RF-02, RF-18, RF-23 |
| M-02 | Proyectos y configuración | Alta del proyecto y parámetros: divisa, AIU, tope, modo de firma, requisito de auditor y visibilidad | `S-03` | RF-03 |
| M-03 | EDT | Estructura recursiva, numeración y códigos únicos, búsqueda por código o nombre, orden y cantidades de obra | `S-04` | RF-04 |
| M-04 | Insumos | Registro, tipología, estados de cotización, listado con filtros, búsqueda, sugerencia de similitud y copia desde otros proyectos | `S-05`, `S-14` | RF-05, RF-06, RF-16, RF-20 |
| M-05 | APUs y matrices | Composición del APU, vínculo 1:1 con el ítem, matrices, copia y reconciliación de insumos | `S-06`, `S-07` | RF-07, RF-08, RF-09 |
| M-06 | Cálculo | Consumos, costo directo, AIU, redondeo y conteo de uso de insumos | `S-09`, `S-16` | RF-10, RF-22 |
| M-07 | Aprobación y cierre | Cotización, firmas por APU, registro de aprobaciones, autoría y transición a `FINALIZADO` | `S-10`, `S-11` | RF-11, RF-12, RF-13, RF-14 |
| M-08 | Catálogo abierto | Consulta y copia de APUs e insumos publicados por proyectos públicos | `S-08` | RF-15 |
| M-09 | Alertas | Evaluación del tope presupuestal y generación de alertas | `S-12` | RF-17 |

### 2.3.3 Servicios transversales
| Servicio | Consumido por | Regla |
|---|---|---|
| Autorización por compañía y proyecto | M-01 a M-09 | Toda operación valida pertenencia del usuario a la compañía y, cuando aplica, asignación al proyecto (`D-13`, `R-10`, `R-14`). Única excepción deliberada: el autorregistro, que es anónimo (`D-72`) |
| Catálogos maestros (roles, tipos de insumo, país y ciudad) | M-01, M-04 | Administrados por el sistema; no se crean desde módulos transaccionales |
| Motor de cálculo | M-04 (solo lectura del conteo), M-05, M-07, M-09 | Una sola implementación; ningún módulo replica fórmulas (`D-06`, `D-07`, `D-33`) |
| Autoría y aprobaciones | M-07 | Registro único consultado por el resto de módulos (`D-08`, `D-11`) |
| Visibilidad pública | M-02, M-05, M-08 | La decisión es del proyecto y afecta a todos sus APUs e insumos (`D-22`) |
| Observabilidad y errores | Todos | Registro de eventos y manejo uniforme de errores (capítulo 3) |

### 2.3.4 Dependencias entre módulos
| Módulo | Depende de | Motivo |
|---|---|---|
| M-01 | — | Base del sistema |
| M-02 | M-01 | Requiere compañía y roles |
| M-03 | M-02 | Requiere proyecto |
| M-04 | M-02 | Los insumos pertenecen a un proyecto y usan el catálogo de tipos |
| M-05 | M-03, M-04 | El APU se vincula a un ítem y se compone de insumos |
| M-06 | M-05 | Calcula sobre la composición del APU y resuelve el conteo de uso |
| M-07 | M-04, M-05, M-06 | Cotiza insumos, firma APUs y valida el costo antes del cierre |
| M-08 | M-02, M-04, M-05 | Publica proyectos con sus APUs e insumos |
| M-09 | M-02, M-06 | Compara el costo calculado con el tope configurado |

**Verificación:** no existen ciclos de dependencia; `M-08` y `M-09` son consumidores finales y ningún módulo depende de ellos.

## 2.4 Estrategia para desarrollar el proyecto progresivamente

### 2.4.1 Principios
1. **Incrementos demostrables:** cada incremento termina con un flujo que se puede ejecutar de punta a punta, no con capas a medias.
2. **Slices verticales:** cada incremento incluye interfaz, API, datos y pruebas del módulo.
3. **Sin saltos de dependencia:** ningún módulo inicia si sus dependencias no están cerradas (2.3.4).
4. **Especificación previa:** cada incremento se describe en OpenSpec antes de codificarse (capítulo 6).
5. **Sin adelantos de alcance:** no se implementan funcionalidades `Baja` ni elementos de `E-` durante el MVP.

### 2.4.2 Incrementos definidos
| ID | Objetivo | Módulos | Funcionalidades | Resultado demostrable | Depende de |
|---|---|---|---|---|---|
| I-1 | Base de acceso y organización | M-01 + catálogos maestros | RF-01, RF-02, RF-18, RF-23 | Autorregistro público, alta de compañía, usuarios y roles; correos de creación de cuenta y recuperación | — |
| I-2 | Proyecto y estructura | M-02, M-03 | RF-03, RF-04 | Proyecto configurado (divisa, AIU, tope, firma, visibilidad) con su EDT | I-1 |
| I-3a | Insumos del proyecto | M-04 | RF-05, RF-06, RF-16, RF-20 | Vista de insumos con listado, filtros, estados y copia desde otros proyectos de la compañía | I-2 |
| I-3b | APUs y matrices | M-05, M-06 (solo RF-22) | RF-07, RF-08, RF-09, RF-22 | APU 1:1 con matrices, copia desde proyectos de la compañía, reconciliación de insumos y conteo de uso | I-3a |
| I-4a | Cálculo y cotización | M-06, M-09 | RF-10, RF-11, RF-14, RF-17 | Presupuesto calculado, insumos cotizados en masa, autoría y alerta de tope | I-3b |
| I-4b | Aprobaciones y cierre | M-07 | RF-12, RF-13, RF-21 | Firmas por APU en los tres modos, registro de aprobaciones y cierre del presupuesto | I-4a |
| I-5 | Catálogo abierto | M-08 | RF-15 | Consulta y copia de APUs e insumos públicos; completa la copia pública de `RF-07` y `RF-08` | I-4b |
| I-6 | Endurecimiento y entrega | Transversal | — | Seguridad, rendimiento, observabilidad, despliegue y documentación final | I-5 |

### 2.4.3 Justificación del orden
- **M-04 antes de M-05:** el APU no existe sin insumos (`R-02`).
- **I-3a antes de I-3b:** el conteo de uso requiere APUs y cantidades de obra; `M-06` inicia en `I-3b` solo con `RF-22` (`D-33`).
- **I-4a antes de I-4b:** no se puede firmar ni cerrar un presupuesto cuyo costo y cotización no están resueltos (`RF-10`, `RF-11`).
- **M-08 al final:** el catálogo abierto depende de proyectos públicos con APUs e insumos completos (`D-22`, `D-28`) y es el punto de mayor exposición de información (`RT-11`).
- **M-09 dentro de I-4a:** las alertas necesitan el costo calculado y el tope configurado (`RF-17`).
- **RF-18 en I-1:** el alta de usuarios requiere el envío de credenciales (`D-17`).
- **RF-23 en I-1:** el autorregistro es la vía de entrada de usuarios sin compañía y no depende de módulos posteriores (`D-72`).

### 2.4.4 Riesgos de la estrategia
| Riesgo | Descripción | Mitigación propuesta |
|---|---|---|
| Concentración en I-3b | Es el incremento más grande: APUs, matrices, copia, reconciliación y conteo de uso. | Dividir su especificación en cambios pequeños de OpenSpec y validar cada uno. |
| Dependencia de `X-15` | El cálculo no puede implementarse sin fórmulas verificables. | Resolver `X-15` al inicio del capítulo 3, antes de abrir I-4a. |
| Exposición en I-5 | Publicar todos los campos del insumo es irreversible para la información ya publicada. | Advertencia explícita al publicar y registro de aceptación (`RT-11`). |
| Sin validación con usuario real | No hay empresa piloto ni datos del proceso (`D-19`). | Pruebas con datos sintéticos y revisión del flujo con el Director. |
| Alcance amplio para una persona | 22 funcionalidades en el MVP (`RT-07`). | Respetar la secuencia y no paralelizar módulos. |
| Abuso del autorregistro | Un endpoint público permite crear cuentas masivas o falsas; el plan no define límite de intentos ni verificación de correo. | Decidir el control antes de abrir E-1 (`RT-22`); mientras no exista, el despliegue queda limitado a local o demo controlada (`D-39`). |

### 2.4.5 Regla de avance
Un incremento no inicia hasta que el anterior esté cerrado con: flujo demostrable, pruebas del módulo en verde, especificación actualizada y sin defectos abiertos que impidan el flujo del siguiente incremento. Los criterios detallados de terminado se definen en el capítulo 4.

## 2.5 Estado de las decisiones abiertas
No quedan puntos abiertos. El detalle de `X-23` y `X-24` consta en 0.6; aquí solo se referencia su cierre.

| ID | Tema | Estado |
|---|---|---|
| X-23 | Registro de insumos de compañía | Cerrado; ver 0.6 |
| X-24 | Numeración de hijos con código propio | Cerrado; ver 0.6 |
| Q-24 | Reutilización de insumos | Respondida → `D-27`, `D-29` |
| Q-25 | Prioridades e incrementos | Respondida → 2.2 y 2.4 |
| Q-26 | Numeración de hijos con código personalizado | Respondida → `D-30` |

## 2.6 Criterios de aceptación del capítulo 2
El capítulo se considera cerrado cuando:

1. Cada capacidad incluida (`S-01`…`S-16`) está cubierta por al menos una funcionalidad (`RF`).
2. Cada funcionalidad tiene módulo asignado, salvo `RF-19`, que está fuera del MVP por `D-18`.
3. Las dependencias entre módulos no presentan ciclos y están justificadas.
4. Cada incremento declara objetivo, módulos, funcionalidades, resultado demostrable y dependencia.
5. Las prioridades y la frontera del MVP son coherentes con `D-14`, `D-17` y `D-18`.
6. No quedan puntos abiertos del capítulo (`Q-24`, `Q-25` y `Q-26` respondidas).

---

**Capítulo 2 cerrado** (v0.8). `Q-24`, `Q-25` y `Q-26` respondidas. Ver capítulo 3.

---

# 3. Decisiones de arquitectura y tecnología

> **Estado del capítulo:** redactado completo (3.1 a 3.5). Pendiente confirmar `Q-45` a `Q-48`. Lo marcado `PROPUESTA` requiere aprobación expresa.

## 3.1 Arquitectura seleccionada

### 3.1.1 Estilo arquitectónico
Se adopta un **monolito modular** organizado con **Clean Architecture** y una **SPA desacoplada** (`PROPUESTA`, coherente con `03 §1`).

| Decisión | Justificación |
|---|---|
| Monolito modular en lugar de microservicios | Un solo desarrollador (`A-04`) y un único servidor (`R-12`); los microservicios añadirían despliegue, observabilidad y transacciones distribuidas sin beneficio |
| Clean Architecture con regla de dependencia | Aísla las reglas de cálculo y de estado (`D-04`, `D-06`) de la base de datos y del framework web (`03 §2`, `03 §8`) |
| SPA Angular separada de la API | Desacoplamiento frontend/backend (`03 §1`, `NFR-005`) |
| Módulos `M-01`…`M-09` como slices verticales | Cada módulo agrupa su dominio, aplicación, infraestructura y endpoints, sin mezclar responsabilidades (2.3) |
| Transacciones locales | La cascada de estados y el cierre requieren atomicidad dentro de una sola base (`D-04`, `BR-014`) |

**Descartado:** microservicios por módulo. Se evaluará formalmente en 3.5.

### 3.1.2 Capas y reglas
| Capa | Responsabilidad | Prohibido |
|---|---|---|
| Domain | Entidades, enums, reglas de cálculo, reglas de estado, contratos de repositorio | Depender de EF Core, ASP.NET o base de datos |
| Application | Casos de uso, validación, DTOs, mapeo, consultas de lectura, transacciones | Ejecutar SQL directo o exponer entidades de EF |
| Infrastructure | Implementación de repositorios, `DbContext`, Identity, correo, logs | Contener reglas de decisión del negocio |
| API | Endpoints, autenticación, autorización, manejo de errores, contratos HTTP | Usar `DbContext` o consultar la base directamente |
| Presentation | SPA Angular, guardias de ruta, servicios HTTP | Replicar fórmulas o reglas de negocio |

**Regla de dependencia:** `API → Application → Domain`; `Infrastructure` implementa los contratos de `Domain`/`Application`; la composición de dependencias se realiza en `API`.

### 3.1.3 Aislamiento multitenant y por proyecto
Implementación decidida (`D-36`):

1. Una sola base de datos con esquema compartido (`03 §1`, `05 §4`).
2. `company_id` obligatorio en toda entidad de compañía; `project_id` en las entidades que pertenecen a un proyecto (`D-05`).
3. Filtro global por compañía en la capa de persistencia y filtro explícito por proyecto en los casos de uso.
4. Validación de pertenencia en `Application` como defensa en profundidad (`D-13`, `D-34`).
5. **Un único bypass controlado**: la consulta del catálogo público, que solo lee proyectos públicos (`D-22`, `D-28`) y nunca se usa en endpoints privados.

**Descartado por ahora:** base de datos por compañía y Row-Level Security de SQL Server. Se comparan en 3.5.

### 3.1.4 Motor de cálculo
Implementación decidida (`D-37`):

- Un **servicio de dominio puro** (`ApuCalculator`) sin dependencias de EF Core ni de HTTP.
- Recibe la composición del APU, las cantidades de obra, los parámetros de AIU y la configuración de redondeo; devuelve costos y el conteo de uso.
- `Application` carga los datos, invoca el servicio y persiste los resultados dentro de la transacción.
- El frontend solo presenta resultados: no replica fórmulas (`RF-10`, 2.3.3).
- La fórmula exacta por tipo de insumo queda pendiente de `X-15`.

### 3.1.5 Consultas de lectura
Implementación decidida (`D-47`): proyecciones de EF Core en `Application`. Con `D-37`, el cálculo por APU vive en el dominio y la agregación por proyecto se resuelve en la consulta, sin duplicar la fórmula del APU.

- Dos **read models** independientes de las entidades de dominio: conteo de uso de insumos (`RF-22`) y catálogo público (`RF-15`).
- Se implementan como proyecciones de EF Core en `Application`; no navegan entidades de otros módulos ni reutilizan repositorios de escritura. Esto evita el ciclo `M-04 ↔ M-06` detectado en la revisión (H-01).
- El catálogo público lee **solo proyectos públicos** (`D-22`), expone todos los campos del insumo (`D-28`) y omite autoría y empresa.
- Paginación y filtros se resuelven en la consulta, no en memoria.

### 3.1.6 Despliegue
| Componente | Contenido | Exposición |
|---|---|---|
| `web` | Nginx con la SPA compilada y proxy inverso | Pública (80/443) |
| `api` | ASP.NET Core `.NET 10` en el puerto interno 8080 | Solo red interna |
| `sqlserver` | SQL Server 2022 con volumen persistente | Solo red interna |

Fuente: `03 §6`, `04 §1`, `R-12`.

**Pendiente:**
- **TLS (`D-39`):** postergado. Solo se permite despliegue local o demo controlada; queda prohibido exponer la aplicación a Internet sin HTTPS.
- Migraciones (`D-40`): se aplican manualmente con un runbook documentado; se elimina la expectativa de inicialización automática de `05 §8`.
- No hay entornos definidos (desarrollo, pruebas, producción) ni estrategia de respaldo.

### 3.1.7 Seguridad
| Control | Estado |
|---|---|
| Autenticación JWT + ASP.NET Identity | CONFIRMADO (`03 §4`, `04 §2`) |
| RBAC con cuatro roles y asignación por proyecto | CONFIRMADO (`D-02`, `D-15`, `D-34`) |
| Aislamiento por compañía y proyecto | PROPUESTA (3.1.3) |
| Registro único de aprobación por APU | CONFIRMADO (`D-52`) |
| Firma como evidencia temporal, sin hash persistido | CONFIRMADO (`D-03`, `D-11`) |
| Catálogo público anónimo | CONFIRMADO (`D-38`); exige límite por IP y prohibición de exportación masiva |
| Política de contraseñas, bloqueo, refresh tokens, CORS, rate limiting y gestión de secretos | **NO DOCUMENTADO** |
| Autorregistro anónimo (`/api/v1/auth/register`) | CONFIRMADO (`D-72`): público, sin sesión; rol inicial y ausencia de compañía `DERIVADO` (`R-24`) |
| Protección del autorregistro (límite de intentos, verificación de correo, captcha) | **NO DOCUMENTADO** (`RT-22`) |
| HTTPS | POSTERGADO (`D-39`); prohibido exponer la aplicación sin TLS |

### 3.1.8 Observabilidad
- Logs estructurados JSON con Serilog, incluyendo identificador de correlación (`03 §7`).
- Health checks de API y de conexión a base de datos (`03 §7`).
- Trazabilidad mínima: último modificador por APU e insumo (`D-08`) y aprobaciones por APU (`D-11`). No hay bitácora histórica (`E-05`).
- Métricas, trazas distribuidas y alertamiento operativo no están definidos.

### 3.1.9 Riesgos técnicos
| Riesgo | Impacto | Mitigación propuesta |
|---|---|---|
| Uso indebido del bypass multitenant | Fuga de datos entre compañías | Un solo punto de bypass, revisado y cubierto con pruebas de aislamiento |
| Publicación irreversible de precios y contactos | Exposición comercial permanente | Advertencia y aceptación explícita al publicar (`RT-11`) |
| Catálogo anónimo con precios y contactos | Scraping y exposición comercial | Límite por IP, sin exportación masiva y advertencia al publicar (`D-38`) |
| Concurrencia sin control de versiones | Sobrescritura de cambios entre usuarios | Evaluar `rowversion` y bloqueo optimista |
| Licenciamiento de SQL Server en producción | Costo no previsto | Definir edición y plan antes de desplegar |
| Ausencia de TLS | Interceptación de tokens | Aceptado solo en entorno local (`D-39`); bloquea cualquier despliegue público |
| Proveedor de correo sin seleccionar | Bloquea `RF-18` | Elegir SendGrid, Mailgun o SES al implementar (`D-45`) |
| Migraciones manuales | Error humano o esquema desactualizado | Runbook y verificación posterior a cada migración (`D-40`) |

### 3.1.10 Decisiones de arquitectura
| ID | Tema | Decisión |
|---|---|---|
| Q-27 | Aislamiento multitenant | Decidida → `D-36` (filtro global + bypass único) |
| Q-28 | Motor de cálculo | Decidida → `D-37` (servicio de dominio puro) |
| Q-29 | Implementación de consultas de conteo y catálogo | Decidida → `D-47` (proyecciones EF) |
| Q-30 | Acceso al catálogo público | Decidida → `D-38` (anónimo, con límite por IP) |
| Q-31 | TLS | Decidida → `D-39` (postergado, solo local) |
| Q-32 | Migraciones | Decidida → `D-40` (manuales con runbook) |

## 3.2 Stack tecnológico

### 3.2.1 Stack confirmado
| Capa | Tecnología | Versión base | Estado |
|---|---|---|---|
| Backend | ASP.NET Core Web API | .NET 10 (LTS) | CONFIRMADO (`04 §1`) |
| ORM | Entity Framework Core | 10.x | **CORREGIDO**: `04 §1` indica 8.x, alineado con .NET 8, no con .NET 10 |
| Base de datos | SQL Server | 2022 | CONFIRMADO (`03 §6`, `04 §1`) |
| Frontend | Angular | 20.x | CONFIRMADO como versión base documentada; verificar soporte al iniciar |
| Proxy y estáticos | Nginx | Versión estable fijada al desplegar | CONFIRMADO (`03 §6`) |
| Contenedores | Docker Compose | v2 | CONFIRMADO (`03 §6`, `04 §1`) |
| Identidad | ASP.NET Core Identity + JWT Bearer | Alineado a .NET 10 | CONFIRMADO (`03 §4`, `04 §2`) |
| Logging | Serilog | Última estable | CONFIRMADO (`03 §7`, `04 §2`) |
| Pruebas backend | xUnit + Moq + FluentAssertions | Últimas estables | CONFIRMADO (`03 §4`, `04 §2`) |

### 3.2.2 Librerías backend
| Librería | Uso | Estado |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validación de tokens | CONFIRMADO |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Usuarios, roles y hash de contraseñas | CONFIRMADO |
| `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` | Validación de DTOs | **CORREGIDO**: `FluentValidation.AspNetCore` está discontinuado |
| `Serilog.AspNetCore` | Logs estructurados | CONFIRMADO |
| Mapeo manual (métodos de extensión) | Entidad → DTO | CONFIRMADO (`D-41`); AutoMapper descartado |
| `Swashbuckle.AspNetCore` | Contrato OpenAPI y Swagger UI | CONFIRMADO (`D-42`) |
| `xUnit`, `Moq`, `FluentAssertions` | Pruebas unitarias | CONFIRMADO |
| `Coverlet` | Cobertura | No documentado; se define en el capítulo 5 |

### 3.2.3 Frontend
| Elemento | Estado |
|---|---|
| Angular 20 con TypeScript y `HttpClient` + interceptores | CONFIRMADO (`04 §2`) |
| RxJS para flujos asíncronos | CONFIRMADO (`04 §2`) |
| Angular Material | CONFIRMADO (`D-43`) |
| `signature_pad` | CONFIRMADO (`D-44`) |
| Gestión de estado (signals, servicios o NgRx) | **NO DEFINIDO** |
| Pruebas de frontend | Fuera de alcance en `00 §5`; se revisa en el capítulo 5 |

### 3.2.4 Servicios de infraestructura
| Servicio | Necesidad | Estado |
|---|---|---|
| Correo transaccional | Creación de cuenta y recuperación (`D-17`, `RF-18`) | Proveedor transaccional (`D-45`); SendGrid, Mailgun o SES se elige al implementar |
| CI/CD | Validación de especificaciones, build y pruebas (`01 §5`) | GitHub Actions (`D-46`) |
| Generación del cliente Angular desde OpenAPI | `04 §5` | CONFIRMADO (`D-56`); se ejecuta en CI (`D-46`) |
| Almacenamiento y retención de logs | Serilog a consola y archivo (`03 §7`) | PARCIAL: no hay retención definida |
| Respaldos de base de datos | Continuidad operativa | **NO DOCUMENTADO** |

### 3.2.5 Herramientas de calidad
| Herramienta | Uso | Estado |
|---|---|---|
| `.editorconfig` + `Microsoft.CodeAnalysis.NetAnalyzers` | Estilo y análisis estático backend | CONFIRMADO (`04 §3`) |
| ESLint + Prettier | Calidad y formato frontend | CONFIRMADO (`04 §3`) |
| `package-lock.json` y versiones fijas en `.csproj` | Reproducibilidad de builds | CONFIRMADO (`04 §4`) |
| SemVer `MAJOR.MINOR.PATCH` | Versionado del producto y de la API | CONFIRMADO (`04 §4`) |
| Cobertura ≥ 80 % | Motor de cálculo (`NFR-005`) | CONFIRMADO como objetivo; el alcance exacto se define en el capítulo 5 |

### 3.2.6 Correcciones al documento `04`
| Punto documentado | Corrección |
|---|---|
| EF Core 8.x con .NET 10 | EF Core 10.x (versión alineada con .NET 10) |
| `FluentValidation.AspNetCore` | Paquete discontinuado; usar `FluentValidation` + `DependencyInjectionExtensions` |
| "Angular Material / PrimeNG" | Resuelto: Angular Material (`D-43`) |
| "`signature_pad` (o librería equivalente)" | Resuelto: `signature_pad` (`D-44`) |
| Nginx "Estable" | Fijar una versión concreta al desplegar |
| Sección 5 (Spec Kit) | `OBSOLETO` por `D-09` (OpenSpec) |
| `AutoMapper` | Descartado; mapeo manual (`D-41`) |

### 3.2.7 Decisiones de stack
| ID | Tema | Decisión |
|---|---|---|
| Q-33 | Mapeo entidad → DTO | Decidida → `D-41` (manual) |
| Q-34 | Contrato OpenAPI | Decidida → `D-42` (Swashbuckle) |
| Q-35 | Biblioteca de UI | Decidida → `D-43` (Angular Material) |
| Q-36 | Biblioteca de firma | Decidida → `D-44` (`signature_pad`) |
| Q-37 | Correo transaccional | Decidida → `D-45` (proveedor transaccional; proveedor concreto al implementar) |
| Q-38 | CI/CD | Decidida → `D-46` (GitHub Actions) |

---

**Estado del capítulo 3:** 3.1 y 3.2 cerrados. Pendientes 3.3 (persistencia), 3.4 (componentes) y 3.5 (comparación de al menos tres decisiones con alternativas).

## 3.3 Estrategia de persistencia de datos

> **Estado:** cerrada. Modelo definido a partir de `D-05`, `D-08`, `D-11`, `D-22`, `D-30`, `D-31`, `D-34`, `D-36`, `D-40` y `D-48` a `D-53`.

### 3.3.1 Principios
1. Una base SQL Server 2022 con esquema compartido y `company_id` en toda entidad de compañía (`D-36`).
2. `project_id` en toda entidad con alcance de proyecto, incluso cuando sea derivable por relación, para filtros e índices.
3. EF Core 10 code-first; migraciones manuales con runbook (`D-40`).
4. Claves primarias `uniqueidentifier`.
5. Sin historial de cambios: solo `last_modified_by` y `last_modified_at` (`D-08`), más las aprobaciones (`D-11`).
6. Sin `BudgetVersion` (`E-12`).
7. Fechas almacenadas en UTC.
8. Enumeraciones persistidas como `VARCHAR` con `CHECK`, no como enteros.

### 3.3.2 Entidades
| Entidad | Campos clave | Cambios y reglas |
|---|---|---|
| `Company` | `id`, `business_name`, `tax_id`, `is_active` | `tax_id` único |
| `Role` | `id`, `code`, `name`, `is_active` | `code` único; catálogo editable |
| `User` | `id`, `company_id`, `role_id`, `full_name`, `username`, `email`, `password_hash`, `is_active` | **Cambio:** `company_id` pasa a **nullable** porque el Administrador no tiene compañía (`D-02`) y porque el usuario autorregistrado nace sin compañía (`D-72`). `username` es obligatorio y único (`D-73`). Se agregan nombre y estado |
| `Project` | `id`, `company_id`, `name`, `currency`, `status`, `is_public`, `created_at` | La visibilidad vive solo en el proyecto (`D-22`) |
| `ProjectConfiguration` | `project_id` (único), `budget_limit`, `aiu_admin`, `aiu_contingency`, `aiu_profit`, `signature_mode`, `requires_auditor` | Se agregan AIU (`D-07`), modo de firma (`D-03`) y requisito de auditor (`D-34`) |
| `ProjectMember` | `id`, `project_id`, `user_id`, `role_in_project` | **Nueva:** asignación de usuarios a proyectos (`D-02`, `D-34`); única por (`project_id`, `user_id`) |
| `WbsCode` | `id`, `project_id`, `code`, `node_type` | **Nueva (`D-51`)**: registro único de códigos; índice único (`project_id`, `code`) |
| `WbsChapter` | `id`, `project_id`, `parent_chapter_id`, `code_id`, `name`, `order_index` | Capítulo o subcapítulo; `code_id` referencia `WbsCode` |
| `WbsItem` | `id`, `wbs_chapter_id`, `apu_id`, `code_id`, `quantity_work`, `order_index` | Ítem de obra; `apu_id` único (1:1); `code_id` referencia `WbsCode` |

| `Apu` | `id`, `project_id`, `description`, `unit`, `yield`, `status`, `last_modified_by`, `last_modified_at` | Se eliminan `code` (`D-31`) e `is_public` (`D-22`). Sin tabla de versiones |
| `Resource` | `id`, `project_id`, `resource_type_id`, `city_id`, `description`, `unit`, `unit_price`, `brand`, `distributor`, `user_key`, `details`, `contact_number`, `quotation_status`, `origin_resource_id`, `last_modified_by`, `last_modified_at` | Se elimina `is_public`; `origin_resource_id` solo registra procedencia del catálogo (`Q-16`); la unicidad es sugerida (`Q-09`) |
| `ApuResource` | `id`, `apu_id`, `resource_id`, `consumption`, `waste_percentage` | Relación APU–insumo; sin cambio funcional |
| `MatrixComponent` | `id`, `matrix_resource_id`, `component_resource_id`, `consumption`, `waste_percentage` | **CONFIRMADO (`D-48`)**: composición de la Matriz en tabla propia, sin FK polimórfica |
| `ApuApproval` | `id`, `apu_id` (único), `user_id`, `role_code`, `approved_at`, `mode`, `signature_captured` | **CONFIRMADO (`D-52`, `D-53`)**: un registro por APU con el último aprobador; sin hash de firma; se elimina si el APU se edita |
| `Country`, `City`, `ResourceType` | Catálogos de ubicación y tipo de insumo | Sin cambios respecto a `05` |
| `AuditLog`, `BudgetVersion` | — | **Eliminadas**: `D-08` (sin historial) y `E-12` (sin congelamiento) |

### 3.3.3 Reglas de integridad
| Regla | Implementación |
|---|---|
| Código único del EDT por proyecto | Índice único (`project_id`, `code`) en `WbsCode` (`D-30`, `D-51`) |
| Un APU por ítem | Índice único sobre `WbsItem.apu_id` (`D-16`) |
| Una configuración por proyecto | Índice único sobre `project_id` |
| Un usuario una vez por proyecto | Índice único (`project_id`, `user_id`) |
| Una aprobación por APU | Índice único sobre `apu_id` en `ApuApproval` (`D-52`) |
| Email único | Índice único con intercalación sin distinción de mayúsculas |
| Nombre de usuario único | Índice único sobre `User.username` sin distinción de mayúsculas (`D-73`) |
| Identificación fiscal única | Índice único sobre `Company.tax_id` |
| Divisa | `CHAR(3)` con `CHECK` de longitud y mayúsculas |
| AIU | `DECIMAL(5,2)` con `CHECK` entre 0 y 100 |
| Rendimiento | `yield > 0` |
| Cantidad de obra | `quantity_work > 0` solo para ítems |
| Consumo y desperdicio | `consumption > 0`; `waste_percentage` entre 0 y 100 |
| Precio unitario | `unit_price >= 0` |
| Tipo de nodo | `node_type IN ('CHAPTER','ITEM')` |
| Coherencia | `WbsItem` exige `apu_id` y `quantity_work`; `WbsChapter` exige `name`; `WbsCode.node_type` coincide con la tabla que lo referencia |
| Alcance | `company_id` obligatorio salvo `User` con rol Administrador y usuarios autorregistrados pendientes de asignación; `project_id` obligatorio en entidades de proyecto |
| Integridad referencial | `RESTRICT` por defecto; cascada solo en hijos de composición (`D-49`) |
| Concurrencia | `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource` (`D-50`) |

**Nota:** las reglas de negocio de estados (`FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`) no se implementan como `CHECK`; se calculan en el dominio y se persisten en la columna correspondiente (`D-04`, `D-11`).

### 3.3.4 Aprobaciones y autoría
- `ApuApproval` guarda **un registro por APU**: APU, último usuario que aprobó, rol, fecha UTC, modo y un booleano `signature_captured`.
- No se persiste imagen, token ni hash de firma; el booleano solo registra que la evidencia se capturó (`D-52`).
- No hay historial de aprobaciones: si otro usuario aprueba después, se sobrescribe el registro (`D-52`).
- `Apu` y `Resource` llevan `last_modified_by` y `last_modified_at`; no existe historial (`D-08`).
- Una copia de APU nace sin aprobación y con el usuario que copió como último modificador (`DERIVADO` de `D-13`).
- Toda edición de un APU aprobado elimina su registro de aprobación y exige una nueva (`D-53`).
- El Director valida, al cerrar, que cada APU tenga la aprobación exigida por el modo configurado (`D-14`).

### 3.3.5 Índices
| Índice | Tabla y columnas | Propósito |
|---|---|---|
| Aislamiento por compañía | `Project(company_id, is_public)` | Listados privados y catálogo público |
| Jerarquía del EDT | `WbsChapter(project_id, parent_chapter_id, order_index)` | Recorrido del árbol y orden |
| Código único | `WbsCode(project_id, code)` único | Regla `D-30`, `D-51` |
| APU único por ítem | `WbsItem(apu_id)` único | Regla 1:1 (`D-16`) |
| Ítems por capítulo | `WbsItem(wbs_chapter_id, order_index)` | Listado del capítulo |
| Búsqueda de APU | `Apu(project_id, status)` | Listados y conteos |
| Insumos por tipo y estado | `Resource(project_id, resource_type_id)`, `Resource(project_id, quotation_status)` | Filtros y cotización masiva |
| Búsqueda de insumos | `Resource(project_id, description)` | Búsqueda parcial (`D-25`) |
| Composición del APU | `ApuResource(apu_id)`, `ApuResource(resource_id)` | Cálculo y conteo de uso |
| Composición de matriz | `MatrixComponent(matrix_resource_id)`, `MatrixComponent(component_resource_id)` | Expansión de matriz |
| Aprobación | `ApuApproval(apu_id)` único | Validación de cierre (`D-52`) |
| Asignaciones | `ProjectMember(user_id, project_id)` | Authorization checks |
| Usuarios por compañía | `User(company_id, role_id)` | Administración |
| Nombre de usuario | `User(username)` único | Autorregistro y login por nombre de usuario o correo (`D-73`) |

### 3.3.6 Concurrencia y borrado
- **Concurrencia (`D-50`):** `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource`; la API responde `409 Conflict` y el cliente recarga.
- **Borrado (`D-49`):** baja lógica (`is_active` o `deleted_at`) para `Project`, `Apu` y `Resource`, y `RESTRICT` para registros referenciados. No se permite borrar un APU con aprobaciones.
- Los hijos de composición (`ApuResource`, `MatrixComponent`, `ApuApproval`) se eliminan en cascada solo cuando se elimina físicamente su padre, situación que la baja lógica evita.

### 3.3.7 Migraciones y datos iniciales
Runbook manual (`D-40`):

1. Respaldo de la base antes de migrar.
2. Generar la migración con `dotnet ef migrations add`.
3. Revisar el script SQL generado (`dotnet ef migrations script`).
4. Aplicar en ventana de mantenimiento y verificar el esquema.
5. Registrar la versión aplicada en el control de cambios.

**Seed inicial obligatorio:**
- Cuatro roles con GUID fijo: Administrador, Director de proyectos, Presupuestador y Auditor.
- Siete tipos de insumo: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matriz.
- Catálogo base de países y ciudades.
- Usuario Administrador inicial **sin compañía** y una empresa demo con usuarios de prueba.

### 3.3.8 Decisiones de persistencia
| ID | Tema | Opciones | Estado |
|---|---|---|---|
| Q-39 | Modelo del EDT | A) Tabla única `WbsNode` · B) Tablas separadas + registro de códigos · C) Validación solo en aplicación | **B → `D-51`** |
| Q-40 | Composición de la Matriz | A) Tabla `MatrixComponent` · B) `ApuResource` polimórfico · C) Matriz como APU | **A → `D-48`** |
| Q-41 | Aprobaciones | A) Por usuario/tipo e invalida al editar · B) Por usuario/tipo y conserva · C) Un registro por APU | **C → `D-52`** |
| Q-42 | Borrado | A) Baja lógica + `RESTRICT` · B) Físico con `RESTRICT` · C) Cascada | **A → `D-49`** |
| Q-43 | Concurrencia | A) `rowversion` en agregados · B) Sin control · C) `rowversion` con fusión | **A → `D-50`** |
| Q-44 | Edición de un APU aprobado | A) Borra la aprobación y exige re-aprobación · B) Conserva la aprobación · C) Solo si cambia la composición | **A → `D-53`** |

---

**Estado de 3.3:** cerrada. Siguen 3.4 (organización de componentes) y 3.5 (comparación de al menos tres decisiones con alternativas).

## 3.4 Organización de componentes

> **Estado:** cerrada (`D-54` a `D-57`).

### 3.4.1 Criterios de organización
1. Las capas se separan en **proyectos**; los módulos son **carpetas** dentro de cada capa (`D-54`).
2. Un módulo no accede a la infraestructura de otro: se comunica mediante contratos de `Application`.
3. La API expone endpoints por módulo y versionados.
4. El frontend se organiza por **features**.
5. No hay reglas de negocio en controladores, componentes Angular ni SQL.

### 3.4.2 Proyectos backend
| Proyecto | Contenido | Depende de |
|---|---|---|
| `BudgetCivil.Domain` | Entidades, enums, `ApuCalculator`, contratos de repositorio | — |
| `BudgetCivil.Application` | Casos de uso, DTOs, validaciones, consultas, `IEmailSender` | `Domain` |
| `BudgetCivil.Infrastructure` | `AppDbContext`, configuraciones EF, repositorios, Identity, correo, migraciones | `Application`, `Domain` |
| `BudgetCivil.Api` | Endpoints, composición de dependencias, autenticación, middleware, Swagger | `Application`, `Infrastructure` |
| `BudgetCivil.Tests.Unit` | Pruebas de dominio y aplicación | `Domain`, `Application` |
| `BudgetCivil.Tests.Integration` | Pruebas de API y persistencia | `Api` |

Las pruebas se detallan en el capítulo 5.

### 3.4.3 Carpetas por módulo
Ejemplo para `M-05` (APUs y matrices):

```text
Domain/Apus/                 Apu.cs, ApuApproval.cs, reglas de aprobación
Application/Apus/            Commands/, Queries/, Dtos/
Infrastructure/Persistence/Configurations/ApuConfiguration.cs
Infrastructure/Persistence/Repositories/ApuRepository.cs
Api/Endpoints/ApusEndpoints.cs
Frontend/src/app/features/apus/
```

| Módulo | Carpeta | Endpoints principales |
|---|---|---|
| M-01 Acceso y organización | `Identity`, `Companies` | `/auth/register` (anónimo), `/auth/login`, `/users`, `/companies` |
| M-02 Proyectos | `Projects` | `/projects`, `/projects/{id}/configuration`, `/projects/{id}/members` |
| M-03 EDT | `Wbs` | `/projects/{id}/wbs` |
| M-04 Insumos | `Resources` | `/projects/{id}/resources` |
| M-05 APUs y matrices | `Apus` | `/apus`, `/apus/{id}/resources`, `/apus/{id}/copy` |
| M-06 Cálculo | `Calculation` | `/projects/{id}/summary`, `/projects/{id}/resource-usage` |
| M-07 Aprobación y cierre | `Approvals` | `/apus/{id}/approval`, `/projects/{id}/close` |
| M-08 Catálogo abierto | `Catalog` | `/public/apus`, `/public/resources` |
| M-09 Alertas | `Alerts` | `/projects/{id}/alerts` |

### 3.4.4 Estructura del frontend
```text
src/app/
  core/        autenticación, interceptores, guardias, configuración de API
  shared/      componentes reutilizables, pipes y directivas
  layout/      navegación y contenedor de la aplicación
  features/    auth, companies, users, projects, wbs, resources, apus, approvals, catalog, alerts
  models/      DTOs y enumeraciones compartidas
```

Cada `feature` contiene sus componentes, rutas y servicio de datos. La lógica de cálculo y las reglas de estado no se replican en el cliente (3.1.4).

**Estado de la aplicación (`D-57`):**
- **Global:** `AuthService` (sesión, rol y permisos) y `ActiveProjectService` (proyecto seleccionado), con signals.
- **Por feature:** servicios con signals para datos de servidor (APUs, insumos, EDT, catálogo y alertas), con invalidación tras cada mutación.
- **Local:** filtros, nodos expandidos, pestañas, diálogos y formularios.
- Los servicios de feature envuelven el cliente generado (`D-56`) y centralizan el manejo de `409 Conflict` (`D-50`).
- No se usa NgRx en el MVP; se evaluará si aparece edición optimista generalizada, deshacer/rehacer o modo sin conexión.

### 3.4.5 Contratos y convenciones de API
| Elemento | Definición |
|---|---|
| Versionado | Prefijo de ruta `/api/v1` |
| Endpoints anónimos | Solo `/api/v1/auth/register` y `/api/v1/auth/login` (`D-72`); el resto exige JWT |
| Formato | JSON |
| Nombres de campos | `camelCase` (`D-55`); los ejemplos con `snake_case` de `02 §5` deben actualizarse |
| Errores | `ProblemDetails` (RFC 7807) con código de negocio |
| Paginación | `page` y `page_size` con límite máximo; filtros por query string |
| Documento | Swashbuckle en `/swagger` (`D-42`) |
| DTOs | Solo DTOs en la frontera; mapeo manual (`D-41`); nunca se exponen entidades |
| Cliente Angular | Generado desde OpenAPI (`D-56`); se regenera en CI (`D-46`) |

### 3.4.6 Reglas de dependencia y verificación
| Regla | Verificación |
|---|---|
| `Domain` no referencia EF Core ni ASP.NET | Revisión de referencias del proyecto |
| `Application` no usa `DbContext` | Revisión de referencias y pruebas |
| `Api` no usa `DbContext` ni repositorios directamente | Revisión de referencias |
| Un módulo no navega entidades de otro | Solo contratos de `Application` y read models (`D-47`) |
| `Infrastructure` no contiene decisiones de negocio | Revisión de código |
| Las fórmulas existen una sola vez | Pruebas del `ApuCalculator` (capítulo 5) |

Las pruebas de arquitectura automáticas se definirán en el capítulo 5.

### 3.4.7 Decisiones abiertas de organización
| ID | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| Q-45 | Organización del backend | A) Proyectos por capa con carpetas por módulo · B) Un proyecto por módulo y capa · C) Un solo proyecto | **A → `D-54`** |
| Q-46 | Estado en el frontend | A) Servicios con signals · B) NgRx · C) Estado local · D) Híbrido (signals + local) | **D → `D-57`** |
| Q-47 | Nombres de campos JSON | A) `snake_case` · B) `camelCase` · C) `PascalCase` | **B → `D-55`** |
| Q-48 | Cliente Angular | A) Generado desde OpenAPI · B) Servicios escritos a mano · C) Mixto | **A → `D-56`** |

## 3.5 Comparación de decisiones técnicas

Se documentan seis decisiones con sus alternativas. Los criterios se valoran cualitativamente para el contexto del proyecto: **un desarrollador, un servidor y un MVP académico** (`A-04`, `R-12`, `D-10`).

### 3.5.1 Estilo arquitectónico
**Contexto:** nueve módulos, transacciones locales para la cascada de estados (`D-04`) y un único equipo.

| Criterio | A) Monolito modular + Clean | B) Microservicios | C) Monolito por capas simple |
|---|---|---|---|
| Complejidad de desarrollo | Media | Alta | Baja |
| Despliegue | Un artefacto | Nueve servicios | Un artefacto |
| Transacciones | Locales | Distribuidas | Locales |
| Testabilidad de reglas | Alta | Media | Baja |
| Costo operativo | Bajo | Alto | Bajo |
| Fronteras entre módulos | Por convención | Fuertes | Nulas |

**Decisión: A.** Es el único punto que combina transacciones locales, reglas aisladas y un solo despliegue.
**Descartadas:** B exigiría orquestación, observabilidad y transacciones distribuidas para una cascada que hoy es atómica; C deja las reglas dispersas en servicios y controladores y hace costoso probarlas.
**Consecuencia:** la separación de módulos se sostiene con disciplina y pruebas de arquitectura (3.4.6).

### 3.5.2 Aislamiento multitenant
**Contexto:** una base compartida, varias compañías y un catálogo público transversal (`D-22`, `D-36`).

| Criterio | A) Esquema compartido + filtro | B) Base por compañía | C) RLS de SQL Server | D) Esquema por compañía |
|---|---|---|---|---|
| Aislamiento | Medio (aplicación) | Alto | Alto (base) | Alto |
| Costo por compañía | Bajo | Alto | Medio | Medio |
| Migraciones | Una | N | Una | N |
| Catálogo público | Simple | Complejo (unión) | Requiere excepción | Complejo (unión) |
| Complejidad de implementación | Baja | Media | Alta | Media |

**Decisión: A.** Con un único punto de bypass controlado y pruebas de aislamiento.
**Descartadas:** B y D multiplican migraciones y hacen inviable el catálogo entre compañías; C se puede añadir después como defensa en profundidad, pero hoy suma complejidad de sesión, pruebas y excepciones.
**Consecuencia:** el `IgnoreQueryFilters()` del catálogo es el punto más sensible del sistema y debe tener una única implementación auditada.

### 3.5.3 Motor de persistencia
**Contexto:** consultas con filtros por compañía y proyecto, migraciones versionadas y agregaciones de lectura (`D-47`).

| Criterio | A) EF Core 10 | B) Dapper | C) ADO.NET |
|---|---|---|---|
| Productividad | Alta | Media | Baja |
| Control del SQL | Medio | Alto | Alto |
| Filtros globales y migraciones | Sí | No | No |
| Rendimiento en agregaciones | Medio | Alto | Alto |
| Curva de aprendizaje | Baja | Media | Alta |

**Decisión: A.** Los filtros globales son un requisito de seguridad (`D-36`) y las migraciones code-first ya están definidas.
**Descartadas:** B obligaría a escribir SQL y a implementar el filtro multitenant a mano en cada consulta; C multiplica el código de acceso a datos. Dapper podría usarse puntualmente para una consulta caliente, no como motor principal.
**Consecuencia:** las agregaciones se resuelven con proyecciones e índices (3.3.5); se debe evitar el N+1.

### 3.5.4 Motor de base de datos
**Contexto:** datos relacionales, transacciones, JSON para snapshots descartados y despliegue en contenedor Linux.

| Criterio | A) SQL Server 2022 | B) PostgreSQL | C) MySQL |
|---|---|---|---|
| Licenciamiento | Comercial | Libre | Libre / dual |
| Características relacionales | Alto | Alto | Medio |
| Contenedor Linux | Sí | Sí | Sí |
| Ecosistema .NET | Alto | Alto | Medio |
| Costo operativo | Medio | Bajo | Bajo |

**Decisión: A**, ya definida en `03 §6` y `04 §1`.
**Descartadas:** B es una alternativa sólida y gratuita, pero cambiar el motor obliga a revisar migraciones, tipos y despliegue sin un problema medido que lo justifique; si el licenciamiento se vuelve un bloqueo, es la primera opción de reemplazo. C ofrece menos capacidades para este modelo.
**Consecuencia:** el costo de licencia queda como riesgo abierto (3.1.9).

### 3.5.5 Modelo del EDT
**Contexto:** jerarquía recursiva con código único por proyecto entre capítulos e ítems (`D-30`).

| Criterio | A) Tabla única `WbsNode` | B) Tablas separadas + `WbsCode` | C) Validación solo en aplicación |
|---|---|---|---|
| Unicidad garantizada en base | Sí | Sí | No |
| Fidelidad al modelo `05` | Baja | Alta | Alta |
| `JOIN` adicional | No | Sí | No |
| Refactor respecto a `05` | Alto | Medio | Nulo |
| Duplicados por concurrencia | No | No | Posibles |

**Decisión: B.**
**Descartadas:** A obliga a columnas nulas según el tipo y a un refactor mayor; C deja la unicidad a la aplicación y dos usuarios podrían crear el mismo código a la vez.
**Consecuencia:** dos inserciones por nodo y sincronización entre `WbsCode` y la entidad.

### 3.5.6 Registro de aprobaciones
**Contexto:** cada APU requiere una aprobación para el cierre (`D-14`), sin historial de cambios (`D-08`).

| Criterio | A) Por usuario/tipo, invalida al editar | B) Por usuario/tipo, conserva | C) Un registro por APU |
|---|---|---|---|
| Detalle por usuario | Alto | Alto | Bajo |
| Verificación de varios auditores | Sí | Sí | No |
| Simplicidad de modelo | Media | Media | Alta |
| Historial de aprobaciones | Sí | Sí | No |
| Re-trabajo al editar | Alto | Bajo | Alto |

**Decisión: C.**
**Descartadas:** A y B conservan detalle por usuario, pero añaden tabla histórica y reglas de invalidación que el proyecto no requiere; C simplifica el modelo y mantiene la trazabilidad mínima exigida.
**Consecuencia:** se pierde la verificación de múltiples auditores (`X-26`) y toda edición elimina la aprobación (`D-53`). Es una limitación aceptada, no un efecto colateral no previsto.

### 3.5.7 Resumen de trade-offs
| Decisión | Elegida | Alternativa principal descartada | Costo aceptado |
|---|---|---|---|
| Estilo arquitectónico | Monolito modular + Clean | Microservicios | Fronteras sostenidas por convención y pruebas |
| Multitenancy | Esquema compartido + filtro | RLS / base por compañía | Bypass único sensible |
| Persistencia | EF Core 10 | Dapper | Rendimiento en agregaciones |
| Base de datos | SQL Server 2022 | PostgreSQL | Licenciamiento |
| Modelo del EDT | Tablas separadas + `WbsCode` | Tabla única | `JOIN` y doble inserción |
| Aprobaciones | Un registro por APU | Registro por usuario | Sin verificación multi-auditor |

---

**Estado del capítulo 3:** cerrado (v0.19). 3.1 arquitectura, 3.2 stack, 3.3 persistencia, 3.4 componentes y 3.5 comparaciones completos. Ver capítulo 4.

---

# 4. Estrategia de desarrollo

> **Estado:** borrador. Las decisiones `Q-49` a `Q-53` están abiertas; el resto se deriva de `D-09`, `D-10`, `D-20`, `D-40`, `D-46` y de los incrementos del capítulo 2.

## 4.1 Metodología de trabajo

### 4.1.1 SDD con OpenSpec
El desarrollo sigue **Spec-Driven Development** con OpenSpec (`D-09`). La especificación es el artefacto principal: el código se deriva de ella y se verifica contra ella.

Cada cambio recorre este flujo:

| # | Paso | Artefacto | Criterio de salida |
|---|---|---|---|
| 1 | Propuesta | Propuesta de cambio | Problema, alcance, RF/BR afectados y criterios de aceptación |
| 2 | Delta de especificación | Cambio de spec | Requisitos añadidos, modificados o eliminados, verificables |
| 3 | Diseño técnico (si aplica) | Documento de diseño | Decisiones, riesgos y alternativas |
| 4 | Tareas | Lista de tareas | Tareas pequeñas con criterio de hecho |
| 5 | Implementación | Código y pruebas | Compila y pruebas en verde |
| 6 | Verificación | Evidencia | Criterios cumplidos y revisión |
| 7 | Archivo | Cambio archivado | Especificaciones actualizadas |

**Reglas:**
1. No se escribe código sin un cambio aprobado.
2. Ningún cambio se archiva sin pruebas y sin actualizar la especificación.
3. Un cambio cubre una funcionalidad o un grupo pequeño; `I-3b` se divide en varios cambios.
4. Las ideas nuevas van al backlog, no al cambio activo.
5. Todo cambio referencia los `RF`, `BR` y decisiones que implementa.

### 4.1.2 Organización del trabajo
| Elemento | Definición |
|---|---|
| Iteración | Una etapa de 4.2; no se fijan fechas (`D-19`, `D-58`) |
| Tablero | Backlog → Especificación → Implementación → Verificación → Archivado |
| Rama | GitFlow: `main`, `develop`, `feature/*`, `release/*`, `hotfix/*` (`D-59`) |
| Commits | Conventional Commits (`D-60`) |
| Revisión | Autoevaluación con checklist y CI (`D-62`) |
| Documentación | `PLAN_INGENIERIA.md` es la fuente; los documentos `00`–`09` se actualizan al cerrar cada capítulo (`D-09`) |

**Control de cambios:** una idea nueva no modifica el alcance del cambio en curso; se registra en el backlog y se prioriza al cerrar la etapa.

**Modelo de ramas (`D-59`):** `main` contiene lo entregado; `develop` integra los cambios; cada cambio usa `feature/<rf>-<descripcion>`; al cerrar una etapa se crea `release/<etapa>`; las correcciones urgentes usan `hotfix/*` y se integran a `main` y `develop`.

### 4.1.3 Roles y responsabilidades
| Rol | Responsabilidad |
|---|---|
| Estudiante | Decide, especifica, implementa, revisa y aprueba los cambios; es el único responsable de las decisiones de negocio |
| IA | Asiste en especificación, código y pruebas; nunca decide reglas de negocio ni aprueba cambios (capítulo 6) |
| Docente o par | Revisión externa opcional (`Q-53`) |

## 4.2 Etapas de implementación
| Etapa | Objetivo | Módulos | Funcionalidades | Entregable | Depende de |
|---|---|---|---|---|---|
| E-0 | Preparación técnica | Transversal | — | Repositorio, solución, `docker-compose` con SQL Server, CI en verde, OpenSpec inicializado y seeds | — |
| E-1 | Base de acceso y organización | M-01 | RF-01, RF-02, RF-18, RF-23 | Autorregistro público, login, alta de compañía, usuarios y roles, correos de cuenta y recuperación | E-0 |
| E-2 | Proyecto y estructura | M-02, M-03 | RF-03, RF-04 | Proyecto configurado (divisa, AIU, tope, firma, visibilidad) con su EDT y códigos | E-1 |
| E-3a | Insumos del proyecto | M-04 | RF-05, RF-06, RF-16, RF-20 | Vista de insumos con listado, filtros, estados y copia desde otros proyectos | E-2 |
| E-3b | APUs y matrices | M-05, M-06 (RF-22) | RF-07, RF-08, RF-09, RF-22 | APU 1:1 con matrices, copia desde proyectos de la compañía, reconciliación de insumos y conteo de uso | E-3a |
| E-4a | Cálculo y cotización | M-06, M-09 | RF-10, RF-11, RF-14, RF-17 | Presupuesto calculado, insumos cotizados en masa, autoría y alerta de tope | E-3b |
| E-4b | Aprobaciones y cierre | M-07 | RF-12, RF-13, RF-21 | Aprobación por APU según el modo configurado y cierre a `FINALIZADO` | E-4a |
| E-5 | Catálogo abierto | M-08 | RF-15 | Consulta y copia de APUs e insumos públicos; completa la copia pública de RF-07 y RF-08 | E-4b |
| E-6 | Endurecimiento y entrega | Transversal | — | Seguridad, rendimiento, observabilidad, respaldo documentado, despliegue local y documentación final | E-5 |

Las etapas `E-3a`/`E-3b` y `E-4a`/`E-4b` corresponden a la división de `I-3` e `I-4` aprobada en `Q-25`.

## 4.3 Orden de desarrollo de funcionalidades
| Orden | Etapa | Funcionalidades | Motivo del orden |
|---|---|---|---|
| 1 | E-0 | — | Sin pipeline ni base local no se puede verificar nada |
| 2 | E-1 | RF-01, RF-02, RF-18, RF-23 | Todo lo demás requiere usuario, compañía y roles; el autorregistro es la vía de entrada sin intervención administrativa |
| 3 | E-2 | RF-03, RF-04 | El proyecto y la EDT son el contenedor de insumos y APUs |
| 4 | E-3a | RF-05, RF-06, RF-16, RF-20 | El APU no existe sin insumos (`R-02`) |
| 5 | E-3b | RF-07, RF-08, RF-09, RF-22 | La composición y el conteo requieren insumos y cantidades de obra |
| 6 | E-4a | RF-10, RF-11, RF-14, RF-17 | No se cotiza ni alerta sin costo calculado |
| 7 | E-4b | RF-12, RF-13, RF-21 | No se aprueba ni cierra sin cálculo y cotización |
| 8 | E-5 | RF-15 | El catálogo depende de proyectos públicos con APUs completos |
| 9 | E-6 | — | Cierra calidad, seguridad y entrega |
| — | — | RF-19 | Excluida del MVP (`D-18`) |

**Notas de alcance:**
- `RF-07` se entrega en dos partes: copia desde proyectos de la compañía en E-3b y desde el catálogo público en E-5.
- `RF-22` se implementa en E-3b porque necesita APUs y cantidades, aunque su módulo sea `M-06`.
- El catálogo público se deja al final por ser el punto de mayor exposición de información (`RT-11`).

## 4.4 Dependencias entre componentes

### 4.4.1 Orden de construcción dentro de cada etapa
| # | Componente | Depende de | Entregable |
|---|---|---|---|
| 1 | Migración y esquema | `docker-compose` y `D-40` | Base local con el esquema de la etapa |
| 2 | Dominio | Esquema y reglas del cambio | Entidades, servicios y pruebas unitarias |
| 3 | Aplicación | Dominio | Casos de uso, DTOs y validaciones |
| 4 | Infraestructura | Aplicación | Repositorios y configuración EF |
| 5 | API | Aplicación e infraestructura | Endpoints y contrato OpenAPI |
| 6 | Cliente y UI | Contrato OpenAPI (`D-56`) | Servicios y pantallas de la funcionalidad |

### 4.4.2 Dependencias técnicas
| Componente | Depende de | Riesgo si falta | Mitigación |
|---|---|---|---|
| Cálculo (RF-10) | Fórmulas cerradas (`X-15`) | Costos incorrectos | Resolver `X-15` antes de abrir E-4a |
| Aprobaciones (RF-12, RF-13) | APUs y modo de firma | Cierre inválido | E-4b después de E-4a |
| Catálogo (RF-15) | Proyectos públicos con APUs e insumos | Catálogo vacío o con fugas | E-5 al final y pruebas de aislamiento |
| Correos (RF-18) | Proveedor elegido (`D-45`) | E-1 bloqueada | Definir SendGrid, Mailgun o SES antes de E-1 |
| Cliente Angular | OpenAPI (`D-42`, `D-56`) | Contrato y cliente divergen | Regenerar en CI |
| Alertas (RF-17) | Cálculo y tope configurado | Alertas falsas | E-4a después de RF-10 |
| Conteo de uso (RF-22) | APUs y cantidades de obra | Conteos erróneos | Implementar al cerrar E-3b |
| Migraciones | Contenedor SQL de E-0 | Sin base no hay pruebas | Compose y runbook en E-0 |
| Cliente y UI | Contrato estable | Retrabajo | Congelar contrato antes de la UI |

### 4.4.3 Ruta crítica y bloqueos
1. **`X-15` bloquea E-4a.** Sin fórmulas verificables no se implementa el motor ni se pueden definir criterios de aceptación de costos.
2. **`D-45` bloquea RF-18 en E-1.** El correo es parte del alta de usuarios y la recuperación de contraseña.
3. **El licenciamiento de SQL Server** no bloquea el MVP local, pero sí cualquier despliegue real.
4. **TLS (`D-39`)** impide exponer la aplicación fuera del entorno local hasta que se resuelva.
5. **La aprobación de un cambio** no puede depender de código aún no escrito: las dependencias se verifican al cerrar la etapa anterior.

## 4.5 Criterios de terminado

### 4.5.1 Preparado para empezar (Definition of Ready)
Un cambio puede iniciar cuando:
1. Tiene propuesta aprobada y delta de especificación.
2. Identifica los `RF`, `BR` y decisiones afectadas.
3. Tiene criterios de aceptación verificables.
4. Sus dependencias están cerradas.
5. Tiene plan de pruebas y datos de prueba definidos.

### 4.5.2 Terminado (Definition of Done)
Un cambio se considera terminado cuando:
1. Compila sin errores y sin advertencias de analizadores.
2. Las pruebas unitarias del dominio y la aplicación están en verde.
3. Las verificaciones de integración se ejecutan manualmente con checklist y evidencia (`D-66`).
4. La cobertura es de al menos 80 % en dominio y motor de cálculo (`D-61`).
5. La migración está aplicada, documentada y con reversión conocida.
6. El contrato OpenAPI está actualizado y el cliente Angular regenerado.
7. Las respuestas públicas no exponen datos prohibidos (`RT-11`, `R-23`).
8. El delta de especificación está actualizado y el cambio archivado.
9. La documentación `00`–`09` se actualizó si el cambio la afecta (`D-09`).
10. El flujo se demostró manualmente y no quedan `TODO` sin registrar.

### 4.5.3 Criterios de salida por etapa
Las verificaciones de integración de esta tabla son manuales (`D-66`); las unitarias, de arquitectura y de contrato se ejecutan en CI.

| Etapa | Flujo demostrable | Evidencia mínima |
|---|---|---|
| E-0 | Compose levanta API y base; `/health` responde | Pipeline en verde y contenedores arriba |
| E-1 | Un visitante se autorregistra sin sesión y un Administrador crea compañía y usuario; llegan los dos correos | Prueba de autorización, de registro y de correo |
| E-2 | Proyecto configurado con EDT, numeración y búsqueda por código | Pruebas de jerarquía y unicidad |
| E-3a | Insumos listados, filtrados, cotizados y copiados entre proyectos | Pruebas de filtros y copia |
| E-3b | APU con matriz, insumos reconciliados y conteo de uso | Pruebas de composición, matriz y conteo |
| E-4a | Presupuesto calculado, cotización masiva y alerta de tope | Pruebas del motor y de la alerta |
| E-4b | Aprobación por APU y cierre bloqueado si falta aprobación o cotización | Pruebas de aprobación y cierre |
| E-5 | Catálogo público con copia de APUs e insumos, sin datos privados | Pruebas de aislamiento y DTO |
| E-6 | Despliegue local documentado, respaldo probado y revisión de rendimiento | Runbook y resultados |

### 4.5.4 Puertas de integración continua
| Puerta | Criterio |
|---|---|
| Build | Compilación sin errores |
| Pruebas | Unitarias, arquitectura y contrato en verde; integración manual documentada (`D-66`) |
| Cobertura | ≥ 80 % en dominio y motor de cálculo (`D-61`); dominio y aplicación como meta |
| Estilo | Analizadores y ESLint sin errores |
| Contrato | OpenAPI válido y cliente regenerado sin diferencias inesperadas |
| Seguridad | Sin secretos en el repositorio y sin datos sensibles en DTOs públicos |

## 4.6 Riesgos de la estrategia
| Riesgo | Efecto | Mitigación |
|---|---|---|
| `X-15` sin cerrar | Bloquea E-4a y los criterios de costo | Resolverlo al inicio de la etapa, antes de escribir código |
| E-3b concentra demasiado | Retrasa el cierre de la etapa | Dividirla en cambios pequeños de OpenSpec |
| Un solo desarrollador | Sin revisión independiente | Autoevaluación con checklist y CI estricto (`Q-53`) |
| Sin usuario piloto | Requisitos no validados en uso real | Pruebas con datos sintéticos y revisión del flujo con el Director (`D-19`) |
| Proveedor de correo sin definir | Bloquea E-1 | Elegir proveedor antes de iniciar la etapa (`D-45`) |
| Generación del cliente | Deriva entre contrato y UI | Regenerar en CI y fallar si hay diferencias (`D-56`) |
| Crecimiento de alcance | Etapas que no cierran | Backlog separado y regla de control de cambios (4.1.2) |
| Licenciamiento de SQL Server | Bloquea despliegue real | Definir edición antes de E-6 |
| Autorregistro sin control de abuso | Cuentas falsas o masivas (`RT-22`) | Decidir límite de intentos y verificación de correo antes de abrir E-1 |

## 4.7 Decisiones de estrategia
| ID | Pregunta | Opciones | Estado |
|---|---|---|---|
| Q-49 | Cadencia de trabajo | A) Una iteración por etapa, sin fechas · B) Sprints de dos semanas · C) Sprints de una semana · D) Hitos por capítulo | **A → `D-58`** |
| Q-50 | Estrategia de ramas | A) Troncal con ramas cortas y PR · B) GitFlow · C) Commits directos a la principal | **B → `D-59`** |
| Q-51 | Convención de commits | A) Conventional Commits · B) Descripción libre · C) Otra convención | **A → `D-60`** |
| Q-52 | Umbral de cobertura | A) ≥ 80 % en dominio y motor de cálculo · B) ≥ 80 % en dominio y aplicación · C) ≥ 80 % en todo el backend · D) Sin umbral | **A → `D-61`** (B como meta) |
| Q-53 | Revisión de cambios | A) Autoevaluación con checklist y CI · B) Revisión por par o docente · C) Sin revisión formal | **A → `D-62`** (B opcional) |

---

**Capítulo 4 cerrado** (v0.21). Siguiente: **capítulo 5 — Estrategia de calidad**.

---

# 5. Estrategia de calidad

> **Estado:** borrador. Las decisiones `Q-54` a `Q-57` están abiertas; el resto se deriva de `D-46`, `D-50`, `D-52`, `D-61` y `D-62`.

## 5.1 Principios
1. **Calidad verificable:** cada `RF` y `BR` tiene al menos una prueba o una evidencia de aceptación.
2. **Prevención antes que detección:** especificación, revisión y pruebas desde el dominio, no al final.
3. **Esfuerzo proporcional al riesgo:** el motor de cálculo, el aislamiento multitenant y el cierre reciben más pruebas que el CRUD.
4. **Automatización donde aporta evidencia repetible:** unitarias, arquitectura y contrato; la integración, el E2E y la aceptación se verifican manualmente con checklist (`D-65`, `D-66`).
5. **Evidencia:** resultados de CI para lo automatizado y checklists firmados para lo manual.

## 5.2 Tipos de prueba
| Tipo | Alcance | Herramientas | Cuándo | Automatización |
|---|---|---|---|---|
| Unitaria de dominio | `ApuCalculator`, reglas de estado, aprobaciones, numeración y reconciliación | xUnit, FluentAssertions | En cada cambio | CI |
| Unitaria de aplicación | Casos de uso con dependencias simuladas | xUnit, Moq, FluentAssertions | En cada cambio | CI |
| Integración de persistencia | Repositorios, filtros globales, migraciones y restricciones | SQL Server local + checklist | Cambios de datos | Manual (`D-66`) |
| Integración de API | Endpoints, autorización, códigos HTTP y `ProblemDetails` | Swagger/curl + SQL Server local | Cambios de API | Manual (`D-66`) |
| Contrato | OpenAPI válido y cliente generado sin diferencias | Swashbuckle + generador | Cambios de contrato | CI |
| Arquitectura | Reglas de capas y módulos | NetArchTest (`D-63`) | Cada cambio | CI |
| Seguridad y aislamiento | Acceso entre compañías, DTO público, bypass del catálogo y autorregistro anónimo | Dos compañías y dos proyectos | Cambios de autorización o consultas | Manual (`D-66`) |
| Extremo a extremo | Login, alta de APU, cierre y catálogo | Checklist manual (`D-65`) | Cierre de etapa | Manual |
| Migraciones | Base limpia y base existente | Runbook + SQL Server | Cada migración | Manual |
| Aceptación | Flujo demostrable por etapa | Checklist y demo | Cierre de etapa | Manual |

**Lo que no se prueba:** getters, configuración de EF, código generado del cliente y detalles de infraestructura sin lógica.

## 5.3 Pirámide, cobertura y datos de prueba
| Elemento | Definición |
|---|---|
| Distribución | Automatizado: unitarias, arquitectura y contrato. Manual: integración, E2E, migraciones y aceptación |
| Cobertura | ≥ 80 % en dominio y motor (`D-61`); dominio + aplicación como meta |
| Calidad de la cobertura | Se mide sobre reglas y ramas, no sobre líneas triviales |
| Datos | Constructores de prueba y seeds; nunca datos reales de una empresa |
| Nombres | `Metodo_Escenario_ResultadoEsperado` |
| Aislamiento | Cada prueba crea y limpia sus datos; sin dependencia de orden |
| Documentación | Las pruebas del motor son la referencia ejecutable de las fórmulas |

## 5.4 Registro de riesgos
El capítulo 1 mantiene `RT-01` a `RT-12`. Este capítulo agrega los riesgos de calidad y operación:

| ID | Riesgo | Prob. | Impacto | Exposición | Mitigación | Verificación |
|---|---|---|---|---|---|---|
| RT-13 | Fuga de datos entre compañías por un filtro mal aplicado o un bypass indebido | Media | Alta | Alta | Un único bypass, revisión obligatoria y verificación manual con dos compañías (`D-66`) | Checklist |
| RT-14 | Sobrescritura de cambios por edición concurrente | Media | Media | Media | `rowversion`, `409 Conflict` y recarga en el cliente (`D-50`) | Pruebas de concurrencia |
| RT-15 | Costo de licencia de SQL Server | Media | Media | Media | Definir edición antes de E-6; evaluar PostgreSQL como reemplazo | Revisión de costos |
| RT-16 | Exposición de tokens por ausencia de TLS | Alta | Alta | Alta | Solo despliegue local (`D-39`); prohibido publicar sin HTTPS | Lista de verificación de despliegue |
| RT-17 | Error humano en migraciones manuales | Media | Media | Media | Runbook, respaldo previo y verificación posterior (`D-40`) | Checklist de migración |
| RT-18 | Correos que no llegan o caen en spam | Media | Media | Media | Proveedor transaccional con SPF/DKIM y envío de prueba (`D-45`) | Prueba de entrega |
| RT-19 | Deriva entre OpenAPI y el cliente Angular | Media | Media | Media | Regeneración en CI y fallo ante diferencias (`D-56`) | Puerta de contrato |
| RT-20 | Regresiones de interfaz no detectadas | Media | Media | Media | Checklist manual de flujos críticos al cerrar etapa (`D-65`) | Checklist |
| RT-21 | Degradación de rendimiento no detectada (sin requisito medible) | Media | Media | Media | Índices y revisión manual de consultas; `NFR-003` eliminado (`D-67`) | Revisión manual |
| RT-22 | Abuso del autorregistro: cuentas falsas, creación masiva o enumeración de correos y usuarios por el endpoint público | Media | Media | Media | Control de límite de intentos y verificación por correo antes de E-1 (`D-72`); mientras no exista, solo despliegue local o demo (`D-39`); nombre de usuario único (`D-73`) reduce suplantación | T-I-21, T-S-07 |

**Riesgos aceptados:** `RT-01` (sin historial de valores), `RT-05` (catálogo sin curaduría), `RT-06` (auditor interno), `RT-10` y `RT-11` (exposición al publicar) y `RT-16` (TLS postergado). Cada uno tiene una condición explícita en su decisión de origen.

## 5.5 Estrategias de mitigación
| Estrategia | Acción | Riesgos que cubre | Evidencia |
|---|---|---|---|
| Prevención | Especificación previa, DoR y revisión de cambios | RT-13, RT-19 | Cambio aprobado y checklist |
| Prevención | Pruebas unitarias del motor y de las reglas | RT-02, RT-14 | Cobertura ≥ 80 % |
| Prevención | Verificación manual de integración con SQL Server local | RT-13, RT-17 | Checklist firmado |
| Prevención | Pruebas de arquitectura | RT-13, RT-19 | Reglas verificadas en CI |
| Detección | Puertas de CI y contrato | RT-19 | Pipeline en verde |
| Detección | Verificación manual de aislamiento y DTO público | RT-13, RT-10 | Checklist con dos compañías |
| Detección | Monitoreo de salud y logs estructurados | RT-18 | `/health` y logs |
| Corrección | Reversión de release y `hotfix/*` | RT-14, RT-18 | Rama y despliegue anterior |
| Corrección | Restauración de respaldo | RT-17 | Prueba de restauración en E-6 |
| Corrección | Reaprobación tras edición | RT-14 | `D-53` |

## 5.6 Verificación del cumplimiento de requisitos

### 5.6.1 Matriz funcionalidad → prueba
| RF | Verificación principal | Nivel |
|---|---|---|
| RF-01 | T-I-01, T-S-01, T-S-04 | Integración y seguridad |
| RF-02 | T-I-02, T-S-05 | Integración y seguridad |
| RF-03 | T-I-03 | Integración |
| RF-04 | T-U-01, T-I-04 | Unitaria e integración |
| RF-05 | T-U-02, T-I-05 | Unitaria e integración |
| RF-06 | T-I-06 | Integración |
| RF-07 | T-I-07, T-E-01 | Integración y E2E |
| RF-08 | T-U-03, T-I-08 | Unitaria e integración |
| RF-09 | T-U-04, T-I-09 | Unitaria e integración |
| RF-10 | T-U-05 | Unitaria |
| RF-11 | T-U-06, T-I-10 | Unitaria e integración |
| RF-12 | T-U-07, T-I-11 | Unitaria e integración |
| RF-13 | T-I-12 | Integración |
| RF-14 | T-I-13 | Integración |
| RF-15 | T-I-14, T-S-02, T-S-06 | Integración y seguridad |
| RF-16 | T-U-08, T-I-15 | Unitaria e integración |
| RF-17 | T-U-09, T-I-16 | Unitaria e integración |
| RF-18 | T-I-17 | Integración con proveedor simulado |
| RF-19 | — | Fuera del MVP (`D-18`) |
| RF-20 | T-I-18 | Integración |
| RF-21 | T-U-10, T-I-19 | Unitaria e integración |
| RF-22 | T-U-11, T-I-20 | Unitaria e integración |
| RF-23 | T-I-21, T-S-07 | Integración y seguridad |

**Convención de identificadores:** `T-U` unitaria, `T-I` integración, `T-S` seguridad, `T-C` contrato, `T-A` arquitectura, `T-E` extremo a extremo. `T-I` y `T-S` son verificaciones manuales (`D-66`).

### 5.6.2 Verificación de requisitos no funcionales
| NFR | Estado | Verificación |
|---|---|---|
| NFR-001 Seguridad y RBAC | Vigente | T-S-01 a T-S-07 y T-A-01 |
| NFR-002 Sanitización pública | **Modificado** por `D-28`: se publican precios y contactos | T-S-02 comprueba que no se expongan autoría, empresa ni firma; la prohibición de precios queda sin efecto |
| NFR-003 Rendimiento p95 ≤ 1.5 s | **Eliminado** por `Q-58` = B (`D-67`) | Ya no es requisito; no se verifica |
| NFR-004 Auditoría inmutable | **Obsoleto** por `D-08` y `D-52`: no hay bitácora histórica | Se reemplaza por T-I-12 (aprobación) y T-I-13 (último modificador) |
| NFR-005 Cobertura ≥ 80 % | Vigente, acotado por `D-61` | Puerta de cobertura en CI |

### 5.6.3 Reglas de negocio modificadas y su verificación
| BR | Estado | Verificación |
|---|---|---|
| BR-001 | **Modificado** por `D-02`/`X-25`: el Administrador no tiene compañía | T-S-04 |
| BR-004 | **Modificado** por `D-08` y `D-52`: último modificador y una aprobación por APU | T-I-12, T-I-13 |
| BR-006 | **Obsoleto** por `D-28`: el catálogo publica precios | T-S-02 |
| BR-007 y BR-011 | **Modificados** por `D-04`, `D-14` y `D-52`: cierre con todo cotizado y aprobación única | T-U-10, T-I-19 |
| BR-012 | Vigente: la firma se captura en el frontend y no se persiste | T-E-01 y revisión manual |
| Resto de BR | Vigentes | Pruebas unitarias del módulo correspondiente |

### 5.6.4 Evidencia de aceptación
1. Resultados de CI: pruebas unitarias, arquitectura, cobertura y contrato; checklists de integración manual (`D-66`).
2. Checklist de la etapa firmado por el estudiante (`D-62`).
3. Cambio archivado con su delta de especificación.
4. Demostración del flujo al cerrar la etapa (`D-58`).
5. Registro de riesgos actualizado.

## 5.7 Herramientas de prueba
| Herramienta | Uso | Estado |
|---|---|---|
| xUnit + FluentAssertions + Moq | Pruebas unitarias | CONFIRMADO (`03 §4`) |
| `WebApplicationFactory` | Integración automatizada | **NO SE USA** (`D-66`) |
| SQL Server local | Verificación manual de persistencia | CONFIRMADO (`D-66`) |
| NetArchTest | Reglas de capas y módulos | CONFIRMADO (`D-63`) |
| NBomber | Rendimiento y p95 | **NO SE USA**: `NFR-003` eliminado (`D-67`) |
| Playwright | Flujos extremo a extremo | **NO SE USA**: checklist manual (`D-65`) |
| Coverlet | Cobertura | Propuesto; se confirma al configurar CI |
| Swashbuckle + generador | Contrato y cliente | CONFIRMADO (`D-42`, `D-56`) |
| GitHub Actions | Ejecución de la suite | CONFIRMADO (`D-46`) |

## 5.8 Decisiones abiertas de calidad
| ID | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| Q-54 | Pruebas de arquitectura | A) NetArchTest · B) Revisión manual · C) Sin verificación automática | **A → `D-63`** |
| Q-55 | Pruebas de rendimiento | A) k6 · B) NBomber · C) Medición manual · D) Sin prueba | **D → `D-64`** |
| Q-56 | Pruebas extremo a extremo | A) Playwright · B) Cypress · C) Solo manual | **C → `D-65`** |
| Q-57 | Base de datos para integración | A) Testcontainers · B) Contenedor SQL como servicio en CI · C) Proveedor en memoria · D) Manual con SQL local | **D → `D-66`** |
| Q-58 | Verificación de `NFR-003` | A) Medición manual en E-6 · B) Eliminar `NFR-003` del alcance · C) Aceptar sin verificar | **B → `D-67`** |

**Criterios de las recomendaciones:**
- **NetArchTest** convierte las reglas de 3.4.6 en pruebas ejecutables; la revisión manual no detecta violaciones en cada cambio.
- **Con integración manual (`D-66`)**, la verificación se hace sobre SQL Server local con un checklist de dos compañías y dos proyectos; no hay puerta automática de aislamiento.

---

**Capítulo 5 cerrado** (v0.24). Siguiente: **capítulo 6 — SDD e inteligencia artificial**.

---

# 6. Uso de SDD e inteligencia artificial

> **Estado:** borrador. Las decisiones `Q-59` a `Q-62` están abiertas; el resto se deriva de `D-09`, `D-20`, `D-40`, `D-46`, `D-58`, `D-62` y `D-66`.

## 6.1 OpenSpec como sistema de especificación

### 6.1.1 Jerarquía de fuentes
| Artefacto | Contiene | Autoridad |
|---|---|---|
| `PLAN_INGENIERIA.md` | Problema, decisiones, arquitectura, plan | Racionalidad y alcance |
| `openspec/project.md` | Contexto, stack, convenciones y restricciones | Contexto operativo |
| `openspec/specs/` | Comportamiento por capacidad | **Fuente de verdad del comportamiento** |
| `openspec/changes/` | Cambios activos y archivados | Trabajo en curso e historial |
| `.github/docs/00`–`09` | Documentación narrativa del proyecto | Apoyo; se actualiza por capítulo (`D-09`) |
| Código y pruebas | Implementación y evidencia | Verificación |

**Regla de conflicto:** si dos artefactos discrepan sobre comportamiento, manda la especificación de OpenSpec; si discrepan sobre alcance o decisión, manda `PLAN_INGENIERIA.md`. En ambos casos se detiene el cambio y se resuelve antes de continuar.

### 6.1.2 Estructura propuesta
```text
openspec/
  project.md
  specs/
    identidad/spec.md
    proyectos/spec.md
    edt/spec.md
    insumos/spec.md
    apus/spec.md
    calculo/spec.md
    aprobaciones/spec.md
    catalogo/spec.md
    alertas/spec.md
  changes/
    e3b-rf07-copia-apu/
      proposal.md
      design.md
      tasks.md
      specs/apus/spec.md
  changes/archive/
```

Una **capacidad** por módulo; un **cambio** por funcionalidad o grupo pequeño; los cambios archivados conservan el historial sin necesidad de una bitácora aparte (`D-08`). El identificador `e3b-rf07-copia-apu` es solo un ejemplo: cada cambio elige un nombre libre, único y descriptivo (`D-68`).

### 6.1.3 Ciclo de un cambio
| # | Paso | Artefacto | Puerta |
|---|---|---|---|
| 1 | Propuesta | `proposal.md` | Problema, alcance, `RF`/`BR` y criterios de aceptación |
| 2 | Delta de especificación | `specs/.../spec.md` | Requisitos añadidos, modificados o eliminados, con escenarios |
| 3 | Diseño técnico | `design.md` | Decisiones, riesgos y alternativas (si aplica) |
| 4 | Tareas | `tasks.md` | Tareas pequeñas con criterio de hecho |
| 5 | Implementación | Código y pruebas | Compila y pruebas en verde |
| 6 | Verificación | Evidencia | Escenarios cumplidos y checklist de revisión |
| 7 | Archivo | `changes/archive/` | Especificación actualizada y `DoD` cumplido |

**Reglas:**
1. No se escribe código sin propuesta aprobada.
2. Un cambio cubre una funcionalidad o un grupo pequeño; `E-3b` se divide en varios.
3. No se archiva un cambio con escenarios sin verificar o con un `X` abierto que lo afecte.
4. Cada requisito debe tener al menos un escenario y un identificador de prueba.
5. Las ideas nuevas van al backlog, no al cambio activo (4.1.2).

### 6.1.4 Convenciones de especificación
| Elemento | Convención |
|---|---|
| Identificador de requisito | `REQ-<CAPACIDAD>-<NN>` (ej. `REQ-APUS-03`) |
| Escenario | `Dado / Cuando / Entonces` |
| Referencias | Cada requisito cita los `RF`, `BR` y `D` que implementa |
| Casos negativos | Obligatorios: permisos, datos inválidos, concurrencia y aislamiento |
| Nivel de detalle | Comportamiento observable; sin estilos, nombres de clases ni SQL |
| Idioma | Español (`D-69`); código e identificadores técnicos en inglés |
| Trazabilidad | Tabla requisito → escenario → prueba en cada cambio |

### 6.1.5 Relación con la documentación existente
- El apartado de Spec Kit de `04 §5` y el documento `09` quedan **obsoletos** (`D-09`).
- `PLAN_INGENIERIA.md` no se copia en OpenSpec: lo alimenta y conserva la justificación.
- Los documentos `00`–`09` se actualizan al cerrar cada capítulo, no en cada cambio.
- El archivo de cambios de OpenSpec es el historial; no se mantiene una bitácora paralela.

## 6.2 Del plan de ingeniería a las especificaciones

### 6.2.1 Método de derivación
1. **Seleccionar la capacidad** afectada (6.2.3).
2. **Extraer los requisitos** de los `RF`, `BR`, `R` y decisiones que aplican.
3. **Escribir escenarios** positivos y negativos en formato `Dado / Cuando / Entonces`.
4. **Definir criterios de aceptación** y asociar los identificadores de prueba.
5. **Verificar dependencias y `X` abiertos**: si hay un `X` que afecta el comportamiento, el cambio no inicia.
6. **Dividir en cambios y tareas** del tamaño de una etapa.
7. **Revisar contra el plan**: alcance, decisiones y restricciones.

### 6.2.2 Reglas de derivación
| Fuente | Se convierte en | Ejemplo |
|---|---|---|
| `RF` | Requisito con escenarios de aceptación | RF-07 → copia de APU desde compañía y catálogo |
| `BR` | Restricción con al menos un escenario negativo | BR-008 → matriz no anidable |
| `D` | Restricción de diseño o de proceso | `D-52` → un registro de aprobación por APU |
| `R` | Límite verificable | `R-19` → códigos únicos y longitud máxima |
| `NFR` | Requisito no funcional o eliminación expresa | `NFR-003` eliminado por `D-67` |
| `X` | Bloqueo temporal del cambio | `X-15` → no especificar cálculo hasta cerrarlo |
| `E` | Fuera de alcance, no genera requisito | `E-05` → sin historial de valores |

### 6.2.3 Mapa de capacidades
| Capacidad | Spec | Funcionalidades | Etapa |
|---|---|---|---|
| Identidad | `identidad` | RF-01, RF-02, RF-18, RF-23 | E-1 |
| Proyectos | `proyectos` | RF-03 | E-2 |
| EDT | `edt` | RF-04 | E-2 |
| Insumos | `insumos` | RF-05, RF-06, RF-16, RF-20 | E-3a |
| APUs | `apus` | RF-07, RF-08, RF-09, RF-22 | E-3b |
| Cálculo | `calculo` | RF-10 | E-4a |
| Aprobaciones | `aprobaciones` | RF-11, RF-12, RF-13, RF-14, RF-21 | E-4b |
| Catálogo | `catalogo` | RF-15 | E-5 |
| Alertas | `alertas` | RF-17 | E-4a |

### 6.2.4 Ejemplo de transformación: RF-07
```text
REQ-APUS-01 Copia desde un proyecto de la compañía
Dado un APU de un proyecto de la misma compañía
Cuando el usuario lo copia a otro proyecto
Entonces se crea un APU nuevo con la misma composición, sin aprobación,
con el código del ítem destino y el usuario que copió como último modificador.

REQ-APUS-02 Copia desde el catálogo público
Dado un APU de un proyecto público
Cuando el usuario lo copia
Entonces se crea una copia independiente y se aplican los mismos controles.

REQ-APUS-03 Reconciliación de insumos
Dado un APU con insumos que ya existen o son similares en el destino
Cuando el usuario confirma la incorporación
Entonces puede usar el insumo existente o conservar el de origen y agregar uno nuevo;
el sistema solo sugiere y no decide.

REQ-APUS-04 Prohibición de copia privada
Dado un APU privado o de otra compañía
Cuando se intenta copiar
Entonces la operación se rechaza sin revelar la existencia del APU.
```

**Trazabilidad:** `REQ-APUS-01` a `03` → T-I-07; `REQ-APUS-04` → T-S-06.

### 6.2.5 Criterio de especificación lista
Una especificación puede pasar a implementación cuando:
1. Tiene alcance, `RF`/`BR` y decisiones referenciadas.
2. Incluye escenarios positivos y negativos.
3. Define criterios de aceptación verificables.
4. Cubre permisos, datos inválidos, concurrencia y aislamiento.
5. No depende de un `X` abierto.
6. Tiene trazabilidad a pruebas.
7. No contiene detalles de implementación.
8. Fue revisada y aprobada por el estudiante.

## 6.3 Delegación a la inteligencia artificial

### 6.3.1 Principio
La IA **propone**; el estudiante **decide y aprueba**. La IA no aprueba cambios, no fusiona ramas, no aplica migraciones ni autoriza despliegues.

### 6.3.2 Actividades delegables
| Actividad | Rol de la IA | Rol del estudiante | Evidencia |
|---|---|---|---|
| Redactar una especificación | Borrador a partir de `RF`, `BR` y `D` | Revisa, corrige y aprueba | Cambio firmado |
| Código repetitivo | Entidades, DTOs, validadores, mapeos y endpoints | Revisa lógica, nombres y dependencias | Diff revisado |
| Pruebas unitarias | Borrador de casos y aserciones | Verifica que prueben comportamiento y cubran negativos | Cobertura y revisión |
| Componentes de interfaz | Estructura y estilos base | Revisa usabilidad, accesibilidad y datos | Revisión visual |
| Migraciones | Borrador de la migración | Revisa el SQL, respalda y aplica manualmente (`D-40`) | Script revisado |
| Configuración de CI | Borrador del flujo | Verifica puertas y secretos | Pipeline en verde |
| Documentación | Borrador de secciones | Valida contra decisiones y código | Documento actualizado |
| Refactorizaciones | Propuesta de cambio pequeño | Aprueba o rechaza | Diff y pruebas |
| Depuración | Hipótesis y parches sugeridos | Reproduce, verifica y cierra | Prueba de regresión |

### 6.3.3 Actividades no delegables
1. Reglas de negocio y fórmulas (`X-15`).
2. Alcance, prioridades y criterios de aceptación.
3. Modelo de datos y decisiones de persistencia.
4. Seguridad, autorización y exposición pública.
5. Aprobación de especificaciones y de cambios.
6. Fusión de ramas, versiones y despliegues.
7. Aplicación de migraciones sobre bases reales.
8. Aceptación de riesgos y limitaciones.

### 6.3.4 Reglas de interacción
1. Entregar contexto: `RF`, decisiones, especificación y archivos implicados.
2. Pedir el **diff mínimo**; prohibido refactorizar fuera del alcance.
3. Ninguna dependencia nueva sin aprobación explícita.
4. Exigir pruebas junto con el código.
5. No incluir secretos, credenciales ni datos personales en los prompts.
6. Un cambio a la vez; no mezclar funcionalidades.
7. Revisar la salida antes de confirmar cualquier commit.
8. No se exige registro de asistencia (`D-70`); la revisión del cambio es la evidencia.
9. Si la IA no puede justificar una decisión, se detiene y se consulta el plan.

### 6.3.5 Riesgos del uso de IA
| Riesgo | Señal | Mitigación |
|---|---|---|
| API o paquete inexistente | Compila solo tras ajustes manuales | Versiones fijas y revisión de dependencias |
| Requisito inventado | El código hace algo que la spec no pide | Revisión contra escenarios |
| Sobreingeniería | Capas, patrones o abstracciones sin uso | Diff mínimo y revisión de alcance |
| Falla de seguridad | Falta filtro de compañía o autorización | Checklist y revisión de consultas |
| Pruebas de fachada | Aserciones triviales o sobre implementación | Revisión de pruebas y casos negativos |
| Dependencia inflada | Paquetes nuevos para tareas simples | Aprobación obligatoria |
| Licenciamiento | Código copiado sin verificar licencia | Revisión de origen y licencia |
| Fuga de datos | Secretos o datos reales en prompts | Prohibición y variables de entorno |
| Deriva del contrato | Cambios de API sin spec | Puerta de contrato y regeneración |

## 6.4 Verificación del código generado

### 6.4.1 Puertas automáticas
| Puerta | Criterio |
|---|---|
| Compilación | Sin errores |
| Analizadores | Sin advertencias |
| Unitarias | Dominio y aplicación en verde |
| Arquitectura | NetArchTest sin violaciones (`D-63`) |
| Contrato | OpenAPI válido y cliente regenerado (`D-56`) |
| Cobertura | ≥ 80 % en dominio y motor (`D-61`) |
| Seguridad básica | Sin secretos en el repositorio |

La integración y el E2E son manuales (`D-65`, `D-66`) y no bloquean la CI, pero sí la salida de etapa.

### 6.4.2 Revisión manual obligatoria
1. El código cumple los escenarios de la especificación, sin comportamiento extra.
2. No inventa reglas, campos ni estados.
3. No agrega dependencias sin aprobación.
4. Aplica el filtro de compañía y no usa el bypass fuera del catálogo.
5. No hay `DbContext` en la API ni lógica de negocio en controladores.
6. No duplica fórmulas: usa `ApuCalculator`.
7. Respeta la aprobación única y la invalidación al editar (`D-52`, `D-53`).
8. No expone autoría, empresa ni firma en respuestas públicas.
9. Los errores usan `ProblemDetails` y no filtran trazas.
10. Las pruebas verifican comportamiento y cubren casos negativos.
11. Nombres, formato y estructura siguen las convenciones.
12. La migración fue revisada y tiene reversión conocida.
13. El autorregistro es anónimo y no permite fijar rol, compañía ni proyecto desde el cliente (`D-72`); el `username` es obligatorio y único (`D-73`).

### 6.4.3 Evidencia
- Resultado de CI y cobertura.
- Diff revisado con alcance declarado.
- Checklist de 6.4.2 firmado.
- Cambio archivado con escenarios verificados.
- No se exige registro de uso de IA (`D-70`).

### 6.4.4 Señales de alerta
- Diff grande para un cambio pequeño.
- Pruebas que pasan sin ejercer la regla.
- Paquetes nuevos sin justificación.
- Cambios silenciosos de contrato.
- Lógica duplicada o fórmulas fuera del calculador.
- `TODO` sin registrar o código muerto.

### 6.4.5 Procedimiento ante fallo
1. Revertir el cambio en la rama.
2. Corregir la especificación si el error fue de definición.
3. Reimplementar con un diff menor y pruebas.
4. Registrar la causa en el cambio.
5. No aplicar parches silenciosos sobre código no verificado.

## 6.5 Decisiones que conserva el estudiante
| Decisión | Por qué no se delega | Momento |
|---|---|---|
| Alcance y prioridades | Define el contrato del producto | Inicio de etapa |
| Reglas de negocio y fórmulas | Determinan corrección y responsabilidad | Antes de la especificación |
| Modelo de datos | Cambiarlo después es costoso | Diseño |
| Seguridad, autorización y exposición pública | Riesgo de fuga o uso indebido | Diseño y revisión |
| Criterios de aceptación | Definen cuándo algo está terminado | Propuesta |
| Aprobación de la especificación y del cambio | Responsabilidad directa | Antes del código |
| Fusión, versión y despliegue | Control de la entrega | Después de verificar |
| Aplicación de migraciones | Riesgo sobre datos reales | Despliegue |
| Aceptación de riesgos y limitaciones | Responsabilidad académica | Revisión |

**Firma de aprobación:** cada cambio archivado incluye el nombre del estudiante, la fecha y la confirmación de que revisó especificación, código y evidencia.

## 6.6 Decisiones abiertas de SDD e IA
| ID | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| Q-59 | Identificador de cambio | A) `e3b-rf07-copia-apu` (etapa + RF + slug) · B) `0007-copia-apu` secuencial · C) Libre | **C → `D-68`** |
| Q-60 | Idioma de las especificaciones | A) Español (código en inglés) · B) Inglés · C) Bilingüe | **A → `D-69`** |
| Q-61 | Registro de uso de IA | A) `changes/<id>/ai.md` por cambio · B) `docs/ai-log.md` global · C) No exigido | **C → `D-70`** |
| Q-62 | Alcance de la delegación | A) La IA puede redactar specs y código; el estudiante aprueba · B) Solo código y pruebas · C) Solo documentación y pruebas | **A → `D-71`** |

**Criterios de las respuestas:**
- **Q-59 = C:** el identificador es libre; se recomienda que sea único y descriptivo para no perder trazabilidad.
- **Q-60 = A:** las especificaciones en español y el código en inglés, coherente con el documento y con la evaluación.
- **Q-61 = C:** sin registro de IA; la evidencia es el cambio revisado y el checklist firmado.
- **Q-62 = A:** la IA redacta especificaciones y código, pero el estudiante revisa y aprueba.

---

**Capítulo 6 cerrado** (v0.26). Documento completo: seis capítulos y 73 decisiones registradas (v0.28). Siguiente paso: crear la estructura de OpenSpec y el primer cambio (`E-0`).

