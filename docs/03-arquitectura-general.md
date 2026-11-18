# Arquitectura general

> Documento actualizado el 30/10/2026 (reconciliación `E-1b`, `D-74`…`D-111`). Detalle y justificación en el capítulo 3 de `PLAN_INGENIERIA.md`.

## 1. Resumen arquitectónico
Plataforma web con **monolito modular** y **Clean Architecture**, SPA desacoplada en Angular 20, API REST en ASP.NET Core sobre .NET 10, persistencia en SQL Server 2022 con EF Core 10 y despliegue con Docker Compose y Nginx.

La separación por módulos se sostiene por convención y pruebas de arquitectura; no se usan microservicios (`D-54`, `D-63`).

## 2. Capas y reglas
| Capa | Responsabilidad | Prohibido |
|---|---|---|
| Dominio | Entidades, enums, `ApuCalculator`, contratos de repositorio | Depender de EF Core o ASP.NET |
| Aplicación | Casos de uso, DTOs, validación, consultas y transacciones | Usar `DbContext` o SQL directo |
| Infraestructura | `DbContext`, repositorios, Identity, correo y migraciones | Contener reglas de negocio |
| API | Endpoints, autenticación, autorización y `ProblemDetails` | Usar `DbContext` o repositorios |
| Presentación | Angular, guardias, servicios y componentes | Replicar fórmulas o reglas |

**Regla de dependencia:** `API → Aplicación → Dominio`; Infraestructura implementa los contratos y la composición se hace en la API.

Las consultas que necesita el middleware o un manejador de autorización se exponen como **puertos de Aplicación** (por ejemplo `IActorAccessReader` para la foto de autorización del actor) y los implementa Infraestructura; así la API no toca `DbContext` (`§3.4.6`). Las reglas de capas se verifican con `NetArchTest` en `BudgetCivil.Tests.Architecture` (`T-A-02`).

## 3. Módulos
| ID | Módulo | Responsabilidad |
|---|---|---|
| M-01 | Acceso y organización | Usuarios, roles, asignaciones, compañías y correos |
| M-02 | Proyectos y configuración | Divisa, AIU, tope, firma, auditor y visibilidad |
| M-03 | EDT | Capítulos, ítems, códigos, numeración y búsqueda |
| M-04 | Insumos | Registro, tipos, estados, filtros y copia |
| M-05 | APUs y matrices | Composición, copia, reconciliación y conteo |
| M-06 | Cálculo | Consumos, costo directo, AIU y redondeo |
| M-07 | Aprobación y cierre | Cotización, aprobación, autoría y `FINALIZADO` |
| M-08 | Catálogo abierto | Consulta y copia de APUs e insumos públicos |
| M-09 | Alertas | Evaluación del tope presupuestal |

## 4. Aislamiento multitenant
- Una base con esquema compartido y `company_id` en las entidades de compañía.
- `project_id` en las entidades con alcance de proyecto.
- Filtro global por compañía y validación de pertenencia en la capa de aplicación.
- **Un único bypass** para el catálogo público, que solo lee proyectos públicos (`D-36`).
- La verificación de aislamiento es manual, con dos compañías y dos proyectos (`D-66`).

## 5. Consultas de lectura
El conteo de uso de insumos y el catálogo público se resuelven con **proyecciones de EF Core** en la capa de aplicación, sin duplicar la fórmula del APU (`D-47`). El catálogo omite autoría y empresa, pero publica precios y contactos (`D-28`).

La **configuración del proyecto** es una entidad propia 1:1 (`ProjectConfiguration`) con divisa base, AIU, tope, modo de firma, firmante y requisito de auditor (`D-93`); el proyecto no repite esos campos. La lectura de proyectos y su configuración valida el alcance de compañía y, para Presupuestador y Auditor, la pertenencia al proyecto.

La decisión de autorización se resuelve con el `permission_key` activo del rol vigente y política **fail-closed**: sin permiso sembrado, la operación se deniega y se registra el evento de seguridad (`D-82`, `D-111`).

## 6. Despliegue
| Contenedor | Contenido | Exposición |
|---|---|---|
| `web` | Nginx con la SPA y proxy inverso | Pública en entorno local |
| `api` | ASP.NET Core .NET 10, puerto 8080 | Red interna |
| `sqlserver` | SQL Server 2022 con volumen persistente | Red interna |

**TLS postergado:** solo se permite entorno local o demo controlada; prohibido exponer a Internet sin HTTPS (`D-39`). Migraciones manuales con runbook (`D-40`).

## 7. Seguridad
| Control | Estado |
|---|---|
| JWT + ASP.NET Identity para el hash | Vigente (`D-81`) |
| Autorización por `permission_key` con semilla versionada e idempotente y política fail-closed | Vigente (`D-82`) |
| Cuatro roles base inmutables | Vigente (`D-82`) |
| Bloqueo de la cuenta a los cinco intentos fallidos consecutivos | Vigente (`D-82`, `REQ-IDENTIDAD-22`) |
| Rehash perezoso al siguiente inicio de sesión correcto | Vigente (`D-81`) |
| Autorregistro con identidad completa, límite por IP y verificación de correo | Vigente (`D-73`, `D-80`) |
| Recuperación de contraseña por correo con respaldo asistido del Administrador | Vigente (`D-79`, `RF-18`) |
| Aislamiento por compañía y proyecto | Vigente con verificación manual (`T-S-09`) |
| Aprobación única por APU | Vigente (`D-52`) |
| Firma persistida en `UserSignature` y nunca expuesta en DTO públicos, catálogo ni logs | Vigente (`D-91`, `R-28`) |
| Catálogo público anónimo con precios y contactos | Vigente (`D-28`, `D-38`) |
| HTTPS | Postergado (`D-39`) |
| Refresh tokens y CORS | CORS local documentado; refresh tokens fuera del MVP |

## 8. Observabilidad
- Logs estructurados JSON con Serilog e identificador de correlación.
- `/health` para API y conexión a base de datos.
- Último modificador por APU e insumo, más un registro de aprobación por APU.
- Sin bitácora histórica (`E-05`).

## 9. Riesgos técnicos
| Riesgo | Mitigación |
|---|---|
| Fuga entre compañías por el bypass | Un solo punto de bypass y checklist con dos compañías |
| Exposición de precios y contactos | Advertencia y aceptación al publicar |
| Concurrencia | `rowversion` y `409 Conflict` |
| Licenciamiento de SQL Server | Definir edición antes de E-6 |
| Migraciones manuales | Runbook y respaldo previo |
| Sin TLS | Solo entorno local |

## 10. Diagramas
Los diagramas de `docs/architecture/` son referenciales y no se mantienen sincronizados. La fuente de verdad es este documento y el capítulo 3 del plan.
