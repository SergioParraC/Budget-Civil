<!--
Sync Impact Report
- Version change: 1.1.0 -> 1.2.0
- Added sections:
  - Articulos Normativos
  - Gobernanza de Cambios de Esquema de Datos
  - Politica de Documentacion de Endpoints para Frontend
  - Checklist de Cumplimiento por Feature
  - Governance
- Removed sections:
  - Template placeholders no operativos
- Templates requiring updates:
	- .specify/templates/plan-template.md: ✅ updated (incluye gates operativos y tabla de evidencia obligatoria para Constitution Check)
	- .specify/templates/spec-template.md: ✅ updated (incluye matriz de cobertura de implementacion y prohibicion de source in-memory para alcance persistente)
	- .specify/templates/tasks-template.md: ✅ updated (incluye gates de convergencia y evidencia SQL obligatoria)
-->

# Budget Civil Constitution

## Core Principles

### Articulos Normativos

### Articulo 1. Jerarquia de fuentes de verdad
Las decisiones tecnicas y funcionales MUST seguir este orden de prioridad:
1. `.github/spec/SPEC.md`
2. `.github/spec/clarify.md`
3. `.github/spec/tasks.md`
4. `.github/docs/02-requerimientos.md`
5. `.github/docs/03-arquitectura-general.md`
6. `.github/docs/04-stack-tecnologico.md`
7. `.github/docs/05-modelo-datos.md`
8. `.github/docs/06-flujos-casos-de-uso.md`
9. `.github/docs/09-operacion-copilot-spec-kit.md`

Regla de aplicacion:
- Si dos artefactos entran en conflicto, prevalece el de mayor prioridad.
- Ningun PR puede marcarse como listo sin citar explicitamente las fuentes usadas.

Criterio medible de revision tecnica:
- 100% de PRs de feature incluyen seccion "Fuentes aplicadas" con enlaces a archivos y secciones usadas.

### Articulo 2. Gobernanza de alcance activo
Todo trabajo MUST limitarse al feature activo y a sus artefactos de spec, plan y tasks.

Regla de aplicacion:
- Cualquier cambio fuera del alcance activo se clasifica como "out-of-scope".
- Un cambio out-of-scope solo puede ejecutarse con autorizacion explicita del usuario
  responsable del proyecto.
- Si se aprueba, se MUST actualizar la documentacion afectada antes de cerrar el feature.

Criterio medible de revision tecnica:
- 0 cambios out-of-scope fusionados sin evidencia de autorizacion en el historial del PR.

### Articulo 3. Esquema de datos no canonico por si solo
La base de datos y su esquema actual MUST NOT considerarse fuente de verdad absoluta.

Regla de aplicacion:
- Si el esquema implementado difiere de la documentacion prioritaria, se considera drift.
- Todo cambio de esquema aprobado MUST reflejarse en los documentos de arquitectura,
  modelo de datos y artefactos de feature impactados.

Criterio medible de revision tecnica:
- 100% de migraciones estructurales tienen actualizacion documental en el mismo ciclo.

### Articulo 4. Cambios estructurales de datos permitidos con control
Se permiten cambios estructurales de datos, incluyendo modificacion o reemplazo de
tablas, cuando un requerimiento aprobado lo justifique.

Regla de aplicacion:
- La justificacion MUST mapearse al menos a un FR y un UC vigentes.
- Se MUST entregar propuesta tecnica de migracion e impacto antes de implementar.

Criterio medible de revision tecnica:
- 100% de cambios estructurales incluyen matriz de impacto y estrategia de migracion.

### Articulo 5. Autorizacion obligatoria para cambios estructurales
Todo cambio estructural de datos MUST contar con autorizacion explicita del usuario
responsable del proyecto.

Regla de aplicacion:
- La autorizacion debe quedar registrada por escrito (issue, PR o acta).
- Sin autorizacion explicita, el cambio queda bloqueado aunque sea tecnicamente viable.

Criterio medible de revision tecnica:
- 0 migraciones estructurales aprobadas sin evidencia de autorizacion explicita.

### Articulo 6. Arquitectura full-stack obligatoria
La arquitectura definida para backend y frontend es no negociable en esta fase.

Backend:
- Clean Architecture obligatoria.
- Reglas de negocio e invariantes en Domain/Application.
- Infrastructure solo para persistencia e integraciones tecnicas.

Frontend:
- Separacion de responsabilidades obligatoria entre UI, estado y acceso a datos.
- Consumo de datos exclusivamente por contratos API documentados.
- Prohibido acoplar UI a detalles internos de persistencia del backend.

Criterio medible de revision tecnica:
- 100% de features nuevos pasan checklist de separacion por capas en backend y frontend.

### Articulo 7. Seguridad obligatoria en ambos frentes
La seguridad MUST aplicarse de forma consistente en backend y frontend.

