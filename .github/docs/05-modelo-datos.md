# Modelo de datos

> Documento actualizado el 30/10/2026 (reconciliación `E-1b`, `D-74`…`D-111`). Modelo definido en el capítulo 3 de `PLAN_INGENIERIA.md`; el esquema vigente es la baseline `202610300438_e1b_reconciliacion_baseline`.

## 1. Entidades
| Entidad | Propósito |
|---|---|
| `Company` | Empresa constructora (tenant), con tipo, nivel territorial y representante legal |
| `Role` | Catálogo de roles base: `ADMINISTRADOR`, `DIRECTOR`, `PRESUPUESTADOR`, `AUDITOR` |
| `Permission` | Catálogo de `permission_key` con `seed_version` (`D-82`) |
| `RolePermission` | Asignación rol ↔ permiso, versionada e idempotente (`D-82`) |
| `User` | Usuario que absorbe el perfil; `company_id` nulo solo para el Administrador y el autorregistrado pendiente de asignación (`D-78`, `D-72`) |
| `UserSignature` | Imagen de firma activa por usuario, nunca expuesta (`D-91`, `R-28`) |
| `PasswordRecoveryToken` | Token de recuperación de contraseña, un solo uso y con caducidad (`D-79`) |
| `EmailVerificationToken` | Token de verificación del correo del autorregistro; la cuenta no se habilita hasta consumirlo (`D-80`) |
| `SecurityAuditEvent` | Eventos de seguridad y denegaciones de autorización (`D-111`) |
| `Project` | Proyecto, estado y visibilidad |
| `ProjectConfiguration` | Divisa base, AIU, tope, modo de firma, firmante y requisito de auditor (`D-93`) |
| `ProjectMember` | Asignación de usuarios a proyectos, única por (`project_id`, `user_id`) |
| `ProjectStatusTransition`, `ProjectVisibilityTransition` | Historial de estado y de visibilidad de solo inserción (`D-92`) |
| `WbsCode` | Registro único de códigos del EDT por proyecto |
| `WbsChapter` | Capítulo o subcapítulo |
| `WbsItem` | Ítem de obra vinculado 1:1 a un APU |
| `Apu` | Análisis de precio unitario |
| `Resource` | Insumo del proyecto, incluida la Matriz |
| `Supplier` | Proveedor de la compañía, reutilizable en los insumos de todos sus proyectos (`REQ-PROVEEDORES-01`) |
| `ApuResource` | Composición del APU |
| `MatrixComponent` | Composición de una Matriz |
| `ApuApproval` | Registro único de aprobación por APU |
| `Country`, `Department`, `Municipality` | Catálogo de ubicación jerárquico |
| `DocumentType`, `BaseCurrency` | Catálogos de identidad y moneda |
| `ResourceType` | Catálogo de tipos de insumo |

**Eliminadas:** `AuditLog` (sin historial de valores, `D-08`), `BudgetVersion` (sin congelamiento, `E-12`), `UserProfile` (absorbida por `User`, `D-78`), `UserCompanyMembership` y `ProjectUserMembership` (sustituidas por `User.company_id` y `ProjectMember`, `D-98`).

