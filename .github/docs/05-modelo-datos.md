# Modelo de datos

> Documento actualizado el 27/10/2026. Modelo definido en el capítulo 3 de `PLAN_INGENIERIA.md` (v0.26).

## 1. Entidades
| Entidad | Propósito |
|---|---|
| `Company` | Empresa constructora (tenant) |
| `Role` | Catálogo de roles: Administrador, Director, Presupuestador, Auditor |
| `User` | Usuario; el Administrador no tiene compañía |
| `Project` | Proyecto y su visibilidad |
| `ProjectConfiguration` | Divisa, AIU, tope, modo de firma y requisito de auditor |
| `ProjectMember` | Asignación de usuarios a proyectos |
| `WbsCode` | Registro único de códigos del EDT por proyecto |
| `WbsChapter` | Capítulo o subcapítulo |
| `WbsItem` | Ítem de obra vinculado 1:1 a un APU |
| `Apu` | Análisis de precio unitario |
| `Resource` | Insumo del proyecto, incluida la Matriz |
| `ApuResource` | Composición del APU |
| `MatrixComponent` | Composición de una Matriz |
| `ApuApproval` | Registro único de aprobación por APU |
| `Country`, `City` | Catálogo de ubicación |
| `ResourceType` | Catálogo de tipos de insumo |

**Eliminadas:** `AuditLog` (sin historial, `D-08`) y `BudgetVersion` (sin congelamiento, `E-12`).

## 2. Relaciones y cardinalidades
| Relación | Cardinalidad | Regla |
|---|---|---|
| `Company` → `User` | 1 : N | El Administrador tiene `company_id` nulo |
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
| `Country` → `City` | 1 : N | Catálogo de ubicación |
| `City` → `Project` / `Resource` | 1 : N | Ubicación seleccionada |
| `ResourceType` → `Resource` | 1 : N | Tipo de insumo |

## 3. Diccionario de datos

### `Company`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `business_name` | VARCHAR(255) | No | Razón social |
| `tax_id` | VARCHAR(20) | No | Único |
| `is_active` | BOOLEAN | No | Baja lógica |

### `Role`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `code` | VARCHAR(30) | No | Único: `ADMIN`, `DIRECTOR`, `ESTIMATOR`, `AUDITOR` |
| `name` | VARCHAR(80) | No | Nombre legible |
| `is_active` | BOOLEAN | No | Catálogo editable |

### `User`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `company_id` | UUID | **Sí** | FK `Company`; nulo solo para Administrador |
| `role_id` | UUID | No | FK `Role` |
| `full_name` | VARCHAR(150) | No | Nombre completo |
| `email` | VARCHAR(150) | No | Único, sin distinguir mayúsculas |
| `password_hash` | VARCHAR(255) | No | Hash de Identity |
| `is_active` | BOOLEAN | No | Baja lógica |

### `Project`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `company_id` | UUID | No | FK `Company` |
| `name` | VARCHAR(255) | No | Nombre |
| `currency` | CHAR(3) | No | ISO 4217 en mayúsculas |
| `status` | VARCHAR(30) | No | `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO` |
| `is_public` | BOOLEAN | No | Default `false`; publica todos sus APUs e insumos |
| `created_at` | TIMESTAMP | No | UTC |

### `ProjectConfiguration`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `project_id` | UUID | No | PK y FK única |
| `budget_limit` | DECIMAL(18,2) | Sí | Tope ≥ 0 |
| `aiu_admin` | DECIMAL(5,2) | No | 0–100 |
| `aiu_contingency` | DECIMAL(5,2) | No | 0–100 |
| `aiu_profit` | DECIMAL(5,2) | No | 0–100 |
| `signature_mode` | VARCHAR(20) | No | `NONE`, `ESTIMATOR`, `ESTIMATOR_AUDITOR` |
| `requires_auditor` | BOOLEAN | No | Default `false` |

### `ProjectMember`
| Campo | Tipo | Nulo | Regla |
|---|---|---|---|
| `id` | UUID | No | PK |
| `project_id` | UUID | No | FK `Project` |
| `user_id` | UUID | No | FK `User` |
| `role_in_project` | VARCHAR(30) | Sí | Rol operativo en el proyecto |

Único por (`project_id`, `user_id`).

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
| Divisa | `CHAR(3)` con `CHECK` |
| AIU | 0–100 |
| Rendimiento y cantidades | `yield > 0`, `quantity_work > 0` |
| Consumo y desperdicio | `consumption > 0`, `waste_percentage` 0–100 |
| Precio | `unit_price >= 0` |
| Coherencia del nodo | `WbsItem` exige APU y cantidad; `WbsChapter` exige nombre; `WbsCode.node_type` coincide con la tabla |
| Alcance | `company_id` obligatorio salvo Administrador; `project_id` obligatorio en entidades de proyecto |
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
- No se persiste imagen, token ni hash de firma (`BR-012`).
- Toda edición de un APU aprobado elimina su aprobación (`BR-017`).
- Una copia de APU nace sin aprobación.
- `Apu` y `Resource` guardan `last_modified_by` y `last_modified_at`; no hay historial (`D-08`).

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
| `ProjectMember(user_id, project_id)` | Autorización |
| `User(company_id, role_id)` | Administración |

## 8. Migraciones y datos iniciales
- Migraciones manuales con runbook: respaldo, `dotnet ef migrations add`, revisión del script, aplicación y verificación (`D-40`).
- Seed de cuatro roles: Administrador, Director de proyectos, Presupuestador y Auditor.
- Seed de siete tipos de insumo: Materiales, Mano de obra, Herramienta, Equipos, Auxiliares, Trabajos y Matriz.
- Catálogo base de países y ciudades.
- Usuario Administrador sin compañía y empresa demo con usuarios de prueba.


