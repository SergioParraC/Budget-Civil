# calculo Specification

## Purpose

Define el cálculo del presupuesto de un proyecto a partir de los APUs y las cantidades de obra del EDT:
costo directo, AIU discriminado, costo indirecto, valor total y alerta de tope presupuestal. La cotización y
las aprobaciones se especifican en sus propias capacidades.

## Requirements

### Requirement: REQ-CALCULO-01 Costo directo del presupuesto

El sistema DEBE (MUST) devolver en `GET /projects/{projectId}/budget` el costo directo del proyecto como
la suma de `costo directo unitario del APU × cantidad de obra` de todos los ítems del EDT, incluidos los
ítems raíz, redondeada con `roundingDecimals`; con ítems incompletos DEBE (MUST) devolverlo nulo e
`isComplete = false`. Citas: RF-10; D-06, D-121; `05 §5`.

#### Scenario: SC-CALCULO-01 Presupuesto con ítems completos

- **Dado** un proyecto con dos ítems cuyos APUs tienen costo directo unitario `100` y `50`, cantidades `10`
  y `2`, y un AIU de `10 %`, `5 %` y `8 %`
- **Cuando** un actor autorizado consulta el presupuesto
- **Entonces** el sistema devuelve `directCost = 1100`, los tres componentes del AIU con sus porcentajes e
  importes, `indirectCosts.total = 253` y `total = 1353`

#### Scenario: SC-CALCULO-02 Proyecto sin ítems

- **Dado** un proyecto sin ítems en el EDT
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `directCost = 0`, AIU con importes `0`, `total = 0` e `isComplete = true`

#### Scenario: SC-CALCULO-03 Ítem sin APU

- **Dado** un proyecto con un ítem sin APU
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `directCost`, los importes del AIU y `total` nulos, `isComplete = false`
  e informa el ítem en el conteo de ítems sin APU

#### Scenario: SC-CALCULO-04 APU con un insumo sin precio

- **Dado** un proyecto con un ítem cuyo APU tiene una línea sin precio unitario
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve los costos nulos, `isComplete = false` y no calcula un total parcial

### Requirement: REQ-CALCULO-02 AIU discriminado y valor total

El sistema DEBE (MUST) devolver el AIU del proyecto discriminado en `administration`, `unforeseen` y
`profit`, con su porcentaje e importe calculado sobre el costo directo total, el costo indirecto total y el
valor total `costo directo + costo indirecto`; DEBE (MUST) aplicar el AIU una sola vez sobre el costo
directo total y redondear cada importe con `roundingDecimals`. Citas: RF-10; D-07, D-121.

#### Scenario: SC-CALCULO-05 El AIU se aplica una sola vez

- **Dado** un proyecto con dos ítems y un AIU configurado
- **Cuando** se consulta el presupuesto
- **Entonces** cada componente del AIU se calcula sobre el costo directo total del proyecto y no sobre cada
  ítem por separado

#### Scenario: SC-CALCULO-06 Redondeo del presupuesto

- **Dado** un proyecto con `roundingDecimals = 2` y un costo directo que produce fracciones
- **Cuando** se consulta el presupuesto
- **Entonces** el costo directo, cada componente del AIU, el costo indirecto y el total se redondean a dos
  decimales con `MidpointRounding.AwayFromZero`

### Requirement: REQ-CALCULO-03 Alerta de tope presupuestal

El sistema DEBE (MUST) devolver en el presupuesto el `budgetLimit` configurado del proyecto y el indicador
`isOverBudget`; DEBE (MUST) activarlo solamente cuando el total calculable supera el tope y DEBE (MUST)
devolverlo en `false` cuando no hay tope configurado, el total es nulo o el total es igual o menor que el
tope. Citas: RF-17; D-94; `02 §RF-17`.

#### Scenario: SC-CALCULO-09 Total superior al tope

- **Dado** un proyecto con `budgetLimit = 1000` y un total calculado de `1353`
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `isOverBudget = true`

#### Scenario: SC-CALCULO-10 Total igual al tope

- **Dado** un proyecto con `budgetLimit = 1353` y un total calculado de `1353`
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `isOverBudget = false`

#### Scenario: SC-CALCULO-11 Proyecto sin tope configurado

