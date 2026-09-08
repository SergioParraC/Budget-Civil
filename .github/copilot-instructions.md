# Instrucciones de Copilot para este proyecto

## Objetivo
Mantener un flujo de trabajo guiado por especificaciones con alcance controlado y alta trazabilidad entre requerimientos, arquitectura, tareas e implementacion.

## Fuentes de contexto por prioridad
1. specs/<feature>/spec.md, plan.md, tasks.md y artefactos asociados.
2. .github/docs/02-requerimientos.md y .github/docs/08-especificacion-inicial-spec-kit.md.
3. .github/docs/03-arquitectura-general.md y .github/docs/05-modelo-datos.md.
4. Codigo existente y pruebas del repositorio.

## Reglas de alcance
- No agregar funcionalidades fuera del alcance activo del feature.
- No introducir roadmap futuro en artefactos de implementacion actual.
- Si falta informacion, marcar NEEDS CLARIFICATION y solicitar decision explicita.

## Reglas de implementacion
- Seguir arquitectura por capas definida para backend.
- Mantener calculos de negocio en Domain y Application.
- Usar Infrastructure para acceso a datos con EF Core y LINQ.
- Mantener seguridad por compania y roles en todos los endpoints.

## Reglas de calidad
- Todo cambio relevante debe incluir pruebas acorde al nivel: unitarias, integracion o contrato.
- No cerrar un feature sin revisar convergencia contra spec, plan y tasks.
- Priorizar cambios pequenos y verificables.

## Ciclo operativo con Spec Kit
1. speckit.specify
2. speckit.clarify
3. speckit.plan
4. speckit.tasks
5. speckit.analyze
6. speckit.implement
7. speckit.converge

## Uso de agente Explore
- Usar Explore para analisis rapido de codigo, busquedas y validaciones de contexto.
- Evitar decisiones de arquitectura sin respaldo en requerimientos y especificaciones.
