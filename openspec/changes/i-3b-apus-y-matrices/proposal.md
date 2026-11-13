# Cambio: i-3b-apus-y-matrices

## Why

`E-3b` abre la composición técnica del presupuesto: sin APU no existe costo, firma ni cierre
(`RF-10`…`RF-13`) y el conteo de uso de insumos (`RF-22`) exige APUs vinculados a cantidades de obra
(`D-33`). Hoy no existe la capacidad `apus` en `openspec/specs/`, el código no tiene `Apu`,
`ApuResource` ni `MatrixComponent`, y `E-3a` difirió a esta etapa la composición de Matriz, su copia y
la reconciliación de sus componentes (`BR-021`). Sin este delta, `E-4a` construiría el cálculo sobre una
composición no especificada y el módulo `M-05` no tendría contrato estable (`D-109`).

**Dependencia declarada:** `E-3b` depende de `E-3a` **y de `E-2`**. `RF-07` exige el vínculo 1:1 con el
ítem del EDT y `RF-22` exige `quantity_work`; ambos son de `M-03`. En el código actual **no existen
entidades de EDT ni su migración**, por lo que la implementación de este cambio queda bloqueada hasta
que `E-2` esté cerrado (`PLAN §4.5.1`). Ver `design.md` → `INC-01` e `INC-02`.

## What Changes

- Se crea la entidad `Apu` **propia de cada proyecto**, sin `code` (`D-31`) y sin `is_public` (`D-22`),
  vinculada **1:1 con un ítem del EDT** del mismo proyecto (`RF-07`, `BR-016`, `D-16`).
- Se crea `ApuResource` como composición del APU con `consumption > 0` y `waste_percentage` entre 0 y 100
  (`05 §3`).
- Se crea `MatrixComponent` para la composición de un insumo de tipo Matriz, con la misma semántica de
  consumo y desperdicio (`RF-09`, `BR-008`, `D-48`); la Matriz **no pertenece al EDT**, **no se anida** y
  se reutiliza en varios APUs del mismo proyecto.
- Se especifica la **copia de un APU desde otro proyecto de la misma compañía** hacia un ítem libre del
  destino, sin aprobación y con el actor como último modificador (`RF-07`, `D-13`, `D-16`, `D-24`).
- Se especifica la **reconciliación de insumos**: el sistema presenta por insumo si existe uno igual, uno
  similar o ninguno, y el usuario decide usar el existente o conservar el de origen (`RF-08`, `D-25`). La
  copia de una **Matriz** exige la misma reconciliación de sus componentes (`BR-021`).
- Se especifica el **conteo de uso** de cada insumo como `Σ(consumption × quantity_work)` por ítem, sin
  desperdicio, con **expansión de un nivel** de los componentes de una Matriz (`RF-22`, `R-21`, `D-33`).
- Se registra **autoría** (`last_modified_by`, `last_modified_at`) en el APU y en el insumo Matriz cuando
  cambia su composición; se aplica **concurrencia optimista** con `rowversion` y respuesta `409`
  (`D-08`, `D-50`, `D-97`).
- Se añaden los permisos `APUS_READ` y `APUS_WRITE` al `seed` versionado e idempotente (`D-82`).
- **Se modifica la capacidad `insumos`** en dos puntos: la completitud de una Matriz no se evalúa con los
  cinco campos de `BR-009` (`REQ-INSUMOS-04`) y la copia genérica de un insumo de tipo Matriz deja de ser
  válida porque produce una Matriz vacía (`REQ-INSUMOS-09`).

## Capabilities

### New Capabilities

- `apus`: composición de un APU propio de un proyecto y vínculo 1:1 con el ítem del EDT, composición de
  Matrices y su reutilización, copia de APU y de Matriz desde otro proyecto de la compañía con
  reconciliación de insumos, conteo de uso, autoría, concurrencia y autorización.

### Modified Capabilities

