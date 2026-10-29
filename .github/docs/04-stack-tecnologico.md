# Stack tecnológico

> Documento actualizado el 27/10/2026. Decisiones en el capítulo 3 de `PLAN_INGENIERIA.md` (v0.26).

## 1. Stack oficial
| Capa | Tecnología | Versión base | Motivo |
|---|---|---|---|
| Backend | ASP.NET Core Web API | .NET 10 (LTS) | Rendimiento, capas y soporte en Linux |
| ORM | Entity Framework Core | 10.x | Migraciones, filtros globales y productividad |
| Base de datos | SQL Server | 2022 | Transacciones, restricciones e índices |
| Frontend | Angular | 20.x | Tipado fuerte, componentes y tablas jerárquicas |
| UI | Angular Material | Alineada a Angular 20 | Componentes oficiales y accesibilidad |
| Proxy | Nginx | Versión fija al desplegar | Estáticos y proxy inverso |
| Contenedores | Docker Compose | v2 | Entorno reproducible en un servidor |
| Identidad | ASP.NET Identity + JWT | .NET 10 | Usuarios, roles y tokens |
| Logging | Serilog | Última estable | Logs estructurados JSON |
| Pruebas | xUnit + Moq + FluentAssertions | Últimas estables | Unitarias de dominio y aplicación |
| Arquitectura | NetArchTest | Última estable | Reglas de capas en CI |
| Contrato | Swashbuckle | Última estable | OpenAPI y Swagger UI |
| CI/CD | GitHub Actions | — | Build, pruebas y contrato |

## 2. Librerías backend
| Librería | Uso | Estado |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validación de tokens | Confirmado |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Usuarios y roles | Confirmado |
| `FluentValidation` + `DependencyInjectionExtensions` | Validación de DTOs | Confirmado; `FluentValidation.AspNetCore` está discontinuado |
| `Serilog.AspNetCore` | Logs | Confirmado |
| `Swashbuckle.AspNetCore` | OpenAPI | Confirmado |
| `xUnit`, `Moq`, `FluentAssertions` | Pruebas unitarias | Confirmado |
| `NetArchTest` | Pruebas de arquitectura | Confirmado |
| `Coverlet` | Cobertura | Confirmado al configurar CI |
| `AutoMapper` | — | **No se usa**: mapeo manual (`D-41`) |

## 3. Frontend
| Elemento | Definición |
|---|---|
| Framework | Angular 20 con TypeScript |
| UI | Angular Material |
| Reactividad | RxJS y signals |
| Estado | Híbrido: servicios con signals para datos compartidos; estado local para formularios y filtros (`D-57`) |
| HTTP | `HttpClient` con interceptores de token y errores |
| Firma | `signature_pad` (`D-44`) |
| Cliente API | Generado desde OpenAPI y regenerado en CI (`D-56`) |
| Pruebas | Sin suite automatizada; flujos críticos manuales (`D-65`) |

## 4. Infraestructura y servicios
| Servicio | Definición |
|---|---|
| CI/CD | GitHub Actions (`D-46`) |
| Correo | Proveedor transaccional; SendGrid, Mailgun o SES se elige al implementar (`D-45`) |
| Base local | SQL Server en Docker para verificación manual de integración (`D-66`) |
| TLS | Postergado; solo entorno local (`D-39`) |
| Migraciones | Manuales con runbook (`D-40`) |

## 5. Calidad
| Herramienta | Uso |
|---|---|
| `.editorconfig` + `Microsoft.CodeAnalysis.NetAnalyzers` | Estilo y análisis estático |
| ESLint + Prettier | Calidad del frontend |
| `package-lock.json` y versiones fijas en `.csproj` | Reproducibilidad |
| SemVer `MAJOR.MINOR.PATCH` | Versionado |
| Cobertura ≥ 80 % | Dominio y motor de cálculo (`D-61`) |

**Pruebas automatizadas:** unitarias, arquitectura y contrato. **Manuales:** integración, E2E, migraciones y aceptación (`D-65`, `D-66`).

## 6. Herramientas de especificación e IA
- **OpenSpec** reemplaza por completo a Spec Kit (`D-09`).
- Las especificaciones se escriben en español; el código y los identificadores técnicos en inglés (`D-69`).
- La IA puede redactar especificaciones y código; el estudiante revisa y aprueba (`D-71`).
- No se exige registro de uso de IA (`D-70`).

## 7. Pendientes
| Tema | Responsable | Momento |
|---|---|---|
| Proveedor de correo concreto | Estudiante | Antes de E-1 |
| Edición de SQL Server | Estudiante | Antes de E-6 |
| TLS para despliegue público | Estudiante | Antes de exponer la aplicación |
