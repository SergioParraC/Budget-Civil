# Modelo de datos

## 1. Entidades principales

* **Company (`Compania`):** Representa la organización/empresa constructora dentro del esquema multitenant.
* **Role (`Rol`):** Catálogo editable de roles del sistema. Cada rol tiene identificador GUID propio y metadatos de activación.
* **User (`Usuario`):** Usuario del sistema autenticado via JWT, asociado a un rol por `role_id`.
* **Project (`Proyecto`):** Objeto contenedor principal del presupuesto, con divisa única por proyecto (ISO 4217), topes y estado de aprobación.
* **ProjectConfiguration (`ConfiguracionProyecto`):** Almacena parámetros de control y tope presupuestal del proyecto.
* **WbsChapter (`Capitulo`):** Agrupador jerárquico de nivel superior dentro del presupuesto (Estructura de Desglose del Trabajo).
* **WbsItem (`Item` / `Actividad`):** Unidad de obra/actividad presupuestable dentro de un capítulo, vinculada a un APU.
* **Apu (`Análisis de Precio Unitario`):** Estructura técnica de composición de costos directos (materiales, equipos, mano de obra, matrices, etc.). Posee la propiedad `Is_Public`.
* **Resource (`Insumo`):** Tabla unificada relacional que almacena todos los insumos (materiales, mano de obra, equipos, etc.) y elementos reutilizables (Matrices).
* **ResourceType (`TipoInsumo`):** Tabla de catálogo que clasifica el insumo (`Materiales`, `Mano de Obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`).
* **ApuResource (`DetalleApu` / `Apu_Insumo`):** Tabla asociativa que vincula un `Apu` o `Matriz` con sus insumos componentes, indicando consumos y desperdicios.
* **Country (`Pais`):** Catálogo parametrizado de países.
* **City (`Ciudad`):** Catálogo parametrizado de ciudades/municipios asociado a `Country`.
* **AuditLog (`BitacoraAuditoria`):** Log de trazabilidad inmutable que registra la participación de personal sobre la creación/modificación de proyectos, APUs e insumos.
* **BudgetVersion (`VersionPresupuesto`):** Captura de estado/congelamiento del presupuesto para el control de cambios e historial.

## 2. Relaciones y cardinalidades

* **Country `1` ──── `N` City:** Un país contiene múltiples ciudades parametrizadas.
* **Role `1` ──── `N` User:** Un rol puede estar asociado a múltiples usuarios.
* **Company `1` ──── `N` User:** Una empresa emplea múltiples usuarios. Un usuario pertenece a una única empresa (`CompanyId` no nulo).
* **Company `1` ──── `N` Project:** Una empresa posee $N$ proyectos aislados lógicamente por tenant.
* **Company `1` ──── `N` Apu:** Una empresa administra múltiples APUs propios.
* **Company `1` ──── `N` Resource:** Una empresa administra múltiples insumos propios.
* **Project `1` ──── `1` ProjectConfiguration:** Un proyecto posee una única configuración de límites y parámetros operativos.
* **Project `1` ──── `N` WbsChapter:** Un proyecto se compone de $N$ capítulos.
* **WbsChapter `1` ──── `N` WbsItem:** Un capítulo contiene $N$ ítems o actividades de obra.
* **WbsItem `1` ──── `1` Apu:** Un ítem de obra referencia un APU para su cálculo de costo unitario.
* **ResourceType `1` ──── `N` Resource:** Un tipo de insumo clasifica múltiples registros dentro de la tabla unificada `Resource`.
* **Apu `1` ──── `N` ApuResource `N` ──── `1` Resource:** Relación muchos a muchos entre APUs e Insumos/Matrices mediante la tabla intermedia `ApuResource`.
* **Resource (Matriz) `1` ──── `N` ApuResource:** Si el `Resource` es de tipo `Matriz` (`ResourceType = Matriz`), actúa como un sub-APU compuesto que se despliega solo a un nivel dentro de otro APU.
* **City `1` ──── `N` Project:** La ubicación de proyecto se selecciona desde el catálogo parametrizado de ciudades.
* **City `1` ──── `N` Resource:** La ubicación de insumos se selecciona desde el catálogo parametrizado de ciudades.
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