- **Dado** un proyecto sin `budgetLimit`
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `budgetLimit = null` e `isOverBudget = false`

#### Scenario: SC-CALCULO-12 Presupuesto incompleto

- **Dado** un proyecto con un ítem sin APU o con un APU incompleto
- **Cuando** se consulta el presupuesto
- **Entonces** el sistema devuelve `isOverBudget = false` y no emite una alerta con datos incompletos

### Requirement: REQ-CALCULO-04 Lectura autorizada y aislada del presupuesto

El sistema DEBE (MUST) permitir la consulta del presupuesto al Administrador, al Director de la compañía
dueña del proyecto y al Presupuestador y al Auditor asignados al proyecto; DEBE (MUST) rechazar a los demás
actores y DEBE (MUST) impedir el acceso a proyectos de otra compañía. Citas: RF-10; D-34, D-83, D-84;
matriz `1.3.2`.

#### Scenario: SC-CALCULO-13 El Administrador consulta cualquier proyecto

- **Dado** un Administrador autenticado y un proyecto de cualquier compañía
- **Cuando** consulta el presupuesto
- **Entonces** el sistema devuelve el presupuesto en solo lectura

#### Scenario: SC-CALCULO-14 El Director consulta su compañía

- **Dado** un Director de la compañía dueña del proyecto
- **Cuando** consulta el presupuesto
- **Entonces** el sistema devuelve el presupuesto

#### Scenario: SC-CALCULO-15 El Presupuestador asignado consulta

- **Dado** un Presupuestador con `ProjectMember` activo en el proyecto
- **Cuando** consulta el presupuesto
- **Entonces** el sistema devuelve el presupuesto

#### Scenario: SC-CALCULO-16 El Presupuestador no asignado intenta consultar

- **Dado** un Presupuestador de la compañía dueña sin `ProjectMember` activo
- **Cuando** consulta el presupuesto
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN`

#### Scenario: SC-CALCULO-17 El Auditor asignado consulta

- **Dado** un Auditor con `ProjectMember` activo en el proyecto
- **Cuando** consulta el presupuesto
- **Entonces** el sistema devuelve el presupuesto en solo lectura

#### Scenario: SC-CALCULO-18 El Auditor no asignado intenta consultar

- **Dado** un Auditor de la compañía dueña sin `ProjectMember` activo
- **Cuando** consulta el presupuesto
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN`

#### Scenario: SC-CALCULO-07 Proyecto de otra compañía

- **Dado** un usuario de la compañía A y un proyecto de la compañía B
- **Cuando** solicita el presupuesto
- **Entonces** el sistema responde `403 WBS_READ_FORBIDDEN` sin revelar los datos del proyecto

#### Scenario: SC-CALCULO-08 Proyecto inexistente

- **Dado** un actor autorizado y un `projectId` que no existe
- **Cuando** solicita el presupuesto
- **Entonces** el sistema responde `404 PROJECT_NOT_FOUND`

### Requirement: REQ-CALCULO-05 Cálculo derivado sin persistencia

El sistema DEBE (MUST) calcular el presupuesto en cada consulta a partir de los APUs, las cantidades de obra,
la configuración del proyecto y los precios vigentes; NO DEBE (MUST NOT) persistir el costo directo, el AIU
ni el total, y DEBE (MUST) reflejar en la siguiente consulta cualquier cambio de precio, composición o
cantidad. Citas: RF-10; D-08, D-121.

#### Scenario: SC-CALCULO-19 Recálculo tras cambiar un precio

- **Dado** un presupuesto calculado y un insumo al que se le cambia el precio unitario
- **Cuando** se vuelve a consultar el presupuesto
- **Entonces** el sistema devuelve el costo directo y el total recalculados con el precio vigente

#### Scenario: SC-CALCULO-20 Recálculo tras cambiar la cantidad de obra

- **Dado** un presupuesto calculado y un ítem al que se le cambia la cantidad de obra
- **Cuando** se vuelve a consultar el presupuesto
- **Entonces** el sistema devuelve el costo directo y el total recalculados con la cantidad vigente

#### Scenario: SC-CALCULO-21 Sin escritura de costos

- **Dado** un presupuesto consultado
- **Cuando** se inspeccionan los datos persistidos del proyecto
- **Entonces** no existe una columna ni un registro de costo directo, AIU o total del proyecto
