# Requerimientos

## 1. Requerimientos funcionales

Usa identificadores FR.

| ID | Titulo | Descripcion | Prioridad | Criterio de aceptacion |
|---|---|---|---|---|
| FR-001 | Gestión de Usuarios y Autenticación | Permite la autenticación JWT, registro y administración de usuarios por empresa (`ADMIN`, `ESTIMATOR`, `AUDITOR`). | Alta | Un usuario `ADMIN` puede crear, editar o desactivar usuarios dentro de su propia organización. |
| FR-002 | Gestión de Empresa / Tenant | Administra los datos generales de la empresa constructora y su configuración de cuenta multitenant. | Alta | CRUD completo de la entidad Empresa con validaciones de campos obligatorios (NIT/CC, Razón Social). |
| FR-003 | Estructuración de Proyectos y WBS/EDT | Permite la creación y gestión de proyectos organizados jerárquicamente: Proyecto -> Capítulos -> Ítems/Actividades. | Alta | El sistema permite crear y reordenar capítulos e ítems recalculando los subtotales automáticamente. |
| FR-004 | Gestión de APUs, Tipos de Insumos y Matrices | Permite estructurar APUs consumiendo la tabla unificada de insumos que clasifica: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matrices. | Alta | Permite vincular un insumo tipo "Matriz" dentro de la relación APU-Insumo, desplegando solo un nivel de composición reutilizable (sin anidamiento Matriz -> Matriz). |
| FR-005 | Configuración de Privacidad (`Is_Public`) | Permite marcar un Proyecto o APU individual como público (`Is_Public = true`) o privado (`Is_Public = false`). | Alta | El estado se persiste en la misma tabla unificada y restringe/habilita la visibilidad global en la API. La exposición pública de APUs exige `Project.Is_Public = true` y `Apu.Is_Public = true`. |
| FR-006 | Registro de Personal Participante y Trazabilidad | Mantiene el historial de usuarios/personal que han participado en la creación o modificación de proyectos, APUs y en la asignación de valores de materiales e insumos. | Alta | La interfaz muestra el listado del personal participante por recurso sin almacenar logs de acciones atómicas innecesarias para la UX. |
| FR-007 | Buscador y Filtrado Deduplicado de Insumos | Permite listar y filtrar la tabla unificada de insumos para evitar duplicidad técnica en la interfaz. | Alta | Permite aplicar filtros combinados por Ubicación, Tipo de Insumo, Marca, Descripción y Precio Unitario. |
| FR-008 | Cascada de Estados de Cotización / Información | Determina el estado del presupuesto evaluando la integridad de Insumos -> APUs -> Proyecto. | Alta | Un insumo sin Descripción, Valor Unitario, Unidad o Marca pasa a `Falta_informacion`, heredando este estado al APU y al Presupuesto. |
| FR-009 | Motor de Alertas de Topes Presupuestales | Genera notificaciones/alertas cuando un capítulo o el total del proyecto supere el presupuesto límite configurado. | Media | Se dispara un indicador visual y alerta en tiempo real en la vista del proyecto si `CostoTotal > PresupuestoLimite`. |
| FR-010 | Exportación e Importación de Presupuestos | Permite importar matrices básicas en formato CSV/JSON y exportar el resumen de cotización estructurado. | Baja | El parser valida el esquema JSON/CSV e ingresa los registros a las tablas unificadas sin romper integridad relacional. |
| FR-011 | Firmado y Aval de APUs desde Configuración | Permite gestionar la firma del profesional realizador o el aval del profesional a cargo sobre los APUs del proyecto. | Media | La firma/aval no requiere selección uno a uno, sino que se gestiona de forma global o por lote desde el módulo de Configuración del Proyecto. |
| FR-012 | Aprobación Dual para Estado `Finalizado` | Permite transicionar el presupuesto al estado `Finalizado` mediante un flujo de doble aprobación. | Alta | El sistema valida las aprobaciones digitales del Presupuestador y de un rol aprobador secundario parametrizable en `ProjectConfiguration` antes de marcar como `Finalizado`. |
| FR-013 | (Opcional) Importación Asistida de APUs con IA | Herramienta opcional para analizar e identificar automáticamente elementos de matrices en Excel mediante modelos de IA. | Baja | Módulo considerado como feature futura / no bloqueante para el MVP. |

