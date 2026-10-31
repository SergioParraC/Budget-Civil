# Cambio: reconciliacion-e1b

## Why

El código implementado (documentado en `specs/001` y `specs/002`, generadas con Spec Kit) y el PLAN
v0.29 difieren en el nombre y el catálogo de roles, el mecanismo de autorización, el modelo de usuario
y compañía, la pertenencia a compañías y proyectos, los contratos de API, la firma y la organización de
las pruebas. Sin alinearlos, `E-2` construye sobre una línea base ambigua y propaga la divergencia a
todos los módulos posteriores (`PLAN §2.4.3`, `§4.8.1`).

La etapa `E-1b` es obligatoria y bloquea `E-2` (`PLAN §4.8`): no añade funcionalidad nueva salvo el
cierre de `RF-18`, y su resultado es que el comportamiento ya construido y la especificación digan lo
mismo (`§4.8.1`).

## What Changes

- **BREAKING** El rol `Gerente de proyectos` pasa a llamarse **Director de proyectos** con
  `role_code = DIRECTOR`; se ajustan semillas, claims y contratos (`D-74`, `GAP-01`).
- **BREAKING** El hash de contraseña pasa de Argon2id a **ASP.NET Core Identity (PBKDF2)**, con rehash
  en el siguiente inicio de sesión si existiera un hash previo (`D-81`, `GAP-02`).
- **BREAKING** `UserAccount` y `UserProfile` se unifican en un único `User` que absorbe el perfil; el
  número de documento deja de ser único (`D-78`, `D-77`, `GAP-03`, `GAP-04`).
- El autorregistro pide **identidad completa**: tipo y número de documento, primer y segundo nombre, primer
  y segundo apellido, fecha de nacimiento, correo, nombre de usuario y contraseña; asigna el rol
  **Presupuestador** y deja la residencia para el perfil, que se completa antes de operar dentro de una
  compañía. La ampliación **modifica `D-76`** (`D-78`, `GAP-05`).
- El autorregistro incorpora **límite de intentos por IP** y **verificación de correo** (`D-80`,
  `GAP-07`).
- `RF-18` queda operativo: correo de creación de cuenta y **recuperación por correo con respaldo
  asistido del Administrador** (`D-79`, `GAP-06`).
- **BREAKING** La autorización se decide por `permission_key` activo con `seed` versionado e
  idempotente y política **fail-closed**; los cuatro roles base son inmutables (`D-82`, `GAP-08`).
- **BREAKING** `M-01` es dueño único de `/auth`, `/profile`, `/users`, `/companies` y `/catalogs`; se
  retira el duplicado de `M-02`, que solo expone `/projects` (`D-102`, `GAP-09`).
- **BREAKING** Contrato v1 congelado: `camelCase`, `ProblemDetails` con `extensions.code` y paginación
  `page`/`pageSize` (`D-99`, `D-100`, `D-101`, `GAP-10`).
- **BREAKING** La pertenencia se modela con `User.company_id` y `ProjectMember`; se retiran las tablas de
  membresía con historial (`D-98`, `GAP-11`). Al estar la base **vacía**, el retiro no migra datos: se
  elimina la cadena de migraciones y se genera **una baseline única** sobre el modelo reconciliado
  (`D-40`, `D-107`).
- **BREAKING** Un solo esquema; se retira la segregación `param`/`iam`/`dbo`/`audit`/`log` (`D-95`,
  `GAP-12`).
- La evidencia de firma se persiste en `UserSignature` y **nunca** se expone en DTO públicos ni en el
  catálogo (`D-91`, `GAP-13`).
- **BREAKING** `Project` incorpora `rowversion` y el estado queda como conjunto cerrado de cinco
  valores (`D-97`, `D-88`, `GAP-14`).
- Se mantienen los **tres modos de firma** y el Director ejecuta el cierre **sin firmar APUs** (`D-90`,
  `GAP-15`).
