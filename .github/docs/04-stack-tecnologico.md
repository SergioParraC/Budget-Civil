# Stack tecnologico

## 1. Stack oficial

| Capa | Tecnología | Versión base | Motivo |
|---|---|---|---|
| **Backend API** | ASP.NET Core Web API | .NET 10 | Alto rendimiento, madurez en arquitectura de capas y soporte nativo en Linux. |
| **ORM** | Entity Framework Core | 8.x | Productividad en acceso a datos, soporte de Query Filters para multitenancy y migraciones code-first. |
| **Base de datos** | SQL Server | 2022 | Motor relacional transaccional, soporte de datos JSON y capacidades de escalabilidad empresarial. |
| **Frontend** | Angular | 20.x | Estructura modular estricta, desacoplamiento, tipeado fuerte con TypeScript y manejo eficiente de WBS/EDT. |
| **Proxy Web / Server** | Nginx | Estable | Servidor de archivos estáticos para la SPA de Angular y reverse proxy hacia la API .NET sobre HTTP. |
| **Contenedores** | Docker Compose | v2 | Orquestación liviana, reproducible y aislada de los servicios para entorno único/servidor Ubuntu. |

## 2. Librerias clave

* **Backend (.NET 10 API / C#):**
  * **Autenticación y Seguridad:** `Microsoft.AspNetCore.Authentication.JwtBearer` para la validación de tokens JWT y `Microsoft.AspNetCore.Identity.EntityFrameworkCore` para el manejo de usuarios y roles (RBAC).
  * **Validación de Payloads:** `FluentValidation.AspNetCore` para ejecutar validaciones declarativas de DTOs en las peticiones HTTP.
  * **Mapeo de Objetos:** `AutoMapper` para la transformación inmutable entre Entidades de Dominio y DTOs (ej. `PublicApuDTO` sanitizado).
  * **Documentación de API:** `Swashbuckle.AspNetCore` (Swagger / OpenAPI v3) para la exposición de contratos que consume el Spec Kit.
  * **Logging:** `Serilog.AspNetCore` con sumideros (sinks) a consola y archivo JSON para trazabilidad de eventos y auditoría.
  * **Testing:** `xUnit` como framework de ejecución, `Moq` para el aislamiento de dependencias y `FluentAssertions` para aserciones legibles en pruebas unitarias backend.
* **Frontend (Angular 20 / TypeScript):**
  * **Estado y Reactividad:** `RxJS` para la gestión de flujos asíncronos y eventos en tiempo real.
  * **UI y Tablas Jerárquicas:** `Angular Material` / `PrimeNG` para vistas tabulares de WBS, componentes de diálogos y controles de formulación.
  * **Http Client:** `@angular/common/http` con interceptores para inyección de Token Bearer y captura global de errores.
  * **Firma en UI:** `signature_pad` (o librería equivalente) para captura de firma manuscrita en frontend como evidencia temporal de aprobación.

## 3. Herramientas de calidad

* **Linter y Formato de Código:**
  * **Backend:** `.editorconfig` con reglas de estilo de Microsoft .NET, enriquecido con `Roslyn Analyzers` (`Microsoft.CodeAnalysis.NetAnalyzers`) obligatorios en el build.
  * **Frontend:** `ESLint` con las reglas recomendadas para Angular y TypeScript, junto con `Prettier` para formateo automatizado de código.
* **Estrategia de Pruebas (TDD / SDD):**
  * **Pruebas Unitarias Backend:** Cobertura de código ($\ge 80\%$) sobre el motor de cálculo de APUs, jerarquía WBS, evaluación de topes presupuestales, reglas en cascada del estado `Falta_informacion` y privacidad por empresa.

## 4. Politica de versiones

* **Semantic Versioning (SemVer - `MAJOR.MINOR.PATCH`):**
  * `MAJOR`: Cambios rompedores en la estructura de la base de datos o contratos de la API REST.
  * `MINOR`: Inclusión de nuevas funcionalidades compatibles (ej. nuevos tipos de insumo o filtros de búsqueda).
  * `PATCH`: Correcciones de errores en reglas de cálculo, parches de seguridad o refactorizaciones internas.
* **Gestión de Dependencias:**
  * Uso de versiones LTS (Long Term Support) para frameworks base (.NET 10, Ubuntu 24.04).
  * Bloqueo estricto de dependencias mediante `package-lock.json` (npm) y archivos `.csproj` con versiones fijas para evitar inconsistencias en builds automáticos de IA.
  * Actualización periódica de librerías mediante parches de seguridad revisados con pruebas automatizadas.

## 5. Herramientas de Copilot y Spec Kit

* **Estructura de Especificación (`.github/spec/`):**
  * `.github/spec/SPEC.md`: Fuente única de verdad (Single Source of Truth) conteniendo los contratos de interfaz, reglas de negocio y esquemas relacionales.
  * `.github/spec/clarify.md`: Bitácora de dudas resolutivas sobre reglas del dominio (ej. especificación del hash de firma auditada o algoritmos de sanitización DTO).
* **Comandos Core del Flujo Spec Kit:**
  * `speckit.init`: Inicializa la estructura del proyecto y los archivos de contexto técnico en el repositorio.
  * `speckit.clarify`: Ejecuta consultas guiadas para resolver ambigüedades técnicas antes de codificar la lógica de negocio.
  * `speckit.plan`: Genera el plan de arquitectura técnica, tareas de backend y diseño de DTOs/Entidades basados en la especificación.
  * `speckit.tasks`: Desglosa las tareas de desarrollo por capas (Domain -> Infrastructure -> Application -> API -> UI) para ejecución asistida.
  * `speckit.implement`: Invocación del agente de IA (GitHub Copilot) para generar código, controladores y pruebas alineados estrictamente con el contrato de la especificación.
* **Herramientas Opcionales / Complementarias:**
  * `GitHub Copilot CLI`: Para asistencia en comandos de terminal, orquestación de Docker Compose y scripting de migraciones.
  * `OpenAPI Generator / Swagger CLI`: Para la generación automática de clientes HTTP de Angular a partir de la especificación `.json`/`.yaml` expuesta por .NET.