## 2. Requerimientos no funcionales

Usa identificadores NFR.

| ID | Categoria | Requisito | Medicion |
|---|---|---|---|
| NFR-001 | Seguridad | API REST protegida mediante JWT, RBAC y aislamiento estricto de multitenancy por `CompanyId`, con excepción controlada del endpoint público de consulta. | 100 % de los endpoints privados requieren token válido y validación de roles; el endpoint público expone solo DTOs sanitizados y visibilidad validada. |
| NFR-002 | Sanitización de Datos (Privacidad) | Garantizar la eliminación de metadatos de autoría (`CompanyId`, proyectos, firmas) en los DTOs de APUs públicos. | 100 % de las respuestas de la base de datos compartida usan el `PublicApuDTO` sin fuga de información. |
| NFR-003 | Rendimiento | Tiempos de respuesta óptimos en el cálculo en tiempo real de la WBS y búsquedas en la base compartida. | p95 $\le$ 1.5 segundos en la respuesta de endpoints de cálculo y lectura de WBS. |
| NFR-004 | Auditabilidad / Inmutabilidad | Los eventos de auditoría no pueden ser modificados ni eliminados por ningún usuario (`INSERT` only). | 0 endpoints de edición o borrado expuestos sobre la tabla/log de trazabilidad. |
| NFR-005 | Arquitectura y Mantenibilidad | Desacoplamiento Frontend/Backend con cobertura de pruebas unitarias sobre el motor de cálculo y controladores. | Cobertura de código (Unit Testing) $\ge$ 80 % en el motor de cálculo de APU/WBS. |

## 3. Reglas de negocio

Usa identificadores BR.

| ID | Regla |
|---|---|
| BR-001 | Un usuario pertenece a una sola empresa (`CompanyId` no nulo). |
| BR-002 | Un proyecto pertenece a una sola empresa, pero sus APUs e insumos residen en la misma tabla unificada. |
| BR-003 | La modificación de un valor unitario, cantidad o rendimiento en un APU recalcula automáticamente el subtotal del ítem, del capítulo y del proyecto global. |
| BR-004 | Trazabilidad de Participación: Se debe mantener un registro explícito del personal que ha intervenido en la modificación/creación de Proyectos, APUs y en la actualización del valor de los materiales/insumos. |
| BR-005 | Un APU o Proyecto con `Is_Public = false` solo es accesible por usuarios autorizados de la empresa propietaria. |
| BR-006 | La consulta compartida pública solo expondrá rendimientos y consumos de insumos; en ningún caso expondrá precios de compra o datos de proveedores privados. |
| BR-007 | Un presupuesto no puede cambiar a estado "Aprobado/Finalizado" si contiene ítems o insumos con estado de cotización `FALTA_INFORMACION`. |
| BR-008 | Estructura de Insumos y Matrices: Los insumos de tipo "Matriz" residen en la tabla unificada de insumos pero funcionan con lógica técnica de APU reusable de un solo nivel. No pueden ser agregados directamente como un ítem de obra final ni anidarse como Matriz dentro de otra Matriz. |
| BR-009 | Campos Obligatorios de Insumo: Un insumo se considera completo únicamente si posee `Descripción`, `Valor Unitario`, `Unidad` y `Marca`. Si falta alguno de estos 4 campos, el insumo se marca automáticamente como `Falta_informacion`. |
| BR-010 | Mapeo de Campos Opcionales de Insumo: Los campos `Distribuidor`, `Lugar (Ciudad/Municipio - País)`, `Clave Usuario`, `Detalles` y `Número de Contacto` son de carácter recomendado/opcional y no bloquean el estado del insumo. |
| BR-011 | Inmutabilidad de Presupuesto Finalizado: Para que un presupuesto pase al estado `Finalizado`, se requiere validar que no existan elementos en `Falta_informacion` y contar con aprobación explícita del Presupuestador y del rol aprobador secundario definido en `ProjectConfiguration`. |
| BR-012 | Gestión Centralizada de Firmas: La aprobación o firma de responsabilidad se procesa desde la Configuración del Proyecto y no por firma unitaria de APU. Los tokens de firma son generados en backend con HMAC-SHA256 y nunca enviados por el cliente como valor final. |
| BR-013 | Regla de Exposición Pública: Un APU solo puede ser expuesto en la base compartida si `Project.Is_Public = true` y `Apu.Is_Public = true`. |
| BR-014 | Cascada de Estados: La propagación y reversión del estado `FALTA_INFORMACION` (Insumo -> APU -> Proyecto) se ejecuta de forma sincrónica y transaccional. |

