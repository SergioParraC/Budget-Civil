# Spec Delta: apus

## MODIFIED Requirements

### Requirement: REQ-APUS-02 Campos del APU y validación

El sistema DEBE (MUST) exigir `description` no vacía y `unit` de 1 a 10 caracteres, y permitir editar ambos
campos junto con el ítem sin alterar el vínculo. `status`, el vínculo y la autoría son de solo lectura para
el cliente. Citas: RF-07; BR-016; D-31; `02 §5`; `05 §3`.

#### Scenario: SC-APUS-06 Alta con los campos obligatorios

- **Dado** un actor autorizado y un ítem libre
- **Cuando** registra el APU con descripción y unidad válidas
- **Entonces** el sistema guarda ambos valores y los devuelve al consultar el APU

#### Scenario: SC-APUS-07 Intento de fijar estado, vínculo o autoría

- **Dado** un actor autorizado que envía `status`, `apuId`, `projectId`, `wbsItemId` o autoría
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema no aplica esos valores y conserva los calculados por el servidor

#### Scenario: SC-APUS-08 Unidad o descripción vacías

- **Dado** un actor autorizado que envía la unidad vacía o de más de 10 caracteres, o la descripción vacía
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-09 Edición de los campos descriptivos

- **Dado** un APU existente y un actor autorizado con la versión vigente
- **Cuando** edita la descripción o la unidad junto con el ítem
- **Entonces** el sistema guarda los cambios y conserva el vínculo con el ítem

#### Scenario: SC-APUS-10 El cliente intenta fijar estado o vínculo

- **Dado** un actor autorizado que envía `status`, `projectId` o `wbsItemId` en una edición
- **Cuando** confirma la operación
- **Entonces** el sistema no aplica esos valores y conserva el estado y el vínculo calculados

#### Scenario: SC-APUS-109 Unidad de hasta diez caracteres

- **Dado** un actor autorizado y un APU con una unidad de diez caracteres
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema acepta la unidad y la devuelve completa

#### Scenario: SC-APUS-110 Unidad fuera del catálogo

- **Dado** un actor autorizado que envía una unidad que no existe en `measurement_units`
- **Cuando** confirma el alta del APU
- **Entonces** el sistema acepta el valor textual, lo guarda en el APU y no inserta ni modifica la unidad en
  `measurement_units`
