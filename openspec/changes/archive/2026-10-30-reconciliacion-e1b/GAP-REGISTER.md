# Registro de brechas — `reconciliacion-e1b`

Registro de las diecisiete brechas de `PLAN §4.8.4` entre el comportamiento implementado (evidencia
histórica `specs/001` y `specs/002`, no normativa por `D-108`) y el PLAN v0.29. Cada brecha indica la
decisión que la cierra, la etapa en que se cierra, el requisito de `openspec/specs` que la fija y la
evidencia que la demuestra.

**Estados**

| Estado | Significado |
|---|---|
| `ESPECIFICADA` | El requisito que fija el comportamiento ya existe en este cambio; el cierre por código está planificado en la etapa indicada. |
| `PARCIAL` | Parte del comportamiento queda especificada en `E-1b` y parte pertenece a una etapa posterior. |
| `ACEPTADA` | La brecha se acepta como deuda con etapa asignada y no bloquea `E-2`. |

## Registro

| ID | Brecha (as-built) | Origen | Decisión de cierre | Etapa | Requisito | Evidencia | Estado |
|---|---|---|---|---|---|---|---|
| GAP-01 | El rol se llama "Gerente de proyectos" en código, semillas, claims y contratos | `D-74` | Nombre canónico Director de proyectos con `role_code = DIRECTOR`; el término histórico deja de ser válido en las entradas | E-1b | `REQ-IDENTIDAD-19` | `T-I-23`; `SC-IDENTIDAD-50`…`SC-IDENTIDAD-52` | ESPECIFICADA (T006) |
| GAP-02 | Hash Argon2id sin decisión en el PLAN | `D-81` | Hash provisto por ASP.NET Core Identity (PBKDF2) con rehash en el siguiente inicio de sesión correcto | E-1b | `REQ-IDENTIDAD-23` | `T-S-11`; `SC-IDENTIDAD-59`…`SC-IDENTIDAD-61` | ESPECIFICADA (T007) |
| GAP-03 | `UserAccount` y `UserProfile` separados | `D-78` | Un único `User` que absorbe el perfil | E-1b | `REQ-IDENTIDAD-08` | `T-I-28`; `SC-IDENTIDAD-17`…`SC-IDENTIDAD-19` | ESPECIFICADA (T008) |
| GAP-04 | `document_number` único con respuesta `409` | `D-77` | El documento no es único; la unicidad de cuenta se garantiza con `email` y `username` | E-1b | `REQ-IDENTIDAD-05` | `T-I-28`; `SC-IDENTIDAD-11`, `SC-IDENTIDAD-12` | ESPECIFICADA (T008) |
| GAP-05 | Autorregistro con siete campos y perfil rico en un solo paso | `D-76` (modificada) | Autorregistro con identidad completa —documento, nombres y apellidos, fecha de nacimiento, correo, usuario y contraseña— y residencia obligatoria en el perfil antes de operar dentro de una compañía | E-1b | `REQ-IDENTIDAD-02`, `REQ-IDENTIDAD-08` | `T-S-07`; `SC-IDENTIDAD-03`…`SC-IDENTIDAD-05` | ESPECIFICADA (T009) |
| GAP-06 | Recuperación de contraseña solo asistida por el Administrador | `D-79`, `RF-18` | Recuperación por correo con respaldo asistido por el Administrador, sin que este conozca la nueva contraseña | E-1b | `REQ-IDENTIDAD-25` | `T-I-22`, `T-S-12`; `SC-IDENTIDAD-64`…`SC-IDENTIDAD-66` | ESPECIFICADA (T010) |
| GAP-07 | Autorregistro sin límite de intentos ni verificación de correo | `D-80` | Límite de intentos por IP y verificación de correo antes de habilitar la cuenta | E-1b | `REQ-IDENTIDAD-06`, `REQ-IDENTIDAD-07` | `T-S-07`; `SC-IDENTIDAD-13`…`SC-IDENTIDAD-16` | ESPECIFICADA (T009): implementado el 30/10/2026 (`email_verification_tokens`, 5 intentos por IP cada 15 minutos) |
| GAP-08 | Autorización con roles fijos y sin `seed` de permisos | `D-82` | `Permission`/`RolePermission` con `seed` versionado e idempotente, decisión por permiso activo y fail-closed | E-1b | `REQ-IDENTIDAD-17`, `REQ-IDENTIDAD-18`, `REQ-IDENTIDAD-19` | `T-I-23`, `T-S-10`; `SC-IDENTIDAD-46`…`SC-IDENTIDAD-52` | ESPECIFICADA (T011) |
| GAP-09 | `/companies` y `/users` definidos e implementados dos veces | `D-102` | `M-01` es dueño único; `M-02` solo expone `/projects` | E-1b | No funcional: se fija en el contrato v1 (`design.md`, DEC-11) | `T-C-01`; documento OpenAPI sin rutas duplicadas | ESPECIFICADA (T013) |
| GAP-10 | `snake_case`, ausencia de `ProblemDetails` y paginación divergente | `D-99`, `D-100`, `D-101` | Contrato v1 con `camelCase`, `ProblemDetails` con `extensions.code` y `page`/`pageSize` | E-1b | No funcional: se fija en el contrato v1 (`design.md`, DEC-01 y DEC-15) | `T-C-01` | ESPECIFICADA (T005) |
| GAP-11 | Tablas de membresía con historial | `D-98` | `User.company_id` y `ProjectMember` única por (`project_id`, `user_id`); **sin migración de datos**, porque la base está vacía (DEC-19) | E-1b | `REQ-IDENTIDAD-12`, `REQ-PROYECTOS-12` | `T-I-29`; `SC-IDENTIDAD-28`…`SC-IDENTIDAD-31`, `SC-PROYECTOS-26`…`SC-PROYECTOS-28` | ESPECIFICADA (T014) |
| GAP-12 | Segregación por esquemas `param`/`iam`/`dbo`/`audit`/`log` | `D-95` | Esquema único con `company_id` y `project_id`; gobernanza por runbook y convención de nombres; **sin mover objetos**, porque la base está vacía (DEC-19) | E-1b | No funcional: se fija en el modelo de datos (`design.md`, DEC-12) | `T-I-24`; baseline única aplicada sobre base vacía (T020, T021) | ESPECIFICADA (T015) |
| GAP-13 | `UserSignature` con imagen sin reglas de exposición | `D-91` | Una firma activa por usuario; la imagen nunca se expone en DTO públicos, catálogo ni registros técnicos | E-1b (modelo y exposición) + E-4b (uso en la aprobación) | `REQ-PROYECTOS-10` (parámetro de firma); la regla de exposición es la restricción `R-28` | `T-S-08` | PARCIAL (T016) |
| GAP-14 | `Project` sin `rowversion` y estados sin formalizar | `D-88`, `D-97` | `rowversion` en `Project`, conjunto cerrado de cinco estados con `CHECK` y reglas de transición en el dominio | E-1b | `REQ-PROYECTOS-03`, `REQ-PROYECTOS-11` | `T-I-24`, `T-U-12`; `SC-PROYECTOS-06`, `SC-PROYECTOS-07`, `SC-PROYECTOS-24`, `SC-PROYECTOS-25` | ESPECIFICADA (T017) |
| GAP-15 | Cierre con firma del Director y solo dos modos de firma | `D-90` | Tres modos de firma y cierre ejecutado por el Director sin firmar APUs | E-1b (cierre sin firma) + E-2 (modos) + E-4b (aplicación) | `REQ-PROYECTOS-06`, `REQ-PROYECTOS-10` | `SC-PROYECTOS-12`…`SC-PROYECTOS-14`; `T-U-10`, `T-I-19` en E-4b | PARCIAL (T018) |
| GAP-16 | El Administrador crea y edita proyectos | `D-86` | Administrador en solo lectura sobre proyectos: consulta, no crea ni configura | E-1b | `REQ-PROYECTOS-15` | `SC-PROYECTOS-02`, `SC-PROYECTOS-36`, `SC-PROYECTOS-37` | ESPECIFICADA (T019) |
| GAP-17 | Pruebas en un solo `BlackBoxTests` con `InMemory` y `WebApplicationFactory` | `D-105`, `D-106` | Se elimina `BlackBoxTests` completo y la suite se reconstruye en `Tests.Unit`, `Tests.Integration` y `Tests.Architecture` con `NetArchTest`; integración manual contra SQL Server local | E-1b | No funcional: se fija en la estrategia de pruebas (`design.md`, DEC-14 y DEC-20) | `T-A-02`; CI en verde; cobertura ≥ 80 % en dominio | ESPECIFICADA (T022–T025) |

