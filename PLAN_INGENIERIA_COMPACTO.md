# PLAN_INGENIERIA_COMPACTO.md

**Proyecto:** Budget Civil — Gestión de presupuestos de obra civil (APU/EDT)
**Uso:** documento de referencia para OpenSpec y para lectura humana. Resume `PLAN_INGENIERIA.md` (v0.28) sin su registro de decisiones.
**Regla de conflicto:** ante duda de comportamiento manda la especificación de OpenSpec; ante duda de alcance manda `PLAN_INGENIERIA.md`.

---

# 1. Análisis del problema

## 1.1 Identificación de la problemática
La gestión de presupuestos se hace en hojas de cálculo independientes por proyecto. Los APUs, insumos y rendimientos se reconstruyen manualmente; el cierre se valida a ojo y no hay autoría ni aprobación formal.

| ID | Problema | Consecuencia |
|---|---|---|
| P-01 | No se sabe quién modificó por última vez un APU o insumo ni quién aprobó | Sin responsabilidad ni evidencia |
| P-02 | Los APUs e insumos se reconstruyen por proyecto | Retrabajo y conocimiento aislado |
| P-03 | Fórmulas manuales y redondeos inconsistentes | Costos incorrectos |
| P-04 | Insumos equivalentes con nombres, unidades o precios distintos | Duplicidad y comparaciones inválidas |
| P-05 | Un presupuesto se cierra sin estar completo ni aprobado | Riesgo contractual |

No existen métricas históricas del proceso; los criterios se apoyan en supuestos declarados.

## 1.2 Necesidades de la institución
Existen tres instancias: la académica (evalúa), el operador de la plataforma (Administrador) y las empresas clientes (tenants).

| ID | Necesidad | Instancia |
|---|---|---|
| N-01 | Aislar información entre empresas y entre proyectos | Operador y empresas |
| N-02 | Controlar el alta de empresas y usuarios y habilitar el autorregistro | Operador |
| N-03 | Separar funciones por rol | Empresas |
| N-04 | Saber quién modificó por última vez | Empresas |
| N-05 | Aprobar según la política de cada proyecto | Empresas |
| N-06 | Cerrar solo con información completa y cotizada | Empresas |
| N-07 | Reutilizar APUs e insumos sin reconstruirlos | Empresas |
| N-08 | Evitar duplicidad de insumos | Empresas |
| N-09 | Reutilizar composiciones complejas (matrices) | Empresas |
| N-10 | Consultar composiciones de referencia | Empresas |
| N-11 | Calcular consumos, costos y AIU automáticamente | Empresas |
| N-12 | Entregar un aplicativo funcional, documentado y desarrollado con SDD | Académica |

## 1.3 Actores involucrados
| Rol | Ámbito | Puede | No puede |
|---|---|---|---|
| **Administrador** | Plataforma, sin compañía | Crear usuarios y compañías; cambiar roles globalmente; leer proyectos e insumos | Editar proyectos, APUs o insumos; pertenecer a una compañía |
| **Director de proyectos** | Una compañía y sus proyectos | Configurar compañía y proyecto; asignar usuarios; definir firma; cerrar el presupuesto | Firmar APUs; tener más de una compañía |
| **Presupuestador** | Proyectos asignados | Crear y editar EDT, insumos, APUs y matrices; cotizar; firmar APUs | Configurar proyecto o compañía |
| **Auditor** | Proyectos asignados | Leer toda la información; aprobar APUs cuando el proyecto lo exige | Editar contenido; aprobar fuera de sus proyectos |

**Reglas de pertenencia:** el Administrador no tiene compañía; un Director tiene máximo una; un usuario no puede estar en dos compañías; Presupuestador y Auditor pueden estar en cero o varios proyectos; todos editan su perfil. El **autorregistro** es público y anónimo (no exige sesión): crea la cuenta sin compañía ni proyecto y con nombre de usuario único (`D-72`, `D-73`).

## 1.4 Alcance y limitaciones

**Incluido en el MVP**
| ID | Capacidad |
|---|---|
| S-01 | Autorregistro público, usuarios, roles, asignación a compañía y proyecto, correos de cuenta y recuperación |
| S-02 | Compañías y configuración de proyecto: divisa, AIU, tope, modo de firma, requisito de auditor, visibilidad |
| S-03 | EDT recursiva con numeración automática editable, códigos únicos y búsqueda |
| S-04 | Insumos por proyecto: siete tipos, estados, filtros, sugerencia de unicidad y copia |
| S-05 | APUs 1:1 con el ítem, matrices, copia y reconciliación de insumos |
| S-06 | Cálculo de consumos, costo directo, AIU, redondeo y conteo de uso |
| S-07 | Cotización masiva, aprobación por APU y cierre a `FINALIZADO` |
| S-08 | Catálogo público de APUs e insumos de proyectos públicos |
| S-09 | Alertas por superación del tope presupuestal |
| S-10 | Autoría mínima: último modificador por APU e insumo |

