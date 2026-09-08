# Flujos o casos de uso principales

## 1. Actores
* **`ADMIN` (Administrador):** Responsable de la parametrización de la empresa (tenant), gestión de accesos, creación de usuarios y configuración global del proyecto.
* **`ESTIMATOR` (Presupuestador / Compras):** Encargado de la estructuración de la WBS (Capítulos/Ítems), creación de Análisis de Precios Unitarios (APUs), actualización del valor de los materiales e insumos, y primera firma técnica del presupuesto.
* **`AUDITOR` (Interventor / Director):** Usuario con permisos de lectura sobre presupuestos, encargado de auditar la trazabilidad de cambios y consultar APUs compartidos. Puede actuar como segundo firmante cuando el proyecto lo configure como rol aprobador secundario.

## 2. Lista de casos de uso

* **UC-001:** Administrar usuarios y control de acceso por empresa.
* **UC-002:** Estructurar WBS del presupuesto (Capítulos e Ítems).
* **UC-003:** Crear y estructurar APU con vinculación de Insumos/Matrices.
* **UC-004:** Consultar base compartida pública de APUs.
* **UC-005:** Evaluar reglas de integridad y cascada de estado `Falta_informacion`.
* **UC-006:** Aprobación dual y congelamiento de presupuesto a estado `Finalizado`.

## 3. Plantilla por caso de uso

### UC-001 Administrar usuarios y control de acceso por empresa
* **Objetivo:** Permitir al administrador de la empresa gestionar las cuentas de usuario (`ADMIN`, `ESTIMATOR`, `AUDITOR`) asociadas a su `CompanyId`.
* **Actor principal:** `ADMIN`.
* **Precondiciones:** El usuario `ADMIN` debe estar autenticado con un token JWT válido vinculado a su empresa.
* **Postcondiciones:** Se crea, modifica o desactiva la cuenta de usuario en la base de datos relacional dentro de la misma empresa.
* **Flujo principal:**
  1. El actor ingresa al panel de administración de usuarios de su organización.
  2. Solicita crear un nuevo usuario ingresando correo electrónico, nombre completo y asignando un rol (`ADMIN`, `ESTIMATOR`, `AUDITOR`).
  3. El sistema valida la unicidad del correo electrónico en el sistema.
  4. El backend inyecta automáticamente el `CompanyId` de la sesión del administrador y guarda la entidad `User`.
  5. Se envía una notificación/credencial de acceso al usuario creado.
* **Flujos alternos:**
  * *Desactivación:* El actor selecciona un usuario existente y cambia su estado `is_active` a `false`.
* **Reglas de validación:** Un usuario `ADMIN` no puede crear ni consultar usuarios pertenecientes a otra empresa (`BR-001`).
* **Errores esperados:** `DuplicateEmailException` si el correo ya existe. `UnauthorizedAccessException` si se intenta modificar un usuario de otro tenant.

---

### UC-002 Estructurar WBS del presupuesto (Capítulos e Ítems)
* **Objetivo:** Definir la estructura jerárquica de trabajo (WBS/EDT) de un proyecto creando capítulos, ítems y asignando cantidades de obra.
* **Actor principal:** `ESTIMATOR`, `ADMIN`.
* **Precondiciones:** El proyecto debe estar creado y en estado distinto de `FINALIZADO`.
* **Postcondiciones:** Se registra la jerarquía WBS y se recalculan automáticamente los subtotales del capítulo y del proyecto.
* **Flujo principal:**
  1. El actor selecciona un proyecto y accede a la vista de presupuesto.
  2. Crea un nuevo capítulo (ej. "01. CIMENTACIÓN").
  3. Dentro del capítulo, agrega un ítem/actividad especificando nombre, cantidad de obra y vinculando un APU existente.
  4. El sistema consulta el costo directo del APU vinculado, calcula el costo total del ítem ($Q \times CD$) y actualiza los subtotales del capítulo.
  5. El sistema registra al usuario en la bitácora de personal participante.
* **Flujos alternos:**
  * *Reordenamiento:* El actor reordena la secuencia de capítulos/ítems; el sistema actualiza los códigos posicionales.
* **Reglas de validación:** No se pueden ingresar cantidades de obra menores o iguales a cero.
* **Errores esperados:** `InvalidQuantityException` si la cantidad es $\le 0$. `ProjectLockedException` si el proyecto está en estado `FINALIZADO`.

---

