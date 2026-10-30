# Proyecto: Budget Civil

Contexto operativo del proyecto para OpenSpec. Este archivo describe el stack, las convenciones y las
restricciones vigentes; **no contiene requisitos**. El comportamiento normativo vive en
`openspec/specs/` y la autoridad sobre alcance y decisiones vive en `PLAN_INGENIERIA.md`.

Autoridad de este documento: `PLAN_INGENIERIA.md` §6.1.1 (contexto operativo).

> Nota de mantenimiento: la CLI de OpenSpec mantiene su configuración de máquina en
> `openspec/config.yaml`. Este archivo se conserva por exigencia del PLAN §6.1.2 y §4.8.2 y se
> actualiza a mano; no sustituye a `config.yaml`.

## 1. Qué es el producto

Plataforma SaaS multiempresa para elaborar presupuestos de obra civil (APU, EDT, insumos, AIU). El
producto es académico (`D-10`), de un solo desarrollador (`A-04`) y se despliega en un servidor Linux
con Docker Compose (`R-12`).

## 2. Jerarquía de fuentes

| Artefacto | Contiene | Autoridad |
|---|---|---|
| `PLAN_INGENIERIA.md` | Problema, decisiones `D`, arquitectura, plan | Alcance y decisiones |
| `openspec/project.md` | Stack, convenciones y restricciones | Contexto operativo |
| `openspec/specs/` | Comportamiento por capacidad | **Comportamiento** |
| `openspec/changes/` | Cambios activos y archivados | Trabajo en curso e historial |
| `.github/docs/00`–`06` y `09` | Documentación narrativa | Apoyo; se actualiza por capítulo |
| `specs/001`, `specs/002` | Evidencia histórica de lo implementado con Spec Kit | **No normativa** (`D-108`) |
| Código y pruebas | Implementación y evidencia | Verificación |

**Regla de conflicto** (§6.1.1): si dos artefactos discrepan sobre comportamiento, manda
`openspec/specs`; si discrepan sobre alcance o decisión, manda `PLAN_INGENIERIA.md`. En ambos casos se
detiene el cambio y se resuelve antes de continuar.

## 3. Stack vigente

| Capa | Tecnología | Decisión |
|---|---|---|
| Runtime | .NET 10 / ASP.NET Core | `03 §2` |
| Persistencia | EF Core 10 code-first sobre SQL Server 2022, esquema único | `D-95`, `D-40` |
| Autenticación | ASP.NET Core Identity (PBKDF2) | `D-81` |
| Contrato | OpenAPI con Swashbuckle en `/swagger` | `D-42` |
| Validación | `FluentValidation` + `DependencyInjectionExtensions` | `D-104` |
| Mapeo | Manual, sin AutoMapper ni Mapster | `D-41`, `D-104` |
| Pruebas | xUnit + FluentAssertions + Moq; `NetArchTest` para arquitectura | `D-105`, `D-63` |
| CI | GitHub Actions; cobertura ≥ 80 % en dominio | `D-46`, `D-61` |
| Cliente | Angular generado desde OpenAPI (**fuera de alcance hasta la etapa de interfaz**) | `D-56`, `D-109` |

**Prohibiciones de stack** (`D-104`, `D-106`): `AutoMapper`, `Mapster`, `FluentValidation.AspNetCore`,
`EF Core InMemory`, `WebApplicationFactory`, Testcontainers. La integración se verifica manualmente
contra SQL Server local (`D-66`).

## 4. Organización del código

Proyectos por capa con carpetas por módulo (`D-54`): `BudgetCivil.Domain`, `BudgetCivil.Application`,
`BudgetCivil.Infrastructure`, `BudgetCivil.Api`, `BudgetCivil.Tests.Unit`,
`BudgetCivil.Tests.Integration`, `BudgetCivil.Tests.Architecture` (`D-105`).

Un módulo `M-0x` es una carpeta dentro de cada capa; no accede a la infraestructura de otro módulo
(`3.4.1`, `3.4.6`).

## 5. Convenciones de especificación

| Elemento | Convención | Decisión |
|---|---|---|
| Idioma | Prosa en español; código, rutas, campos y `permission_key` en inglés | `D-69` |
| Identificador de requisito | `REQ-<CAPACIDAD>-<NN>` | `PLAN §6.1.4` |
| Identificador de escenario | `SC-<CAPACIDAD>-<NN>` | `PLAN §6.1.4` |
| Escenario | `Dado` / `Cuando` / `Entonces` | `PLAN §6.1.4` |
| Verbo normativo | `DEBE (MUST)`, porque el validador de OpenSpec exige el literal `MUST` en el cuerpo del requisito; se conserva la lectura en español | `D-69`, `PLAN §6.1.4` |
| Citas por requisito | Cada requisito cita los `RF`, `BR`, `R` y `D` que implementa, en una línea `Citas:` | `PLAN §6.1.4` |
| Casos negativos | Obligatorios en cada capacidad: permisos, datos inválidos, aislamiento entre compañías y concurrencia | `PLAN §6.1.4` |
| Nivel de detalle | Comportamiento observable; sin clases, SQL, tablas ni frameworks | `PLAN §6.2.5` |
| Trazabilidad | Tabla `requisito → escenario → RF/BR/R → decisión → prueba` al cierre de cada delta | `PLAN §6.1.4`, `§9` |