- El Administrador queda en **solo lectura** sobre proyectos: no crea ni edita (`D-86`, `GAP-16`).
- **BREAKING** Se elimina el proyecto `BudgetCivil.Backend.BlackBoxTests` completo —39 archivos, 85 casos
  y un arnés con `WebApplicationFactory` y `EF Core InMemory` que `D-106` prohíbe— y las pruebas se
  reconstruyen en `Tests.Unit`, `Tests.Integration` y `Tests.Architecture` con `NetArchTest`
  (`D-105`, `D-106`, `GAP-17`).
- Se crea la **línea base de OpenSpec**: `openspec/project.md`, las capacidades `identidad` y
  `proyectos`, el registro `GAP-01`…`GAP-17` y el registro de impacto en el frontend.

## Capabilities

### New Capabilities

- `identidad`: autorregistro anónimo, cuentas, perfil, roles y permisos, asignación de usuarios a
  compañías, ciclo de vida de la compañía, autorización por `permission_key`, bloqueo por intentos
  fallidos, correos transaccionales y eventos de seguridad.
- `proyectos`: pertenencia de usuarios a proyectos, ciclo de vida del proyecto con cinco estados y
  transiciones explícitas, configuración del proyecto como entidad propia, cierre y registro de
  historial de estado y visibilidad.

### Modified Capabilities

- Ninguna: `openspec/specs/` está vacío. `specs/001` y `specs/002` son evidencia histórica **no
  normativa** y no se editan (`D-108`).

## Fuera de alcance

- `RF-04` (EDT) y todo `M-03`: se especifican en `E-2` (`§4.8.3`).
- `RF-17` (tope y alertas): se implementa en `E-4a` (`D-94`).
- `M-04`…`M-09`: etapas `E-3a` a `E-5`.
- Frontend, componentes, rutas y cliente Angular (`D-109`); el impacto se **documenta** en
  `IMPACTO_FRONTEND.md`, no se especifica.
- Cambios de alcance del MVP y dependencias nuevas (`D-104`).
- Edición de `specs/001` y `specs/002` (`D-108`).

## Decisiones que implementa

`D-74`…`D-111` (`PLAN §0.5`), en cuatro bloques:

| Bloque | Decisiones |
|---|---|
| Identidad y roles | `D-72`, `D-73`, `D-74`, `D-75`, `D-76`, `D-77`, `D-78`, `D-79`, `D-80`, `D-81`, `D-84`, `D-110`, `D-111` |
| Autorización y compañía | `D-21`, `D-34`, `D-82`, `D-83`, `D-87`, `D-96` |
| Contrato y persistencia | `D-93`, `D-95`, `D-97`, `D-98`, `D-99`, `D-100`, `D-101`, `D-102`, `D-103`, `D-107` |
| Proyectos, firma y calidad | `D-85`, `D-86`, `D-88`, `D-89`, `D-90`, `D-91`, `D-92`, `D-94`, `D-104`, `D-105`, `D-106`, `D-108`, `D-109` |

**Modificaciones de alcance aprobadas por el autor el 29/10/2026** (este cambio las aplica; el PLAN
v0.30 debe registrarlas en `§0.5`):

| Decisión | Qué cambia | Motivo |
|---|---|---|
| `D-76` | El autorregistro deja de pedir tres datos y pide la identidad completa; la residencia queda en el perfil | El autor lo decidió con el formulario de referencia del frontend |
| `D-98`, `D-95` | El cierre en `E-1b` no migra datos: baseline única sobre base vacía | El autor eliminó todas las tablas |
| `D-105` | La suite no conforme se **elimina** y se reconstruye; no se redistribuye | El arnés vigente incumple `D-106` |

**Decisiones cerradas por el autor el 29/10/2026** (resuelven las preguntas que el diseño había detenido):

