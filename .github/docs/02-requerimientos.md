# Requerimientos

> Documento actualizado el 30/10/2026 (reconciliación `E-1b`, `D-74`…`D-111`). Usa los identificadores `RF`, `NFR` y `BR` del plan. La verificación detallada está en el capítulo 5 de `PLAN_INGENIERIA.md`.

## 1. Requerimientos funcionales

| ID | Funcionalidad | Prioridad | MVP | Criterio de aceptación |
|---|---|---|---|---|
| RF-01 | Usuarios, roles y asignación a compañías y proyectos | Alta | Sí | La autorización se decide por `permission_key` activo (fail-closed); el Administrador crea y cambia roles globalmente y edita o desactiva cuentas; el Director asigna usuarios de su compañía a proyectos |
| RF-02 | Compañías: creación y datos de registro | Alta | Sí | Se crea la compañía con razón social y NIT únicos y datos de registro, representante legal y coherencia territorial; el Administrador inhabilita o reactiva cualquiera y el Director la propia |
| RF-03 | Configuración del proyecto | Alta | Sí | Una sola configuración por proyecto con divisa base, AIU, tope, modo de firma (tres), firmante y requisito de auditor; la visibilidad vive en el proyecto |
| RF-04 | EDT recursiva con códigos | Alta | Sí | Capítulos e ítems sin límite de profundidad; códigos únicos por proyecto; búsqueda parcial por código o nombre |
| RF-05 | Insumos del proyecto | Alta | Sí | Siete tipos, estados de cotización y sugerencia de unicidad; los cuatro campos obligatorios definen `FALTA_INFORMACION` |
| RF-06 | Copia de insumos entre proyectos | Media | Sí | Se copia un insumo con su precio a otro proyecto de la compañía; queda independiente |
| RF-07 | APUs por proyecto | Alta | Sí | APU 1:1 con el ítem; creación y copia desde proyectos de la compañía o del catálogo público |
| RF-08 | Reconciliación de insumos al copiar | Alta | Sí | El sistema sugiere insumos similares; el usuario decide usar el existente o agregar el de origen |
| RF-09 | Matrices | Alta | Sí | Insumo compuesto reutilizable en varios APUs; no pertenece a la EDT y no se anida |
| RF-10 | Motor de cálculo | Alta | Sí | Costo directo, AIU y redondeo con una sola implementación; la fórmula exacta cierra `X-15` |
| RF-11 | Cotización y cambio masivo | Alta | Sí | Director y Presupuestador marcan insumos como `COTIZADO` por selección, filtro o totalidad |
| RF-12 | Aprobación por APU según modo | Alta | Sí | Modos sin firma, firma de presupuestador o firma de presupuestador con auditor |
| RF-13 | Registro único de aprobación | Alta | Sí | Un registro por APU con último aprobador, rol, fecha y modo |
| RF-14 | Autoría de última modificación | Media | Sí | APU e insumo guardan usuario y fecha de la última modificación |
| RF-15 | Catálogo público | Media | Sí | Consulta anónima de APUs e insumos de proyectos públicos; copia independiente |
| RF-16 | Búsqueda y similitud de insumos | Media | Sí | Búsqueda parcial y sugerencia de similitud; la decisión es del usuario |
| RF-17 | Alertas de tope presupuestal | Media | Sí | Se muestra alerta cuando el costo supera el tope configurado |
| RF-18 | Correos transaccionales | Media | Sí | Se envían correos de creación de cuenta y recuperación de contraseña |
| RF-19 | Importación y exportación | Baja | No | Excluida del MVP (`D-18`) |
| RF-20 | Listado y filtrado de insumos | Alta | Sí | Filtros por tipo, estado, marca, descripción, ciudad y precio |
| RF-21 | Transición a `FINALIZADO` | Alta | Sí | Solo el Director; exige 100 % cotizado y la aprobación requerida |
| RF-22 | Conteo de uso de insumos | Media | Sí | Suma por ítem de `consumo × cantidad de obra`, para todos los tipos |
| RF-23 | Autorregistro público de usuarios | Alta | Sí | Alta anónima con identidad completa (documento, nombres, fecha de nacimiento, correo, usuario y contraseña), rol Presupuestador, sin compañía, con límite por IP y verificación de correo (`D-72`, `D-73`, `D-80`) |

## 2. Requerimientos no funcionales