### UC-003 Crear y estructurar APU con vinculación de Insumos/Matrices
* **Objetivo:** Definir la composición técnica de un APU vinculando materiales, mano de obra, equipos, auxiliares, trabajos o matrices reutilizables.
* **Actor principal:** `ESTIMATOR`.
* **Precondiciones:** Existencia de registros en la tabla unificada `Resource` y catálogo `ResourceType`.
* **Postcondiciones:** El costo directo unitario del APU se recalcula y se registra al personal participante en `AuditLog`.
* **Flujo principal:**
  1. El actor crea o edita un APU ingresando código, descripción, unidad y rendimiento ($> 0$).
  2. Busca en el catálogo unificado deduplicado (filtrando por ubicación, tipo, marca o descripción) e inserta un recurso a la tabla `ApuResource`.
  3. Asigna el consumo requerida y el porcentaje de desperdicio.
  4. El sistema calcula el costo del recurso y actualiza el valor del APU.
  5. Registra el evento en el log de participación de personal.
* **Flujos alternos:**
  * *Vinculación de Matriz:* Si el recurso vinculado posee `ResourceType = Matriz`, el sistema lo procesa como sub-APU compuesto desplegando solo un nivel de su composición interna.
  * *Detección de Datos Faltantes:* Si el recurso carece de descripción, valor unitario, unidad o marca, se marca con el estado `Falta_informacion`.
* **Reglas de validación:** Rendimiento debe ser $> 0$. No se permite anidamiento `Matriz -> Matriz` ni autorreferencia.
* **Errores esperados:** `ValidationException` (rendimiento $\le 0$). `MatrixNestingNotAllowedException` (anidamiento de matrices).

---

### UC-004 Consultar base compartida pública de APUs
* **Objetivo:** Consultar la composición técnica (rendimientos y consumos) de APUs compartidos colaborativamente por la comunidad.
* **Actor principal:** `ESTIMATOR`, `AUDITOR`.
* **Precondiciones:** Existen APUs vinculados a proyectos que cumplan `Apu.is_public = true` y `Project.is_public = true`.
* **Postcondiciones:** El sistema retorna el objeto `PublicApuDTO` omitiendo cualquier metadato de autoría o empresa.
* **Flujo principal:**
  1. El actor accede al buscador de la base pública compartida.
  2. Ingrese criterios de búsqueda (palabra clave, tipo de actividad, rendimiento).
  3. El backend ejecuta la consulta pública con bypass controlado del filtro multitenant, forzando `WHERE Apu.is_public = true AND Project.is_public = true`.
  4. La capa de aplicación mapea los resultados hacia el DTO sanitizado (`PublicApuDTO`).
  5. Se despliega en pantalla la composición técnica (consumos y rendimientos) sin mostrar precios ni empresas creadoras.
* **Flujos alternos:** Ninguno.
* **Reglas de validación:** La API usa whitelist estricta de `PublicApuDTO` y jamás incluye `company_id`, `project_id`, firmas, precios, distribuidores ni contactos (`NFR-002`).
* **Errores esperados:** Ninguno. Si no hay coincidencias, devuelve una lista vacía `200 OK []`.

---

### UC-005 Evaluar reglas de integridad y cascada de estado `Falta_informacion`
* **Objetivo:** Evaluar automáticamente la integridad de los datos de un presupuesto propagando el estado `Falta_informacion` desde Insumos hasta el Proyecto global.
* **Actor principal:** Sistema (Proceso Automatizado / Middleware).
* **Precondiciones:** Creación o modificación de un recurso en la tabla `Resource` o en un `Apu`.
* **Postcondiciones:** Se actualizan las banderas de estado en las entidades `Resource`, `Apu` y `Project`.
* **Flujo principal:**
  1. El sistema evalúa los campos de un `Resource`: si `description`, `unit_price`, `unit` o `brand` están vacíos/nulos, asigna el estado `FALTA_INFORMACION` al recurso.
  2. El motor de reglas busca todos los APUs que utilicen dicho recurso en `ApuResource` y cambia su estado a `FALTA_INFORMACION`.
  3. El motor evalúa los proyectos que utilicen dichos APUs y recalcula `Project.status` (`FALTA_INFORMACION`, `POR_COTIZAR` o `COTIZADO` según mapeo de dominio).
  4. La propagación y reversión se ejecutan de forma sincrónica en la misma transacción.
  5. Se despliega una alerta visual en la interfaz del presupuesto notificando los campos faltantes.
* **Flujos alternos:**
  * *Subsanación:* Cuando el usuario completa los 4 campos obligatorios del insumo, el sistema recalcula en la misma transacción y remueve `FALTA_INFORMACION` si no existen otros elementos incompletos.
* **Reglas de validación:** Los campos obligatorios son: Descripción, Valor Unitario, Unidad y Marca (`BR-009`). La cascada debe ser transaccional (`BR-014`).
* **Errores esperados:** Ninguno (Lógica interna de dominio).

