# Viabilidad basica

## 1. Viabilidad tecnica

* **Stack Tecnológico:Frontend:**
  * Framework basado en componentes (TypeScript/Angular) orientado a la gestión de datos jerárquicos (WBS/EDT).  
  * Backend: API REST desacoplada sobre C# (.NET) para el motor de cálculo en tiempo real y lógica de negocio.  
  * Persistencia: Base de datos relacional (SQL Server) con esquema unificado multitenant para Proyectos, APUs e Insumos.  
  H* Seguridad y Auditoría: Autenticación por JWT/OAuth2 con RBAC (Control de Acceso Basado en Roles).
* **Infraestructura:** Servicios local de servidor en Linux, con despliegue de contenedores (Docker) y base de datos administrada.
* **Metodología de Desarrollo:** Spec-Driven Development (SDD) con enfoque Spec-anchored. Uso de GitHub Copilot y Spec Kit (.github/spec/) para acelerar la codificación, generación de contratos API y cobertura de pruebas unitarias en backend.

## 2. Viabilidad operativa

Describe como operara la solucion en el dia a dia.
Incluye usuarios responsables, soporte y flujo de trabajo.

* **Flujo de Trabajo del Sistema:** 
  1. El Administrador parametriza la empresa y permisos de usuarios (ADMIN, ESTIMATOR, AUDITOR).  
  2. El Presupuestador crea el proyecto, define la WBS/EDT (Capítulos/Ítems) e ingresa/importa los APUs e insumos.  
  3. El motor de cálculo procesa automáticamente el presupuesto en tiempo real sin formulas expuestas ni riesgo de corrupción de datos.  
  4. Cada cambio en un valor o rendimiento activa el middleware de trazabilidad, registrando un log inmutable asociado al ID de usuario y firma registrada.  
  5. Para la consulta de referencia, el backend expone un endpoint que filtra la tabla unificada por Is_Public = true y utiliza DTOs sanitizados para omitir datos de autoría/empresa.  
* **Soporte y Mantenimiento:** Trazabilidad de fallos mediante logs centralizados.

## 3. Viabilidad economica inicial

* **Costos de Desarrollo:** 
  * Reducción de costos de horas/hombre de desarrollo mediante el uso de Spec Kit y GitHub Copilot para la generación automatizada de boilerplate, DTOs, mappers y pruebas unitarias.
* **Costos de Infraestructura (Nube - Fase MVP):**
  * Servidor de aplicaciones / Contenedores (Instancia básica / Serverless): USD 20 - 50 / mes.
  * Base de datos relacional administrada (vCPU y almacenamiento básico): USD 25 - 60 / mes.
  * Dominio y almacenamiento de logs: USD 5 - 15 / mes.
  * Costo mensual estimado MVP: USD 50 - 125 / mes.

## 4. Restricciones y dependencias

* **Restricciones:**
  * Modelo de Datos Unificado: Obligatoriedad de mantener Proyectos, APUs e Insumos en la misma estructura de tablas relacionales, manejando la privacidad a nivel de API/DTOs y no por bases de datos separadas.
  * Sin Auto-asignación por IA: La IA no debe decidir los valores ni rendimientos de los presupuestos; su alcance es prescriptivo, de soporte y consulta de referencias.
* **Dependencias:**
  * Definición técnica del flujo de firma en frontend (Signature Pad) y validación de aprobación en backend.
  * Definición de los esquemas de importación (CSV) para migrar matrices desde Excel. 

## 5. Riesgos y mitigaciones

* Riesgo 1: Fuga de información corporativa al consultar APUs públicos.
  * Mitigación: Implementar pruebas unitarias en backend sobre la capa de DTOs sanitizados para garantizar que el CompanyId y los nombres de proyectos nunca se envíen en la respuesta JSON de la API compartida.
* Riesgo 2: Drift/desalineación de la API frente a las especificaciones escritas (Deuda técnica por IA).
  * Mitigación: Adoptar el enfoque Spec-anchored de SDD, donde las especificaciones OpenAPI/Markdown en .github/spec/ se validan mediante pipelines automatizados de CI/CD antes de permitir merges al código fuente.  
* Riesgo 3: Bajo desempeño en consultas complejas sobre la tabla unificada de APUs.
  * Mitigación: Creación de índices compuestos sobre la columna Is_Public y claves de insumos para acelerar las búsquedas en la base compartida.

## 6. Decision

* Estado Actual: GO CON CONDICIONES
* Condiciones para pasar a Sprint 1 de Desarrollo:
  1. Implementar en código la aprobación con firma única desde Signature Pad en frontend y validación de autorización en backend.  
  2. Implementar y validar por pruebas unitarias backend el diseño de `PublicApuDTO` por whitelist para exposición de la base compartida sobre la tabla unificada.