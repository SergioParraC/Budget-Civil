# Diseño técnico: reconciliacion-e1b

Estado: borrador para revisión del estudiante. Este documento no reemplaza al `PLAN_INGENIERIA.md`:
registra **cómo** se alinea la implementación con el PLAN y con las capacidades `identidad` y
`proyectos`. El **porqué** del cambio está en `proposal.md`; el **qué** observable, en
`specs/identidad/spec.md` y `specs/proyectos/spec.md`.

## Contexto

- El sistema implementado hoy se describe en `specs/001` (usuarios y compañías) y `specs/002`
  (compañías y proyectos), generadas con Spec Kit y declaradas evidencia histórica **no normativa**
  (`D-108`). No se editan.
- `openspec/specs/` estaba vacío: este cambio crea las dos primeras capacidades y fija su índice en
  `openspec/project.md` (§8) para que `E-2` añada `edt` sin reestructurar.
- Restricciones vigentes que condicionan el diseño: un solo desarrollador (`A-04`), un servidor Linux
  con Docker Compose (`R-12`), integración manual contra SQL Server local (`D-66`), migraciones
  manuales con runbook (`D-40`) y despliegue solo local o demo (`D-39`).
- Las brechas a cerrar están en `GAP-REGISTER.md`; el impacto en el cliente, en
  `IMPACTO_FRONTEND.md`.

**Estado verificado del repositorio al actualizar este diseño (29/10/2026)**

- La base de datos fue vaciada por el autor: **no hay datos que migrar**. Se elimina la estrategia de
  copia de membresías y de movimiento de objetos entre esquemas (DEC-19).
- `backend/tests/` contiene un único proyecto, `BudgetCivil.Backend.BlackBoxTests`: 39 archivos `.cs`,
  85 casos (`[Fact]`/`[Theory]`) y 191 referencias a su arnés, que se apoya en `WebApplicationFactory` y
  `EF Core InMemory`, ambos prohibidos por `D-106`.
- `BudgetCivil.Infrastructure/Persistence/Migrations/` tiene cinco migraciones, una de ellas
  (`202610010900_feature002_schema_baseline.cs`) **sin archivo `.Designer.cs`**, más el snapshot del
  modelo.
- `BudgetCivil.Api/Program.cs` ejecuta `Database.MigrateAsync()` al arrancar, lo que contradice el runbook
  manual de `D-40` (OBS-08).
- `DatabaseSchemas` todavía declara `param`, `iam`, `dbo`, `audit` y `log` (OBS-09), y el modelo conserva
  `UserAccount`/`UserProfile`, `UserCompanyMembership` y `ProjectUserMembership`.
- El autorregistro vigente expone `POST /api/v1/auth/register` con diez campos y **no existe formulario ni
  llamada de registro en `Frontend/`**: `AuthService` solo consume `/auth/login`. La referencia para
  ampliar los datos es, por tanto, la del endpoint vigente más los campos de identidad del perfil
  (DEC-18, INT-06).

## Objetivos y no objetivos

**Objetivos**

1. Una sola línea base de comportamiento para `identidad` y `proyectos`, con requisitos verificables y
   trazables a `RF`, `R` y `D`.
2. Contrato v1 congelado en una única ventana incompatible (`D-99`, `D-100`, `D-101`, `D-109`).
3. Modelo de identidad, autorización y tenencia coherente con `D-78`, `D-82`, `D-87` y `D-98`.
4. `RF-18` operativo y autorregistro con identidad completa, conforme a `D-73` y `D-80` (DEC-18).
5. Pruebas conformes con `D-105` y `D-106`: la suite prohibida se elimina y se reconstruye (DEC-20).

**No objetivos**

1. Especificar o implementar EDT, tope, alertas, insumos, APUs, cálculo, aprobaciones por APU,
   catálogo o frontend (`§4.8.3`, `D-109`).
2. Cambiar el alcance del MVP ni añadir dependencias (`D-104`).
3. Reescribir `specs/001` y `specs/002` (`D-108`).

## Decisiones de diseño

Cada decisión indica la alternativa descartada y por qué. Ninguna reabre una decisión de `§0.5`: la
implementan.

### DEC-01 Congelar el contrato v1 sin compatibilidad paralela

Se publica un único contrato `/api/v1` con `camelCase`, `ProblemDetails` y paginación
`page`/`pageSize`, y se retiran los formatos anteriores en la misma ventana (`D-99`, `D-100`, `D-101`,
`D-109`).