---

### UC-006 Aprobación dual y congelamiento de presupuesto a estado `Finalizado`
* **Objetivo:** Otorgar aprobación digital dual (Presupuestador + rol aprobador secundario parametrizable) para congelar el presupuesto y transicionar al estado `FINALIZADO`.
* **Actor principal:** `ESTIMATOR` y usuario con rol configurado en `ProjectConfiguration.second_approver_role_id`.
* **Precondiciones:** El proyecto no debe contener ningún APU o insumo en estado `FALTA_INFORMACION`.
* **Postcondiciones:** Generación de un snapshot inmutable en `BudgetVersion` y cambio del estado del proyecto a `FINALIZADO`.
* **Flujo principal:**
  1. El `ESTIMATOR` registra la firma técnica desde Configuración del Proyecto y el backend genera `estimator_signature_token` (HMAC-SHA256).
  2. El segundo aprobador definido por `ProjectConfiguration.second_approver_role_id` revisa el presupuesto y presiona "Aprobar y Finalizar".
  3. El sistema valida que el proyecto no posea `FALTA_INFORMACION`, que exista firma del estimador y que el usuario actual tenga el rol configurado.
  4. El backend genera y almacena `director_signature_token` (HMAC-SHA256) como segunda firma.
  5. Se serializa la WBS y APUs en un JSON inmutable grabado en la tabla `BudgetVersion` y el proyecto pasa a `FINALIZADO`.
* **Flujos alternos:**
  * *Bloqueo por Incompleto:* Si existe al menos un elemento en `Falta_informacion`, el sistema rechaza la transición y despliega la lista de tareas/insumos pendientes.
* **Reglas de validación:** Ambos tokens de firma son requeridos y se generan exclusivamente en backend. Un proyecto en estado `FINALIZADO` queda congelado en modo lectura (`BR-011`).
* **Errores esperados:** `IncompleteBudgetException` si se intenta finalizar con datos faltantes. `MissingSignatureException` si falta alguna de las dos firmas. `InvalidApproverRoleException` si el segundo firmante no coincide con el rol configurado.

## 4. Mapeo a requerimientos

| Caso de Uso (UC) | Requerimientos Funcionales (FR) | Reglas de Negocio (BR) |
|---|---|---|
| **UC-001** (Administrar usuarios) | FR-001, FR-002 | BR-001 |
| **UC-002** (Estructurar WBS) | FR-003, FR-009 | BR-002, BR-003 |
| **UC-003** (Estructurar APU/Matriz) | FR-004, FR-006, FR-007 | BR-003, BR-004, BR-008 |
| **UC-004** (Consultar base APU) | FR-005, FR-007 | BR-005, BR-006, BR-013 |
| **UC-005** (Cascada `Falta_informacion`) | FR-008 | BR-007, BR-009, BR-010, BR-014 |
| **UC-006** (Aprobación dual) | FR-011, FR-012 | BR-011, BR-012 |

## 5. Mapeo a pruebas

| Caso de Uso (UC) | Nombre / Archivo de Prueba | Tipo de Prueba | Criterio a Evaluar |
|---|---|---|---|
| **UC-001** | `test_admin_creates_tenant_users` | Integración API | Creación de rol `ESTIMATOR` restringida al tenant actual. |
| **UC-002** | `test_wbs_auto_recalculation` | Unitaria (Domain) | Agregar un `WbsItem` recalcula el total del `WbsChapter`. |
| **UC-003** | `test_apu_direct_cost_calculation` | Unitaria (Domain) | El costo directo coincide con consumos, precios unitarios y desperdicio, dividido por rendimiento. |
| **UC-003** | `test_matrix_single_level_expansion` | Unitaria (Domain) | Una Matriz se expande solo un nivel y rechaza anidamiento Matriz -> Matriz. |
| **UC-003** | `test_audit_log_participation` | Unitaria (App Layer) | Se genera un registro de participante al guardar un APU. |
| **UC-004** | `test_public_apu_dto_sanitization` | Integración API | El response HTTP cumple whitelist de `PublicApuDTO` y no contiene campos sensibles. |
| **UC-004** | `test_public_apu_visibility_and_rule` | Integración API | Solo se exponen APUs con `Project.Is_Public = true` y `Apu.Is_Public = true`. |
| **UC-005** | `test_missing_info_cascade_transactional` | Unitaria (Domain) | La propagación y reversión de estado se ejecutan en una sola transacción. |
| **UC-006** | `test_dual_approval_freeze` | Unitaria (Domain) | Lanza excepción si falta una firma, hay datos incompletos o el segundo rol no coincide con configuración. |