## 2. Relaciones y cardinalidades
| Relación | Cardinalidad | Regla |
|---|---|---|
| `Company` → `User` | 1 : N | El Administrador y el autorregistrado pendiente tienen `company_id` nulo |
| `Role` → `RolePermission` → `Permission` | 1 : N | Autorización por `permission_key` activo (`D-82`) |
| `User` → `UserSignature` | 1 : 1 | Una firma activa por usuario (`D-91`) |
| `Company` → `Project` | 1 : N | Aislamiento por compañía |
| `Project` → `ProjectConfiguration` | 1 : 1 | Una configuración por proyecto |
| `Project` → `ProjectMember` | 1 : N | Un usuario una vez por proyecto |
| `Project` → `WbsCode` | 1 : N | Códigos únicos por proyecto |
| `Project` → `WbsChapter` | 1 : N | Capítulos raíz |
| `WbsChapter` → `WbsChapter` | 1 : N | Subcapítulos sin límite de profundidad |
| `WbsChapter` → `WbsItem` | 1 : N | Ítems del capítulo; el ítem raíz no tiene capítulo (`D-118`) |
| `WbsCode` → `WbsChapter` / `WbsItem` | 1 : 1 | El código pertenece a un nodo y no se reasigna (`D-117`) |
| `Project` → `WbsProjectActivity` | 1 : N | Una fila por proyecto y usuario con la última modificación del EDT (`D-120`) |
| `WbsItem` → `Apu` | 1 : 1 | Un APU por ítem; opcional hasta `I-3b` y único cuando existe (`D-113`) |
| `Project` → `Apu` | 1 : N | El APU pertenece al proyecto |
| `Project` → `Resource` | 1 : N | El insumo pertenece al proyecto |
| `Apu` → `ApuResource` | 1 : N | Composición del APU |
| `Resource` → `ApuResource` | 1 : N | Un insumo puede usarse en varios APUs |
| `Resource` (Matriz) → `MatrixComponent` | 1 : N | Composición de la Matriz |
| `Apu` → `ApuApproval` | 1 : 1 | Un registro de aprobación por APU |
| `Country` → `Department` | 1 : N | Catálogo de ubicación |
| `Department` → `Municipality` | 1 : N | Catálogo de ubicación |
| `Municipality` → `Project` / `User` | 1 : N | Ubicación seleccionada |
| `ResourceType` → `Resource` | 1 : N | Tipo de insumo |
| `Company` → `Supplier` | 1 : N | Directorio de proveedores de la compañía, reutilizable entre proyectos (`REQ-PROVEEDORES-01`) |
| `Supplier` → `Resource` | 1 : N | Un proveedor suministra muchos insumos; un insumo referencia 0..1 proveedor (`REQ-PROVEEDORES-04`) |
| `Municipality` → `Supplier` | 1 : N | Ubicación opcional del proveedor (`D-96`) |

## 3. Diccionario de datos

### `Company`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `business_name` | VARCHAR(255) | No | Razón social |
| `tax_id` | VARCHAR(20) | No | Único |
| `company_type` | VARCHAR(20) | Sí | `PUBLICA` o `PRIVADA` (`D-87`) |
| `territorial_level` | VARCHAR(50) | Sí | Obligatorio en compañías públicas |
| `country_id`, `department_id`, `municipality_id` | UUID | Sí | Coherencia territorial |
| `representative_mode` | VARCHAR(20) | Sí | `USER_REFERENCE` o `MANUAL_DATA`, excluyentes |
| `representative_user_id` | UUID | Sí | FK `User` en modo referencia |
| `representative_full_name`, `representative_document_number` | VARCHAR | Sí | Datos manuales del representante |
| `representative_document_type_id` | UUID | Sí | FK `DocumentType` |
| `is_active` | BOOLEAN | No | Baja lógica |
| `created_by_user_id`, `created_at_utc` | UUID / TIMESTAMP | Sí / No | Autoría mínima |
| `last_modified_by_user_id`, `last_modified_at_utc` | UUID / TIMESTAMP | Sí | Última modificación |

### `Role`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `code` | VARCHAR(30) | No | Único: `ADMINISTRADOR`, `DIRECTOR`, `PRESUPUESTADOR`, `AUDITOR` |
| `name` | VARCHAR(80) | No | Nombre legible |
| `is_active` | BOOLEAN | No | Catálogo |
| `is_base_role` | BOOLEAN | No | Los cuatro roles base son inmutables (`D-82`) |

