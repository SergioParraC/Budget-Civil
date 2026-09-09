# Arquitectura general

## 1. Resumen arquitectonico

Plataforma web de gestión de presupuestos de obra civil construida bajo Clean Architecture (Arquitectura Limpia) con desacoplamiento total entre frontend y backend. El cliente es una aplicación SPA en Angular que interactúa mediante APIs HTTP/JSON con un servidor backend en ASP.NET Core Web API (.NET 10). La persistencia se gestiona en SQL Server 2022 utilizando Entity Framework Core con un esquema relacional unificado y privacidad por empresa (`CompanyId`). El despliegue de producción se realiza mediante Docker Compose sobre Ubuntu 24.04 LTS, utilizando Nginx como proxy inverso.

## 2. Capas y responsabilidades

┌─────────────────────────────────────────────────────────┐
│              Presentation Layer (Angular SPA)            │
└────────────────────────────┬────────────────────────────┘
                             │ HTTP / JSON
┌────────────────────────────▼────────────────────────────┐
│              API Layer (ASP.NET Core Web API)           │
│  Controllers | JWT Middleware | Request/Response DTOs   │
└────────────────────────────┬────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────┐
│            Application Layer (Use Cases / SDD)          │
│  Services | FluentValidation | AutoMapper | DTO Mappers │
└────────────────────────────┬────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────┐
│              Domain Layer (Core Rules & Entities)       │
│  Entities | Enums | Domain Rules | Repository Contracts   │
└────────────────────────────▲────────────────────────────┘
                             │ Implementation
┌────────────────────────────┴────────────────────────────┐
│          Infrastructure Layer (Data & Security)         │
│  EF Core Repositories | AppDbContext | Identity Store   │
└────────────────────────────┬────────────────────────────┘
                             │ SQL / TDS
┌────────────────────────────▼────────────────────────────┐
│                  Database (SQL Server)                  │
└─────────────────────────────────────────────────────────┘

* Presentation Layer (Angular SPA):
  * Contiene: Componentes de UI, vistas jerárquicas (WBS/EDT), servicios HTTP cliente, guardias de ruta por rol.
  * No contiene: Lógica de cálculo de APUs, reglas de negocio ni acceso a base de datos.
* API Layer (ASP.NET Core):
  * Contiene: Controladores REST, Middlewares (JWT Authentication, Error Handling), DTOs de entrada/salida.
  * No contiene: Consultas directas a BD ni lógica de negocio compleja.
* Application Layer (Use Cases / Services):
  * Contiene: Servicios de aplicación, orquestación de casos de uso, mapeo de DTOs (AutoMapper), validación de payloads (FluentValidation).
  * No contiene: Entidades de EF Core expuestas ni código dependiente de la infraestructura de almacenamiento.
* Domain Layer (Core Business Logic):
  * Contiene: Entidades de dominio, Enums, interfaces de repositorios, reglas de cálculo inmutables (AIU, APU, cascada transaccional de estados, expansión de Matriz a un nivel).
  * No contiene: Dependencias de frameworks web, librerías ORM ni código de base de datos.
* Infrastructure Layer (Persistence & External Services):
  * Contiene: Implementación de repositorios (EF Core), AppDbContext, ASP.NET Identity, integración con almacenamiento de archivos/logs.
  * No contiene: Lógica de decisión o control de la aplicación.

## 3. Diagramas base

Para la representación visual y contratos arquitectónicos, los diagramas de arquitectura se ubican en la carpeta de documentación del repositorio:
* Diagrama de Componentes (PlantUML): docs/architecture/components.puml
* Diagrama de Despliegue (PlantUML): docs/architecture/deployment.puml
* Diagrama de Dominio (Referencial, dbdiagram.io): docs/architecture/domain-model.dbdiagram
´´´text
Nota de arquitectura sobre el Modelo de Dominio: El archivo domain-model.puml sirve como referencia histórica inicial. Durante la fase de desarrollo, el modelo de datos real se ajustará en el código mediante el archivo de especificidad funcional (incluyendo la entidad Insumo unificada, el discriminador TipoInsumo, las Matrices, la regla en cascada del estado Falta_informacion y los DTOs de sanitización Is_Public).
´´´

## 4. Integraciones

* Autenticación e Identidad: ASP.NET Core Identity + JWT Token Bearer para el manejo de sesiones de usuario.
* Firma en Frontend: Captura de firma manuscrita mediante Signature Pad para acciones de aprobación. El backend registra el evento `SIGN`.
* Servicio de Proxy Inverso (Nginx): Enrutamiento de peticiones HTTP (Puerto 80) y despacho de estáticos de Angular.
* Motor de Pruebas: xUnit / Moq / FluentAssertions en backend para garantizar cobertura de reglas de cálculo y privacidad por empresa.

## 5. Seguridad

* Autenticación: Tokens JWT firmados enviados en la cabecera HTTP Authorization: Bearer <token>.
* Autorización: Control de Acceso Basado en Roles (RBAC) a nivel de endpoints y servicios (`ADMIN`, `ESTIMATOR`, `AUDITOR`) con aprobación única de cierre.
* Privacidad por Empresa: El acceso a registros privados exige coincidencia de `CompanyId` entre el usuario autenticado y la entidad objetivo.
* Sanitización Pública: La consulta de la base compartida usa exclusivamente `PublicApuDTO` por whitelist y solo publica registros que cumplan `Project.Is_Public = true` y `Apu.Is_Public = true`.
* Transición a `FINALIZADO`: Las invariantes de aprobación única y ausencia de `FALTA_INFORMACION` se validan en Domain/Application Layer.

## 6. Despliegue

* Entorno Target: Servidor dedicado / VPS con Ubuntu 24.04 LTS.
* Orquestación: Docker Compose ejecutando tres contenedores principales:
  1. `web`: Contenedor Nginx que sirve la SPA compilada de Angular y actúa como reverse proxy.
  2. `api`: Contenedor `.NET 10 Web API` expuesto en el puerto interno 8080.
  3. `sqlserver`: Contenedor de `SQL Server 2022 (Linux)` mapeado a un volumen persistente `sqlserver-data`.

## 7. Observabilidad

* **Logging:** Logs estructurados (formato JSON via Serilog) registrando eventos HTTP, excepciones no controladas y transacciones de auditoría/firma.
* **Trazas de Auditoría:** Registro obligatorio en tabla de auditoría para operaciones de creación/modificación de APUs e Insumos (guardando `UserId`, `Timestamp` y la Firma Registrada).
* **Métricas y Health Checks:** Endpoints `/health`  para la supervisión del estado del contenedor API y el estado de la conexión a SQL Server.

## 8. Riesgos tecnicos

* **Riesgo 1:** Degradación del modelo por código generado por IA (Drift).
  * Mitigación: Estricta separación de capas (Clean Architecture). La IA no debe importar `AppDbContext` o entidades de EF Core dentro de los controladores de la capa API.

* **Riesgo 2:** Inconsistencias en el modelo de dominio frente a diagramas legados.
 * Mitigación: Se establece que la fuente de verdad del modelo de datos son los archivos de requerimientos y las especificaciones `.github/spec/`, estando el diagrama `domain-model.puml` sujeto a refactorización durante las iteraciones del desarrollo.