**Fuera de alcance:** IA que asigne precios o cantidades; scraping o precios de proveedores; ejecución de obra, inventario, compras o facturación; CAD/BIM/3D; historial de cambios y valores; conversión de divisas; importación/exportación en el MVP; congelamiento o versionado del presupuesto; aplicación móvil, offline e integraciones ERP.

**Restricciones**
1. Un APU pertenece a un solo proyecto y se vincula 1:1 con un ítem.
2. Los insumos son propios de cada proyecto; su unicidad es una sugerencia, no una restricción.
3. La Matriz es un insumo compuesto, no pertenece a la EDT, no se anida y puede copiarse a otro proyecto.
4. El AIU se define solo por proyecto; una sola divisa por proyecto, sin conversión.
5. Se finaliza solo con el 100 % de insumos `COTIZADO` y la aprobación exigida.
6. La aprobación es única por APU; editar un APU aprobado elimina su aprobación.
7. La publicación es por proyecto: un proyecto público expone todos sus APUs e insumos.
8. Un Director no puede tener más de una compañía; el Administrador no tiene compañía.
9. El autorregistro es público y anónimo, sin sesión previa; el `username` es obligatorio y único, y la cuenta nace sin compañía ni proyecto.
---

# 2. Propuesta de solución

## 2.1 Descripción general
Plataforma web multiempresa que sustituye el Excel por un flujo verificable: la empresa organiza su EDT, administra insumos y APUs, calcula el presupuesto, gestiona cotización y aprobación, y publica composiciones en un catálogo abierto. No asigna precios ni cantidades por IA.

| # | Paso | Actor |
|---|---|---|
| 1 | Autorregistro del usuario (sin sesión) y alta administrativa de usuarios, compañías y Directores | Visitante y Administrador |
| 2 | Configuración del proyecto: divisa, AIU, tope, firma, auditor, visibilidad | Director |
| 3 | Asignación de Presupuestadores y Auditores | Director |
| 4 | Construcción de la EDT con códigos únicos | Presupuestador o Director |
| 5 | Registro o copia de insumos | Presupuestador o Director |
| 6 | Creación o copia de APU y reconciliación de insumos | Presupuestador o Director |
| 7 | Cálculo de costos y conteo de uso | Sistema |
| 8 | Cotización, incluido el cambio masivo a `COTIZADO` | Director y Presupuestador |
| 9 | Aprobación por APU según el modo configurado | Presupuestador y Auditor |
| 10 | Cierre a `FINALIZADO` | Director |
| 11 | Exposición en el catálogo si el proyecto es público | Sistema |

## 2.2 Funcionalidades prioritarias
| ID | Funcionalidad | Prioridad | MVP |
|---|---|---|---|
| RF-01 | Usuarios, roles y asignación a compañías y proyectos | Alta | Sí |
| RF-02 | Compañías: creación y datos de registro | Alta | Sí |
| RF-03 | Configuración de proyecto (divisa, AIU, tope, firma, auditor, visibilidad) | Alta | Sí |
| RF-04 | EDT recursiva, numeración y códigos únicos, búsqueda | Alta | Sí |
| RF-05 | Insumos: siete tipos, estados y sugerencia de unicidad | Alta | Sí |
| RF-06 | Copia de insumos entre proyectos de la compañía | Media | Sí |
| RF-07 | APUs 1:1 con el ítem; creación y copia | Alta | Sí |
| RF-08 | Reconciliación de insumos al incorporar un APU | Alta | Sí |
| RF-09 | Matrices reutilizables y copiables | Alta | Sí |
| RF-10 | Motor de cálculo: consumo, costo directo, AIU, redondeo | Alta | Sí |
| RF-11 | Cotización y cambio masivo a `COTIZADO` | Alta | Sí |
| RF-12 | Aprobación por APU según los tres modos de firma | Alta | Sí |
| RF-13 | Registro único de aprobación por APU | Alta | Sí |
| RF-14 | Autoría de la última modificación | Media | Sí |
| RF-15 | Catálogo público: consulta y copia | Media | Sí |
| RF-16 | Búsqueda y sugerencia de similitud de insumos | Media | Sí |
| RF-17 | Alertas por tope presupuestal | Media | Sí |
| RF-18 | Correos de creación de cuenta y recuperación | Media | Sí |
| RF-19 | Importación y exportación | Baja | No |
| RF-20 | Listado y filtrado de insumos | Alta | Sí |
| RF-21 | Transición a `FINALIZADO` | Alta | Sí |
| RF-22 | Conteo de uso de insumos | Media | Sí |
| RF-23 | Autorregistro público con nombre de usuario y sin sesión | Alta | Sí |

