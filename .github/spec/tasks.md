# Tareas Iniciales de Implementacion

## Objetivo
Implementar en codigo los ajustes aprobados en clarificaciones 1..13 respetando Clean Architecture y SDD.

## Fase A - Domain
1. Definir value objects y reglas para Resource completeness.
2. Implementar servicio de evaluacion de estados (Resource -> Apu -> Project) con transaccion unica.
3. Implementar regla Matriz de un nivel (sin Matriz -> Matriz).
4. Implementar policy de transicion a FINALIZADO con aprobacion unica y validacion por compania.

## Fase B - Application
1. Definir comandos/casos de uso para:
   - Crear/editar Resource.
   - Crear/editar Apu.
   - Vincular ApuResource.
   - Registrar aprobacion unica de proyecto.
2. Implementar validadores FluentValidation por endpoint canonico.
3. Implementar mapeos a PublicApuDTO por whitelist.

## Fase C - Infrastructure
1. Ajustar entidades EF Core:
   - WbsChapter y WbsItem completos.
   - Remover campos de token de firma en ProjectConfiguration.
   - Project.currency (ISO 4217 3).
2. Configurar control de acceso por CompanyId para registros privados.
3. Implementar validacion de visibilidad publica por regla AND (`Project.is_public` y `Apu.is_public`).
4. Actualizar migraciones y seed de roles (ADMIN, ESTIMATOR, AUDITOR).

## Fase D - API
1. Exponer rutas canonicas:
   - POST /api/v1/users
   - POST|PUT /api/v1/apus
   - POST|PUT /api/v1/resources
   - POST|PUT /api/v1/apu-resources
   - GET /api/v1/public-apus
   - POST /api/v1/projects/{id}/approve
2. Garantizar que company_id no se reciba donde no corresponde.
3. Garantizar salida sanitizada de PublicApuDTO.
4. Aceptar `signature_pad_data` solo como evidencia temporal para la accion de aprobacion.

## Fase E - Pruebas
1. Unit tests:
   - tests/backend/unit/matrix_single_level_expansion_tests.cs
   - tests/backend/unit/missing_info_cascade_transactional_tests.cs
   - tests/backend/unit/single_approval_freeze_tests.cs
   - tests/backend/unit/apu_calculation_tests.cs
   - tests/backend/unit/budget_calculation_tests.cs
   - tests/backend/unit/public_dto_sanitization_tests.cs
   - tests/backend/unit/company_privacy_access_tests.cs

## Definition of Done
1. Todas las pruebas nuevas pasan.
2. No fuga de datos en DTO publico.
3. No se puede finalizar proyecto sin aprobacion valida y pertenencia a la compania propietaria.
4. No se permite Matriz en anidamiento ni como item directo WBS.
5. Cascada de estados funciona en ida y vuelta dentro de una transaccion.
