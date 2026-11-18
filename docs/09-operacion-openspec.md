# Operación con OpenSpec

> Documento actualizado el 27/10/2026. Reemplaza la guía de Spec Kit (`D-09`).

## 1. Objetivo
Definir la rutina para transformar el plan de ingeniería en especificaciones y ejecutar cambios con OpenSpec.

## 2. Estructura
```text
openspec/
  project.md                 contexto, stack y convenciones
  specs/<capacidad>/spec.md  comportamiento vigente
  changes/<cambio>/          propuesta, diseño, tareas y delta de spec
  changes/archive/           cambios cerrados
```

Capacidades: `identidad`, `proyectos`, `edt`, `insumos`, `apus`, `calculo`, `aprobaciones`, `catalogo`, `alertas`.

## 3. Flujo de un cambio
1. **Propuesta:** problema, alcance, `RF`/`BR` y criterios de aceptación.
2. **Delta de especificación:** requisitos añadidos, modificados o eliminados, con escenarios.
3. **Diseño:** decisiones y riesgos, si el cambio lo requiere.
4. **Tareas:** pasos pequeños con criterio de hecho.
5. **Implementación:** código y pruebas.
6. **Verificación:** escenarios cumplidos y checklist.
7. **Archivo:** especificación actualizada y cambio cerrado.

## 4. Entrada mínima por paso
| Paso | Entrada |
|---|---|
| Propuesta | `RF`, `BR`, decisiones y alcance |
| Delta | Comportamiento observable y casos negativos |
| Diseño | Restricciones de arquitectura y datos |
| Tareas | Delta aprobado y dependencias cerradas |
| Implementación | Tareas y convenciones de código |
| Verificación | Escenarios, pruebas y checklist |
| Archivo | Evidencia y `DoD` cumplido |

## 5. Convenciones
- Identificador de cambio: **libre**, único y descriptivo (`D-68`).
- Requisitos: `REQ-<CAPACIDAD>-<NN>`.
- Escenarios: `Dado / Cuando / Entonces`.
- Idioma: español; código e identificadores técnicos en inglés (`D-69`).
- Cada requisito cita los `RF`, `BR` y `D` que implementa.
- Obligatorios los casos negativos: permisos, datos inválidos, concurrencia y aislamiento.
- Sin detalles de implementación: ni clases, ni SQL, ni estilos.

## 6. Uso de la IA
- La IA puede redactar especificaciones y código; el estudiante revisa y aprueba (`D-71`).
- No se exige registro de uso de IA (`D-70`).
- La IA no decide reglas de negocio, fórmulas, alcance, modelo de datos ni exposición pública.
- Cada salida se revisa contra la especificación y el checklist del capítulo 6.

## 7. Verificación y cierre
Puertas automáticas: compilación, analizadores, unitarias, arquitectura, contrato y cobertura ≥ 80 % en dominio y motor.
Verificación manual: integración con SQL local, flujos críticos y migraciones (`D-65`, `D-66`).

Una iteración cierra cuando:
1. El flujo de la etapa es demostrable.
2. Las pruebas automatizadas están en verde.
3. La verificación manual está registrada.
4. El cambio está archivado y la especificación actualizada.
5. No quedan `X` abiertos que afecten el cambio.

## 8. Relación con la documentación
- `PLAN_INGENIERIA.md` conserva la justificación y las decisiones.
- `openspec/specs/` es la fuente de verdad del comportamiento.
- Los documentos `00`–`06` y `09` se actualizan al cerrar cada capítulo (`D-09`).
- No existen documentos `07` y `08`; cualquier referencia anterior a ellos queda sin efecto.

## 9. Reglas de alcance
- No se incluyen funcionalidades futuras ni `RF-19` (fuera del MVP).
- Las ideas nuevas se registran en el backlog, no en el cambio activo.
- Un cambio se divide si no cabe en una etapa.
