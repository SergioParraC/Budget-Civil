# Requerimientos

## 1. Requerimientos funcionales

Usa identificadores FR.

| ID | Titulo | Descripcion | Prioridad | Criterio de aceptacion |
|---|---|---|---|---|
| FR-001 | Gestion de usuarios | El sistema permite crear, editar y desactivar usuarios por compania | Alta | Admin de compania puede completar el flujo sin error |
| FR-002 | Gestion de companias | El sistema administra datos de companias | Alta | CRUD disponible con validaciones basicas |
| FR-001 | Gestión de Usuarios y Autenticación | Permite la autenticación JWT, registro y administración de usuarios por empresa (`ADMIN`, `ESTIMATOR`, `AUDITOR`). | Alta | Un usuario `ADMIN` puede crear, editar o desactivar usuarios dentro de su propia organización. |
| FR-002 | Gestión de Empresa / Tenant | Administra los datos generales de la empresa constructora y su configuración de cuenta multitenant. | Alta | CRUD completo de la entidad Empresa con validaciones de campos obligatorios (NIT/CC, Razón Social). |
| FR-003 | Estructuración de Proyectos y WBS/EDT | Permite la creación y gestión de proyectos organizados jerárquicamente: Proyecto -> Capítulos -> Ítems/Actividades. | Alta | El sistema permite crear y reordenar capítulos e ítems recalculando los subtotales automáticamente. |
| FR-004 | Gestión de APUs, Tipos de Insumos y Matrices | Permite estructurar APUs consumiendo la tabla unificada de insumos que clasifica: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matrices. | Alta | Permite vincular un insumo tipo "Matriz" dentro de la relación APU-Insumo, desplegando su composición reutilizable. |
| FR-005 | Configuración de Privacidad (`Is_Public`) | Permite marcar un Proyecto o APU individual como público (`Is_Public = true`) o privado (`Is_Public = false`). | Alta | El estado se persiste en la misma tabla unificada y restringe/habilita la visibilidad global en la API. |
| FR-006 | Registro de Personal Participante y Trazabilidad | Mantiene el historial de usuarios/personal que han participado en la creación o modificación de proyectos, APUs y en la asignación de valores de materiales e insumos. | Alta | La interfaz muestra el listado del personal participante por recurso sin almacenar logs de acciones atómicas innecesarias para la UX. |
| FR-007 | Buscador y Filtrado Deduplicado de Insumos | Permite listar y filtrar la tabla unificada de insumos para evitar duplicidad técnica en la interfaz. | Alta | Permite aplicar filtros combinados por Ubicación, Tipo de Insumo, Marca, Descripción y Precio Unitario. |
| FR-008 | Cascada de Estados de Cotización / Información | Determina el estado del presupuesto evaluando la integridad de Insumos -> APUs -> Proyecto. | Alta | Un insumo sin Descripción, Valor Unitario, Unidad o Marca pasa a `Falta_informacion`, heredando este estado al APU y al Presupuesto. |
| FR-009 | Motor de Alertas de Topes Presupuestales | Genera notificaciones/alertas cuando un capítulo o el total del proyecto supere el presupuesto límite configurado. | Media | Se dispara un indicador visual y alerta en tiempo real en la vista del proyecto si `CostoTotal > PresupuestoLimite`. |
| FR-010 | Exportación e Importación de Presupuestos | Permite importar matrices básicas en formato CSV/JSON y exportar el resumen de cotización estructurado. | Baja | El parser valida el esquema JSON/CSV e ingresa los registros a las tablas unificadas sin romper integridad relacional. |
| FR-011 | Firmado y Aval de APUs desde Configuración | Permite gestionar la firma del profesional realizador o el aval del profesional a cargo sobre los APUs del proyecto. | Media | La firma/aval no requiere selección uno a uno, sino que se gestiona de forma global o por lote desde el módulo de Configuración del Proyecto. |
| FR-012 | Aprobación Dual para Estado `Finalizado` | Permite transicionar el presupuesto al estado `Finalizado` mediante un flujo de doble aprobación. | Alta | El sistema valida las aprobaciones digitales del Presupuestador y del Director de Proyecto antes de marcar como `Finalizado`. |
| FR-013 | (Opcional) Importación Asistida de APUs con IA | Herramienta opcional para analizar e identificar automáticamente elementos de matrices en Excel mediante modelos de IA. | Baja | Módulo considerado como feature futura / no bloqueante para el MVP. |

## 2. Requerimientos no funcionales

Usa identificadores NFR.