**Reglas de negocio esenciales**
1. El APU se calcula con una sola implementación; el frontend no replica fórmulas.
2. Un insumo está completo con descripción, precio, unidad y marca; si falta uno pasa a `FALTA_INFORMACION` y contagia al APU y al proyecto.
3. El cierre exige todo cotizado y la aprobación definida por el modo de firma.
4. La copia de APUs solo se permite desde proyectos de la misma compañía o desde el catálogo público, nunca desde otra compañía y sin restricción por estado; la copia desde catálogo conserva procedencia, la copia desde proyecto no.
5. No existe registro de insumos por compañía: se copian desde otros proyectos y quedan independientes. Al copiar un APU se agregan los insumos inexistentes y se sugieren los similares; el usuario decide usar el existente con su precio o conservar el valor de origen.
6. El código del EDT es único por proyecto, sin distinguir mayúsculas: 1–10 caracteres en capítulos y 1–5 en ítems; se permite `.`; la numeración es automática y editable, se recalcula solo lo no personalizado y los hijos heredan el prefijo del padre. El APU no almacena código: lo muestra desde el ítem.
7. La publicación no expone autoría ni empresa, pero sí precios y contactos.

## 2.3 Organización en módulos
| ID | Módulo | Responsabilidad | Depende de |
|---|---|---|---|
| M-01 | Acceso y organización | Autorregistro, usuarios, roles, asignaciones, compañías, correos | — |
| M-02 | Proyectos y configuración | Divisa, AIU, tope, firma, auditor, visibilidad | M-01 |
| M-03 | EDT | Capítulos, ítems, códigos, numeración y búsqueda | M-02 |
| M-04 | Insumos | Registro, tipos, estados, filtros, copia y similitud | M-02 |
| M-05 | APUs y matrices | Composición, copia y reconciliación | M-03, M-04 |
| M-06 | Cálculo | Costos, AIU, redondeo y conteo de uso | M-05 |
| M-07 | Aprobación y cierre | Cotización, aprobación, autoría y `FINALIZADO` | M-04, M-05, M-06 |
| M-08 | Catálogo abierto | Consulta y copia de APUs e insumos públicos | M-02, M-04, M-05 |
| M-09 | Alertas | Evaluación del tope presupuestal | M-02, M-06 |

Sin ciclos: M-08 y M-09 son consumidores finales.

## 2.4 Estrategia progresiva
| Etapa | Objetivo | Funcionalidades | Resultado demostrable |
|---|---|---|---|
| E-0 | Preparación | — | Repositorio, Compose, CI, OpenSpec y seeds |
| E-1 | Acceso y organización | RF-01, RF-02, RF-18, RF-23 | Autorregistro, login, compañías, usuarios, correos |
| E-2 | Proyecto y EDT | RF-03, RF-04 | Proyecto configurado y árbol con códigos |
| E-3a | Insumos | RF-05, RF-06, RF-16, RF-20 | Listado, filtros, estados y copia |
| E-3b | APUs y matrices | RF-07, RF-08, RF-09, RF-22 | APU completo, matriz, reconciliación y conteo |
| E-4a | Cálculo y cotización | RF-10, RF-11, RF-14, RF-17 | Presupuesto calculado, cotizado y alertado |
| E-4b | Aprobación y cierre | RF-12, RF-13, RF-21 | Aprobación por APU y cierre |
| E-5 | Catálogo | RF-15 | Consulta y copia pública |
| E-6 | Endurecimiento | — | Seguridad, respaldo, despliegue local y documentación |

**Orden:** E-0 → E-1 → E-2 → E-3a → E-3b → E-4a → E-4b → E-5 → E-6. `RF-19` queda fuera.
---

# 3. Decisiones de arquitectura y tecnología

## 3.1 Arquitectura y justificación
**Estilo:** monolito modular con Clean Architecture y SPA desacoplada. Se descartan microservicios porque hay un solo desarrollador y un solo servidor, y la cascada de estados requiere transacciones locales.

| Capa | Responsabilidad | Prohibido |
|---|---|---|
| Dominio | Entidades, reglas, `ApuCalculator`, contratos de repositorio | EF Core, ASP.NET |
| Aplicación | Casos de uso, DTOs, validación, consultas y transacciones | `DbContext`, SQL directo |
| Infraestructura | `DbContext`, repositorios, Identity, correo, migraciones | Reglas de negocio |
| API | Endpoints, autenticación, autorización, `ProblemDetails` | `DbContext`, repositorios |
| Presentación | Angular, guardias, servicios y componentes | Fórmulas o reglas |