### `User`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `company_id` | UUID | **Sí** | FK `Company`; nulo para el Administrador y el autorregistrado pendiente |
| `role_id` | UUID | No | FK `Role` |
| `email` | VARCHAR(150) | No | Único, sin distinguir mayúsculas (`D-73`) |
| `username` | VARCHAR(100) | No | Único, sin distinguir mayúsculas (`D-73`) |
| `password_hash` | VARCHAR(255) | No | Hash PBKDF2 de Identity (`D-81`) |
| `account_enabled`, `account_locked`, `failed_login_attempts` | BOOLEAN, BOOLEAN, INT | No | Habilitación, bloqueo a los cinco intentos y contador |
| `last_login_at_utc` | TIMESTAMP | Sí | Último acceso |
| `first_name`, `middle_name`, `last_name`, `second_last_name` | VARCHAR(100) | Solo el primer nombre y el primer apellido | Identidad completa; los segundos son opcionales (`D-76`, INT-06) |
| `document_type`, `document_number` | VARCHAR(20), VARCHAR(50) | No | El número **no** es único (`D-77`) |
| `birth_date` | DATE | No | Fecha de nacimiento |
| `country_id`, `department_id`, `municipality_id` | UUID | Sí | Residencia del perfil (no se pide en el autorregistro) |
| `professional_card`, `professional_card_issued_at`, `professional_card_file_path` | VARCHAR / DATE / VARCHAR | Sí | Tarjeta profesional del perfil |

### `Project`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `company_id` | UUID | No | FK `Company` |
| `code`, `name`, `description`, `objective` | VARCHAR | Sí / No | Identificación del proyecto |
| `country_id`, `department_id`, `municipality_id` | UUID | No | Ubicación |
| `status` | VARCHAR(50) | No | `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`, `INHABILITADO` (`D-88`) |
| `is_public` | BOOLEAN | No | Default `false`; publica todos sus APUs e insumos |
| `director_owner_user_id`, `created_by_user_id` | UUID | No | Responsable y creador |
| `last_modified_by_user_id`, `last_modified_at_utc` | UUID / TIMESTAMP | Sí | Última modificación |
| `created_at_utc`, `updated_at_utc` | TIMESTAMP | No | UTC |
| `row_version` | ROWVERSION | No | Concurrencia optimista (`D-97`) |

### `ProjectConfiguration`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `project_id` | UUID | No | PK y FK única (1:1) |
| `base_currency_id` | UUID | No | FK `BaseCurrency` |
| `budget_limit` | DECIMAL(18,2) | Sí | Tope mayor que cero cuando se informa |
| `aiu_admin`, `aiu_unforeseen`, `aiu_profit` | DECIMAL(5,2) | Sí | 0–100 |
| `signature_mode` | VARCHAR(50) | No | `SIN_FIRMA`, `UNICA_POR_PRESUPUESTADOR_ASIGNADO` o `INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE` |
| `signer_user_id` | UUID | Sí | Firmante designado |
| `requires_auditor` | BOOLEAN | No | Default `false`. Es independiente de `signature_mode`: con el check activo el proyecto admite auditores en cualquier modo. Desactivarlo conserva los `project_members` de rol `AUDITOR` (`D-112`) |
| `row_version` | ROWVERSION | No | Concurrencia de la configuración |

### `ProjectMember`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `user_id` | UUID | No | FK `User` |
| `role_in_project` | VARCHAR(50) | No | Rol operativo en el proyecto |
| `assigned_by_user_id`, `assigned_at_utc` | UUID / TIMESTAMP | No | Autoría de la asignación |

Único por (`project_id`, `user_id`); sin historial de asignaciones (`D-08`, `D-98`).

### `WbsCode`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `code` | VARCHAR(10) | No | Código completo manual de 1–10 caracteres; único por proyecto, sin distinguir mayúsculas (`D-115`, `D-117`); admite mayúsculas y minúsculas |
| `node_type` | VARCHAR(10) | No | `CHAPTER` o `ITEM` |

### `WbsChapter`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `parent_chapter_id` | UUID | Sí | FK autorreferenciada |
| `code_id` | UUID | No | FK `WbsCode`, único |
| `name` | VARCHAR(255) | No | Nombre |
| `order_index` | INT | No | Orden |
| `last_modified_by` | UUID | No | FK `User`; actor autenticado (`D-116`) |
| `last_modified_at` | DATETIME2 | No | UTC (`D-116`) |
| `row_version` | ROWVERSION | No | Concurrencia optimista (`D-50`, `D-97`) |

