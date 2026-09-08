# Operacion de Copilot con Spec Kit

## 1. Objetivo de esta guia
Definir una rutina estable para producir especificaciones y desarrollo asistido por Copilot.

## 2. Flujo recomendado
1. Actualizar .github/docs/00 a 08.
2. Ejecutar speckit.constitution.
3. Ejecutar speckit.specify.
4. Ejecutar speckit.clarify.
5. Ejecutar speckit.plan.
6. Ejecutar speckit.tasks.
7. Ejecutar speckit.analyze.
8. Ejecutar speckit.implement.
9. Ejecutar speckit.converge.

## 3. Entrada minima por comando
- speckit.constitution: principios tecnicos y de calidad del proyecto.
- speckit.specify: problema, objetivos, historias, criterios y alcance cerrado.
- speckit.plan: stack, arquitectura, datos, seguridad y despliegue.
- speckit.tasks: plan aprobado y artefactos de soporte.
- speckit.converge: codigo y pruebas vs spec, plan y tasks.

## 4. Reglas de alcance
- No incluir funcionalidades futuras no aprobadas.
- Registrar ideas futuras fuera del spec activo.

## 5. Uso del agente Explore
- Usar Explore para busqueda de contexto, rastreo de codigo y consultas rapidas.
- Mantener decisiones finales en artefactos de especificacion del feature.

## 6. Criterio de cierre de iteracion
Una iteracion cierra cuando converge sin brechas criticas de requerimiento, pruebas y arquitectura.
