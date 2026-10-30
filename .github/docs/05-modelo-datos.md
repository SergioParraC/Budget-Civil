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
| `WbsChapter` → `WbsItem` | 1 : N | Ítems del capítulo |
| `WbsCode` → `WbsChapter` / `WbsItem` | 1 : 1 | El código pertenece a un nodo |
| `WbsItem` → `Apu` | 1 : 1 | Un APU por ítem |
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
| `requires_auditor` | BOOLEAN | No | Default `false` |
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
| `code` | VARCHAR(10) | No | Único por proyecto, sin distinguir mayúsculas |
| `node_type` | VARCHAR(10) | No | `CHAPTER` o `ITEM` |

### `WbsChapter`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `parent_chapter_id` | UUID | Sí | FK autorreferenciada |
| `code_id` | UUID | No | FK `WbsCode` |
| `name` | VARCHAR(255) | No | Nombre |
| `order_index` | INT | No | Orden |

### `WbsItem`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `wbs_chapter_id` | UUID | No | FK `WbsChapter` |
| `apu_id` | UUID | No | FK `Apu`, único |
| `code_id` | UUID | No | FK `WbsCode` |
| `quantity_work` | DECIMAL(18,4) | No | > 0 |
| `order_index` | INT | No | Orden |

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
| `city_id` | UUID | Sí | FK `City` |
| `description` | VARCHAR(255) | No | Obligatoria |
| `unit` | VARCHAR(20) | Sí | Requerida para cotizar |
| `unit_price` | DECIMAL(18,2) | Sí | ≥ 0 |
| `brand` | VARCHAR(100) | Sí | Requerida para cotizar |
| `distributor` | VARCHAR(150) | Sí | Opcional |
| `user_key` | VARCHAR(50) | Sí | Editable; el sistema sugiere unicidad, no la impone (`Q-09`) |
| `details` | TEXT | Sí | Opcional |
| `contact_number` | VARCHAR(50) | Sí | Opcional |
| `quotation_status` | VARCHAR(30) | No | `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO` |
| `origin_resource_id` | UUID | Sí | FK `Resource`; procedencia del catálogo |
| `last_modified_by` | UUID | Sí | FK `User` |
| `last_modified_at` | TIMESTAMP | Sí | UTC |

Sin `is_public`; la visibilidad es del proyecto.

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
| `City` | `id`, `country_id`, `name` |
| `ResourceType` | `id`, `code` único (`MATERIAL`, `LABOR`, `TOOL`, `EQUIPMENT`, `AUXILIARY`, `WORK`, `MATRIX`), `name` |

## 4. Reglas de integridad
| Regla | Implementación |
|---|---|
| Código único del EDT | Índice único (`project_id`, `code`) en `WbsCode` |
| Un APU por ítem | Índice único en `WbsItem.apu_id` |
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
| Coherencia del nodo | `WbsItem` exige APU y cantidad; `WbsChapter` exige nombre; `WbsCode.node_type` coincide con la tabla |
| Alcance | `company_id` obligatorio salvo el Administrador y el autorregistrado pendiente de asignación; `project_id` obligatorio en entidades de proyecto |
| Borrado | Baja lógica en `Project`, `Apu` y `Resource`; `RESTRICT` en lo referenciado |
| Concurrencia | `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource` |

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
| `WbsItem(apu_id)` único | 1:1 |
| `WbsItem(wbs_chapter_id, order_index)` | Listado |
| `Apu(project_id, status)` | Búsqueda |
| `Resource(project_id, resource_type_id)` | Filtros |
| `Resource(project_id, quotation_status)` | Cotización |
| `Resource(project_id, description)` | Búsqueda |
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
- Seed versionado e idempotente de cuatro roles base, permisos y `RolePermission` (`D-82`).
- Seed de siete tipos de insumo: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matriz.
- Catálogo base de países, departamentos, municipios, tipos de documento y monedas (`D-96`).
- Administrador inicial por semilla en local o por bootstrap idempotente y auditado en demo, excluyentes (`D-110`).