| ID | Categoría | Requisito | Medición | Estado |
|---|---|---|---|---|
| NFR-001 | Seguridad | JWT, autorización por `permission_key` con política fail-closed y aislamiento por compañía y proyecto en todo endpoint privado (`D-82`) | 100 % de endpoints privados validan token, permiso y pertenencia | Vigente |
| NFR-002 | Exposición pública | Las respuestas del catálogo no incluyen autoría, empresa ni firma; los precios y contactos sí se publican (`D-28`) | 0 campos prohibidos en la respuesta | Modificado |
| NFR-003 | Rendimiento | p95 ≤ 1,5 s en WBS y catálogo | — | **Eliminado** (`D-67`) |
| NFR-004 | Auditoría | Sin historial de valores; se admiten el último modificador, una aprobación por APU, el historial de estado y visibilidad y los eventos de seguridad (`D-08`, `D-52`, `D-92`, `D-111`) | Registros de solo inserción en los dos historiales | Modificado |
| NFR-005 | Mantenibilidad | Cobertura de pruebas | ≥ 80 % en dominio y motor de cálculo | Vigente (`D-61`) |

## 3. Reglas de negocio

| ID | Regla |
|---|---|
| BR-001 | Un usuario pertenece a una sola compañía vigente, registrada en la tabla `users_company` (`account_enable = 1`), que conserva el historial de vínculos. Excepción: el Administrador y el usuario autorregistrado pendiente de asignación no tienen vínculo vigente (`D-02`, `D-72`, `X-25`) |
| BR-002 | Un proyecto pertenece a una compañía; sus APUs e insumos pertenecen a ese proyecto |
| BR-003 | Cambiar precio, consumo o rendimiento recalcula ítem, capítulo y proyecto |
| BR-004 | Se registra el último usuario que modificó cada APU o insumo y una aprobación por APU |
| BR-005 | Un proyecto o APU privado solo es accesible por la compañía propietaria |
| BR-006 | El catálogo público expone composición, precios y contactos; no expone autoría ni empresa (`D-28`) |
| BR-007 | No se finaliza un presupuesto con insumos en `FALTA_INFORMACION` o sin cotizar |
| BR-008 | La Matriz es un insumo compuesto, reutilizable en varios APUs, no pertenece a la EDT y no se anida |
| BR-009 | Un insumo está completo con descripción, precio, unidad y marca; si falta uno, pasa a `FALTA_INFORMACION` |
| BR-010 | Distribuidor, contacto, ciudad, clave de usuario y detalles son opcionales |
| BR-011 | El paso a `FINALIZADO` exige la aprobación definida por el modo de firma del proyecto; el Director ejecuta el cierre sin firmar APUs (`D-14`, `D-90`) |
| BR-012 | **OBSOLETO** por `D-91`: la imagen de firma se persiste en `UserSignature` y nunca se expone en DTO públicos, catálogo ni logs |
| BR-013 | Un APU se publica solo si su proyecto es público; un proyecto público expone todos sus APUs e insumos |
| BR-014 | La cascada de estados se ejecuta en una sola transacción |
| BR-015 | Los códigos del EDT son únicos por proyecto, sin distinguir mayúsculas: 1–10 caracteres en capítulos y 1–5 en ítems |
| BR-016 | Un APU se vincula a un único ítem de la EDT |
| BR-017 | Editar un APU aprobado elimina su aprobación y exige una nueva |
| BR-018 | Se copia un APU solo desde un proyecto de la misma compañía o desde el catálogo público; nunca desde otra compañía |
| BR-019 | El AIU se define únicamente por proyecto |
| BR-020 | No hay conversión automática de divisas; cada proyecto usa una sola moneda |
| BR-021 | La copia de una Matriz es independiente y exige reconciliar sus insumos en el proyecto destino |

## 4. Reglas de autorización
El control efectivo es el `permission_key` activo del rol (`D-82`); la matriz `1.3.2` es referencia funcional. Los cuatro roles base son inmutables.