**Aislamiento:** una base con esquema compartido; `company_id` en entidades de compañía y `project_id` en entidades de proyecto; filtro global por compañía y validación de pertenencia. Un único bypass para el catálogo público, que solo lee proyectos públicos.

**Consultas de lectura:** el conteo de uso y el catálogo se resuelven con proyecciones de EF Core en Aplicación, sin duplicar la fórmula del APU.

**Despliegue:** Nginx (SPA y proxy), API .NET 10 en 8080 y SQL Server 2022, con Docker Compose. TLS postergado: solo entorno local o demo controlada; migraciones manuales con runbook.

## 3.2 Stack tecnológico
| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core Web API sobre .NET 10 |
| ORM | EF Core 10 |
| Base de datos | SQL Server 2022 |
| Frontend | Angular 20 + Angular Material + `signature_pad` |
| Proxy | Nginx |
| Contenedores | Docker Compose v2 |
| Identidad | ASP.NET Identity + JWT Bearer |
| Contrato | Swashbuckle (OpenAPI + Swagger UI) |
| Logs | Serilog |
| Pruebas | xUnit, Moq, FluentAssertions, NetArchTest, Coverlet |
| CI/CD | GitHub Actions |
| Especificaciones | OpenSpec |
| Mapeo | Manual; AutoMapper descartado |
| Correo | Proveedor transaccional (SendGrid, Mailgun o SES) |

## 3.3 Estrategia de persistencia
| Entidad | Campos y reglas |
|---|---|
| `Company` | `id`, `business_name`, `tax_id` único, `is_active` |
| `Role` | `id`, `code` único (`ADMIN`, `DIRECTOR`, `ESTIMATOR`, `AUDITOR`), `name`, `is_active` |
| `User` | `id`, `company_id` **nullable solo para Administrador y para el usuario autorregistrado**, `role_id`, `full_name`, `username` único, `email` único, `password_hash`, `is_active` |
| `Project` | `id`, `company_id`, `name`, `currency` ISO, `status`, `is_public`, `created_at` |
| `ProjectConfiguration` | `project_id` único, `budget_limit`, `aiu_admin`, `aiu_contingency`, `aiu_profit`, `signature_mode`, `requires_auditor` |
| `ProjectMember` | `id`, `project_id`, `user_id`, `role_in_project`; único (`project_id`, `user_id`) |
| `WbsCode` | `id`, `project_id`, `code`, `node_type`; único (`project_id`, `code`) |
| `WbsChapter` | `id`, `project_id`, `parent_chapter_id`, `code_id`, `name`, `order_index` |
| `WbsItem` | `id`, `wbs_chapter_id`, `apu_id` único, `code_id`, `quantity_work`, `order_index` |
| `Apu` | `id`, `project_id`, `description`, `unit`, `yield`, `status`, `last_modified_by`, `last_modified_at`; sin `code` ni `is_public` |
| `Resource` | `id`, `project_id`, `resource_type_id`, `city_id`, `description`, `unit`, `unit_price`, `brand`, `distributor`, `user_key`, `details`, `contact_number`, `quotation_status`, `origin_resource_id`, `last_modified_by`, `last_modified_at` |
| `ApuResource` | `id`, `apu_id`, `resource_id`, `consumption`, `waste_percentage` |
| `MatrixComponent` | `id`, `matrix_resource_id`, `component_resource_id`, `consumption`, `waste_percentage` |
| `ApuApproval` | `id`, `apu_id` único, `user_id`, `role_code`, `approved_at`, `mode`, `signature_captured` |
| Catálogos | `Country`, `City`, `ResourceType` (`MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`, `WORK`, `MATRIX`) |

**Reglas de integridad:** códigos del EDT únicos por proyecto (1–10 capítulos, 1–5 ítems, `.` permitido, sin distinguir mayúsculas); un APU por ítem; una configuración y una aprobación por APU; un usuario una vez por proyecto; `username`, email y NIT únicos; `yield > 0`; `quantity_work > 0`; `consumption > 0`; desperdicio y AIU entre 0 y 100; `unit_price ≥ 0`; baja lógica en proyecto, APU e insumo; no se elimina un APU con aprobaciones; `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource`.

**Cálculo:** materiales por unidad de obra (`consumo × cantidad`); mano de obra por jornada o cuadrilla; AIU por proyecto (`costo directo × (1 + %A + %I + %U)`); Banker's Rounding a 2 decimales; 4 decimales en consumos y rendimientos; una divisa por proyecto sin conversión. La fórmula exacta queda pendiente de `X-15`.

**Aprobaciones:** un registro por APU con el último aprobador; sin imagen ni hash de firma; editar un APU aprobado elimina la aprobación; la copia nace sin aprobación. En el modo de firmante único, cualquier Presupuestador asignado puede firmar y su firma cubre todos los APUs. No se valida la aprobación de varios auditores.

