# Spec Delta: insumos

## MODIFIED Requirements

### Requirement: REQ-INSUMOS-04 Estado de cotización por completitud

El sistema DEBE (MUST) asignar `FALTA_INFORMACION` cuando falte descripción, unidad, precio, marca o
proveedor, y `POR_COTIZAR` cuando los cinco estén presentes; DEBE (MUST) recalcularlo en cada alta y
edición. El proveedor es opcional para crear el insumo, pero sin él el insumo permanece en
`FALTA_INFORMACION` (`REQ-PROVEEDORES-05`). La Matriz se evalúa según `REQ-APUS-06`. La transición a
`COTIZADO` pertenece a `RF-11` y no se especifica aquí. Citas: RF-05; BR-009; D-89.

#### Scenario: SC-INSUMOS-11 Alta incompleta

- **Dado** un actor autorizado que registra un insumo con descripción y precio pero sin unidad
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-12 Alta completa

- **Dado** un actor autorizado que registra un insumo con descripción, unidad, precio, marca y proveedor
- **Cuando** confirma el alta
- **Entonces** el insumo queda en estado `POR_COTIZAR`

#### Scenario: SC-INSUMOS-13 Edición que completa el insumo

- **Dado** un insumo en estado `FALTA_INFORMACION` al que le faltaba la marca y el proveedor
- **Cuando** un actor autorizado registra la marca y asocia un proveedor activo
- **Entonces** el insumo pasa a `POR_COTIZAR`

#### Scenario: SC-INSUMOS-14 Edición que retira un campo obligatorio

- **Dado** un insumo en estado `POR_COTIZAR`
- **Cuando** un actor autorizado deja la unidad vacía
- **Entonces** el insumo pasa a `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-52 Alta completa sin proveedor

- **Dado** un actor autorizado que registra un insumo con descripción, unidad, precio y marca, sin proveedor
- **Cuando** confirma el alta
- **Entonces** el insumo se guarda y queda en estado `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-53 Retiro del proveedor

- **Dado** un insumo en estado `POR_COTIZAR` con proveedor
- **Cuando** un actor autorizado retira el proveedor
- **Entonces** el insumo vuelve a `FALTA_INFORMACION`

#### Scenario: SC-INSUMOS-54 Matriz sin composición

- **Dado** un insumo de tipo Matriz con unidad pero sin componentes
- **Cuando** se consulta su estado de cotización
- **Entonces** el insumo está en `FALTA_INFORMACION` y no se le exigen precio, marca ni proveedor

#### Scenario: SC-INSUMOS-55 Matriz con unidad y composición

- **Dado** un insumo de tipo Matriz con unidad y al menos un componente
- **Cuando** se consulta su estado de cotización
- **Entonces** el insumo está en `POR_COTIZAR`

### Requirement: REQ-INSUMOS-09 Copia desde otro proyecto de la compañía

El sistema DEBE (MUST) permitir copiar un insumo desde otro proyecto de la misma compañía a un proyecto
destino con permiso de escritura, conservando tipo, datos, precio y municipio. La copia de un insumo de
tipo Matriz se realiza con la operación de copia de Matriz, que reconcilia sus componentes
(`REQ-APUS-08`); la copia genérica la rechaza. Citas: RF-06, RF-09; BR-021; D-27, D-29.

#### Scenario: SC-INSUMOS-28 Copia exitosa

- **Dado** un insumo del proyecto A de la misma compañía y un actor con permiso de escritura en el
  proyecto B
- **Cuando** solicita copiar el insumo al proyecto B
- **Entonces** el sistema crea un insumo nuevo en el proyecto B con los mismos datos y precio, y con un
  identificador distinto

#### Scenario: SC-INSUMOS-29 Copia desde otra compañía

- **Dado** un insumo de un proyecto de la compañía B y un actor de la compañía A
- **Cuando** intenta copiarlo a un proyecto de su compañía
- **Entonces** el sistema rechaza la operación sin revelar si el insumo de origen existe

#### Scenario: SC-INSUMOS-30 Destino sin permiso de escritura

- **Dado** un actor que solo puede leer el proyecto destino
- **Cuando** intenta copiar un insumo hacia ese proyecto
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-INSUMOS-56 Copia genérica de una Matriz

- **Dado** un insumo de tipo Matriz con componentes en el proyecto de origen
- **Cuando** un actor autorizado intenta copiarlo con la operación de copia de insumo
- **Entonces** el sistema rechaza la operación con un error de validación e indica que debe usarse la copia de Matriz