## Resumen

| Estado | Cantidad | Brechas |
|---|---|---|
| ESPECIFICADA, cierre completo en E-1b | 15 | GAP-01, GAP-02, GAP-03, GAP-04, GAP-05, GAP-06, GAP-07, GAP-08, GAP-09, GAP-10, GAP-11, GAP-12, GAP-14, GAP-16, GAP-17 |
| PARCIAL, con etapa posterior asignada | 2 | GAP-13 (E-4b), GAP-15 (E-2 y E-4b) |
| ACEPTADA como deuda | 0 | — |
| **Total** | **17** | — |

## Brechas sin requisito funcional

`GAP-09`, `GAP-10`, `GAP-12` y `GAP-17` no producen un requisito de comportamiento: afectan al contrato,
al modelo de datos y a la estrategia de pruebas, no a lo que el usuario observa. Se cierran con las
decisiones `DEC-01`, `DEC-11`, `DEC-12`, `DEC-14`, `DEC-15`, `DEC-19` y `DEC-20` de `design.md`, con el checklist de
`PLAN §4.8.7` y con las pruebas `T-C-01`, `T-I-24` y `T-A-02`. Se registran aquí para que ninguna de las
diecisiete brechas quede sin dueño.

## Brechas y decisiones resueltas el 29/10/2026