**Índices clave:** `Project(company_id, is_public)`, `WbsCode(project_id, code)` único, `WbsItem(apu_id)` único, `Apu(project_id, status)`, `Resource(project_id, resource_type_id)`, `Resource(project_id, quotation_status)`, `ApuResource(apu_id)`, `ApuResource(resource_id)`, `ApuApproval(apu_id)` único, `User(username)` único.

## 3.4 Organización de componentes
**Backend:** proyectos por capa con carpetas por módulo.

| Proyecto | Contenido |
|---|---|
| `BudgetCivil.Domain` | Entidades, enums, `ApuCalculator`, contratos |
| `BudgetCivil.Application` | Casos de uso, DTOs, validación, consultas, `IEmailSender` |
| `BudgetCivil.Infrastructure` | `AppDbContext`, repositorios, Identity, correo, migraciones |
| `BudgetCivil.Api` | Endpoints, DI, autenticación, middleware, Swagger |
| `BudgetCivil.Tests.Unit` | Dominio y aplicación |
| `BudgetCivil.Tests.Integration` | API y persistencia (ejecución manual) |

**Frontend:** `core/` (sesión, interceptores, guardias), `shared/`, `layout/`, `features/` (auth, companies, users, projects, wbs, resources, apus, approvals, catalog, alerts) y `models/`.

**Estado:** híbrido. Sesión y proyecto activo en servicios con signals; datos de servidor en servicios por feature; filtros, formularios y diálogos locales. Si un estado lo usan dos features, se promueve a servicio.

**API:** `/api/v1`, JSON en `camelCase`, errores con `ProblemDetails`, paginación `page`/`pageSize`, Swashbuckle en `/swagger` y cliente Angular generado desde OpenAPI. Endpoints anónimos: solo `/v1/auth/register` y `/v1/auth/login`; el resto exige JWT.

**Dependencias:** `API → Aplicación → Dominio`; Infraestructura implementa contratos; la API no usa `DbContext`; un módulo no navega entidades de otro.

## 3.5 Comparación de decisiones técnicas

### 3.5.1 Estilo arquitectónico
| Criterio | Monolito modular + Clean | Microservicios | Capas simple |
|---|---|---|---|
| Complejidad | Media | Alta | Baja |
| Despliegue | Uno | Nueve servicios | Uno |
| Transacciones | Locales | Distribuidas | Locales |
| Testabilidad | Alta | Media | Baja |

**Elegido:** monolito modular. **Descartado:** microservicios por costo operativo y transacciones distribuidas; capas simples porque dispersan las reglas.

### 3.5.2 Aislamiento multitenant
| Criterio | Esquema compartido + filtro | Base por compañía | RLS |
|---|---|---|---|
| Aislamiento | Medio | Alto | Alto |
| Migraciones | Una | N | Una |
| Catálogo público | Simple | Complejo | Requiere excepción |

**Elegido:** esquema compartido con filtro y bypass único. **Descartado:** base por compañía por migraciones y catálogo; RLS queda como defensa futura.

### 3.5.3 Motor de persistencia
| Criterio | EF Core 10 | Dapper | ADO.NET |
|---|---|---|---|
| Productividad | Alta | Media | Baja |
| Filtros y migraciones | Sí | No | No |
| Rendimiento | Medio | Alto | Alto |

**Elegido:** EF Core. **Descartado:** Dapper y ADO.NET porque obligan a implementar filtros y migraciones a mano.

### 3.5.4 Motor de base de datos
| Criterio | SQL Server 2022 | PostgreSQL | MySQL |
|---|---|---|---|
| Licenciamiento | Comercial | Libre | Libre |
| Ecosistema .NET | Alto | Alto | Medio |
| Costo operativo | Medio | Bajo | Bajo |

**Elegido:** SQL Server. **Descartado:** PostgreSQL por cambio de stack sin problema medido; es el reemplazo si la licencia bloquea. MySQL ofrece menos capacidades para el modelo.

### 3.5.5 Modelo del EDT
| Criterio | Tablas separadas + `WbsCode` | Tabla única | Validación solo en app |
|---|---|---|---|
| Unicidad en base | Sí | Sí | No |
| Fidelidad al modelo | Alta | Baja | Alta |
| `JOIN` adicional | Sí | No | No |

**Elegido:** tablas separadas con registro de códigos. **Descartado:** tabla única por columnas nulas y refactor; validación solo en app por duplicados concurrentes.

### 3.5.6 Registro de aprobaciones
| Criterio | Un registro por APU | Registro por usuario |
|---|---|---|
| Detalle por usuario | Bajo | Alto |
| Verificación multi-auditor | No | Sí |
| Simplicidad | Alta | Media |

---

# 4. Estrategia de desarrollo

