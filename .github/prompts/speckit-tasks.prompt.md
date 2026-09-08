# Prompt base para speckit.tasks

Objetivo:
Generar tareas ejecutables, pequenas y trazables desde el plan y contratos.

Contexto fuente:
- plan.md del feature activo
- data-model.md
- contracts
- research.md

Instrucciones:
1. Desglosa tareas por modulo y capa.
2. Incluye pruebas antes de implementacion cuando aplique.
3. Marca tareas paralelizables de forma segura.
4. Cada tarea debe mapearse a requerimientos o criterios de aceptacion.

Salida esperada:
tasks.md accionable para speckit.implement.