### `WbsItem`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project`; principio `PLAN §3.3.1.2` |
| `wbs_chapter_id` | UUID | Sí | FK `WbsChapter`; nulo = ítem raíz (`D-118`) |
| `apu_id` | UUID | Sí | FK `Apu` (se añade en `I-3b`); único filtrado cuando no es nulo (`D-113`, `BR-016`) |
| `code_id` | UUID | No | FK `WbsCode`, único; no se recalcula después del alta (`D-117`) |
| `quantity_work` | DECIMAL(18,4) | No | > 0 |
| `order_index` | INT | No | Orden |
| `last_modified_by` | UUID | No | FK `User`; actor autenticado (`D-116`) |
| `last_modified_at` | DATETIME2 | No | UTC (`D-116`) |
| `row_version` | ROWVERSION | No | Concurrencia optimista (`D-50`, `D-97`) |

### `WbsProjectActivity`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `project_id` | UUID | No | PK compuesta y FK `Project`; una fila por proyecto y usuario (`D-120`) |
| `user_id` | UUID | No | PK compuesta y FK `User`; participante que modificó el EDT |
| `last_modified_at` | DATETIME2 | No | UTC; se sobrescribe en cada escritura del usuario, sin historial (`D-08`, `D-120`) |

### `Apu`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `description` | TEXT | No | Descripción de la actividad |
| `unit` | VARCHAR(20) | No | Unidad de medida |
| `yield` | DECIMAL(18,4) | No | > 0 |
| `status` | VARCHAR(30) | No | `COMPLETO` o `FALTA_INFORMACION` |
| `last_modified_by` | UUID | Sí | FK `User` |
| `last_modified_at` | TIMESTAMP | Sí | UTC |

Sin `code` (`D-31`) y sin `is_public` (`D-22`).

### `Resource`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `resource_type_id` | UUID | No | FK `ResourceType` |
| `municipality_id` | UUID | No | FK `Municipality` (`D-96`) |
| `description` | VARCHAR(255) | No | Obligatoria |
| `unit` | VARCHAR(20) | Sí | Requerida para cotizar |
| `unit_price` | DECIMAL(18,2) | Sí | ≥ 0 |
| `brand` | VARCHAR(100) | Sí | Requerida para cotizar |
| `distributor` | VARCHAR(150) | Sí | Obsoleto; la fuente es el proveedor |
| `user_key` | VARCHAR(50) | Sí | Editable; el sistema sugiere unicidad, no la impone (`Q-09`) |
| `details` | TEXT | Sí | Opcional |
| `contact_number` | VARCHAR(50) | Sí | Obsoleto; la fuente es el proveedor |
| `supplier_id` | UUID | Sí | FK `Supplier` `RESTRICT`; opcional y no único (`REQ-PROVEEDORES-04`); sin él el insumo queda en `FALTA_INFORMACION` (`REQ-PROVEEDORES-05`) |
| `quotation_status` | VARCHAR(30) | No | `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO` |
| `origin_resource_id` | UUID | Sí | FK `Resource`; procedencia de la copia, sin vínculo de actualización |
| `last_modified_by` | UUID | No | FK `User` |
| `last_modified_at` | TIMESTAMP | No | UTC |
| `row_version` | ROWVERSION | No | Concurrencia optimista (`D-50`) |
| `is_active` | BIT | No | Baja lógica (`D-49`) |
| `deleted_at` | TIMESTAMP | Sí | Baja lógica (`D-49`) |

Sin `is_public`; la visibilidad es del proyecto.

