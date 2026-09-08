# Tareas Iniciales de Implementacion

## Objetivo
Implementar en codigo los ajustes aprobados en clarificaciones 1..13 respetando Clean Architecture y SDD.

## Fase A - Domain
1. Definir value objects y reglas para Resource completeness.
2. Implementar servicio de evaluacion de estados (Resource -> Apu -> Project) con transaccion unica.
3. Implementar regla Matriz de un nivel (sin Matriz -> Matriz).
4. Implementar policy de transicion a FINALIZADO con segundo rol parametrizable.

## Fase B - Application
1. Definir comandos/casos de uso para:
   - Crear/editar Resource.
   - Crear/editar Apu.
   - Vincular ApuResource.
   - Registrar aprobaciones de proyecto.
2. Implementar validadores FluentValidation por endpoint canonico.
3. Implementar mapeos a PublicApuDTO por whitelist.

## Fase C - Infrastructure
1. Ajustar entidades EF Core:
   - WbsChapter y WbsItem completos.
   - ProjectConfiguration.second_approver_role_id.
   - Project.currency (ISO 4217 3).
2. Configurar Query Filters por CompanyId.
3. Implementar bypass controlado para consulta publica.
4. Implementar servicio HMAC-SHA256 para firmas.
5. Actualizar migraciones y seed de roles (ADMIN, ESTIMATOR, AUDITOR).

## Fase D - API
1. Exponer rutas canonicas:
   - POST /api/v1/users
   - POST|PUT /api/v1/apus
   - POST|PUT /api/v1/resources
   - POST|PUT /api/v1/apu-resources
   - GET /api/v1/public-apus
   - POST /api/v1/projects/{id}/approvals
2. Garantizar que company_id y signature_token no se reciban donde no corresponde.
3. Garantizar salida sanitizada de PublicApuDTO.

## Fase E - Pruebas
1. Unit tests:
   - test_matrix_single_level_expansion
   - test_missing_info_cascade_transactional
   - test_dual_approval_freeze
2. Integration tests:
   - test_public_apu_visibility_and_rule
   - test_public_apu_dto_sanitization
   - aislamiento CompanyId en endpoints privados

## Definition of Done
1. Todas las pruebas nuevas pasan.
2. No fuga de datos en DTO publico.
3. No se puede finalizar proyecto sin doble aprobacion valida.
4. No se permite Matriz en anidamiento ni como item directo WBS.
5. Cascada de estados funciona en ida y vuelta dentro de una transaccion.
