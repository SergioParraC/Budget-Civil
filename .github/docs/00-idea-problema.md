# Idea y problema a resolver

## 1. Resumen de la idea
Desarrollar una aplicación web de gestión de presupuestos de obra civil para empresas del sector construcción. El sistema debe permitir la estructuración de Análisis de Precios Unitarios (APUs) e insumos, garantizar un registro de trazabilidad y firma de autoría sobre cada modificación, y proveer una base de datos de consulta compartida para comparar la composición técnica de APUs (rendimientos y consumos de insumos) entre proyectos decidiendo por cada empresa si esta información es ´pública. El desarrollo se ejecuta bajo la metodología Spec-Driven Development (SDD).

## 2. Problema actual
El proceso actual basado en hojas de cálculo (Excel) genera las siguientes limitantes técnicas en el sistema:
* **Falta de Trazabilidad:** Inexistencia de registros de auditoría sobre modificaciones de los APUs. No hay asociación de firma ni responsabilidad de usuario.
* **Aislamiento de Datos de Rendimiento:** La información de rendimientos de mano de obra y equipo queda aislada en archivos locales, impidiendo la reutilización estructurada y el contraste de composiciones técnicas entre proyectos.

## 3. Usuarios y actores
* **Gerente de proyectos (`Role: ADMIN`):**
  * Gestión de cuenta corporativa y usuarios de la empresa.
  * Configuración de parámetros globales del sistema.
  * Configuración de divisas por proyecto.
  * Creación y gestión de proyectos (WBS / Capítulos / Ítems), APUs y materiales.
  * Control de visibilidad/privacidad de los poryectos, APUs y materiales (publicar en base compartida o mantener privado).
  * Establecer usuario firmador de los APUs o si cada usuario firma por cada APU y/o presupuesto.
* **Gestor de Proyecto / Presupuestador (`Role: PROJECT_MANAGER` / `Role: ESTIMATOR`):**
  * Creación y gestión de proyectos y presupuestos (WBS / Capítulos / Ítems).
  * Creación, edición y firmado de APUs e insumos.
  * Configuración de divisas por proyecto.
  * Consulta de la base de APUs de referencia.
  * Actualización del estado de cotización por ítem/insumo.
* **Usuario Lector / Auditor (`Role: AUDITOR` / `Role: READ_ONLY`):**
  * Lectura de resúmenes de presupuesto y cotización.
  * Inspección de la bitácora auditable de cambios y verificación de firmas registradas.
  * Visualización de alertas por superación de topes presupuestales.

## 4. Objetivo del producto
Proveer un backend y frontend desacoplados que automaticen el cálculo de presupuestos de obra estructurados (Estructura de Desglose del Trabajo - EDT/WBS), eliminen la manipulación manual de fórmulas mediante motores de cálculo validados, registren el 100 % de los cambios mediante eventos auditables con firma de autoría, y expongan APIs para la consulta anonimizada de composiciones técnicas de APUs.

## 5. Alcance MVP
* **Gestión de Proyectos y Presupuestos:**
  * Estructura jerárquica: Proyecto -> Capítulos o subcapítulos (se debe de definir en BD el rango que pertenece) -> Ítems/Actividades -> APU (Materiales, Mano de Obra, Equipos, Subcontratos).
  * Parametrización de divisa por proyecto.
  * Estados de cotización por ítem: `POR_COTIZAR`, `FALTA_INFORMACION`, `COTIZADO`, tener en cuenta que los estados pueden aumentar en el tiempo.
  * Motor de alertas activas para topes presupuestales configurados o proyecto.
  * Generación de resúmenes de cotización y exportación de presupuestos.
* **Módulo de Auditoría y Trazabilidad (Firma de Autoría):**
  * Registro inmutable (log de auditoría) de cada acción se realice en el presupuesto como en los APUs y cambios en los costos de materiales.
  * Vinculación obligatoria del ID de usuario, timestamp y firma registrada en cada cambio, según el elemento modificado.
* **Base de Datos Compartida de Consulta APU:**
  * Repositorio de consulta colaborativa para composición técnica (consumos y rendimientos, no precios).
  * Endpoints para buscar y comparar la composición de un APU propio frente a las referencias de la base de datos.
* **Módulo de Configuración de Privacidad:**
  * Flag/Toggle de privacidad a nivel de APU o Proyecto: `Is_Public` (`true`/`false`) para autorizar su replicación en la base de datos compartida.
* **Pruebas y Verificación:**
  * Cobertura de pruebas unitarias sobre el motor de cálculo de APUs y pruebas de integración (caja negra) para las APIs REST.

## 6. Fuera de alcance
* **Cálculo Autónomo por IA:** El sistema no debe asignar precios o cantidades de forma automática sin la interacción del usuario. La IA del sistema es puramente prescriptiva y de consulta.
* **Scraping o Comparación de Precios Comerciales en Tiempo Real:** La base de datos compartida solo almacena y compara *composición técnica* (rendimientos/cantidades), no valores monetarios de proveedores.
* **Módulos de Ejecución de Obra (Post-planeación):** No implementar módulos de inventario de almacén, control de entrada de materiales en obra, facturación ni gestión de compras.
* **Visores 3D / Integración CAD/BIM:** No procesar archivos IFC, DWG ni visores tridimensionales geométricos.

## 7. Criterios de exito
* **Automatización del Motor de Cálculo:** Cálculo en tiempo real de costos directos (APU) e indirectos (AIU) sin inconsistencias de redondeo ni fórmulas rotas.
* **Trazabilidad Inmutable:** 100 % de las transacciones de modificación registran un evento de auditoría asociado a un `UserId` y un hash/registro de firma.
* **Validación de Integridad:** El sistema debe rechazar la consolidación de un presupuesto si existen insumos en estado `FALTA_INFORMACION` o sin cotización asignada.

## 8. Supuestos y riesgos de negocio
* **Usabilidad e Interfaz (UX):** La carga de APUs y creación de WBS/EDT debe requerir la menor cantidad de clics posible para no perder la agilidad percibida en hojas de cálculo.
* **Persistencia Unificada y Visibilidad (Multitenant):** Proyectos, APUs, Materiales e Insumos deben convivir en el mismo esquema de tablas relacionales (modelo unificado). La condición de visibilidad (pública o privada) estará determinada por un estado/flag en la misma entidad.
* **Privacidad y Sanitización de DTOs en Consultas Públicas:** Cuando se consulte un APU en la base de datos compartida, el backend debe omitir/sanitizar en los Data Transfer Objects (DTOs) la información del `CompanyId`, nombres de proyectos específicos, firmas y metadatos sensibles de la empresa creadora.
* **Seguridad y Control de Accesos:** Aislamiento estricto multitenant (multi-empresa) para evitar fugas de información entre organizaciones cuando los APUs estén marcados como privados.

## 9. Preguntas abiertas y estado de clarificacion
* **Mecanismo de Firma de Autoría (Resuelto):** Se implementa firma auditada HMAC-SHA256 generada en backend sobre payload canónico (`project_id`, `user_id`, `approval_stage`, `timestamp_utc`).
* **Lógica de Filtrado y Mapping DTO en la API (Resuelto):** Se mantiene Query Filter global por `CompanyId` y se habilita un bypass controlado solo para `GET /api/v1/public-apus`, con validación `Project.Is_Public = true AND Apu.Is_Public = true` y salida `PublicApuDTO` por whitelist.
* **Estructura de Importación desde Excel (Abierto):** Pendiente definir esquemas JSON/CSV y reglas de parseo estandarizadas para mapear matrices de presupuestos tradicionales hacia la estructura relacional del sistema.