### `Supplier`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `company_id` | UUID | No | FK `Company` `RESTRICT`; el directorio es de la compañía y no lleva `project_id` (`REQ-PROVEEDORES-01`) |
| `business_name` | VARCHAR(200) | No | Único dato obligatorio; **sin** restricción única (`REQ-PROVEEDORES-06`) |
| `contact_name` | VARCHAR(150) | Sí | Opcional |
| `contact_phone` | VARCHAR(30) | Sí | Opcional |
| `email` | VARCHAR(254) | Sí | Opcional; se valida el formato cuando se informa (`REQ-PROVEEDORES-02`); sin índice único |
| `address` | VARCHAR(300) | Sí | Opcional |
| `observations` | VARCHAR(1000) | Sí | Opcional; se devuelve solo en el detalle completo |
| `country_id` | UUID | Sí | FK `Country` `RESTRICT`; ubicación opcional (`REQ-PROVEEDORES-03`) |
| `department_id` | UUID | Sí | FK `Department` `RESTRICT`; coherente con el país |
| `municipality_id` | UUID | Sí | FK `Municipality` `RESTRICT`; coherente con el departamento |
| `is_active` | BIT | No | Baja lógica; un proveedor inactivo conserva sus insumos (`REQ-PROVEEDORES-11`) |
| `row_version` | ROWVERSION | No | Concurrencia optimista (`REQ-PROVEEDORES-12`) |
| `created_by` | UUID | No | FK `User`; autoría del alta |
| `created_at` | TIMESTAMP | No | UTC |
| `last_modified_by` | UUID | No | FK `User`; el cliente no puede fijarlo (`SC-PROVEEDORES-46`) |
| `last_modified_at` | TIMESTAMP | No | UTC |

Sin `project_id` ni unicidad sobre `business_name`/`email`: dos proveedores pueden llamarse igual y el
mismo registro se reutiliza en todos los proyectos de la compañía.

### `ApuResource`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `apu_id` | UUID | No | FK `Apu` |
| `resource_id` | UUID | No | FK `Resource` |
| `consumption` | DECIMAL(18,4) | No | > 0 |
| `waste_percentage` | DECIMAL(5,2) | No | 0–100 |

### `MatrixComponent`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `matrix_resource_id` | UUID | No | FK `Resource` de tipo Matriz |
| `component_resource_id` | UUID | No | FK `Resource` componente |
| `consumption` | DECIMAL(18,4) | No | > 0 |
| `waste_percentage` | DECIMAL(5,2) | No | 0–100 |

### `ApuApproval`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `apu_id` | UUID | No | FK `Apu`, único |
| `user_id` | UUID | No | Último aprobador |
| `role_code` | VARCHAR(30) | No | Rol al aprobar |
| `approved_at` | TIMESTAMP | No | UTC |
| `mode` | VARCHAR(20) | No | Modo de firma vigente |
| `signature_captured` | BOOLEAN | No | Evidencia capturada; no se guarda imagen ni hash |

### Catálogos
| Entidad | Campos |
|---|---|
| `Country` | `id`, `code` único, `name` |
| `Department` | `id`, `country_id`, `code` único por país, `name` |
| `Municipality` | `id`, `department_id`, `code` único por departamento, `name` (`D-96`) |
| `ResourceType` | `id`, `code` único (`MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`, `WORK`, `MATRIX`), `name`, `is_active` |