## 4.1 Metodología de trabajo
SDD con **OpenSpec**: la especificación manda; el código se deriva y se verifica contra ella.

| # | Paso | Artefacto | Puerta |
|---|---|---|---|
| 1 | Propuesta | `proposal.md` | Problema, alcance, RF/BR y aceptación |
| 2 | Delta de especificación | `specs/.../spec.md` | Requisitos con escenarios |
| 3 | Diseño | `design.md` | Decisiones y riesgos, si aplica |
| 4 | Tareas | `tasks.md` | Tareas pequeñas con criterio de hecho |
| 5 | Implementación | Código y pruebas | Compila y pruebas en verde |
| 6 | Verificación | Evidencia | Escenarios y checklist |
| 7 | Archivo | `changes/archive/` | Spec actualizada y DoD cumplido |

**Reglas:** sin propuesta no hay código; sin pruebas y spec actualizada no se archiva; un cambio cubre una funcionalidad o grupo pequeño; lo nuevo va al backlog.

**Organización:** una etapa por iteración, sin fechas fijas. GitFlow (`main`, `develop`, `feature/*`, `release/*`, `hotfix/*`), Conventional Commits y autoevaluación con checklist y CI.

## 4.2 Etapas de implementación
| Etapa | Objetivo | Funcionalidades | Salida |
|---|---|---|---|
| E-0 | Preparación | — | Compose, CI, OpenSpec y seeds funcionando |
| E-1 | Acceso y organización | RF-01, RF-02, RF-18, RF-23 | Autorregistro, login, compañías, usuarios y correos |
| E-2 | Proyecto y EDT | RF-03, RF-04 | Proyecto configurado y árbol con códigos |
| E-3a | Insumos | RF-05, RF-06, RF-16, RF-20 | Listado, filtros, estados y copia |
| E-3b | APUs y matrices | RF-07, RF-08, RF-09, RF-22 | APU completo, reconciliación y conteo |
| E-4a | Cálculo y cotización | RF-10, RF-11, RF-14, RF-17 | Presupuesto calculado, cotizado y alertado |
| E-4b | Aprobación y cierre | RF-12, RF-13, RF-21 | Aprobación por APU y cierre |
| E-5 | Catálogo | RF-15 | Consulta y copia pública |
| E-6 | Endurecimiento | — | Despliegue local, respaldo y documentación |

## 4.3 Orden de desarrollo
1. E-0: base técnica.
2. E-1: identidad, autorregistro y compañías.
3. E-2: proyecto y EDT.
4. E-3a: insumos (sin ellos no hay APU).
5. E-3b: APUs, matrices y conteo (requiere APUs y cantidades).
6. E-4a: cálculo y cotización (requiere composición).
7. E-4b: aprobación y cierre (requiere cálculo y cotización).
8. E-5: catálogo (requiere proyectos públicos completos).
9. E-6: entrega.

`RF-07` se completa en dos partes: copia desde proyectos de la compañía en E-3b y desde el catálogo en E-5. `RF-19` queda fuera.

## 4.4 Dependencias entre componentes
| Componente | Depende de | Bloqueo |
|---|---|---|
| Cálculo | Fórmulas cerradas (`X-15`) | No abrir E-4a sin ellas |
| Aprobaciones | APUs y modo de firma | E-4b después de E-4a |
| Catálogo | Proyectos públicos con APUs | E-5 al final |
| Correos | Proveedor transaccional | Elegirlo antes de E-1 |
| Cliente Angular | OpenAPI actualizado | Regenerar en CI |
| Alertas | Cálculo y tope | E-4a |
| Conteo de uso | APUs y cantidades | Cerrar E-3b |
| Migraciones | SQL Server local | Runbook en E-0 |

**Ruta crítica:** `X-15` bloquea E-4a; el proveedor de correo bloquea E-1; el licenciamiento de SQL Server y TLS bloquean cualquier despliegue real.

## 4.5 Criterios de terminado

**Preparado para empezar:** propuesta aprobada, delta de spec, RF/BR y aceptación, dependencias cerradas y plan de pruebas.

**Terminado:** compila sin advertencias; unitarias en verde; integración manual documentada; cobertura ≥ 80 % en dominio y motor; migración aplicada y reversible; OpenAPI y cliente actualizados; sin datos sensibles en respuestas públicas; spec actualizada y cambio archivado; flujo demostrado; sin `TODO` abiertos.

**Puertas de CI:** build, analizadores, unitarias, NetArchTest, contrato y cobertura. La integración y el E2E no bloquean CI, pero sí la salida de etapa.

---

# 5. Estrategia de calidad

