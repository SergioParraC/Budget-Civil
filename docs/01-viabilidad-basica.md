# Viabilidad básica

> Documento actualizado el 27/10/2026. Detalle en `PLAN_INGENIERIA.md` (v0.26).

## 1. Viabilidad técnica
| Componente | Definición |
|---|---|
| Frontend | Angular 20 con TypeScript, Angular Material y `signature_pad` |
| Backend | ASP.NET Core Web API sobre .NET 10, Clean Architecture y monolito modular |
| Persistencia | SQL Server 2022 con EF Core 10, esquema compartido y filtro por compañía |
| Seguridad | ASP.NET Identity + JWT, RBAC de cuatro roles y asignación por proyecto |
| Despliegue | Docker Compose con Nginx, API y SQL Server en Ubuntu 24.04 |
| Integración | OpenAPI con Swashbuckle y cliente Angular generado |

La arquitectura y el stack están decididos en los capítulos 3 del plan. El riesgo técnico principal es el aislamiento multitenant con un único bypass para el catálogo público (`D-36`).

## 2. Viabilidad operativa
Flujo diario:

1. El Administrador crea compañías y usuarios, y designa Directores.
2. El Director configura la compañía y el proyecto: divisa, AIU, tope, modo de firma, requisito de auditor y visibilidad.
3. El Director asigna Presupuestadores y Auditores de su compañía.
4. El Presupuestador construye la EDT y registra insumos; puede copiarlos desde otros proyectos.
5. Crea o copia APUs, reconcilia insumos y se calcula el costo.
6. Los insumos se cotizan; el paso a `COTIZADO` puede ser masivo.
7. Se aprueba cada APU según el modo configurado.
8. El Director cierra el presupuesto: todo cotizado y aprobado.
9. Si el proyecto es público, sus APUs e insumos se exponen en el catálogo.

**Soporte y mantenimiento:** logs estructurados, `/health`, migraciones manuales con runbook (`D-40`) y respaldos documentados. No hay soporte 24/7 ni alta disponibilidad.

## 3. Viabilidad económica inicial
| Concepto | Estimación |
|---|---|
| Servidor de aplicaciones / contenedores | USD 20–50 / mes |
| Base de datos | USD 25–60 / mes (más licencia si es edición comercial) |
| Dominio y logs | USD 5–15 / mes |
| Correo transaccional | Plan gratuito o de bajo costo |
| TLS | Postergado; sin costo en entorno local |

**Costo mensual estimado del MVP:** USD 50–125, sin incluir la licencia de SQL Server en producción (`RT-15`).

## 4. Restricciones y dependencias
1. Migraciones manuales; no hay inicialización automática (`D-40`).
2. TLS postergado: solo entorno local o demo controlada (`D-39`).
3. Proveedor de correo por definir (`D-45`).
4. Fórmulas de cálculo pendientes (`X-15`).
5. Licenciamiento de SQL Server antes de un despliegue real (`RT-15`).
6. Pruebas de integración y E2E manuales (`D-65`, `D-66`).

## 5. Riesgos y mitigaciones
| Riesgo | Mitigación |
|---|---|
| Fuga entre compañías (`RT-13`) | Un único bypass y checklist con dos compañías |
| Exposición al publicar precios (`RT-11`) | Advertencia y aceptación al publicar |
| Concurrencia (`RT-14`) | `rowversion` y `409 Conflict` |
| Licenciamiento de SQL Server (`RT-15`) | Definir edición o migrar a PostgreSQL |
| Migraciones manuales (`RT-17`) | Runbook y respaldo previo |
| Correo no entregado (`RT-18`) | Proveedor transaccional y prueba de envío |
| Deriva del cliente (`RT-19`) | Regeneración en CI |

## 6. Decisión
**GO CON CONDICIONES.**

Condiciones para iniciar E-0:
1. Resolver `X-15` antes de abrir E-4a.
2. Elegir proveedor de correo antes de E-1.
3. Definir la edición de SQL Server antes de E-6.
4. No exponer la aplicación a Internet sin TLS (`D-39`).