## 4. Reglas de integridad
| Regla | Implementación |
|---|---|
| Código único del EDT | Índice único (`project_id`, `code`) en `WbsCode` |
| Código inmutable | Ningún proceso renumera `WbsCode` después del alta (`D-117`) |
| Un APU por ítem | Índice único filtrado en `WbsItem.apu_id` (`WHERE apu_id IS NOT NULL`) (`D-113`) |
| Actividad del EDT | Una fila por `(project_id, user_id)` en `WbsProjectActivity` (`D-120`) |
| Una configuración por proyecto | PK/FK única en `ProjectConfiguration.project_id` |
| Un usuario por proyecto | Único (`project_id`, `user_id`) en `ProjectMember` |
| Una aprobación por APU | Único en `ApuApproval.apu_id` |
| Email y NIT únicos | Índices únicos sin distinguir mayúsculas |
| Nombre de usuario único | Índice único en `User.username` (`D-73`) |
| Documento no único | Solo índice de búsqueda en `User.document_number` (`D-77`) |
| Permisos | `Permission.permission_key` único y `RolePermission` único por (`role_id`, `permission_id`) (`D-82`) |
| Firma | `UserSignature` única por usuario (`D-91`) |
| Un solo esquema | `dbo`; sin `param`/`iam`/`audit`/`log` (`D-95`) |
| Estado del proyecto | Texto con `CHECK` sobre los cinco valores (`D-88`) |
| Tope presupuestal | `budget_limit` mayor que cero |
| AIU | 0–100 |
| Rendimiento y cantidades | `yield > 0`, `quantity_work > 0` |
| Consumo y desperdicio | `consumption > 0`, `waste_percentage` 0–100 |
| Precio | `unit_price >= 0` |
| Coherencia del nodo | `WbsItem` exige cantidad; su capítulo es nulo solo si es ítem raíz (`D-118`); `apu_id` es opcional hasta `I-3b` y único cuando existe (`D-113`); `WbsChapter` exige nombre; `WbsCode.node_type` coincide con la tabla |
| Alcance | `company_id` obligatorio salvo el Administrador y el autorregistrado pendiente de asignación; `project_id` obligatorio en entidades de proyecto |
| Borrado | Baja lógica en `Project`, `Apu` y `Resource`; `RESTRICT` en lo referenciado. La EDT se elimina en cascada por la aplicación previa confirmación (`D-119`); el ítem con APU se elimina y el APU no |
| Concurrencia | `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource`; el movimiento exige la `rowversion` del nodo movido |

## 5. Reglas de cálculo
> `X-15` sigue abierto: la fórmula exacta se cierra antes de E-4a. La base decidida es `D-32`.

| Concepto | Base |
|---|---|
| Materiales | Consumo por unidad de obra: `consumo × cantidad del ítem` |
| Mano de obra | Unidad de esfuerzo configurable (hora-hombre o día-hombre) por jornada o cuadrilla |
| Insumo con desperdicio | `precio × consumo × (1 + desperdicio / 100)` |
| Costo directo del APU | Suma de costos de sus insumos y matrices expandidas a un nivel, según la base anterior |
| Costo del ítem | `cantidad de obra × costo directo del APU` |
| Costo directo del proyecto | Suma de subtotales de capítulos |
| AIU | `costo directo × (1 + %administración + %imprevistos + %utilidad)`; solo por proyecto (`D-07`) |
| Conteo de uso | `Σ (consumo × cantidad de obra)` por ítem, para todos los tipos (`D-33`) |
| Redondeo | Banker's Rounding a 2 decimales en valores monetarios; 4 decimales en consumos y rendimientos |
| Divisa | Una por proyecto, sin conversión (`BR-020`) |

La Matriz se expande un solo nivel; no se permite Matriz dentro de Matriz (`BR-008`).

## 6. Aprobaciones y autoría
- `ApuApproval` guarda **un registro por APU** con el último aprobador, su rol, fecha, modo y evidencia capturada.
- La imagen de firma se persiste una vez por usuario en `UserSignature` y **nunca** se expone en DTO públicos, catálogo ni logs (`D-91`, `R-28`); `BR-012` queda obsoleto.
- Toda edición de un APU aprobado elimina su aprobación (`BR-017`).
- Una copia de APU nace sin aprobación.
- `Apu` y `Resource` guardan `last_modified_by` y `last_modified_at`; el historial de valores no existe (`D-08`).
- Los historiales de estado y de visibilidad son tablas de **solo inserción** (`D-92`).

