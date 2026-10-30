# Checklist de cierre de la etapa `E-1b`

**Etapa:** `E-1b` — Reconciliación de la línea base (`PLAN §4.8.7`)
**Cambio:** `reconciliacion-e1b`
**Fecha:** 30/10/2026
**Estado:** firmado el 30/10/2026. Las verificaciones manuales de API y seguridad las ejecuta el estudiante
sobre la baseline aplicada (sección 3) y el cambio se archiva al completarlas.

## 1. Criterios de aceptación (`§4.8.7`)

| # | Criterio | Estado | Evidencia |
|---|---|---|---|
| 1 | `openspec/specs/identidad` y `openspec/specs/proyectos` existen y validan. | ⚠ Parcial | El delta `specs/identidad` y `specs/proyectos` valida (`openspec validate`); la copia a `openspec/specs/` ocurre al archivar (`§8.4`). |
| 2 | `GAP-01`…`GAP-17` cerradas o aceptadas con etapa asignada. | ✅ Cumple | `GAP-REGISTER.md`: 15 `ESPECIFICADA` y 2 `PARCIAL` con etapa posterior (`GAP-13` en `E-4b`; `GAP-15` en `E-2` y `E-4b`). `GAP-07` se implementó el 30/10/2026. |
| 3 | Contrato v1 documentado, sin `snake_case`, sin sobres propios y con `ProblemDetails`. | ⚠ Parcial | Por inspección: `camelCase` (`Program.cs`), `ProblemDetails` con `extensions.code` y `traceId` (`ExceptionHandlingMiddleware`), paginación `page`/`pageSize` (`PaginationQuery`), sin `JsonPropertyName` de `snake_case` (`T005`). Falta el test de contrato automatizado y el documento OpenAPI versionado. |
| 4 | `RF-18` verificado con proveedor simulado (`T-I-22`). | ⏳ Ejecución manual | Implementado (`T010`: `IEmailSender` + `LoggingEmailSender`, correos de creación y recuperación). La verificación `T-I-22` la ejecuta el estudiante. |
| 5 | El autorregistro cumple `D-73` y `D-80` (`T-S-07`). | ✅ Cumple | `D-73` (correo y usuario únicos). `D-80` implementado el 30/10/2026: la cuenta nace deshabilitada, se envía un token de un solo uso (24 h) guardado como hash SHA-256 en `email_verification_tokens`, `POST /auth/email-verification/confirm` la habilita, el inicio de sesión sin verificar se rechaza y registra el evento, y el autorregistro limita a **5 intentos por IP cada 15 minutos** con `429` y `ProblemDetails`. La ejecución manual de `T-S-07` la realiza el estudiante. |
| 6 | Roles y permisos se crean por `seed` idempotente (`T-I-23`). | ✅ Cumple | `SeedIdempotencyTests`: ejecutar el `seed` dos veces deja el mismo inventario; matriz coherente con `DEC-21`, `DEC-22` y `D-86`. |
| 7 | La migración se aplicó con respaldo, convención `D-107` y reversión conocida. | ⚠ Parcial | Baseline `202610300438_e1b_reconciliacion_baseline` y migración `202610300527_e1b_email_verification`, ambas con el formato exigido y aplicadas por el estudiante; la reversión está documentada en `docs/runbook-migraciones.md §4`. El respaldo previo no consta en el repositorio. |
| 8 | `Tests.Unit`, `Tests.Integration` y `Tests.Architecture` en verde; cobertura ≥ 80 % en dominio (`D-61`). | ⚠ Parcial | 147/147 unitarias, 7/7 arquitectura y 14/14 integración ejecutadas y en verde (30/10/2026). **La cobertura no se midió**: el testhost de VSTest no arranca en el entorno del agente; se requiere `dotnet test --collect:"XPlat Code Coverage"` en el equipo local. |
| 9 | `IMPACTO_FRONTEND.md` firmado y `A-10` aceptado. | ✅ Cumple | `IMPACTO_FRONTEND.md` actualizado con `IF-01`…`IF-08` y firmado por Sergio Parra el 30/10/2026. |
| 10 | `.github/docs` actualizados y `specs/001`/`specs/002` marcadas como históricas (`D-108`). | ✅ Cumple | `00`, `02`, `03`, `04`, `05` y `06` reflejan `D-74`…`D-111`; `README.md` marca `specs/001` y `specs/002` como evidencia histórica no normativa y `git status` confirma que no se editaron. |

## 2. Verificación de la etapa (`§5.6.5` y nuevos identificadores)

