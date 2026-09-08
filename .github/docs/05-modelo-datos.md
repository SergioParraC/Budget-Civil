# Modelo de datos

## 1. Entidades principales

* **Company (`Compania`):** Representa la organización/empresa constructora dentro del esquema multitenant.
* **User (`Usuario`):** Usuario del sistema autenticado via JWT, con rol asignado (`ADMIN`, `ESTIMATOR`, `AUDITOR`).
* **Project (`Proyecto`):** Objeto contenedor principal del presupuesto, parámetros de divisas, topes y firmas de aprobadores.
* **ProjectConfiguration (`ConfiguracionProyecto`):** Almacena los parámetros de control, tope presupuestal, firmas registradas del profesional realizador y del director de proyecto a cargo.
* **WbsChapter (`Capitulo`):** Agrupador jerárquico de nivel superior dentro del presupuesto (Estructura de Desglose del Trabajo).
* **WbsItem (`Item` / `Actividad`):** Unidad de obra/actividad presupuestable dentro de un capítulo, vinculada a un APU.
* **Apu (`Análisis de Precio Unitario`):** Estructura técnica de composición de costos directos (materiales, equipos, mano de obra, matrices, etc.). Posee la propiedad `Is_Public`.
* **Resource (`Insumo`):** Tabla unificada relacional que almacena todos los insumos (materiales, mano de obra, equipos, etc.) y elementos reutilizables (Matrices).
* **ResourceType (`TipoInsumo`):** Tabla de catálogo que clasifica el insumo (`Materiales`, `Mano de Obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`).
* **ApuResource (`DetalleApu` / `Apu_Insumo`):** Tabla asociativa que vincula un `Apu` o `Matriz` con sus insumos componentes, indicando consumos, rendimientos y desperdicios.
* **AuditLog (`BitacoraAuditoria`):** Log de trazabilidad inmutable que registra la participación de personal sobre la creación/modificación de proyectos, APUs e insumos.
* **BudgetVersion (`VersionPresupuesto`):** Captura de estado/congelamiento del presupuesto para el control de cambios e historial.

## 2. Relaciones y cardinalidades

* **Company `1` ──── `N` User:** Una empresa emplea múltiples usuarios. Un usuario pertenece a una única empresa (`CompanyId` no nulo).
* **Company `1` ──── `N` Project:** Una empresa posee $N$ proyectos aislados lógicamente por tenant.
* **Project `1` ──── `1` ProjectConfiguration:** Un proyecto posee una única configuración de límites, aprobaciones y firmas.
* **Project `1` ──── `N` WbsChapter:** Un proyecto se compone de $N$ capítulos.
* **WbsChapter `1` ──── `N` WbsItem:** Un capítulo contiene $N$ ítems o actividades de obra.
* **WbsItem `1` ──── `1` Apu:** Un ítem de obra referencia un APU para su cálculo de costo unitario.
* **ResourceType `1` ──── `N` Resource:** Un tipo de insumo clasifica múltiples registros dentro de la tabla unificada `Resource`.
* **Apu `1` ──── `N` ApuResource `N` ──── `1` Resource:** Relación muchos a muchos entre APUs e Insumos/Matrices mediante la tabla intermedia `ApuResource`.
* **Resource (Matriz) `1` ──── `N` ApuResource:** Si el `Resource` es de tipo `Matriz` (`ResourceType = Matriz`), actúa como un sub-APU compuesto que se despliega recursivamente dentro de otro APU.
* **Project `1` ──── `N` BudgetVersion:** Un proyecto registra múltiples versiones congeladas de presupuesto.
* **User `1` ──── `N` AuditLog:** Un usuario registra múltiples participaciones en la bitácora de auditoría.

## 3. Diccionario de datos

### `Company`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `business_name` | VARCHAR(255) | No | Razón social de la empresa. |
| `tax_id` | VARCHAR(20) | No | Unico. NIT/CC de la empresa. |
| `is_active` | BOOLEAN | No | Default `true`. Estado de la cuenta. |

### `User`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `email` | VARCHAR(150) | No | Unico. Email corporativo válido. |
| `password_hash` | VARCHAR(255) | No | Hash seguro de contraseña. |
| `role` | VARCHAR(30) | No | Enum: `ADMIN`, `ESTIMATOR`, `AUDITOR`. |