- `insumos`: `REQ-INSUMOS-04` incorpora la excepción de completitud de la Matriz y `REQ-INSUMOS-09` deja
  de admitir la copia genérica de una Matriz, que pasa a la operación de copia de Matriz con
  reconciliación (`BR-021`).

## Fuera de alcance

- **Cálculo** (`RF-10`): costo directo, AIU, redondeo y la cascada de recálculo de `BR-003` pertenecen a
  `E-4a` y dependen de `X-15`. Este cambio **no persiste ni devuelve costos**.
- **Cotización** (`RF-11`) y el paso a `COTIZADO`, incluidas las Matrices (`E-4a`).
- **Aprobaciones y cierre** (`RF-12`, `RF-13`, `RF-21`): `ApuApproval`, `D-52` y `D-53` se implementan en
  `E-4b`; este cambio solo define los puntos de edición que invalidarán la aprobación.
- **Catálogo abierto** (`RF-15`): publicación y copia desde el catálogo público (`E-5`, `D-28`, `D-35`).
- **EDT** (`RF-04`): capítulos, ítems, numeración y cantidades de obra (`E-2`, `M-03`).
- **Baja lógica expuesta**: `is_active`/`deleted_at` se modelan (`D-49`) sin endpoint de baja.
- **Frontend y cliente Angular** (`D-109`): el contrato se documenta; el cliente se regenera al abrir la
  etapa de interfaz (`D-56`).

## Decisiones aplicadas

| Tema | Decisiones |
|---|---|
| Modelo APU y Matriz | `D-05`, `D-16`, `D-22`, `D-31`, `D-48`, `D-49` |
| Copia y reconciliación | `D-13`, `D-24`, `D-25`, `D-27`, `D-29`, `R-17`, `BR-018`, `BR-021` |
| Conteo de uso | `D-33`, `R-21`, `RF-22` |
| Persistencia y concurrencia | `D-50`, `D-95`, `D-97`, `D-107` |
| Contrato y datos | `D-41`, `D-55`, `D-99`, `D-100`, `D-101` |
| Autorización | `D-02`, `D-34`, `D-82`, `D-83`, `D-84`; matriz `1.3.2` |
| Autoría | `D-08`, `RF-14` |

## Precisiones de alcance

| Punto | Decisión de este cambio | Motivo |
|---|---|---|
| Rutas | `/api/v1/projects/{projectId}/apus`, `.../matrices` y `.../resource-usage` | `D-05`/`R-01` hacen el APU propio de un proyecto y el contrato v1 existente ya es *project-scoped*; corrige la tabla de `PLAN §3.4.3` (`INC-03`) |
| Alta del APU | Exige un `wbsItemId` existente, del mismo proyecto y sin APU | `BR-016` y `D-16` fijan 1:1; `UC-05` crea el ítem antes del APU (`INC-09`) |
| Completitud del APU | `COMPLETO` con descripción, unidad, rendimiento > 0 y al menos una línea; si no, `FALTA_INFORMACION` | Conjunto cerrado de `05 §3`; la regla se deriva de sus campos (`INC-06`, `DERIVADO`) |
| Completitud de la Matriz | Sin unidad o sin componentes ⇒ `FALTA_INFORMACION`; con ambos ⇒ `POR_COTIZAR` | `BR-009` no puede aplicarse a un insumo compuesto (`INC-08`) |
| Similitud | Descripción parcial o `user_key` igual, igual que `REQ-INSUMOS-05` | `D-25` no define el criterio; se reutiliza el ya especificado (`INC-05`, `DERIVADO`) |
| Expansión de Matriz en el conteo | Un solo nivel, porque `BR-008` prohíbe anidar | `UC-05` y `BR-008`; `R-21` no lo precisaba (`INC-04`, `DERIVADO`) |
| Desperdicio en el conteo | No se aplica `waste_percentage` | `R-21` define el conteo como `consumo × cantidad`; el desperdicio pertenece a `RF-10` (`E-4a`) |
| Unicidad de líneas | Sin restricción única en `apu_resources` ni `matrix_components` | No existe fuente que la exija; la unicidad de insumos es sugerencia (`Q-09`) |
| APU sin ítem | No se admite | `BR-016`; requiere que `E-2` permita crear el ítem antes del APU (`INC-02`) |
| Catálogo público | Fuera de alcance | `RF-07` se completa con `E-5` (`PLAN §4.3`) |

