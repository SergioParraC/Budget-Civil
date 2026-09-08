# SPEC - Budget Civil Backend

## 1. Proposito
Consolidar la especificacion funcional y tecnica para iniciar implementacion de codigo con enfoque Spec-Driven Development (SDD), sin ambiguedades abiertas en dominio, arquitectura y contratos API.

## 2. Fuente de verdad
Este documento consolida decisiones resueltas el 2026-09-08 y debe leerse junto con:
- .github/docs/02-requerimientos.md
- .github/docs/03-arquitectura-general.md
- .github/docs/05-modelo-datos.md
- .github/docs/06-flujos-casos-de-uso.md

## 3. Alcance activo
Incluye:
- Modelo unificado Resource/Insumo con catalogo ResourceType.
- Cascada de estado FALTA_INFORMACION (Resource -> Apu -> Project).
- Regla de Matriz reusable con expansion de un solo nivel.
- Flujo de doble aprobacion para FINALIZADO.
- Consulta publica sanitizada de APUs.

Excluye:
- Multi-moneda por item.
- Firma PKI asimetrica (se usa HMAC en MVP).
- Expansion recursiva de matrices en multiples niveles.

## 4. Lenguaje canonico
- Resource: entidad persistente de insumo.
- ApuResource: detalle de composicion APU-Resource.
- Matriz: Resource de tipo Matriz, reusable como sub-APU de un nivel.
- PublicApuDTO: DTO sanitizado de salida para base compartida.

## 5. Reglas de dominio aprobadas

### 5.1 Completitud de Resource
Un Resource se considera completo solo cuando tiene:
- description
- unit_price
- unit
- brand

Transiciones:
- Si falta al menos uno de esos campos: quotation_status = FALTA_INFORMACION.
- Si los 4 campos estan completos y pendiente cotizacion: quotation_status = POR_COTIZAR.
- COTIZADO se establece por flujo de cotizacion.

### 5.2 Mapeo de estados por entidad (enums separados)
- Resource.quotation_status: POR_COTIZAR | FALTA_INFORMACION | COTIZADO.
- Apu.status: COMPLETO | FALTA_INFORMACION.
- Project.status: POR_COTIZAR | FALTA_INFORMACION | COTIZADO | FINALIZADO.

Regla de evaluacion:
1. Si existe Apu en FALTA_INFORMACION -> Project = FALTA_INFORMACION.
2. Si no hay faltantes y existen pendientes de cotizacion -> Project = POR_COTIZAR.
3. Si todo esta completo y cotizado -> Project = COTIZADO.
4. FINALIZADO solo por doble aprobacion valida.

### 5.3 Cascada transaccional
La propagacion y reversion de estado debe ejecutarse de forma sincrona en una sola transaccion de dominio:
- Cambio en Resource -> reevaluar Apu(s) afectados -> reevaluar Project(s) afectados.

### 5.4 Regla Matriz
- Un Resource tipo Matriz no puede ser item directo de WBS.
- Solo se permite dentro de ApuResource.
- La expansion de Matriz es de un nivel.
- No se permite Matriz -> Matriz ni autorreferencia.

### 5.5 Visibilidad publica
Regla AND obligatoria:
- Solo se publica un APU si Project.is_public = true AND Apu.is_public = true.

### 5.6 Doble aprobacion y cierre
Para pasar a FINALIZADO:
1. No debe existir FALTA_INFORMACION en el proyecto.
2. Debe existir estimator_signature_token.
3. Debe existir director_signature_token (segunda firma).
4. El segundo firmante debe tener el rol configurado en ProjectConfiguration.second_approver_role_id.

### 5.7 Mecanismo de firma
- Algoritmo: HMAC-SHA256.
- Payload canonico: project_id, user_id, approval_stage, timestamp_utc.
- El cliente no envia el hash final; backend lo genera y persiste.

### 5.8 Politica de moneda
- Unica moneda activa por proyecto.
- Codigo ISO 4217 en mayuscula (3 caracteres).
- Sin conversion automatica en backend.

## 6. Arquitectura y seguridad
- Clean Architecture obligatoria.
- Invariantes de FINALIZADO y cascada en Domain/Application.
- Query Filters globales por CompanyId en consultas privadas.
- Unica excepcion: consulta publica de APUs con bypass controlado y DTO sanitizado.

## 7. Contrato API base (canonico)
- POST /api/v1/users
- POST|PUT /api/v1/apus
- POST|PUT /api/v1/apu-resources
- POST|PUT /api/v1/resources
- GET /api/v1/public-apus
- POST /api/v1/projects/{id}/approvals

Reglas clave:
- company_id no viaja en payload de users; se inyecta desde JWT.
- signature_token no se recibe para crear/editar APU.
- quotation_status de Resource no se asigna libremente al crear/editar.

## 8. PublicApuDTO (whitelist)
Campos permitidos:
- apu_id
- code
- description
- unit
- yield
- resources[]: resource_type, description, unit, consumption, waste_percentage

Campos prohibidos:
- company_id
- project_id
- created_by
- signature_token
- unit_price
- distributor
- contact_number
- shared_origin_resource_id

## 9. Datos y migraciones requeridas
Cambios minimos en modelo:
- Agregar tabla/definicion completa de WbsChapter.
- Agregar tabla/definicion completa de WbsItem.
- Agregar ProjectConfiguration.second_approver_role_id (FK Role).
- Ajustar Project.currency a politica ISO 4217 (3).

Seed minimo de roles:
- ADMIN
- ESTIMATOR
- AUDITOR

## 10. Pruebas minimas obligatorias
- test_matrix_single_level_expansion
- test_public_apu_visibility_and_rule
- test_public_apu_dto_sanitization
- test_missing_info_cascade_transactional
- test_dual_approval_freeze

## 11. Criterio de inicio de implementacion
Se puede iniciar codigo cuando:
1. Este SPEC y clarify.md esten aprobados.
2. Las tareas de .github/spec/tasks.md se acepten.
3. No queden ambiguedades activas en decisiones 1..13.