### `Project`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `name` | VARCHAR(255) | No | Nombre del proyecto. |
| `currency` | VARCHAR(10) | No | Divisa asignada (ej. `COP`, `USD`). |
| `status` | VARCHAR(30) | No | Enum: `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`. |
| `is_public` | BOOLEAN | No | Default `false`. Define si el proyecto y sus APUs son públicos. |

### `ProjectConfiguration`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `project_id` | UUID | No | FK `Project(id)`, Único. |
| `budget_limit` | DECIMAL(18,2) | Sí | Tope presupuestal máximo configurado. |
| `estimator_signature_token` | VARCHAR(500) | Sí | Hash/Token de firma del profesional presupuestador. |
| `director_signature_token` | VARCHAR(500) | Sí | Hash/Token de aval del Director de Proyecto. |

### `Resource` (Tabla Unificada de Insumos y Matrices)
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `resource_type_id` | UUID | No | FK `ResourceType(id)`. |
| `description` | VARCHAR(255) | No | Campo obligatorio (evalúa `Falta_informacion`). |
| `unit` | VARCHAR(20) | No | Campo obligatorio (ej. m2, m3, kg, und, hr). |
| `unit_price` | DECIMAL(18,2) | No | Campo obligatorio. Valor unitario $\ge 0$. |
| `brand` | VARCHAR(100) | No | Campo obligatorio. Marca del insumo. |
| `distributor` | VARCHAR(150) | Sí | Opcional/Recomendado. Nombre del distribuidor. |
| `location_city_country` | VARCHAR(150) | Sí | Opcional/Recomendado. Ubicación geográfica. |
| `user_key` | VARCHAR(50) | Sí | Opcional/Recomendado. Clave asignada por el usuario. |
| `details` | TEXT | Sí | Opcional/Recomendado. Detalle técnico. |
| `contact_number` | VARCHAR(50) | Sí | Opcional/Recomendado. Teléfono de contacto. |
| `is_public` | BOOLEAN | No | Default `false`. Habilita visibilidad en la base compartida. |

### `Apu`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `code` | VARCHAR(50) | No | Código interno del APU (ej. `APU-CON-01`). |
| `description` | TEXT | No | Descripción de la actividad. |
| `unit` | VARCHAR(20) | No | Unidad de medida del APU. |
| `yield` | DECIMAL(18,4) | No | Rendimiento de producción ($> 0$). |
| `status` | VARCHAR(30) | No | Enum: `COMPLETO`, `FALTA_INFORMACION`. |
| `is_public` | BOOLEAN | No | Default `false`. Control de privacidad DTO. |

### `ApuResource` (Tabla Asociativa / Detalle)
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `apu_id` | UUID | No | FK `Apu(id)`. |
| `resource_id` | UUID | No | FK `Resource(id)` (Puede ser Insumo o Matriz). |
| `consumption` | DECIMAL(18,4) | No | Cantidad de consumo requerida por unidad de APU. |
| `waste_percentage` | DECIMAL(5,2) | No | Default `0.00`. Porcentaje de desperdicio (0 a 100%). |

### `AuditLog` (Participación de Personal)
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `user_id` | UUID | No | FK `User(id)`. Identifica al participante. |
| `entity_name` | VARCHAR(50) | No | Entidad afectada (`Project`, `Apu`, `Resource`). |
| `entity_id` | UUID | No | ID del registro modificado/creado. |
| `action_type` | VARCHAR(20) | No | Enum: `CREATE`, `UPDATE_VALUE`, `SIGN`. |
| `created_at` | TIMESTAMP | No | Default `CURRENT_TIMESTAMP`. |

## 4. Reglas de integridad

* **Aislamiento Multitenant:** En las entidades `User`, `Project`, `Apu` y `Resource`, la columna `company_id` es obligatoria y está restringida por una clave foránea referenciando `Company(id)`.
* **Condición de Insumo Completo / Regla de Cascada `Falta_informacion`:**
  * Si en la entidad `Resource` alguno de los campos (`description`, `unit_price`, `unit`, `brand`) es NULL o vacío, el backend marca el estado del recurso como `FALTA_INFORMACION`.
  * Si un `Apu` posee al menos un `ApuResource` cuyo `Resource` está en `FALTA_INFORMACION`, el estado del `Apu` se evalúa automáticamente a `FALTA_INFORMACION`.
  * Si un `Project` posee al menos un `Apu` en `FALTA_INFORMACION`, el estado global del proyecto se fuerza a `FALTA_INFORMACION`.
