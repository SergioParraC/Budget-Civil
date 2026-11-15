# Spec Delta: proyectos

## MODIFIED Requirements

### Requirement: REQ-PROYECTOS-10 Configuración del proyecto como entidad propia

El sistema DEBE (MUST) mantener exactamente una configuración por proyecto, con divisa base, componentes del
AIU, decimales de redondeo y de cantidades (`rounding_decimals` y `quantity_decimals`, entre 0 y 10, por
defecto 2), modo de firma y requisito de auditor. Solo el Director de proyectos puede modificar esos
parámetros (`D-112`). Citas: RF-03; D-90, D-93, D-94, D-112.

#### Scenario: SC-PROYECTOS-22 Configuración única del proyecto

- **Dado** un proyecto creado
- **Cuando** se consulta su configuración
- **Entonces** el sistema devuelve una sola configuración con la divisa base, los tres componentes del AIU,
  los decimales de redondeo y de cantidades, el modo de firma y el requisito de auditor

#### Scenario: SC-PROYECTOS-23 Intento de crear una segunda configuración

- **Dado** un proyecto que ya tiene configuración
- **Cuando** se intenta crear otra configuración para ese proyecto
- **Entonces** el sistema rechaza la operación con conflicto y conserva la configuración existente

#### Scenario: SC-PROYECTOS-54 Decimales válidos

- **Dado** un Director autenticado y un proyecto de su compañía
- **Cuando** configura `roundingDecimals` y `quantityDecimals` entre 0 y 10
- **Entonces** el sistema guarda ambos valores y los devuelve al consultar la configuración

#### Scenario: SC-PROYECTOS-55 Decimales fuera de rango

- **Dado** un Director autenticado que envía un valor menor que 0 o mayor que 10
- **Cuando** confirma la configuración
- **Entonces** el sistema rechaza la operación con un error de validación y conserva los valores vigentes