## 4. Reglas de autorizacion

Define acciones por rol para cada modulo (`ADMIN`, `ESTIMATOR`, `AUDITOR`).

| Modulo / Entidad | Accion | Role: ADMIN | Role: ESTIMATOR | Role: AUDITOR |
|---|---|---|---|---|
| **Gestion Usuarios** | Crear / Editar / Desactivar |  Permitido |  Denegado |  Denegado |
| **Gestion Empresa** | Modificar Parametros |  Permitido |  Denegado |  Denegado |
| **Proyectos / WBS** | Crear / Editar / Eliminar |  Permitido |  Permitido |  Denegado (Lectura) |
| **APUs e Insumos** | Crear / Editar / Firmar |  Permitido |  Permitido |  Denegado (Lectura) |
| **Configuracion Visibilidad** | Toggle `Is_Public` |  Permitido |  Permitido |  Denegado |
| **Base Compartida APU** | Buscar / Consultar DTO |  Permitido |  Permitido |  Permitido |
| **Log de Auditoria** | Consultar Bitacora/Firmas |  Permitido |  Denegado |  Permitido |

* **Regla adicional FR-012:** El segundo aprobador es parametrizable en `ProjectConfiguration.second_approver_role_id`; solo usuarios con ese rol pueden registrar la segunda firma.

## 5. Reglas de validacion

Documenta validaciones de datos por formulario y endpoint. **NOTA:** No se va a centrar solamente en estos Endpoints, pueden ser mayores dependiendo de la complegidad de la vista requerida, estos son los Endpoints principales, por lo cual se debe actualizar a medida del desarrollo del proyecto.

* **Formulario / Endpoint: Usuario (`POST /api/v1/users`)**
  * `email`: Formato de correo válido, único en el sistema.
  * `company_id`: No se recibe en payload; se inyecta desde el JWT del `ADMIN` autenticado.
  * `role`: Enum válido (`ADMIN`, `ESTIMATOR`, `AUDITOR`).
* **Formulario / Endpoint: APU (`POST / PUT /api/v1/apus`)**
  * `code`: Cadena no vacía, máx. 50 caracteres.
  * `description`: Cadena no vacía.
  * `unit`: Cadena no vacía (ej. m2, m3, kg, und).
  * `is_public`: Booleano obligatorio (`true` / `false`).
  * `signature_token`: No se recibe en este endpoint. La autoría se registra en `AuditLog` con el `UserId` del JWT.
  * `yield` (Rendimiento): Número flotante estricto $> 0$.
* **Formulario / Endpoint: Detalle APU (`POST / PUT /api/v1/apu-resources`)**
  * `apu_id`: UUID requerido.
  * `resource_id`: UUID requerido.
  * `consumption`: Flotante $> 0$.
  * `waste_percentage`: Numérico entre 0 y 100.
  * `quotation_status`: No se recibe en este endpoint; se deriva del `Resource` vinculado.