| Módulo | Acción | `permission_key` | Administrador | Director | Presupuestador | Auditor |
|---|---|---|---|---|---|---|
| Autorregistro | Crear cuenta propia | — (anónimo) | — | — | — | — |
| Usuarios | Crear | `USERS_CREATE` | Sí | No | No | No |
| Usuarios | Editar o desactivar una cuenta | `USERS_UPDATE`, `USERS_DEACTIVATE` | Sí | No | No | No |
| Usuarios | Buscar | `USERS_READ` | Sí | Sí (su compañía) | Sí (su compañía, `D-112`) | Sí (lectura) |
| Usuarios | Cambiar rol | `USERS_ROLE_CHANGE` | Sí | Sí (su compañía, sin elevar a Administrador) | No | No |
| Usuarios | Asignar a compañía | `USERS_COMPANY_CHANGE` | Sí | Sí (su compañía) | No | No |
| Compañía | Crear | `COMPANIES_CREATE` | Sí (ilimitadas) | Sí (una) | No | No |
| Compañía | Editar datos de registro | `COMPANIES_UPDATE` | Sí | Sí (la propia) | No | No |
| Compañía | Inhabilitar o reactivar | `COMPANIES_DEACTIVATE`, `COMPANIES_REACTIVATE` | Sí (cualquiera) | Solo la propia | No | No |
| Proyectos | Crear | `PROJECTS_CREATE` | No | Sí | Sí | No |
| Proyectos | Editar y configurar | `PROJECTS_UPDATE` | No (solo lectura) | Sí (todos los parámetros) | Solo datos generales: nombre, descripción y objetivo (`D-112`) | No |
| Proyectos | Leer | `PROJECTS_READ` | Sí | Sí | Sí (asignados) | Sí (asignados) |
| Proyectos | Cambiar estado | `PROJECTS_STATUS_CHANGE` | No | Sí | Sí (solo entre `FALTA_INFORMACION` y `COTIZADO`) | No |
| Proyectos | Inhabilitar o reactivar | `PROJECTS_STATUS_CHANGE` | No | Sí | No | No |
| Proyectos | Cambiar visibilidad | `PROJECTS_VISIBILITY_CHANGE` | No | Sí | No | No |
| Pertenencia | Asignar miembros al proyecto | `PROJECT_MEMBERS_ASSIGN` | No | Sí (su compañía) | Sí (Presupuestadores y Auditores) | No |
| EDT | Crear y editar | — | No (lectura) | Sí | Sí | No (lectura) |
| Insumos | Crear y editar | — | No (lectura) | Sí | Sí | No (lectura) |
| APUs y matrices | Crear, editar y copiar | — | No (lectura) | Sí | Sí | No (lectura) |
| Cotización | Cambio masivo a `COTIZADO` | — | No | Sí | Sí | No |
| Aprobación | Firmar APU | — | No | No | Sí | No |
| Aprobación | Visto bueno | — | No | No | No | Sí |
| Cierre | Transición a `FINALIZADO` | `PROJECTS_STATUS_CHANGE` | No | Sí | No | No |
| Perfil | Leer y editar el perfil propio | `PROFILE_READ_SELF`, `PROFILE_UPDATE_SELF` | Sí | Sí | Sí | Sí |
| Catálogo | Consultar | `APU_PUBLIC_READ` | Sí | Sí | Sí | Sí (también anónimo) |
| Autoría | Ver último modificador | — | Sí (lectura) | Sí | Sí | Sí (lectura) |

## 5. Reglas de validación
**General:** JSON en `camelCase` (`D-99`), errores `ProblemDetails` con el código de negocio en `extensions.code` (`D-100`), paginación `page`/`pageSize` (`D-101`), `companyId` y `projectId` tomados del token o de la ruta, nunca del cuerpo.