## Criterios de aceptación

1. `openspec validate i-3b-apus-y-matrices --strict` pasa sin errores ni advertencias.
2. Cada `REQ-APUS-*` y cada `REQ-INSUMOS-*` modificado tiene al menos un escenario en formato
   `Dado / Cuando / Entonces` y cita sus `RF`, `BR`, `R` y `D`.
3. Los escenarios cubren permisos, datos inválidos, concurrencia, aislamiento entre compañías y los
   errores declarados en `UC-05`: rendimiento ≤ 0, anidamiento de matrices, copia desde otra compañía y
   APU ya vinculado a otro ítem.
4. La copia demuestra independencia en ambos sentidos y la reconciliación nunca decide por el usuario
   (`D-25`).
5. El esquema especificado contiene `apus`, `apu_resources` y `matrix_components` con `CHECK`, FKs
   `RESTRICT`/`CASCADE`, índices y `rowversion` (`PLAN §3.3.2`, `§3.3.5`, `§3.3.6`).
6. El conteo de uso es verificable con la fórmula de `R-21` y con la expansión de un nivel.
7. Ningún requisito depende de un `X` abierto; `X-15` queda fuera porque el cálculo no se especifica.
8. La matriz de trazabilidad `requisito → escenario → RF/BR/R → decisión → prueba` queda completa.

## Impact

- **Contrato de API**: nuevos endpoints bajo `/api/v1/projects/{projectId}/apus`,
  `/api/v1/projects/{projectId}/matrices` y `/api/v1/projects/{projectId}/resource-usage`, con
  `camelCase`, `ProblemDetails` y paginación `page`/`pageSize` (`D-99`, `D-100`, `D-101`). Se modifica el
  comportamiento del endpoint existente `POST /projects/{projectId}/resources/copy` para rechazar el tipo
  Matriz (`REQ-INSUMOS-09`).
- **Base de datos**: tablas `apus`, `apu_resources` y `matrix_components`; índice único filtrado sobre
  `wbs_items.apu_id`; `rowversion` en `apus`; migración manual `YYYYMMDDHHMM_apus_add_e3b` con respaldo y
  reversión conocida (`D-40`, `D-107`). Requiere el ajuste de `wbs_items.apu_id` descrito en `INC-02`.
- **Permisos**: `APUS_READ` para Administrador, Director, Presupuestador y Auditor; `APUS_WRITE` para
  Director y Presupuestador; el conteo de uso se lee con `RESOURCES_READ` (`D-82`, `D-83`, `D-84`).
- **Código**: módulo `M-05` en las cuatro capas (`Domain/Apus`, `Application/Apus`,
  `Infrastructure/Persistence`, endpoints en `Api`) y el read model de conteo de `M-06`; sin dependencias
  de infraestructura de otros módulos (`D-54`, `D-95`).
- **Pruebas**: unitarias de dominio y casos de uso; integración manual contra SQL Server local para
  `CHECK`, FKs, índices, `rowversion`, copia y conteo; `NetArchTest` para las reglas de capas y módulos
  (`D-63`, `D-66`, `D-105`).
- **Dependencias**: ninguna nueva; se mantienen las prohibiciones de `D-104` y `D-106`.
- **Etapas posteriores**: `E-4a` consume la composición para `RF-10` y cierra la fórmula de `X-15`;
  `E-4b` añade `ApuApproval` y la invalidación de `D-53`; `E-5` añade la copia desde el catálogo público.