Reglas obligatorias:
- JWT + RBAC en endpoints privados y acciones sensibles.
- Privacidad por `CompanyId` en endpoints privados.
- `CompanyId` derivado del contexto autenticado en backend.
- El cliente MUST NOT controlar `CompanyId` en operaciones sensibles.

Criterio medible de revision tecnica:
- 100% de endpoints privados verifican autenticacion, rol y aislamiento por compania.

### Articulo 8. Politica de contratos API backend/frontend
Ningun endpoint se considera terminado sin documentacion contractual consumible por
frontend.

Minimo obligatorio por endpoint:
- Metodo y ruta.
- Requerimiento de autenticacion.
- Roles permitidos.
- Request schema y response schema.
- Validaciones funcionales y de negocio.
- Errores esperados con codigos y condiciones.
- Ejemplos de request y response.

Criterio medible de revision tecnica:
- 100% de endpoints nuevos o modificados tienen contrato actualizado y revisado.

### Articulo 9. Estrategia de pruebas de esta fase
En esta fase, la estrategia de pruebas se define asi:
- La validacion funcional oficial se realiza mediante pruebas manuales de caja negra ejecutadas por QA sobre entorno de prueba.
- No se exige como criterio de cierre crear o ejecutar pruebas automatizadas en codigo (unitarias, integracion, contrato o E2E), salvo excepcion aprobada explicitamente por el usuario responsable del proyecto.
- Si se decide automatizar alguna prueba, se considera apoyo interno y no reemplaza la evidencia QA manual de caja negra.

Regla de aplicacion:
- Ningun cambio funcional se cierra sin evidencia de ejecucion QA manual de caja negra, incluyendo al menos:
	- casos probados,
	- resultado por caso,
	- version/commit evaluado,
	- fecha, ambiente y responsable QA.
- Toda excepcion que habilite pruebas automatizadas como criterio de cierre MUST tener autorizacion explicita y quedar registrada en el feature.

Criterio medible de revision tecnica:
- 100% de PRs funcionales incluyen evidencia QA manual de caja negra.
- 0 cierres de feature basados solo en pruebas automatizadas sin evidencia QA manual, salvo excepcion aprobada.

### Articulo 10. Trazabilidad extrema obligatoria
Cada feature MUST mantener trazabilidad bidireccional entre:
- Requerimientos (FR/NFR/BR)
- Casos de uso (UC)
- Endpoints/contratos
- Pruebas
- Tareas

Regla de aplicacion:
- Toda tarea tecnica debe poder rastrearse hasta un requerimiento y un caso de uso.
- Toda prueba debe indicar que requerimiento y endpoint valida.
- Toda entidad con datos persistentes MUST incluir una matriz de cobertura campo-a-implementacion con evidencia por cada campo obligatorio en:
	- modelo de dominio/aplicacion,
	- mapeo de persistencia,
	- migracion/columna/FK,
	- contrato API,
	- evidencia QA manual de caja negra o criterio de aceptacion verificable aprobado.
- Ninguna tarea de implementacion puede cerrarse si existe al menos un campo obligatorio sin evidencia de cobertura completa.

Criterio medible de revision tecnica:
- 100% de tareas y pruebas en el feature tienen referencia de trazabilidad valida.
- 100% de campos obligatorios definidos en spec/data-model tienen evidencia de implementacion y validacion asociada.

### Articulo 11. Enmiendas de constitucion
La constitucion SI puede modificarse siguiendo control de cambios formal.

Regla de aplicacion:
- Toda enmienda MUST registrar motivo, fecha, impacto y artefactos afectados.
- Si la enmienda impacta features en curso, se MUST revalidar con `analyze` y
  `converge`, y actualizar `plan` y `tasks`.
- Versionado semantico:
  - MAJOR: cambio incompatible de reglas constitucionales.
  - MINOR: nuevo articulo o expansion sustantiva.
  - PATCH: aclaraciones sin cambio de politica.

Criterio medible de revision tecnica:
- 100% de enmiendas incluyen registro completo y versionado correcto.

### Articulo 12. Paridad obligatoria entre spec y entregable
Cuando el spec/data-model define una entidad persistente, el runtime productivo MUST usar persistencia real como fuente de verdad.

Regla de aplicacion:
- Se prohibe marcar como "Done" una entidad persistente si su source of truth en runtime es un arreglo estatico/in-memory hardcodeado.
- Los endpoints de catalogos o entidades persistentes MUST leer desde repositorio/persistencia o proyecciones derivadas de ella.
- Excepciones temporales solo se permiten con autorizacion explicita del usuario responsable y fecha de retiro definida.

Criterio medible de revision tecnica:
- 0 endpoints productivos usan source in-memory para entidades declaradas como persistentes en el feature activo.