## 5.1 Tipos de prueba
| Tipo | Alcance | Automatización |
|---|---|---|
| Unitaria de dominio | Cálculo, estados, aprobaciones, numeración y reconciliación | CI |
| Unitaria de aplicación | Casos de uso con dependencias simuladas | CI |
| Integración de persistencia | Repositorios, filtros, restricciones y migraciones | Manual con SQL local |
| Integración de API | Endpoints, autorización, errores y códigos HTTP | Manual con Swagger/curl |
| Contrato | OpenAPI válido y cliente regenerado | CI |
| Arquitectura | Reglas de capas y módulos con NetArchTest | CI |
| Seguridad y aislamiento | Acceso entre compañías, DTO público y autorregistro anónimo | Manual con dos compañías |
| Extremo a extremo | Login, APU, cierre y catálogo | Manual por checklist |
| Migraciones | Base limpia y base existente | Manual con runbook |
| Aceptación | Flujo demostrable por etapa | Manual con demo |

**Cobertura:** ≥ 80 % en dominio y motor; dominio + aplicación como meta. No se prueba código generado, configuración de EF ni getters.

## 5.2 Riesgos
`R-xx` identifica un riesgo en este documento.
| ID | Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|---|
| R-01 | Fuga de datos entre compañías | Media | Alta | Bypass único y checklist con dos compañías |
| R-02 | Exposición de precios y contactos al publicar | Alta | Alta | Advertencia y aceptación al publicar |
| R-03 | Fórmulas ambiguas (`X-15`) | Alta | Alta | Cerrar antes de E-4a |
| R-04 | Concurrencia y sobrescritura | Media | Media | `rowversion` y `409 Conflict` |
| R-05 | Licenciamiento de SQL Server | Media | Media | Definir edición antes de E-6 |
| R-06 | Ausencia de TLS | Alta | Alta | Solo entorno local; prohibido publicar sin HTTPS |
| R-07 | Error en migraciones manuales | Media | Media | Runbook y respaldo previo |
| R-08 | Correo no entregado | Media | Media | Proveedor transaccional y prueba de envío |
| R-09 | Deriva entre OpenAPI y cliente | Media | Media | Regenerar en CI |
| R-10 | Regresiones de interfaz no detectadas | Media | Media | Checklist de flujos críticos |
| R-11 | Alcance amplio para un desarrollador | Alta | Alta | Respetar etapas y no paralelizar |
| R-12 | Rendimiento no medido (sin requisito) | Media | Media | Índices y revisión manual de consultas |
| R-13 | Abuso del autorregistro público (cuentas falsas o masivas) | Media | Media | Definir límite de intentos y verificación de correo antes de E-1; solo entorno local mientras no exista |

## 5.3 Estrategias de mitigación
- **Prevención:** especificación aprobada, unitarias del motor, NetArchTest, contrato generado y revisión de cambios.
- **Detección:** puertas de CI, checklist manual de aislamiento, logs y health checks.
- **Corrección:** reversión de release, `hotfix/*`, restauración de respaldo y re-aprobación tras editar un APU.

## 5.4 Verificación de requisitos
| Etapa | Funcionalidades | Verificación |
|---|---|---|
| E-1 | RF-01, RF-02, RF-18, RF-23 | T-I-01, T-I-02, T-I-17, T-I-21, T-S-01, T-S-04, T-S-05, T-S-07 |
| E-2 | RF-03, RF-04 | T-U-01, T-I-03, T-I-04 |
| E-3a | RF-05, RF-06, RF-16, RF-20 | T-U-02, T-U-08, T-I-05, T-I-06, T-I-15, T-I-18 |
| E-3b | RF-07, RF-08, RF-09, RF-22 | T-U-03, T-U-04, T-U-11, T-I-07, T-I-08, T-I-09, T-I-20, T-S-06 |
| E-4a | RF-10, RF-11, RF-14, RF-17 | T-U-05, T-U-06, T-U-09, T-I-10, T-I-13, T-I-16 |
| E-4b | RF-12, RF-13, RF-21 | T-U-07, T-U-10, T-I-11, T-I-12, T-I-19, T-S-02 |
| E-5 | RF-15 | T-I-14, T-S-02 |
| Fuera | RF-19 | No aplica |

`T-U` = unitaria automatizada; `T-I` = integración manual; `T-S` = seguridad manual.

**Requisitos no funcionales**
| NFR | Estado | Verificación |
|---|---|---|
| NFR-001 Seguridad | Vigente | T-S-01 a T-S-06 y T-A-01 |
| NFR-002 Exposición pública | Modificado: se publican precios y contactos, no autoría ni empresa | T-S-02 |
| NFR-003 Rendimiento | Eliminado | No se verifica |
| NFR-004 Auditoría inmutable | Obsoleto: sustituido por último modificador y aprobación única | T-I-12, T-I-13 |
| NFR-005 Cobertura | Vigente: ≥ 80 % en dominio y motor | Puerta de CI |