| ID | Qué comprueba | Resultado | Evidencia |
|---|---|---|---|
| `T-I-22` | Correos de creación de cuenta y recuperación con proveedor simulado | ⏳ Ejecución manual | Implementado (`T010`); pendiente de ejecutar la API. |
| `T-I-23` | `seed` idempotente y fail-closed | ✅ Ejecutada | `SeedIdempotencyTests` (integración, SQL Server local). El fail-closed se verificó por inspección del manejador de permisos. |
| `T-I-24` | `rowversion` y `409 Conflict` en `Project` | ✅ Ejecutada (persistencia) | `ProjectConcurrencyTests`: la versión anterior provoca `DbUpdateConcurrencyException` y la vigente avanza la versión; `BaselineSchemaTests` verifica la baseline. La respuesta HTTP `409` se confirma en la revisión manual de API. |
| `T-I-25` | Historial de estados y visibilidad | ✅ Ejecutada | `HistoryPersistenceTests`. |
| `T-C-01` | Contrato v1: `camelCase`, `ProblemDetails`, `page`/`pageSize` | ⚠ Parcial | Inspección de `Program.cs`, `ExceptionHandlingMiddleware` y `PaginationQuery`; falta test automatizado y OpenAPI versionado. |
| `T-A-02` | Reglas de capas y módulos con `NetArchTest` | ✅ Ejecutada | `LayerDependencyTests`: 7/7 en verde. |
| `T-S-07` | Autorregistro: límite por IP y verificación de correo | ✅ Implementada | `RegisterUserUseCaseTests` y `ConfirmEmailVerificationUseCaseTests` (unitarias) y `EmailVerificationPersistenceTests` (integración). La ejecución manual completa la realiza el estudiante. |
| `T-S-08` | La imagen de firma no se expone | ✅ Ejecutada (inspección) | `SignatureImageBase64` solo aparece en `UserSignature`, su configuración EF y el snapshot; ningún DTO ni respuesta lo expone. |
| `T-I-26` | Ciclo de vida de la compañía | ⏳ Ejecución manual | Pendiente de ejecutar con Swagger. |
| `T-I-27` | Alta, búsqueda y cambio de rol de usuarios | ⏳ Ejecución manual | Pendiente de ejecutar con Swagger. |
| `T-I-28` | Modelo de usuario unificado y documento no único | ✅ Ejecutada | `UserModelTests`. |
| `T-I-29` | `User.company_id` y `ProjectMember` | ✅ Ejecutada | `MembershipPersistenceTests`. |
| `T-I-30` | Configuración única del proyecto | ✅ Ejecutada | `ProjectConfigurationPersistenceTests`. |
| `T-I-31` | Bootstrap del primer Administrador | ⏳ Ejecución manual | Pendiente de ejecutar con Swagger. |
| `T-I-32` | Transiciones de estado por rol | ⚠ Parcial | `ProjectStatusTransitionPolicyTests` (dominio) en verde; la verificación HTTP queda pendiente. |
| `T-U-12` | Reglas de estado en el dominio | ✅ Ejecutada | `ProjectStatusTransitionPolicyTests`. |
| `T-S-09` | Aislamiento entre compañías y proyectos | ⏳ Ejecución manual | Checklist manual con dos compañías y dos proyectos (`D-66`). |
| `T-S-10` | Fail-closed y eventos de seguridad | ⚠ Parcial | Implementado y verificado por inspección; falta la ejecución manual del `403` y del evento registrado. |
| `T-S-11` | Bloqueo por intentos y rehash | ⏳ Ejecución manual | Ejecución manual de seguridad. |
| `T-S-12` | Recuperación asistida de contraseña | ⏳ Ejecución manual | Ejecución manual de seguridad. |

## 3. Pendientes asumidos al firmar

1. Ejecutar el checklist manual de API y seguridad (`T-I-22`, `T-I-26`, `T-I-27`, `T-I-31`, `T-I-32`, `T-S-07`, `T-S-09`…`T-S-12`) sobre la baseline aplicada y registrar el resultado por identificador.
2. Producir la medición de cobertura (`dotnet test tests\BudgetCivil.Tests.Unit --collect:"XPlat Code Coverage"`) y verificar ≥ 80 % en dominio.
3. Aplicar la migración `202610300527_e1b_email_verification` en la base local antes de probar el autorregistro.
4. Archivar el cambio cuando los puntos 1, 4 y 7 estén en ✅ y la cobertura esté medida.

## 4. Firma de cierre

| Rol | Nombre | Fecha | Firma |
|---|---|---|---|
| Estudiante responsable | Sergio Parra | 30/10/2026 | Sergio Parra |