## 7. Índices
| Índice | Tabla |
|---|---|
| `Project(company_id, is_public)` | Aislamiento y catálogo |
| `WbsChapter(project_id, parent_chapter_id, order_index)` | Árbol |
| `WbsCode(project_id, code)` único | Códigos |
| `WbsChapter(code_id)` y `WbsItem(code_id)` únicos | Un código por nodo |
| `WbsItem(apu_id)` único filtrado (`WHERE apu_id IS NOT NULL`) | 1:1 (`D-113`) |
| `WbsItem(wbs_chapter_id, order_index)` | Listado |
| `WbsItem(project_id, wbs_chapter_id, order_index)` | Capítulo e ítems raíz (`D-118`) |
| `WbsProjectActivity(project_id, user_id)` único y `(project_id, last_modified_at)` | Una fila por proyecto y usuario (`D-120`) |
| `Apu(project_id, status)` | Búsqueda |
| `Resource(project_id, resource_type_id)` | Filtros |
| `Resource(project_id, quotation_status)` | Cotización |
| `Resource(project_id, description)` | Búsqueda |
| `Resource(supplier_id)` | Asociación con el proveedor (`REQ-PROVEEDORES-04`) |
| `Supplier(company_id, business_name)` | Listado y filtro por razón social |
| `Supplier(company_id, country_id)`, `(company_id, department_id)`, `(company_id, municipality_id)` | Filtro por ubicación (`REQ-PROVEEDORES-08`) |
| `Supplier(company_id, is_active)` | Selector de proveedores activos (`REQ-PROVEEDORES-09`) |
| `ApuResource(apu_id)` y `ApuResource(resource_id)` | Cálculo y conteo |
| `MatrixComponent(matrix_resource_id)` y `(component_resource_id)` | Expansión |
| `ApuApproval(apu_id)` único | Cierre |
| `ProjectMember(user_id, project_id)` único | Autorización |
| `User(company_id, role_id)` | Administración |
| `User(email)` y `User(username)` únicos | Inicio de sesión |
| `Project(company_id, status)` y `Project(company_id, name)` | Listados |
| `ProjectConfiguration(base_currency_id)` y `(signer_user_id)` | Configuración |
| `RolePermission(role_id, permission_id)` único | Autorización por permiso |
| `SecurityAuditEvent(occurred_at_utc)` | Consulta de eventos |

## 8. Migraciones y datos iniciales
- Migraciones manuales con runbook (`docs/runbook-migraciones.md`): respaldo, revisión del script, aplicación y verificación; el arranque de la API **no** aplica migraciones (`D-40`).
- Baseline única `202610300438_e1b_reconciliacion_baseline` sobre base vacía, con la convención `YYYYMMDDHHMM_<scope>_<change>` (`D-107`, `DEC-19`).
- Seed versionado e idempotente de cuatro roles base, permisos y `RolePermission` (`D-82`). La versión `v5` añade `SUPPLIERS_READ` (Administrador, Director, Presupuestador y Auditor) y `SUPPLIERS_WRITE` (Director y Presupuestador) (`REQ-PROVEEDORES-07`). La versión `v6` reconcilia por diferencia: las asignaciones rol-permiso y los permisos que ya no figuran en la matriz quedan inactivos con `revoked_at_utc` y `seed_version` vigente (`REQ-IDENTIDAD-30`). La versión `v7` añade `WBS_READ` (Administrador, Director, Presupuestador y Auditor) y `WBS_WRITE` (Director y Presupuestador) (`REQ-EDT-13`, `REQ-EDT-14`).
- Migración `20261113040910_e3a_suppliers`: tabla `suppliers`, columna `resources.supplier_id`, sus FKs `RESTRICT` e índices, sin migración de datos (`D-40`, `D-107`).
- Migración `20261114065800_wbs_add_i2b` (aplicada el 14/11/2026): tablas `wbs_codes`, `wbs_chapters`,
  `wbs_items` y `wbs_project_activity` con sus índices, `CHECK` y FKs `RESTRICT`,
  `wbs_items.wbs_chapter_id` nullable, índice único filtrado de `apu_id` y sin migración de datos (`D-40`,
  `D-107`, `D-113`…`D-120`).
- Seed de siete tipos de insumo: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matriz.
- Catálogo base de países, departamentos, municipios, tipos de documento y monedas (`D-96`).
- Administrador inicial por semilla en local o por bootstrap idempotente y auditado en demo, excluyentes (`D-110`).