* **Regla de Matriz Reutilizable:**
  * Un `Resource` clasificado con `ResourceType = Matriz` **no puede ser asignado directamente a un `WbsItem`** como ítem final de obra. Solo puede vincularse como insumo componente dentro de la relación `ApuResource` de otro APU.
* **Aprobación Dual para Estado `Finalizado`:**
  * Para cambiar `Project.status` a `FINALIZADO`, el backend valida obligatoriamente que:
    1. Ningún APU o Insumo del proyecto esté en `FALTA_INFORMACION`.
    2. `ProjectConfiguration.estimator_signature_token` NO sea nulo.
    3. `ProjectConfiguration.director_signature_token` NO sea nulo.

## 5. Reglas de calculo

### A. Costo Unitario de Insumo con Desperdicio ($C_{insumo}$)
Para materiales u otros insumos con porcentaje de desperdicio:
$$C_{insumo} = \text{PrecioUnitario} \times \text{Consumo} \times \left(1 + \frac{\text{PorcentajeDesperdicio}}{100}\right)$$

### B. Costo Directo Unitario del APU ($CD_{APU}$)
Suma del costo de todos sus insumos directos y matrices compuestas dividida entre o ajustada por el rendimiento:
$$CD_{APU} = \sum_{i=1}^{n} C_{insumo\_i}$$

### C. Costo Total del Ítem de Obra ($CT_{Item}$)
Calculado a partir de la cantidad de obra presupuestada ($Q_{Item}$) y el costo directo del APU:
$$CT_{Item} = Q_{Item} \times CD_{APU}$$

### D. Costo Directo del Proyecto ($CD_{Proyecto}$)
$$CD_{Proyecto} = \sum \text{Subtotales de Capítulos}$$

### E. Presupuesto Total con Indirectos / AIU ($PT_{Proyecto}$)
Aplicación de factores indirectos (Administración $\%A$, Imprevistos $\%I$, Utilidad $\%U$):
$$PT_{Proyecto} = CD_{Proyecto} \times (1 + \%A + \%I + \%U)$$

* **Regla de Redondeo:** Todos los cálculos monetarios finales se redondean a dos decimales (`DECIMAL(18,2)`) utilizando **Banker's Rounding** (redondeo al par más cercano). Los consumos y rendimientos intermedia se conservan a 4 decimales (`DECIMAL(18,4)`).

## 6. Estrategia de versionado de presupuesto

* **Congelamiento de Versión (`BudgetVersion`):**
  * Cuando se firma o aprueba un presupuesto, se crea un snapshot completo en la tabla `BudgetVersion` serializando la estructura jerárquica WBS y la matriz de APUs en formato JSON.
  * Una versión congelada es estrictamente de **solo lectura**.
* **Recálculo por Modificación:**
  * Si un usuario modifica el costo de un `Resource` o el rendimiento de un `Apu`, el motor de cálculo marca el presupuesto activo como "Desactualizado" y ejecuta una recálculo en cascada desde el insumo hasta el subtotal del proyecto en tiempo real (`BR-003`).

## 7. Indices y rendimiento

Para asegurar tiempos de respuesta $p95 \le 1.5$ segundos en consultas WBS y en la base pública compartida:

* **Índice Multitenancy Combinado:** `CREATE INDEX idx_project_company ON Project(company_id, id);`
* **Índice para la Base Compartida (Anonimizada):** `CREATE INDEX idx_apu_public_search ON Apu(is_public, id) INCLUDE (code, description, unit);`
* **Índice Deduplicado de Insumos:** `CREATE INDEX idx_resource_dedup ON Resource(company_id, resource_type_id, brand, description, unit_price);`
* **Índice de Trazabilidad:** `CREATE INDEX idx_audit_entity ON AuditLog(entity_name, entity_id, created_at);`

## 8. Migraciones y datos iniciales
* **Estrategia de Migraciones:** Uso de **EF Core Migrations Code-First**. Las migraciones deben ser idempotentes para ejecutarse mediante Docker Compose en la inicialización del servidor.
* **Seed Data Inicial Obligatorio:**
  1. Registros de la tabla `ResourceType`: `Materiales`, `Mano de obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`.
  2. Usuario Administrador Inicial (`ADMIN`) y Empresa Demo por defecto para ejecución de pruebas integradas.