### `Role`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria (GUID). |
| `code` | VARCHAR(30) | No | Unico. Código de rol editable (ej. `ADMIN`, `ESTIMATOR`). |
| `name` | VARCHAR(80) | No | Nombre del rol. |
| `is_active` | BOOLEAN | No | Default `true`. Permite desactivar rol sin eliminarlo. |

### `User`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `role_id` | UUID | No | FK `Role(id)`. Rol asignado por catálogo editable. |
| `email` | VARCHAR(150) | No | Unico. Email corporativo válido. |
| `password_hash` | VARCHAR(255) | No | Hash seguro de contraseña. |

### `Country`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `code` | VARCHAR(10) | No | Unico. Código de país. |
| `name` | VARCHAR(120) | No | Nombre del país. |

### `City`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `country_id` | UUID | No | FK `Country(id)`. |
| `name` | VARCHAR(120) | No | Nombre del municipio/ciudad parametrizado. |

### `Project`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `name` | VARCHAR(255) | No | Nombre del proyecto. |
| `currency` | VARCHAR(3) | No | Código ISO 4217 en mayúscula (ej. `COP`, `USD`). Moneda única por proyecto. |
| `status` | VARCHAR(30) | No | Enum: `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, `FINALIZADO`. |
| `is_public` | BOOLEAN | No | Default `false`. Participa en visibilidad pública con regla AND junto a `Apu.is_public`. |

### `ProjectConfiguration`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `project_id` | UUID | No | FK `Project(id)`, Único. |
| `budget_limit` | DECIMAL(18,2) | Sí | Tope presupuestal máximo configurado. |

### `WbsChapter`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `project_id` | UUID | No | FK `Project(id)`. |
| `parent_chapter_id` | UUID | Sí | FK autorreferenciada opcional a `WbsChapter(id)` para subcapítulos. |
| `code` | VARCHAR(30) | No | Código posicional jerárquico (ej. `01`, `01.01`). |
| `name` | VARCHAR(255) | No | Nombre del capítulo. |
| `order_index` | INT | No | Orden de visualización dentro del proyecto. |

### `WbsItem`
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `wbs_chapter_id` | UUID | No | FK `WbsChapter(id)`. |
| `apu_id` | UUID | No | FK `Apu(id)`. |
| `code` | VARCHAR(30) | No | Código posicional del ítem dentro del capítulo. |
| `description` | VARCHAR(255) | No | Descripción de la actividad de obra. |
| `quantity_work` | DECIMAL(18,4) | No | Cantidad de obra. Debe ser estrictamente mayor a 0. |
| `item_total_cost` | DECIMAL(18,2) | No | Subtotal calculado del ítem (`quantity_work * CD_{APU}`). |
| `order_index` | INT | No | Orden de visualización dentro del capítulo. |

### `Resource` (Tabla Unificada de Insumos y Matrices)
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `company_id` | UUID | No | FK `Company(id)`. |
| `resource_type_id` | UUID | No | FK `ResourceType(id)`. |
| `city_id` | UUID | Sí | FK `City(id)`. Ubicación seleccionada desde catálogo parametrizado. |
| `shared_origin_resource_id` | UUID | Sí | FK opcional autorreferenciada a `Resource(id)` cuando el insumo proviene de base compartida. |
| `description` | VARCHAR(255) | No | Campo mínimo obligatorio para guardar el insumo. |
| `unit` | VARCHAR(20) | Sí | Campo requerido para salir de `FALTA_INFORMACION`; puede iniciar vacío. |
| `unit_price` | DECIMAL(18,2) | Sí | Campo requerido para salir de `FALTA_INFORMACION`; puede iniciar vacío. Valor unitario $\ge 0$. |
| `brand` | VARCHAR(100) | Sí | Campo requerido para salir de `FALTA_INFORMACION`; puede iniciar vacío. |
| `quotation_status` | VARCHAR(30) | No | Enum: `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`. Estado calculado por backend (no editable libremente en creación). |
| `distributor` | VARCHAR(150) | Sí | Opcional/Recomendado. Nombre del distribuidor. |
| `user_key` | VARCHAR(50) | Sí | Identificador de negocio editable por usuario. Debe ser único por compañía salvo excepción de recurso compartido. |
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
| `status` | VARCHAR(30) | No | Enum: `COMPLETO`, `FALTA_INFORMACION`. Estado evaluado desde la completitud de `ApuResource`/`Resource`. |
| `is_public` | BOOLEAN | No | Default `false`. Control de privacidad para exposición pública con regla AND junto a `Project.is_public`. |

### `ApuResource` (Tabla Asociativa / Detalle)
| Campo | Tipo | Nulo | Descripción / Validaciones |
|---|---|---|---|
| `id` | UUID | No | Clave Primaria. |
| `apu_id` | UUID | No | FK `Apu(id)`. |
| `resource_id` | UUID | No | FK `Resource(id)` (puede ser Insumo o Matriz). Si es `Matriz`, solo se permite expansión de un nivel. |
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

* **Catálogo Editable de Roles:** La entidad `Role` es editable y usa `id` GUID. `User.role_id` siempre referencia un rol existente y activo.
* **Aislamiento Multitenant:** En las entidades `User`, `Project`, `Apu` y `Resource`, la columna `company_id` es obligatoria y está restringida por una clave foránea referenciando `Company(id)`.
* **Propiedad Exclusiva por Compañía Creadora (sin interventoría):** El acceso y modificación de `Project`, `Apu` y `Resource` se limita a la compañía propietaria (`company_id` del token = `company_id` del registro). No existe acceso por rol/empresa de interventoría en esta fase.
* **Ubicación Parametrizada Obligatoria por Catálogo:** Los campos de ubicación se resuelven por FK (`city_id`) hacia tablas parametrizadas (`City`, `Country`). No se permite creación de nuevos municipios desde módulos transaccionales.
* **Regla de Clave de Insumo (`user_key`) por Compañía:**
  * `user_key` es editable.
  * Debe ser único por compañía para insumos propios.
  * Se permite repetición cuando el recurso sea compartido (por ejemplo, cuando `shared_origin_resource_id` no sea nulo).
  * El `id` técnico (`UUID`) se mantiene único e inmutable.
* **Condición de Insumo Completo / Regla de Cascada `Falta_informacion`:**
  * Para persistir un `Resource`, solo `description` es obligatorio.
  * Si alguno de los campos (`unit_price`, `unit`, `brand`) es NULL o vacío, el backend marca el estado del recurso como `FALTA_INFORMACION`.
  * Si los 4 campos obligatorios están completos y aún no hay confirmación de cotización, el recurso queda en `POR_COTIZAR`.
  * El paso a `COTIZADO` se realiza por caso de uso de cotización, nunca por asignación libre en el DTO de creación/edición.
* **Mapa de Estado por Entidad (Enums separados con mapeo explícito):**
  * `Resource.quotation_status`: `FALTA_INFORMACION`, `POR_COTIZAR`, `COTIZADO`.
  * `Apu.status`: `FALTA_INFORMACION` si al menos un `Resource` vinculado está en `FALTA_INFORMACION`; `COMPLETO` en caso contrario.
  * `Project.status`:
    1. `FALTA_INFORMACION` si existe al menos un `Apu` en `FALTA_INFORMACION`.
    2. `POR_COTIZAR` si no hay faltantes, pero existe al menos un insumo/actividad pendiente de cotización.
    3. `COTIZADO` si todo está completo y cotizado.
    4. `FINALIZADO` solo por flujo de aprobación única válida.
* **Ejecución de Cascada Sincrónica y Transaccional:**
  * Cualquier cambio en un `Resource` que afecte completitud/cotización dispara en la misma transacción la reevaluación de `Apu.status` y `Project.status`.
  * En subsanación (completar campos faltantes), el sistema revierte `FALTA_INFORMACION` en cascada cuando ya no existan bloqueantes.
* **Regla de Matriz Reutilizable:**
  * Un `Resource` clasificado con `ResourceType = Matriz` **no puede ser asignado directamente a un `WbsItem`** como ítem final de obra. Solo puede vincularse como insumo componente dentro de la relación `ApuResource` de otro APU.
  * La expansión de una Matriz se limita a un solo nivel (Matriz -> Insumos directos).
  * No se permite anidamiento Matriz -> Matriz ni autorreferencia.
* **Regla de Visibilidad Pública (AND):**
  * Un APU se expone en la base compartida solo si `Project.is_public = true` y `Apu.is_public = true`.
  * La consulta pública aplica validación de visibilidad en ese caso y siempre retorna DTO sanitizado.
* **Aprobación Única para Estado `Finalizado`:**
  * Para cambiar `Project.status` a `FINALIZADO`, el backend valida obligatoriamente que:
    1. Ningún APU o Insumo del proyecto esté en `FALTA_INFORMACION`.
    2. El usuario que aprueba tenga permisos sobre el proyecto de su misma compañía.
    3. Exista captura temporal de firma desde Signature Pad en frontend para el acto de aprobación.
  * Esta validación se resuelve en Domain/Application Layer y el backend registra un evento `SIGN` en `AuditLog`.
* **Mecanismo de Firma de Aprobación:**
  * La firma se captura en frontend mediante Signature Pad como evidencia temporal de la acción.
  * El backend no persiste token/hash de firma.
* **Política de Divisa por Proyecto:**
  * `Project.currency` usa código ISO 4217 en mayúsculas y longitud fija de 3 caracteres.
  * Cada proyecto maneja una sola moneda activa.
  * No existe conversión automática de valores monetarios en backend.

## 5. Reglas de calculo

### A. Costo Unitario de Insumo con Desperdicio ($C_{insumo}$)
Para materiales u otros insumos con porcentaje de desperdicio:
$$C_{insumo} = \text{PrecioUnitario} \times \text{Consumo} \times \left(1 + \frac{\text{PorcentajeDesperdicio}}{100}\right)$$

### B. Costo Directo Unitario del APU ($CD_{APU}$)
Suma del costo de todos sus insumos directos (incluyendo Matriz expandida a un nivel) dividida por el rendimiento del APU:
$$CD_{APU} = \frac{\sum_{i=1}^{n} C_{insumo\_i}}{R_{APU}}$$

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
* **Cierre por Aprobación:**
  * Al aprobar un proyecto, el backend registra en `AuditLog` la acción `SIGN` con `user_id`, `company_id`, `entity_name`, `entity_id` y `created_at`.

## 7. Indices y rendimiento

Para asegurar tiempos de respuesta $p95 \le 1.5$ segundos en consultas WBS y en la base pública compartida:

* **Índice de Catálogo de Roles:** `CREATE UNIQUE INDEX uq_role_code ON Role(code);`
* **Índice Multitenancy Combinado:** `CREATE INDEX idx_project_company ON Project(company_id, id);`
* **Índice para la Base Compartida (Anonimizada):** `CREATE INDEX idx_apu_public_search ON Apu(is_public, id) INCLUDE (code, description, unit);`
* **Índice de Visibilidad de Proyecto:** `CREATE INDEX idx_project_public ON Project(is_public, id);`
* **Índice de Relación WBS -> APU:** `CREATE INDEX idx_wbsitem_apu ON WbsItem(apu_id, wbs_chapter_id);`
* **Índice Deduplicado de Insumos:** `CREATE INDEX idx_resource_dedup ON Resource(company_id, resource_type_id, brand, description, unit_price);`
* **Índice de Clave de Insumo por Compañía (con excepción compartida):** `CREATE UNIQUE INDEX uq_resource_company_user_key ON Resource(company_id, user_key) WHERE user_key IS NOT NULL AND shared_origin_resource_id IS NULL;`
* **Índice de Ubicación Parametrizada de Insumos:** `CREATE INDEX idx_resource_company_city_type ON Resource(company_id, city_id, resource_type_id);`
* **Índice de Trazabilidad:** `CREATE INDEX idx_audit_entity ON AuditLog(entity_name, entity_id, created_at);`

## 8. Migraciones y datos iniciales
* **Estrategia de Migraciones:** Uso de **EF Core Migrations Code-First**. Las migraciones deben ser idempotentes para ejecutarse mediante Docker Compose en la inicialización del servidor.
* **Seed Data Inicial Obligatorio:**
  1. Registros de la tabla `Role` (catálogo editable) con GUID fijo inicial:
     * `ADMIN` = `7f62fdd0-6f67-4b7b-8f4a-1af4f4ab0001`
     * `ESTIMATOR` = `7f62fdd0-6f67-4b7b-8f4a-1af4f4ab0002`
  * `AUDITOR` = `7f62fdd0-6f67-4b7b-8f4a-1af4f4ab0003`
  2. Registros de la tabla `ResourceType`: `Materiales`, `Mano de obra`, `Herramienta`, `Equipos`, `Auxiliares`, `Trabajos`, `Matriz`.
  3. Catálogo base de `Country` y `City` para selección de ubicación (solo parametrización administrada).
  4. Usuario Administrador Inicial (`ADMIN`) y Empresa Demo por defecto para ejecución de pruebas integradas.