Las cuatro preguntas que bloqueaban el cierre quedaron resueltas por el autor e incorporadas a la
especificación; ninguna brecha queda detenida.

| Brecha | Pregunta | Respuesta | Requisito | Decisión de diseño |
|---|---|---|---|---|
| GAP-08 | `Q-E1B-02` permisos de las transiciones | Solo el Director inhabilita y reactiva; Director y Presupuestador alternan `FALTA_INFORMACION` y `COTIZADO` | `REQ-PROYECTOS-05`, `16`, `17` | DEC-22 |
| GAP-02, GAP-07 | `Q-E1B-01` umbral de bloqueo | Cinco intentos fallidos consecutivos | `REQ-IDENTIDAD-22` | DEC-23 |
| GAP-05 | `Q-E1B-03` administración de usuarios | Solo el Administrador edita y desactiva; los demás solo su perfil | `REQ-IDENTIDAD-29` | DEC-21 |
| GAP-16 | `Q-E1B-04` compañía sin Director activo | Se rechaza el alta del proyecto | `REQ-PROYECTOS-18` | DEC-24 |

También quedó resuelta `Q-E1B-05`: la reapertura de `FINALIZADO` que ya existe en el dominio se
conserva y se especifica en `REQ-PROYECTOS-19` (DEC-25). No queda ninguna brecha ni pregunta abierta.

## Evidencia de cierre

| Tipo | Evidencia | Responsable |
|---|---|---|
| Especificación | `openspec/specs/identidad/spec.md` y `openspec/specs/proyectos/spec.md` sin errores de validación | Este cambio |
| Prueba automatizada | `T-A-02`, `T-C-01`, `T-U-12` y unitarias de dominio en CI | T023, T025 |
| Prueba manual | `T-I-22`…`T-I-32`, `T-S-07`…`T-S-12` con checklist y resultado | T024, T026 |
| Operación | Baseline única aplicada sobre base vacía, revisada y con reversión conocida (`D-40`, `D-107`) | T014, T015, T017, T020, T021 |
| Proceso | Checklist de `§4.8.7` firmado y `IMPACTO_FRONTEND.md` firmado | T030 |