| Tema | Decisión | Requisito |
|---|---|---|
| Bloqueo por intentos | Cinco intentos fallidos consecutivos | `REQ-IDENTIDAD-22` |
| Administración de usuarios | Solo el Administrador edita y desactiva cuentas; los demás solo el perfil propio | `REQ-IDENTIDAD-29` |
| Transiciones de estado | Solo el Director inhabilita y reactiva; Director y Presupuestador alternan `FALTA_INFORMACION` y `COTIZADO` | `REQ-PROYECTOS-05`, `REQ-PROYECTOS-16`, `REQ-PROYECTOS-17` |
| Alta de proyecto | Se rechaza si la compañía no tiene Director activo | `REQ-PROYECTOS-18` |
| Reapertura de `FINALIZADO` | Se conserva el comportamiento vigente: solo el Director reabre hacia `FALTA_INFORMACION` | `REQ-PROYECTOS-19` |

## Criterios de aceptación

Los diez del checklist de cierre de la etapa (`PLAN §4.8.7`):

1. `openspec/specs/identidad` y `openspec/specs/proyectos` existen y validan.
2. `GAP-01`…`GAP-17` están cerradas o aceptadas con etapa asignada.
3. El contrato v1 está documentado, sin `snake_case`, sin sobres propios y con `ProblemDetails`.
4. `RF-18` verificado con proveedor simulado (`T-I-22`, `T-I-17`).
5. El autorregistro cumple `D-73`, `D-80` y la `D-76` modificada: pide la identidad completa y no la residencia (`T-S-07`).
6. Roles y permisos se crean por `seed` idempotente (`T-I-23`).
7. La migración se aplicó con respaldo, convención `D-107` y reversión conocida.
8. `Tests.Unit`, `Tests.Integration` y `Tests.Architecture` en verde; cobertura ≥ 80 % en dominio.
9. `IMPACTO_FRONTEND.md` firmado y `A-10` aceptado.
10. `.github/docs` actualizados y `specs/001`/`specs/002` marcadas como históricas.

Además, este cambio cumple el criterio de especificación lista (`PLAN §6.2.5`): alcance y decisiones
referenciadas, escenarios positivos y negativos, criterios verificables, cobertura de permisos, datos
inválidos, concurrencia y aislamiento, sin `X` abiertos, con trazabilidad a pruebas, sin detalles de
implementación y pendiente de revisión del estudiante.

## Impact

- **Contrato de API**: cambios incompatibles en todos los endpoints de identidad, compañía y proyecto;
  una sola ventana de cambio (`IMPACTO_FRONTEND.md`, `IF-01`…`IF-06`). El cliente Angular `v0.0.1` no se
  mantiene compatible (`A-10`).
- **Base de datos**: se eliminan las cinco migraciones existentes y el snapshot del modelo; se genera
  **una sola baseline** sobre el modelo reconciliado (usuario unificado, `Permission`/`RolePermission`,
  `UserSignature`, `ProjectConfiguration`, `ProjectMember`, historial de estado y visibilidad, esquema
  único, `rowversion` en `Project`) **sin migración de datos**, porque la base está vacía. Runbook manual
  con respaldo y reversión conocida (`D-40`, `D-107`); el arranque deja de aplicar migraciones
  automáticamente.
- **Código**: `M-01` (identidad, compañías, usuarios, catálogos) y `M-02` (proyectos) en las cuatro
  capas; middleware de errores, autenticación y autorización por permiso; semillas de seguridad.
- **Pruebas**: se elimina `BudgetCivil.Backend.BlackBoxTests` (39 archivos, 85 casos, arnés con
  `WebApplicationFactory` + `InMemory`) y se crean `BudgetCivil.Tests.Unit`,
  `BudgetCivil.Tests.Integration` y `BudgetCivil.Tests.Architecture` con `NetArchTest`; la integración se
  ejecuta manualmente contra SQL Server local (`D-105`, `D-106`).
- **Documentación**: `.github/docs/00`, `02`, `03`, `04`, `05` y `06` según `PLAN §0.7`; `specs/001` y
  `specs/002` quedan congeladas como evidencia histórica (`D-108`).
- **Dependencias**: ninguna nueva; se confirman las prohibiciones de `D-104`.