---

# 6. Uso de SDD e inteligencia artificial

## 6.1 Cómo se utiliza OpenSpec
OpenSpec es la **fuente de verdad del comportamiento**; `PLAN_INGENIERIA.md` conserva la justificación.

```text
openspec/
  project.md                 contexto, stack y convenciones
  specs/<capacidad>/spec.md  comportamiento vigente
  changes/<cambio>/          propuesta, diseño, tareas y delta
  changes/archive/           cambios cerrados
```

Capacidades: `identidad`, `proyectos`, `edt`, `insumos`, `apus`, `calculo`, `aprobaciones`, `catalogo`, `alertas`.

**Ciclo:** propuesta → delta de spec → diseño → tareas → implementación → verificación → archivo. Sin propuesta no hay código; sin pruebas y spec actualizada no se archiva. Un cambio cubre una funcionalidad o grupo pequeño.

**Convenciones:** requisitos `REQ-<CAPACIDAD>-<NN>`; escenarios `Dado / Cuando / Entonces`; casos negativos obligatorios; español para las specs y código en inglés; sin clases, SQL ni estilos; cada requisito cita sus `RF`/`BR`.

## 6.2 Del plan a las especificaciones
1. Seleccionar la capacidad y los `RF`/`BR` que aplican.
2. Escribir escenarios positivos y negativos.
3. Definir aceptación y asociar pruebas.
4. Verificar dependencias y vacíos; si hay un `X` abierto, no se inicia.
5. Dividir en cambios y tareas de una etapa.
6. Revisar contra alcance y decisiones.

| Fuente | Se convierte en |
|---|---|
| `RF` | Requisito con escenarios |
| `BR` | Restricción con escenario negativo |
| Decisión de diseño | Restricción de la spec |
| `NFR` | Requisito no funcional o eliminación expresa |
| Vacío abierto | Bloqueo del cambio |

**Ejemplo (RF-07 copia de APU):** copia desde proyecto de la misma compañía; copia desde catálogo público; reconciliación de insumos con decisión del usuario; rechazo de APU privado o de otra compañía sin revelar su existencia.

**Spec lista:** alcance y referencias; escenarios positivos y negativos; aceptación verificable; permisos, datos inválidos, concurrencia y aislamiento; sin `X` abierto; trazabilidad a pruebas.

## 6.3 Delegación a la IA
| Actividad | IA | Estudiante |
|---|---|---|
| Redactar especificación | Borrador | Revisa y aprueba |
| Código repetitivo (entidades, DTOs, validadores, endpoints) | Borrador | Revisa lógica y dependencias |
| Pruebas unitarias | Borrador | Verifica comportamiento y negativos |
| Componentes de interfaz | Estructura base | Revisa usabilidad y datos |
| Migraciones | Borrador | Revisa SQL, respalda y aplica |
| Configuración de CI | Borrador | Verifica puertas y secretos |
| Documentación | Borrador | Valida contra decisiones |

**No delegable:** reglas de negocio y fórmulas; alcance y prioridades; modelo de datos; seguridad y exposición pública; criterios de aceptación; aprobación de specs y cambios; fusión, versiones y despliegues; aplicación de migraciones; aceptación de riesgos.

**Reglas:** entregar contexto; pedir diff mínimo; ninguna dependencia nueva sin aprobación; exigir pruebas; no incluir secretos; un cambio a la vez; revisar antes de commitear; sin registro obligatorio de IA.

## 6.4 Verificación del código generado
**Automático:** compilación, analizadores, unitarias, NetArchTest, contrato OpenAPI, cliente regenerado y cobertura ≥ 80 % en dominio y motor.

**Revisión manual obligatoria:**
1. Cumple los escenarios sin comportamiento extra.
2. No inventa reglas, campos ni estados.
3. No agrega dependencias sin aprobación.
4. Aplica el filtro de compañía y no abusa del bypass.
5. No hay `DbContext` en la API ni reglas en controladores.
6. No duplica fórmulas.
7. Respeta aprobación única e invalidación al editar.
8. No expone autoría ni empresa en respuestas públicas.
9. Usa `ProblemDetails` sin filtrar trazas.
10. Las pruebas cubren comportamiento y casos negativos.

**Evidencia:** CI, diff revisado, checklist firmado y cambio archivado.

## 6.5 Decisiones del estudiante
- Alcance, prioridades y criterios de aceptación.
- Reglas de negocio y fórmulas.
- Modelo de datos y persistencia.
- Seguridad, autorización y exposición pública.
- Aprobación de especificaciones y cambios.
- Fusión de ramas, versiones y despliegues.
- Aplicación de migraciones.
- Aceptación de riesgos y limitaciones.

Cada cambio archivado lleva nombre del estudiante, fecha y confirmación de revisión.