* **Endpoint: Base Compartida (`GET /api/v1/public-apus`)**
  * Filtro forzado en query: `WHERE Apu.is_public = true AND Project.is_public = true`.
  * Mapeo de salida obligatorio a `PublicApuDTO` usando whitelist estricta.
  * **Campos permitidos en `PublicApuDTO`:** `apu_id`, `code`, `description`, `unit`, `yield`, `resources[]` con `resource_type`, `description`, `unit`, `consumption`, `waste_percentage`.
  * **Campos prohibidos en `PublicApuDTO`:** `company_id`, `project_id`, `created_by`, `signature_token`, `unit_price`, `distributor`, `contact_number`, `shared_origin_resource_id`.
* **Insumo / Elemento Base (`POST / PUT /api/v1/resources`)**
  * **Campos Requeridos (Evaluación de `Falta_informacion`):**
    * `description`: Cadena de texto no vacía.
    * `unit_price`: Numérico $\ge 0$.
    * `unit`: Cadena de texto (sigla o nombre estándar).
    * `brand` (Marca): Cadena de texto no vacía.
  * **Campos Opcionales / Recomendados:**
    * `distributor`, `location_city_country`, `user_key`, `details`, `contact_number`.
  * **Discriminador de Tipo (`tipo_insumo_id`):**
    * FK obligatorio correspondiente a: `Materiales`, `Mano de obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`.
  * **Estado de Cotización (`quotation_status`):** Calculado por backend según completitud y flujo de cotización; no editable manualmente al crear el recurso.
* **Endpoint: Aprobaciones (`POST /api/v1/projects/{id}/approvals`)**
  * `approval_stage`: Enum (`ESTIMATOR_SIGN`, `SECOND_APPROVER_SIGN`).
  * `confirmation_phrase`: Cadena no vacía para consentimiento explícito.
  * `signature_token`: Generado por backend con HMAC-SHA256 usando payload canónico (`project_id`, `user_id`, `approval_stage`, `timestamp_utc`).

## 6. Matriz de trazabilidad

Relaciona FR con casos de uso y pruebas. **NOTA:** Estos son algunos de los elementos a requerir, ya que durante la ejecución del proyecto puede aumentar, por lo cual se debe actualizar a medida del desarrollo del proyecto.

| Requerimiento (FR) | Caso de Uso (UC) | Tipo de Prueba (Unit / Integration) | Archivo de Prueba / Spec |
|---|---|---|---|
| **FR-001** (Gestión Usuarios) | UC-01: Administrar Usuarios | Prueba de Integración API REST | `test/integration/user_management.spec.ts` |
| **FR-002** (Gestión Empresa) | UC-02: Registrar / Configurar Empresa | Prueba de Integración DB Multitenant | `test/integration/company_tenant.spec.ts` |
| **FR-003** (Estructura WBS) | UC-03: Construir WBS del Proyecto | Prueba Unitaria (Motor WBS) | `test/unit/wbs_calculation.spec.ts` |
| **FR-004** (Gestión APUs) | UC-04: Estructurar APU e Insumos | Prueba Unitaria (Cálculo APU) | `test/unit/apu_calculation.spec.ts` |
| **FR-005** (Config. Visibilidad) | UC-05: Cambiar Estado `Is_Public` | Prueba de Integración Repositorio | `test/integration/apu_visibility.spec.ts` |
| **FR-006** (Trazabilidad / Firma) | UC-06: Registrar Log y Firma Auditada | Prueba Unitaria (Middleware Audit) | `test/unit/audit_signature.spec.ts` |
| **FR-007** (Base Compartida) | UC-07: Consultar APU Público Anonimizado | Prueba de Integración DTO Sanitizado | `test/integration/public_dto_sanitization.spec.ts` |
| **FR-008** (Estados Cotización) | UC-08: Actualizar Estado Insumo | Prueba Unitaria (Reglas de Validación) | `test/unit/quotation_status.spec.ts` |
| **FR-009** (Alertas Presupuesto) | UC-09: Evaluar Topes Presupuestales | Prueba Unitaria (Alert Engine) | `test/unit/budget_alerts.spec.ts` |
| **FR-010** (Import/Export Excel) | UC-10: Importar Matriz CSV/JSON | Prueba de Integración Parser | `test/integration/excel_parser.spec.ts` |