| Endpoint | Validaciones |
|---|---|
| `POST /auth/register` | Identidad completa obligatoria: tipo y número de documento, primer nombre y apellido, fecha de nacimiento, correo único, nombre de usuario único y contraseña con mayúscula, número y carácter especial; segundo nombre y segundo apellido opcionales; sin residencia; el cuerpo no puede fijar `roleCode`, `companyId` ni `projectId` (`D-72`, `D-73`, `D-76`, `D-80`, INT-06) |
| `POST /users` | `email` y `username` únicos; identidad completa y residencia; `role` válido; `companyId` solo lo fija el Administrador |
| `POST /companies` | `businessName` no vacío; `taxId` único; tipo `PUBLICA`/`PRIVADA`; nivel territorial obligatorio en públicas; representante legal por referencia o datos manuales, no ambos (`D-87`) |
| `POST /projects` | `name` no vacío; ubicación y `baseCurrencyId` válidos; `aiuAdmin`, `aiuUnforeseen` y `aiuProfit` entre 0 y 100; `budgetLimit` mayor que cero; `signingMode` en `SIN_FIRMA`, `UNICA_POR_PRESUPUESTADOR_ASIGNADO` o `INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE`; `requiresAuditor` e `isPublic` booleanos; se rechaza si la compañía no tiene Director activo (`D-93`, DEC-24) |
| `GET /projects/{id}/configuration` | Requiere `PROJECTS_READ`; devuelve la configuración única del proyecto |
| `PATCH /projects/{id}/status` | Transición válida del dominio; actor por transición (`DEC-22`); `409` si el estado no lo permite |
| `PUT /projects/{id}` | `name` no vacío; `signingMode` válido; AIU 0–100 con máximo dos decimales; `budgetLimit` > 0 con máximo dos decimales. El Director de proyectos es el único que puede cambiar ubicación, divisa, tope, AIU, modo de firma, firmante y `requiresAuditor` (`D-112`); el Presupuestador que intente cambiarlos recibe `403 PROJECT_CONFIGURATION_FORBIDDEN` o `403 PROJECT_SIGNATURE_CONFIGURATION_FORBIDDEN` |
| `POST /projects/{id}/memberships` | Rol objetivo en `DIRECTOR`, `PRESUPUESTADOR` o `AUDITOR`; el usuario debe pertenecer a la compañía del proyecto; el Administrador y el Auditor no gestionan membresías; se permite asignar auditores con `requiresAuditor = false` (`D-112`) |
| `POST /projects/{id}/wbs` | `code` único por proyecto; 1–10 caracteres en capítulos y 1–5 en ítems; `parentId` del mismo proyecto; `quantityWork` > 0 en ítems |
| `POST /projects/{id}/resources` | `description` obligatoria; `unitPrice` ≥ 0; `unit` y `brand` requeridos para salir de `FALTA_INFORMACION`; `resourceTypeId` válido; opcionales sin bloquear |
| `POST /apus` | `description` no vacía; `unit` no vacía; `yield` > 0; `projectId` con pertenencia validada |
| `POST /apus/{id}/resources` | `resourceId` del mismo proyecto; `consumption` > 0; `wastePercentage` entre 0 y 100 |
| `POST /apus/{id}/approval` | APU del actor; `signatureCapture` como evidencia temporal; frase de confirmación no vacía |
| `GET /public/apus` | Sin autenticación; solo proyectos públicos; sin autoría, empresa ni firma; precios permitidos |

## 6. Matriz de trazabilidad
`T-U` = unitaria automatizada; `T-I` = integración manual; `T-S` = seguridad manual; `T-E` = flujo manual (`D-65`, `D-66`).

| RF | Caso de uso | Verificación |
|---|---|---|
| RF-01 | UC-01 | T-I-01, T-S-01, T-S-04, T-I-27, T-I-28, T-I-29, T-I-31, T-S-09, T-S-10, T-S-11 |
| RF-02 | UC-01 | T-I-02, T-S-05, T-I-26 |
| RF-03 | UC-02 | T-I-03, T-I-30, T-I-32, T-U-12 |
| RF-04 | UC-03 | T-U-01, T-I-04 |
| RF-05 | UC-04 | T-U-02, T-I-05 |
| RF-06 | UC-04 | T-I-06 |
| RF-07 | UC-05 | T-I-07, T-E-01 |
| RF-08 | UC-05 | T-U-03, T-I-08 |
| RF-09 | UC-05 | T-U-04, T-I-09 |
| RF-10 | UC-06 | T-U-05 |
| RF-11 | UC-06 | T-U-06, T-I-10 |
| RF-12 | UC-07 | T-U-07, T-I-11 |
| RF-13 | UC-07 | T-I-12 |
| RF-14 | UC-07 | T-I-13 |
| RF-15 | UC-08 | T-I-14, T-S-02, T-S-06 |
| RF-16 | UC-04 | T-U-08, T-I-15 |
| RF-17 | UC-06 | T-U-09, T-I-16 |
| RF-18 | UC-01 | T-I-17, T-I-22, T-S-12 |
| RF-19 | — | Fuera del MVP (`D-18`) |
| RF-20 | UC-04 | T-I-18 |
| RF-21 | UC-07 | T-U-10, T-I-19, T-U-12, T-I-32 |
| RF-22 | UC-05 | T-U-11, T-I-20 |
| RF-23 | UC-01 | T-I-21, T-S-07 |

`T-I` y `T-S` se ejecutan manualmente sobre la baseline aplicada (`D-66`); las verificaciones de `E-1b` y sus identificadores nuevos están en `PLAN §5.6.5`.