## Gobernanza de Cambios de Esquema de Datos

Flujo obligatorio para cambios estructurales (incluye alter, split, merge, replace
de tablas):
1. Identificar requerimiento aprobado que motiva el cambio (FR/UC/BR).
2. Presentar propuesta tecnica con:
	- alcance del cambio,
	- estrategia de migracion,
	- impacto en API/frontend,
	- impacto en pruebas,
	- plan de rollback.
3. Obtener autorizacion explicita del usuario responsable del proyecto.
4. Implementar cambio y migraciones.
5. Actualizar documentacion minima obligatoria:
	- `.github/spec/SPEC.md` (si aplica)
	- `.github/spec/tasks.md` (si aplica)
	- `.github/docs/03-arquitectura-general.md` (si aplica)
	- `.github/docs/05-modelo-datos.md`
	- `.github/docs/06-flujos-casos-de-uso.md` (si cambia comportamiento)
6. Ejecutar revalidacion de consistencia (analyze/converge) si afecta features activos.
7. Adjuntar evidencia de verificacion del esquema final en la BD objetivo (tablas, columnas NOT NULL/NULL, FK y acciones de borrado).

Criterios de rechazo automatico:
- Falta de autorizacion explicita.
- Falta de propuesta tecnica de migracion/impacto.
- Falta de actualizacion documental.
- Falta de evidencia SQL de esquema final alineado con spec/data-model.

## Politica de Documentacion de Endpoints para Frontend

Politica obligatoria:
- Ningun endpoint nuevo o modificado puede pasar a "Done" sin contrato actualizado.
- El contrato debe estar disponible antes de integracion frontend productiva.

Plantilla minima por endpoint:
1. Identificacion:
	- Metodo HTTP
	- Ruta
	- Version de API
2. Seguridad:
	- Auth requerida (si/no)
	- Roles permitidos
	- Reglas de `CompanyId`
3. Entrada:
	- Campos requeridos/opcionales
	- Tipos
	- Reglas de validacion
	- Ejemplo de request
4. Salida:
	- Campos response
	- Restricciones de sanitizacion
	- Ejemplo de response exitoso
5. Errores:
	- Codigo HTTP
	- Codigo funcional
	- Condicion de disparo
	- Ejemplo de payload de error
6. Trazabilidad:
	- FR/UC/BR relacionados
	- Tareas y pruebas asociadas

Definicion de terminado para endpoints:
- Codigo implementado.
- Contrato documentado.
- Validaciones implementadas.
- Evidencia QA manual de caja negra segun Articulo 9.
- Revisado por backend y frontend.

## Checklist de Cumplimiento por Feature

Este checklist MUST completarse y adjuntarse en cada PR de feature.

- [ ] C1 - Se citaron fuentes de verdad en orden de prioridad (Articulo 1).
- [ ] C2 - El cambio se mantiene en alcance activo o tiene autorizacion de
		out-of-scope registrada (Articulo 2).
- [ ] C3 - Si hubo cambio estructural de datos, se aplico el flujo de gobernanza y
		existe autorizacion explicita (Articulos 3, 4 y 5).
- [ ] C4 - Se mantuvo separacion de responsabilidades backend/frontend segun
		arquitectura obligatoria (Articulo 6).
- [ ] C5 - Se verifico JWT, RBAC y privacidad por `CompanyId`; el cliente no controla
		`CompanyId` en operaciones sensibles (Articulo 7).
- [ ] C6 - Todos los endpoints nuevos/modificados tienen contrato completo para
		frontend (Articulo 8).
- [ ] C7 - La evidencia QA manual de caja negra respeta la estrategia de esta fase (Articulo 9).
- [ ] C8 - Existe trazabilidad FR/UC/endpoint/pruebas/tareas (Articulo 10).
- [ ] C9 - Si hubo enmienda de constitucion, se registro motivo, impacto y versionado;
		si aplica, se corrio analyze/converge y se actualizo plan/tasks (Articulo 11).
- [ ] C10 - Existe matriz de cobertura campo-a-implementacion para todas las entidades persistentes del feature (Articulo 10).
- [ ] C11 - No hay source in-memory en runtime productivo para entidades persistentes del spec y se adjunta evidencia SQL de paridad (Articulo 12).

## Governance

1. Esta constitucion prevalece sobre practicas locales que entren en conflicto.
2. Ninguna excepcion es valida sin autorizacion explicita del usuario responsable
	del proyecto.
3. Toda excepcion aprobada debe documentar fecha, alcance, riesgo y plazo.
4. El incumplimiento de articulos bloquea aprobacion tecnica hasta su correccion.

**Version**: 1.2.0 | **Ratified**: 2026-09-20 | **Last Amended**: 2026-10-01