| ID | Categoria | Requisito | Medicion |
|---|---|---|---|
| NFR-001 | Seguridad | API REST protegida mediante JWT, RBAC y aislamiento estricto de multitenancy por `CompanyId`. | 100 % de los endpoints privados requieren token válido y validación de roles. |
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
| BR-008 | Estructura de Insumos y Matrices: Los insumos de tipo "Matriz" residen en la tabla unificada de insumos pero funcionan con la lógica técnica de un APU reusable. No pueden ser agregados directamente como un ítem de obra final sin evaluar su relación `Apu_Insumo`. |
| BR-009 | Campos Obligatorios de Insumo: Un insumo se considera completo únicamente si posee `Descripción`, `Valor Unitario`, `Unidad` y `Marca`. Si falta alguno de estos 4 campos, el insumo se marca automáticamente como `Falta_informacion`. |
| BR-010 | Mapeo de Campos Opcionales de Insumo: Los campos `Distribuidor`, `Lugar (Ciudad/Municipio - País)`, `Clave Usuario`, `Detalles` y `Número de Contacto` son de carácter recomendado/opcional y no bloquean el estado del insumo. |
| BR-011 | Inmutabilidad de Presupuesto Finalizado: Para que un presupuesto pase al estado `Finalizado`, se requiere la validación de no tener elementos en `Falta_informacion` y contar con la aprobación explícita en sistema tanto del Profesional Presupuestador como del Director de Proyecto. |
| BR-012 | Gestión Centralizada de Firmas: La aprobación o firma de responsabilidad sobre los APUs se procesa mediante los parámetros establecidos en la Configuración del Proyecto, sin obligar al usuario a firmar unitariamente cada APU. |

## 4. Reglas de autorizacion

Define acciones por rol para cada modulo (`ADMIN`, `PROJECT_MANAGER` / `ESTIMATOR`, `AUDITOR`).

| Modulo / Entidad | Accion | Role: ADMIN | Role: ESTIMATOR | Role: AUDITOR |
|---|---|---|---|---|
| **Gestion Usuarios** | Crear / Editar / Desactivar |  Permitido |  Denegado |  Denegado |
| **Gestion Empresa** | Modificar Parametros |  Permitido |  Denegado |  Denegado |
| **Proyectos / WBS** | Crear / Editar / Eliminar |  Permitido |  Permitido |  Denegado (Lectura) |
| **APUs e Insumos** | Crear / Editar / Firmar |  Permitido |  Permitido |  Denegado (Lectura) |
| **Configuracion Visibilidad** | Toggle `Is_Public` |  Permitido |  Permitido |  Denegado |
| **Base Compartida APU** | Buscar / Consultar DTO |  Permitido |  Permitido |  Permitido |
| **Log de Auditoria** | Consultar Bitacora/Firmas |  Permitido |  Denegado |  Permitido |

## 5. Reglas de validacion

Documenta validaciones de datos por formulario y endpoint. **NOTA:** No se va a centrar solamente en estos Endpoints, pueden ser mayores dependiendo de la complegidad de la vista requerida, estos son los Endpoints principales, por lo cual se debe actualizar a medida del desarrollo del proyecto.

* **Formulario / Endpoint: Usuario (`POST /api/v1/users`)**
  * `email`: Formato de correo válido, único en el sistema.
  * `company_id`: Requerido, UUID existente en la tabla de empresas.
  * `role`: Enum válido (`ADMIN`, `ESTIMATOR`, `AUDITOR`).
* **Formulario / Endpoint: APU (`POST / PUT /api/v1/apus`)**
  * `name`: Cadena no vacía, máx. 255 caracteres.
  * `unit`: Cadena no vacía (ej. m2, m3, kg, und).
  * `is_public`: Booleano obligatorio (`true` / `false`).
  * `signature_token`: Cadena/Hash obligatorio para registrar la autoría del cambio.
  * `yield` (Rendimiento): Número flotante estricto $> 0$.
* **Formulario / Endpoint: Insumo / Detalle APU (`POST /api/v1/apus/{id}/items`)**
  * `quantity_consumption`: Flotante $> 0$.
  * `unit_price`: Flotante $\ge$ 0.
  * `quotation_status`: Enum obligatorio (`POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`).
* **Endpoint: Base Compartida (`GET /api/v1/public-apus`)**
  * Filtro forzado en query: `WHERE is_public = true`.
  * Mapeo de salida obligatorio a `PublicApuDTO` (excluye `company_id`, `created_by`, `signature_token`).
* **Insumo / Elemento Base (`POST / PUT /api/v1/inputs`)**
  * **Campos Requeridos (Evaluación de `Falta_informacion`):**
    * `description`: Cadena de texto no vacía.
    * `unit_price`: Numérico $\ge 0$.
    * `unit`: Cadena de texto (sigla o nombre estándar).
    * `brand` (Marca): Cadena de texto no vacía.
  * **Campos Opcionales / Recomendados:**
    * `distributor`, `location_city_country`, `user_key`, `details`, `contact_number`.
  * **Discriminador de Tipo (`tipo_insumo_id`):**
    * FK obligatorio correspondiente a: `Materiales`, `Mano de obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`.

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
