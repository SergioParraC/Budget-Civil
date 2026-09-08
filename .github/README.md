# Documentacion base del proyecto

## Proposito
Esta carpeta define la base documental del proyecto para trabajar con estrategia Spec-Driven Development usando Spec Kit y Copilot.
La meta es mantener alcance concreto del MVP y evitar expansion prematura.

## Estructura propuesta
- .github/README.md
  Mapa general, orden de llenado y flujo de trabajo con Spec Kit.
- .github/copilot-instructions.md
  Reglas operativas para Copilot durante documentacion y desarrollo.
- .github/docs/00-idea-problema.md
  Idea principal, problema a resolver, alcance y fuera de alcance.
- .github/docs/01-viabilidad-basica.md
  Viabilidad tecnica, operativa y economica inicial.
- .github/docs/02-requerimientos.md
  Requerimientos funcionales y no funcionales con criterios de aceptacion.
- .github/docs/03-arquitectura-general.md
  Arquitectura por capas, despliegue, seguridad y observabilidad.
- .github/docs/04-stack-tecnologico.md
  Stack oficial, versiones base y decisiones tecnologicas.
- .github/docs/05-modelo-datos.md
  Entidades, relaciones, reglas de integridad y calculos.
- .github/docs/06-flujos-casos-de-uso.md
  Flujos criticos y casos de uso con precondiciones y errores.
- .github/docs/07-prototipo-interfaces.md
  Navegacion, pantallas clave y estados de UI.
- .github/docs/08-especificacion-inicial-spec-kit.md
  Especificacion base lista para alimentar speckit.specify.
- .github/docs/09-operacion-copilot-spec-kit.md
  Uso de comandos, prompts, agente Explore y ciclo de convergencia.
- .github/prompts/speckit-constitution.prompt.md
  Prompt sugerido para generar la constitucion del proyecto.
- .github/prompts/speckit-specify.prompt.md
  Prompt sugerido para crear specs de features sin salir del alcance.
- .github/prompts/speckit-plan.prompt.md
  Prompt sugerido para planes tecnicos consistentes con arquitectura.
- .github/prompts/speckit-tasks.prompt.md
  Prompt sugerido para desglose de tareas por feature.
- .github/prompts/speckit-converge.prompt.md
  Prompt sugerido para validar brechas y cerrar implementacion.

## Orden recomendado para completar documentacion
1. .github/docs/00-idea-problema.md
2. .github/docs/01-viabilidad-basica.md
3. .github/docs/02-requerimientos.md
4. .github/docs/03-arquitectura-general.md
5. .github/docs/04-stack-tecnologico.md
6. .github/docs/05-modelo-datos.md
7. .github/docs/06-flujos-casos-de-uso.md
8. .github/docs/07-prototipo-interfaces.md
9. .github/docs/08-especificacion-inicial-spec-kit.md
10. .github/docs/09-operacion-copilot-spec-kit.md

## Flujo recomendado con Spec Kit
1. Completar documentos base sin agregar alcance futuro.
2. Ejecutar speckit.constitution usando .github/prompts/speckit-constitution.prompt.md.
3. Ejecutar speckit.specify usando .github/prompts/speckit-specify.prompt.md.
4. Ejecutar speckit.clarify hasta resolver dudas marcadas como NEEDS CLARIFICATION.
5. Ejecutar speckit.plan usando .github/prompts/speckit-plan.prompt.md.
6. Ejecutar speckit.tasks usando .github/prompts/speckit-tasks.prompt.md.
7. Ejecutar speckit.analyze para revisar consistencia entre artefactos.
8. Ejecutar speckit.implement y luego speckit.converge hasta cerrar brechas.

## Regla de alcance para esta etapa
- Incluir solo el alcance actual de documentacion y MVP definido.
- No incluir roadmap ni modulos futuros dentro de specs iniciales.
- Si aparece una idea nueva, registrarla en lista de pendientes, no en alcance activo.
