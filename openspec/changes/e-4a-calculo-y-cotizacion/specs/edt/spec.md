# Spec Delta: edt

## ADDED Requirements

### Requirement: REQ-EDT-23 Valores de costo en el árbol

El sistema DEBE (MUST) devolver en `GET /projects/{projectId}/wbs` el costo unitario y el costo directo de
cada ítem, y el costo directo de cada capítulo como la suma de los costos directos de los ítems de su
subárbol, cada uno una sola vez y sin sumar los subtotales de los capítulos hijos. Un capítulo sin ítems en
su subárbol DEBE (MUST) devolver `0`; un ítem sin APU o con APU incompleto DEBE (MUST) devolver costos
nulos. Citas: RF-04, RF-10; D-06, D-07, D-118, D-121.

#### Scenario: SC-EDT-89 Ítem con APU completo

- **Dado** un ítem con APU completo, costo directo unitario `100` y cantidad de obra `10`
- **Cuando** se consulta el árbol
- **Entonces** el sistema devuelve `unitCost = 100`, `directCost = 1000` y `apu.unitPrice = 100`

#### Scenario: SC-EDT-90 Ítem sin APU

- **Dado** un ítem sin APU
- **Cuando** se consulta el árbol
- **Entonces** el sistema devuelve `apu = null`, `unitCost = null` y `directCost = null`

#### Scenario: SC-EDT-91 Ítem con APU incompleto

- **Dado** un ítem cuyo APU tiene una línea sin precio unitario
- **Cuando** se consulta el árbol
- **Entonces** el sistema devuelve `unitCost = null`, `directCost = null` y `apu.status = FALTA_INFORMACION`

#### Scenario: SC-EDT-92 Capítulo con ítems directos

- **Dado** un capítulo con dos ítems de costos directos `1000` y `100`
- **Cuando** se consulta el árbol
- **Entonces** el capítulo devuelve `directCost = 1100`

#### Scenario: SC-EDT-93 Capítulo con subcapítulos

- **Dado** un capítulo con un ítem directo de `1000` y un subcapítulo con un ítem de `100`
- **Cuando** se consulta el árbol
- **Entonces** el capítulo devuelve `directCost = 1100` y el subcapítulo `100`, sin sumar dos veces el
  subtotal del subcapítulo

#### Scenario: SC-EDT-94 Capítulo sin ítems en su subárbol

- **Dado** un capítulo sin ítems ni subcapítulos con ítems
- **Cuando** se consulta el árbol
- **Entonces** el capítulo devuelve `directCost = 0`

#### Scenario: SC-EDT-95 Capítulo con un ítem incompleto

- **Dado** un capítulo con un ítem cuyo APU no tiene precio completo
- **Cuando** se consulta el árbol
- **Entonces** el capítulo devuelve `directCost = null` y no un total parcial

#### Scenario: SC-EDT-96 Movimiento de un capítulo

- **Dado** un árbol con un capítulo con ítems en un capítulo padre
- **Cuando** el capítulo se mueve a otro padre y se vuelve a consultar el árbol
- **Entonces** los subtotales de los capítulos de origen y destino reflejan los ítems en su nueva ubicación
  y ningún ítem se cuenta dos veces

#### Scenario: SC-EDT-97 Ítem raíz

- **Dado** un ítem raíz sin capítulo padre y un capítulo con ítems
- **Cuando** se consulta el árbol
- **Entonces** el ítem raíz devuelve su costo directo y el capítulo no lo incluye en su subtotal

#### Scenario: SC-EDT-98 Redondeo del costo del ítem

- **Dado** un ítem con cantidad de obra y costo unitario que producen una fracción y `roundingDecimals = 2`
- **Cuando** se consulta el árbol
- **Entonces** el costo directo del ítem se redondea a dos decimales con `MidpointRounding.AwayFromZero`

#### Scenario: SC-EDT-99 Árbol sin ítems

- **Dado** un proyecto con capítulos sin ítems
- **Cuando** se consulta el árbol
- **Entonces** cada capítulo devuelve `directCost = 0`

#### Scenario: SC-EDT-100 El cliente no fija costos

- **Dado** un actor autorizado que intenta enviar `unitCost`, `directCost` o `apu.unitPrice` en el alta o la
  edición de un nodo
- **Cuando** el sistema procesa la solicitud
- **Entonces** no aplica esos valores y los devuelve calculados en la siguiente consulta