- **Alternativa descartada:** mantener el formato anterior en un prefijo `/api/v0` durante una etapa.
  Se descarta porque duplica DTOs, dobla la superficie de autorización y contradice `D-109` ("no se
  publican dos contratos en paralelo").
- **Consecuencia:** el cliente Angular `v0.0.1` deja de funcionar hasta regenerarse (`A-10`); queda
  registrado en `IMPACTO_FRONTEND.md` con una sola ventana.

### DEC-02 Autorización por permiso con semilla versionada

La decisión de autorización se resuelve por `permission_key` activo del rol vigente; la matriz `1.3.2`
es referencia funcional, no control (`D-82`). La semilla de `Permission` y `RolePermission` es
idempotente, versionada y se aplica en cada arranque; no se crean permisos ni asignaciones en tiempo de
ejecución.

- **Alternativa descartada:** políticas por rol hardcodeadas por endpoint. Se descarta porque exige
  recompilar para cambiar un permiso y porque el inventario de permisos quedaría invisible en la base,
  imposibilitando la auditoría de la denegación.
- **Consecuencia:** el contenido inicial de la matriz de permisos es un dato operativo versionado, no
  código. Los cuatro roles base son inmutables y se siembran con identificadores fijos.

### DEC-03 Un único `User` que absorbe el perfil

`UserAccount` y `UserProfile` se fusionan en una sola entidad con los datos de perfil (`D-78`), y la
unicidad se apoya solo en `email` y `username` (`D-73`, `D-77`).

- **Alternativa descartada:** conservar dos tablas y unirlas por proyección. Se descarta porque obliga a
  dos escrituras por alta, impide una restricción única simple sobre `username` y mantiene la
  ambigüedad que `D-78` cierra.
- **Consecuencia:** el alta administrativa y el autorregistro escriben la misma entidad; la diferencia
  entre ambos caminos queda en el rol y la compañía, no en el modelo.

### DEC-04 Migración de hash por rehash perezoso

El hash lo produce ASP.NET Core Identity (`D-81`). Un usuario con hash heredado se autentica una vez
contra el verificador antiguo y, en ese mismo inicio de sesión correcto, su hash se reemplaza por el
vigente.

- **Alternativa descartada:** migración masiva con contraseñas temporales o reseteo obligatorio. Se
  descarta porque rompe a todos los usuarios a la vez y exige un canal de comunicación que el sistema no
  tiene (`D-17` limita el correo a dos casos).
- **Alternativa descartada:** conservar Argon2id. Se descarta porque el PLAN fija Identity (`D-81`) y
  mantener dos algoritmos duplica la superficie de verificación.

### DEC-05 Pertenencia con `User.company_id` y `ProjectMember`

La pertenencia a compañía vive en el usuario y la pertenencia a proyecto en una tabla única por
(`project_id`, `user_id`), sin historial (`D-98`). Las tablas de membresía con historial de
`specs/001`/`specs/002` quedan obsoletas y se migran en este cambio.

- **Alternativa descartada:** conservar `CompanyUserMembership` y `ProjectUserMembership` con fechas de
  alta y baja. Se descarta porque `D-08` prohíbe el historial de asignaciones y porque la regla "una
  sola compañía activa" (`R-10`) se garantiza mejor con una columna única que con una tabla de
  vigencias.
- **Consecuencia:** la consulta de aislamiento se resuelve con `company_id` en el usuario y con
  `ProjectMember` para el alcance por proyecto.

### DEC-06 Configuración del proyecto en entidad separada

`ProjectConfiguration` es 1:1 con el proyecto y concentra divisa base, componentes del AIU, tope
presupuestal, modo de firma, firmante y requisito de auditor (`D-93`). La especificación fija la
existencia y unicidad de la configuración; el comportamiento de cada parámetro se especifica en su
etapa (`D-94`).

- **Alternativa descartada:** embeber los parámetros en `Project`. Se descarta porque mezcla identidad
  del proyecto con parámetros que cambian con otra frecuencia y porque `rowversion` del proyecto
  obligaría a bloquear la configuración completa por cualquier cambio de AIU.

### DEC-07 Firma persistida y no expuesta

La imagen de firma se guarda una vez por usuario en `UserSignature` y la aprobación referencia al
firmante (`D-91`). La imagen no se serializa en DTO públicos ni en el catálogo, ni se escribe en los
registros técnicos (`R-28`).

- **Alternativa descartada:** guardar la imagen junto a cada aprobación. Se descarta por duplicación de
  binarios y porque `D-91` fija una firma activa por usuario.
- **Alternativa descartada:** no persistir la firma (`D-52` anterior). Queda obsoleta por `D-91`.
- **Consecuencia:** el modo de firma y el requisito de auditor existen como parámetros en `E-1b`, pero
  su efecto en la aprobación por APU se especifica en `E-2`/`E-4b`.

### DEC-08 Concurrencia optimista solo en agregados de proyecto

`rowversion` en `Project` desde `E-1b` y en `WbsChapter`, `WbsItem`, `Apu` y `Resource` cuando se creen
(`D-50`, `D-97`). La API responde `409` y el cliente recarga.

- **Alternativa descartada:** control de concurrencia global en todas las tablas. Se descarta porque
  añade ruido a catálogos y semillas, y porque los agregados que el usuario edita en paralelo son los
  del proyecto.

### DEC-09 Estado como conjunto cerrado con reglas en el dominio

El estado se persiste como texto con `CHECK` sobre los cinco valores (`D-88`) y las reglas de transición
se evalúan en el dominio, no como restricción de base (`D-89`).

- **Alternativa descartada:** enteros con tabla de estados. Se descarta porque `3.3.1` fija `VARCHAR`
  con `CHECK` y porque una tabla de estados invita a editarla en tiempo de ejecución, algo que `D-88` no
  permite.
- **Consecuencia:** la precondición del cien por ciento de insumos cotizados es una regla de dominio
  verificable con pruebas unitarias (`T-U-12`), no un `CHECK`.

### DEC-10 Historial de estado y visibilidad como tablas de solo inserción

Cada transición de estado y cada cambio de visibilidad insertan un registro propio (`D-92`), sin
actualización ni borrado. Es la excepción acotada a `D-08`.

- **Alternativa descartada:** un registro único con ambos tipos de evento. Se descarta porque `D-92`
  nombra dos historiales y porque la consulta de visibilidad no necesita el estado anterior del ciclo de
  vida.
- **Consecuencia:** el historial no sustituye al "último modificador" (`D-08`); ambos coexisten.

### DEC-11 Dueño único de rutas

`M-01` es dueño de `/auth`, `/profile`, `/users`, `/companies` y `/catalogs`; `M-02` solo expone
`/projects`, `/projects/{id}/configuration` y `/projects/{id}/memberships` (`D-102`, `D-103`).

- **Alternativa descartada:** mantener las dos copias y deprecar una. Se descarta porque obliga a
  autorizar dos veces el mismo recurso con permisos distintos, que es exactamente el defecto `X-41`.
- **Consecuencia:** los endpoints duplicados de `M-02` se retiran en la misma ventana del contrato
  (`GAP-09`, `IF-04`).

### DEC-12 Un solo esquema de base de datos

Se mantiene un esquema con `company_id` y `project_id` en las entidades que los requieren (`D-95`). La
segregación `param`/`iam`/`dbo`/`audit`/`log` se retira.

- **Alternativa descartada:** conservar la segregación y documentarla. Se descarta porque obliga a
  calificar cada consulta y porque la gobernanza ya se cubre con el runbook y la convención de nombres
  (`D-107`).

### DEC-13 Semilla y bootstrap del primer Administrador

En local el Administrador inicial se crea por semilla; en demo, por un bootstrap idempotente y auditado
habilitado por bandera de entorno; ambos caminos se excluyen y el bootstrap se cierra al existir un
Administrador (`D-110`).

- **Alternativa descartada:** bootstrap disponible siempre. Se descarta porque convierte un endpoint
  público en una puerta de creación de administradores.
- **Consecuencia:** el arranque falla de forma explícita si ambas banderas están activas
  (`SC-IDENTIDAD-70`).

### DEC-14 Distribución de pruebas

`Tests.Unit` (dominio y aplicación), `Tests.Integration` (API y persistencia) y `Tests.Architecture`
(`NetArchTest`); sin `InMemory` ni `WebApplicationFactory` (`D-105`, `D-106`).

- **Alternativa descartada:** mantener un único proyecto de caja negra. Se descarta porque impide
  verificar reglas de dominio sin base y porque `D-61` exige cobertura medida sobre dominio.
- **Consecuencia:** las pruebas de integración son manuales con checklist y evidencia (`D-66`); la
  arquitectura y el contrato sí están automatizados en CI.

### DEC-15 Errores y validación uniformes

Toda validación de aplicación usa `FluentValidation` y su resultado se proyecta a `ProblemDetails` con
el código de negocio en `extensions.code` (`D-100`, `D-104`). Un rechazo de regla de estado o de
precondición usa `409`; un fallo de autorización usa `403`; un recurso fuera del alcance del actor no
revela su existencia.

- **Alternativa descartada:** un sobre propio `{error, meta}`. Queda obsoleto por `D-100`.
- **Consecuencia:** los códigos de negocio son parte del contrato y se documentan en Swagger.

### DEC-16 Migraciones con convención y reversión conocida

Cada migración sigue `YYYYMMDDHHMM_<scope>_<change>` y las restricciones usan `PK_`/`FK_`/`IX_`/`UQ_`/
`CK_` (`D-107`), sobre el runbook manual de `D-40`. Antes de aplicar se respalda la base y se revisa el
script generado.

- **Alternativa descartada:** migraciones automáticas al arrancar. Queda obsoleta por `D-40`.

### DEC-17 Verificación de correo antes de habilitar

La cuenta autorregistrada no queda habilitada hasta confirmar el correo (`D-80`); el perfil completo se
exige antes de operar dentro de una compañía (`D-76`, `REQ-IDENTIDAD-08`).

- **Alternativa descartada:** habilitar la cuenta sin verificar y bloquear solo las operaciones de
  compañía. Se descarta porque `D-80` condiciona la habilitación de la cuenta a la verificación y deja
  la alternativa solo como mitigación temporal.
- **Consecuencia:** mientras el envío de correo no esté operativo, el despliegue queda limitado a local
  o demo controlada (`D-39`).

### DEC-18 Autorregistro con identidad completa (modifica `D-76`)

El autorregistro captura tipo y número de documento, primer y segundo nombre, primer y segundo apellido,
fecha de nacimiento, correo, nombre de usuario y contraseña. El rol y la pertenencia siguen fijándose
por el sistema (`D-72`, `D-75`). La residencia y la tarjeta profesional **no** se piden en el registro:
se completan en el perfil antes de operar dentro de una compañía (`REQ-IDENTIDAD-08`).

- **Alternativa descartada:** mantener los tres datos de `D-76`. Se descarta por decisión del autor: el
  alta debe producir una identidad verificable y no solo una credencial.
- **Alternativa descartada:** pedir también residencia y tarjeta profesional. Se descarta porque alarga
  el alta anónima y porque esos datos solo son exigibles al operar dentro de una compañía.
- **Consecuencia:** el autorregistro y el alta administrativa comparten los campos de identidad; la
  diferencia queda únicamente en el rol y la compañía, que solo fija el Administrador.

### DEC-19 Baseline única sin migración de datos

Al estar la base vacía, se eliminan las cinco migraciones y el snapshot del modelo, y se genera **una
sola migración baseline** con el modelo ya reconciliado, nombrada según `D-107`. El arranque deja de
ejecutar migraciones: el esquema se aplica con el script del runbook (`D-40`).

- **Alternativa descartada:** conservar la cadena y añadir migraciones correctivas. Se descarta porque
  arrastra cinco migraciones con nombres y esquemas obsoletos y una sin archivo `.Designer.cs`, lo que
  rompe la reproducibilidad de una base limpia.
- **Alternativa descartada:** migrar datos desde las tablas de membresía. Es imposible: las tablas no
  existen y el autor confirmó que no hay datos.
- **Consecuencia:** la baseline se genera **al final** de la fase de persistencia, cuando el modelo ya
  incorpora `User` unificado, esquema único, `ProjectMember`, `ProjectConfiguration`, historiales y
  `rowversion`. Si se genera antes, se descarta y se vuelve a generar.

### DEC-20 Purga y reconstrucción de la suite de pruebas

Se elimina `BudgetCivil.Backend.BlackBoxTests` completo —proyecto, arnés, 39 archivos y 85 casos— y su
referencia en la solución. La cobertura se reconstruye en `Tests.Unit` (dominio y aplicación),
`Tests.Integration` (persistencia y migraciones contra SQL Server local, ejecución manual) y
`Tests.Architecture` (`NetArchTest`) (`D-105`, `D-106`).

- **Alternativa descartada:** redistribuir los 85 casos existentes. Se descarta porque están escritos
  contra el arnés prohibido y contra comportamiento obsoleto (esquemas segregados, cuenta y perfil
  separados, documento único); portarlos conserva la dependencia que se quiere eliminar.
- **Alternativa descartada:** sacarlos del build y conservarlos como referencia. El autor pidió
  eliminarlos.
- **Consecuencia:** entre la purga y la reconstrucción la cobertura es cero; la reconstrucción es la
  primera tarea de la fase de pruebas y bloquea el cierre de la etapa.

### DEC-21 Administración de usuarios exclusiva del Administrador

La edición de los datos de un usuario y la desactivación de su cuenta quedan **solo en el Administrador**,
de forma global; el Director conserva alta, búsqueda y cambio de rol, y todos los roles editan únicamente
su propio perfil (`REQ-IDENTIDAD-29`, `REQ-IDENTIDAD-28`).

- **Alternativa descartada:** la del as-built, que otorgaba al Director la edición y desactivación dentro
  de su compañía. Se descarta por decisión del autor y porque concentra la administración de identidad en
  el rol que ya es dueño de usuarios y roles (`D-34`).
- **Consecuencia:** el permiso de edición de usuarios no se siembra para el rol Director; la matriz
  `1.3.2`, que no tenía fila para esta capacidad, se completa con esta decisión.

### DEC-22 Transiciones de estado y actor por transición

La inhabilitación y la reactivación del proyecto las ejecuta **solo el Director**; el paso entre
`FALTA_INFORMACION` y `COTIZADO`, en ambos sentidos, lo ejecutan **Director o Presupuestador**; el cierre
a `FINALIZADO` sigue siendo solo del Director (`REQ-PROYECTOS-05`, `16`, `17`, `06`).

- **Alternativa descartada:** dejar la matriz de actores en la semilla de permisos sin fijarla en la
  especificación. Se descarta porque la respuesta del autor define comportamiento observable y debe ser
  verificable, no solo configurable.
- **Consecuencia:** el `seed` siembra el permiso de cambio de estado para Director y Presupuestador, y la
  regla por transición se evalúa en el dominio; la reactivación de `INHABILITADO` vuelve a
  `FALTA_INFORMACION`, comportamiento adoptado del dominio actual.

### DEC-23 Umbral de bloqueo en cinco intentos

La cuenta se bloquea a los **cinco intentos fallidos consecutivos** (`REQ-IDENTIDAD-22`), valor que el
autor confirma desde `specs/001`.

- **Alternativa descartada:** umbral configurable sin valor por defecto. Se descarta porque impide
  escribir un escenario verificable (`SC-IDENTIDAD-57`).
- **Consecuencia:** el valor queda como constante de configuración con `5` por defecto; cambiarlo exige
  una decisión nueva, no una edición silenciosa.

### DEC-24 Alta de proyecto sin Director activo

La creación de un proyecto se **rechaza** cuando la compañía no tiene ningún Director activo
(`REQ-PROYECTOS-18`), en lugar de crear un proyecto sin responsable.

- **Alternativa descartada:** permitir el alta y completar el responsable después. Se descarta porque deja
  proyectos sin Director, que es la precondición de `D-85` y del cierre por Director (`D-14`).
- **Consecuencia:** el rechazo es un error funcional explícito, no un `403`, porque no depende del actor
  sino del estado de la compañía.

### DEC-25 Reapertura de `FINALIZADO` conservada

Se mantiene el comportamiento vigente: el **Director** puede reabrir un proyecto `FINALIZADO` hacia
`FALTA_INFORMACION` (`REQ-PROYECTOS-19`, `SC-PROYECTOS-44`, `SC-PROYECTOS-45`).

- **Alternativa descartada:** eliminar la reapertura del dominio. Se descarta por decisión del autor: el
  cierre puede necesitar corrección y el comportamiento ya existe.
- **Alternativa descartada:** aplazarla a `E-2`. Se descarta porque `E-1b` existe para que la
  especificación diga lo mismo que el código ya construido (`§4.8.1`).
- **Consecuencia:** `FINALIZADO` no es un estado terminal y la reapertura se registra en el historial de
  estado como una transición más (`REQ-PROYECTOS-08`).

## Riesgos y trade-offs

| Riesgo | Mitigación |
|---|---|
| Una sola ventana incompatible rompe el cliente Angular y deja la aplicación sin interfaz utilizable durante `E-1b` y `E-2`. | Es deliberado (`D-109`, `A-10`): se acepta el corte y se regenera el cliente al abrir la etapa de interfaz; `IMPACTO_FRONTEND.md` fija el alcance de la ventana. |
| La baseline única se genera antes de terminar las reconciliaciones de modelo y hay que regenerarla. | Se genera como **último** paso de la fase de persistencia (T021); mientras tanto la base se recrea desde el modelo con el script del runbook y no se versiona ninguna migración intermedia. |
| La purga deja el sistema sin pruebas hasta que la suite se reconstruya. | La reconstrucción (T022–T025) es lo primero de la fase de pruebas y bloquea el cierre de la etapa; los escenarios de las dos capacidades son la fuente de los casos nuevos. |
| El `seed` de permisos puede dejar roles sin permiso y, con política fail-closed, bloquear operaciones legítimas. | `seed` idempotente y versionado, prueba `T-I-23` y verificación de que cada rol base tiene al menos los permisos de su fila en la matriz `1.3.2`. |
| El rehash perezoso deja cuentas con el algoritmo anterior indefinidamente si no vuelven a iniciar sesión. | Se acepta: el hash heredado solo se usa para verificar, nunca para crear; `T-S-11` comprueba la ruta de rehash. |
| Las pruebas de integración son manuales y pueden ejecutarse tarde. | Checklist de la etapa (`§4.8.7`) y evidencia registrada en `tasks.md`; CI cubre unitarias, arquitectura y contrato. |
| Los requisitos de estado dependen de insumos que aún no existen (`E-3a`). | `REQ-PROYECTOS-05` y `REQ-PROYECTOS-06` se especifican ahora y se verifican con `T-U-10` y `T-I-19` en `E-4b`; hasta entonces la precondición se prueba con datos de prueba del dominio, no contra insumos reales. |
| El registro de trazabilidad obligatorio al cierre de cada delta produce dos avisos de `openspec validate` (secciones que no se archivan). | Se acepta: `PLAN §9` exige la tabla en el delta. `openspec validate` sin `--strict` no reporta errores; con `--strict` los avisos se convierten en fallo y se documentan aquí como esperados. |

## Plan de migración

Orden de trabajo (`§4.8.5`), con la verificación de cada paso:

1. **Contrato v1 congelado.** Publicar el documento OpenAPI con `camelCase`, `ProblemDetails` y
   `page`/`pageSize`; retirar el sobre propio y los nombres anteriores. Verificación: `T-C-01`.
2. **Línea base OpenSpec.** `openspec/project.md` y las capacidades `identidad` y `proyectos`.
   Verificación: `openspec validate` sin errores.
3. **Renombrado del rol.** Migrar el dato de rol a `DIRECTOR`, ajustar semillas, claims y constantes del
   contrato. Verificación: `T-I-23` y `SC-IDENTIDAD-50`.
4. **Modelo de identidad.** Fusionar cuenta y perfil en `User`, retirar la unicidad del documento,
   añadir `username` único. Verificación: `T-I-28`.
5. **Seguridad de credenciales.** Sustituir el verificador por Identity, migrar el hash por rehash,
   añadir bloqueo por intentos y eventos de seguridad. Verificación: `T-S-11`.
6. **Autorización.** Crear `Permission` y `RolePermission`, cargar el `seed` versionado, aplicar
   fail-closed en todos los endpoints privados. Verificación: `T-I-23`, `T-S-10`.
7. **Compañía.** Ampliar `Company`, añadir el ciclo de vida y las coberturas de representante legal.
   Verificación: `T-I-26`.
8. **Pertenencia.** Incorporar `User.company_id` y `ProjectMember` al modelo y retirar
   `UserCompanyMembership` y `ProjectUserMembership`. **No hay copia de datos**: la base está vacía.
   Verificación: `T-I-29` sobre la base recreada.
9. **Esquema único.** Retirar `param`/`iam`/`dbo`/`audit`/`log` de `DatabaseSchemas`, de las
   configuraciones y de las migraciones. **No hay objetos que mover**. Verificación: `T-I-24` y
   `T-A-02` sobre la base recreada.
10. **Proyecto.** Añadir `ProjectConfiguration`, `rowversion` en `Project`, los cinco estados con
    `CHECK` y los dos historiales. Verificación: `T-I-24`, `T-I-25`, `T-I-30`.
11. **Firma.** Crear `UserSignature`, ajustar la aprobación para referenciarla y bloquear su exposición.
    Verificación: `T-S-08`.
12. **RFC-18.** Implementar los dos correos con proveedor simulado y el respaldo asistido.
    Verificación: `T-I-22`.
13. **Autorregistro.** Ampliar a identidad completa (DEC-18), añadir límite por IP y verificación de
    correo. Verificación: `T-S-07`, `SC-IDENTIDAD-03`…`SC-IDENTIDAD-05` y `SC-IDENTIDAD-13`…`SC-IDENTIDAD-19`.
14. **Baseline.** Eliminar las cinco migraciones y el snapshot; generar la baseline única del modelo
    reconciliado con la convención `D-107`; quitar `MigrateAsync()` del arranque. Verificación: aplicar
    el script sobre una base vacía y comprobar que el esquema coincide con el snapshot; `T-I-24`.
15. **Pruebas y documentos.** Eliminar `BlackBoxTests` y crear `Tests.Unit`, `Tests.Integration` y
    `Tests.Architecture` (DEC-20); actualizar `.github/docs/00`, `02`, `03`, `04`, `05` y `06`.
    Verificación: `T-A-02`, CI en verde y checklist de `§0.7`.

**Reversión.** Al no haber datos, la reversión de cualquier paso es recrear la base vacía y aplicar la
baseline vigente; no se conservan tablas antiguas ni se necesitan respaldos de datos. La reversión de
código es la del cambio en la rama. Una vez aplicada la baseline en local o demo, volver a una migración
anterior exige restaurar la base desde el respaldo del runbook (`D-40`).

## Interpretaciones adoptadas

Se registran para que la revisión pueda refutarlas; todas se apoyan en una decisión existente.

| ID | Interpretación | Fuente |
|---|---|---|
| INT-01 | "Operar dentro de una compañía" significa ejecutar operaciones sobre datos de la compañía; el perfil incompleto no impide consultar el perfil propio ni cambiar la contraseña. | `D-76`, `D-34` |
| INT-02 | El umbral de verificación del cierre se mide sobre el conjunto total de insumos del proyecto, no por tipo de insumo. | `D-89`, as-built `specs/002` |
| INT-03 | El rol con `role_code = DIRECTOR` es el único código de rol que el PLAN fija; los demás códigos se fijan en la semilla y no son parte del contrato funcional. | `D-74` |
| INT-04 | La matriz `1.3.2` se usa como referencia de comportamiento para los escenarios de permisos; el control efectivo es el permiso (`D-82`). | `D-82`, `1.3.2` |
| INT-05 | El historial de estado y de visibilidad no se expone en el catálogo público ni en DTO de otros tenants. | `D-92`, `D-13` |
| INT-06 | El segundo nombre y el segundo apellido se solicitan pero son **opcionales**; el autor lo confirmó el 29/10/2026. Exigirlos excluiría a personas que no los tienen, y el alta administrativa y el perfil ya los modelan como opcionales. | `D-76` (modificada), as-built `specs/001` |
| INT-07 | La residencia no se solicita en el autorregistro porque solo es exigible al operar dentro de una compañía; se completa en el perfil. | `D-76` (modificada), `REQ-IDENTIDAD-08` |

## Decisiones detenidas: ninguna

Las cuatro preguntas de la revisión anterior quedaron **resueltas por el autor el 29/10/2026** y se
incorporaron a la especificación. No queda ninguna duda que impida aplicar el cambio.

| ID | Respuesta del autor | Se convierte en | Tarea |
|---|---|---|---|
| Q-E1B-01 | El umbral de bloqueo es **5 intentos fallidos consecutivos**, como en `specs/001` | `REQ-IDENTIDAD-22` (DEC-23) | T007 |
| Q-E1B-02 | **Solo el Director** inhabilita y reactiva el proyecto; **Director y Presupuestador** cambian entre `FALTA_INFORMACION` y `COTIZADO`, en ambos sentidos | `REQ-PROYECTOS-05`, `REQ-PROYECTOS-16`, `REQ-PROYECTOS-17` (DEC-22) | T011, T017, T018 |
| Q-E1B-03 | **Solo el Administrador** edita y desactiva usuarios, de forma global; todos los demás solo modifican su propio perfil | `REQ-IDENTIDAD-29` (DEC-21) | T008, T009 |
| Q-E1B-04 | **No se puede crear** un proyecto cuando la compañía no tiene ningún Director activo | `REQ-PROYECTOS-18` (DEC-24) | T013 |
| Q-E1B-05 | **Se conserva** el comportamiento vigente: el Director reabre `FINALIZADO` hacia `FALTA_INFORMACION` | `REQ-PROYECTOS-19` (DEC-25) | T017 |

También confirmó el autor que el **segundo nombre y el segundo apellido no son obligatorios** (INT-06),
y que el registro de las modificaciones en `PLAN_INGENIERIA.md` se hace **al finalizar la
implementación** (tarea T032).

La quinta pregunta también quedó resuelta: la reapertura de `FINALIZADO` que ya existe en el dominio
(`ProjectStatusTransitionPolicy.cs`, líneas 54-57 y 78-81) **se conserva** y queda especificada como
`REQ-PROYECTOS-19`. No queda ninguna pregunta abierta.

## Observaciones sobre decisiones cerradas

Se registran sin reabrirlas (`§6.4`, prohibición 4).

| ID | Observación |
|---|---|
| OBS-01 | `D-94` sitúa `requires_auditor`, `signature_mode` y el tope en `E-2`/`E-4a`, mientras que el encargo de `E-1b` pide incluirlos en la configuración. Se resolvió especificando **la existencia y unicidad** de la configuración (`REQ-PROYECTOS-10`) y dejando su comportamiento a su etapa. Si se quería el comportamiento en `E-1b`, `D-94` debería revisarse. |
| OBS-02 | `D-89` reformula `D-04` y `R-06`; conviene que la próxima revisión del PLAN marque `D-04` como reformulada también en su fila de `§0.5`, no solo en la de `X-03`. |
| OBS-03 | `D-55` y `D-99` dicen lo mismo sobre `camelCase`; se mantienen ambas porque la segunda añade el alcance "todos los endpoints" y el estado `OBSOLETO` de los ejemplos de `specs/001`. |
| OBS-04 | `§4.8.7` verifica `RF-18` con `T-I-17` y `§5.6.5` con `T-I-22`. Ambos describen la misma verificación; este cambio usa `T-I-22` como identificador principal de la etapa y cita `T-I-17` como referencia de la matriz `§5.6.1`. |
| OBS-05 | `openspec/project.md` es un artefacto heredado para la CLI 1.14.3, que mantiene su contexto en `openspec/config.yaml`. El PLAN lo exige (`§6.1.2`, `§4.8.2`), así que se conserva como documento operativo; la CLI solo emite un aviso de migración y no lo elimina. |
| OBS-06 | Existía un cambio vacío `openspec/changes/e-1b-reconciliacion-linea-base` (solo `.openspec.yaml`, sin artefactos), creado antes de este cambio y sin contenido. Se **eliminó el 29/10/2026 con autorización del estudiante** para dejar un solo cambio activo y permitir que `openspec validate --all` pase sin errores. |
| OBS-07 | La tabla de trazabilidad que `PLAN §9` exige al cierre de cada delta genera dos avisos de `openspec validate` por archivo ("no se archiva"). Se mantiene por exigencia del PLAN; la validación sin `--strict` no reporta errores. |
| OBS-08 | `Program.cs` aplica `Database.MigrateAsync()` en el arranque; `D-40` exige migraciones manuales con runbook. Se retira del arranque en la tarea T020 y queda solo el script del runbook. |
| OBS-09 | La migración `202610010900_feature002_schema_baseline.cs` no tiene archivo `.Designer.cs`, de modo que la cadena actual no es reproducible desde cero. Es una razón adicional para la baseline única (DEC-19). |
| OBS-10 | `D-76` quedó **modificada** por decisión del autor y el cierre de `D-98`, `D-95` y `D-105` se ajustó a una base vacía y a una suite eliminada. **Registrado en el PLAN v0.30 (T032, 30/10/2026):** `§0.5` marca `D-76` como modificada y el cierre de `D-95`/`D-98`/`D-105` sin migración de datos, con purga y reconstrucción de la suite; `§4.8.4` y `§4.8.5` ya no hablan de migrar datos y `§5.6.1`/`§5.6.5` incorporan los identificadores nuevos. |

## Preguntas abiertas diferibles

Ninguna. Todo lo que podía cambiar los requisitos, el enfoque o las tareas está en "Decisiones
detenidas".

## Estado de cumplimiento de `openspec validate`

- `openspec validate reconciliacion-e1b --type change`: válido, sin errores; cuatro avisos por las dos
  secciones de cierre de cada delta (`## Trazabilidad` y `## Fuera de alcance de esta capacidad`),
  explicados en `OBS-07`.
- `openspec validate --strict`: falla solo por esos avisos.