**Prohibiciones de redacción**: no se especifica `RF-04` (EDT), `RF-17` (tope y alertas) ni `M-03`…`M-09`
en `E-1b`; no se especifica frontend, componentes, rutas ni cliente Angular (`D-109`); no se convierten
en requisito elementos marcados `DERIVADO` o `POR CONFIRMAR`.

## 6. Convenciones de contrato v1

| Elemento | Definición | Decisión |
|---|---|---|
| Versionado | Prefijo de ruta `/api/v1` | `3.4.5` |
| Endpoints anónimos | Solo el autorregistro y el inicio de sesión | `D-72` |
| Nombres de campos | `camelCase` en todos los endpoints | `D-55`, `D-99` |
| Errores | `ProblemDetails` (RFC 7807) con el código de negocio en `extensions.code` | `D-100` |
| Paginación | `page` y `pageSize`; conjunto por defecto `{10, 50, 100}` y máximo configurable | `D-101` |
| Dueño de rutas | `M-01` es dueño único de `/auth`, `/profile`, `/users`, `/companies` y `/catalogs`; `M-02` solo expone `/projects` | `D-102` |
| Asignaciones a proyecto | `/projects/{id}/memberships` | `D-103` |
| DTOs | Solo DTOs en la frontera; nunca se exponen entidades | `D-41` |

## 7. Convenciones de datos y operación

| Elemento | Convención | Decisión |
|---|---|---|
| Esquema | Uno solo, con `company_id` y `project_id` donde aplica | `D-95`, `D-36` |
| Claves | `uniqueidentifier`; enumeraciones como `VARCHAR` con `CHECK` | `3.3.1` |
| Fechas | UTC | `3.3.1` |
| Concurrencia | `rowversion` en `Project`, `WbsChapter`, `WbsItem`, `Apu` y `Resource`; la API responde `409` | `D-50`, `D-97` |
| Borrado | Baja lógica en `Project`, `Apu` y `Resource`; `RESTRICT` por defecto | `D-49` |
| Historial | Solo las excepciones `D-92` (estado y visibilidad del proyecto) y `D-111` (seguridad) | `D-08`, `D-92`, `D-111` |
| Migraciones | Manuales, con runbook y formato `YYYYMMDDHHMM_<scope>_<change>`; prefijos `PK_`/`FK_`/`IX_`/`UQ_`/`CK_` | `D-40`, `D-107` |
| Primer Administrador | Semilla en local y bootstrap idempotente y auditado en demo, mutuamente excluyentes | `D-110` |

## 8. Índice de capacidades

Una capacidad por módulo funcional (`PLAN §6.2.3`). El índice fija el nombre y la ruta para que las
etapas siguientes **añadan** capacidades sin reestructurar el árbol.

| Capacidad | Ruta de la especificación | Funcionalidades | Módulo | Etapa | Estado |
|---|---|---|---|---|---|
| Identidad | `openspec/specs/identidad/spec.md` | RF-01, RF-02, RF-18, RF-23 | M-01 | E-1b | En este cambio (`reconciliacion-e1b`) |
| Proyectos | `openspec/specs/proyectos/spec.md` | RF-03 y la parte de RF-01 ya implementada | M-02 | E-1b | En este cambio (`reconciliacion-e1b`) |
| EDT | `openspec/specs/edt/spec.md` | RF-04 | M-03 | E-2 | Prevista |
| Insumos | `openspec/specs/insumos/spec.md` | RF-05, RF-06, RF-16, RF-20 | M-04 | E-3a | Prevista |
| APUs | `openspec/specs/apus/spec.md` | RF-07, RF-08, RF-09, RF-22 | M-05 | E-3b | Prevista |
| Cálculo | `openspec/specs/calculo/spec.md` | RF-10 | M-06 | E-4a | Prevista |
| Aprobaciones | `openspec/specs/aprobaciones/spec.md` | RF-11, RF-12, RF-13, RF-14, RF-21 | M-07 | E-4b | Prevista |
| Catálogo | `openspec/specs/catalogo/spec.md` | RF-15 | M-08 | E-5 | Prevista |
| Alertas | `openspec/specs/alertas/spec.md` | RF-17 | M-09 | E-4a | Prevista |

**Regla de crecimiento:** una capacidad nueva se crea con su propio `spec.md` y su fila en esta tabla;
no se renombran ni se mueven las capacidades existentes. `E-2` añade `edt` y completa `proyectos`, sin
reorganizar el índice.

## 9. Alcance vigente

- **Dentro del MVP**: identidad y tenancy, proyectos, EDT, insumos, APUs y matrices, cálculo,
  aprobaciones y cierre, catálogo abierto, alertas de tope y los dos correos transaccionales (`1.4.5`).
- **Fuera del MVP**: importación y exportación (`D-18`), historial de valores (`E-05`), conversión de
  divisas (`E-06`), aplicación móvil (`E-07`), integraciones ERP (`E-08`), firma electrónica certificada
  (`A-08`), frontend durante `E-1b` y `E-2` (`D-109`).
- **Despliegue**: solo local o demo controlada; prohibido exponer sin HTTPS (`D-39`, `R-22`).
