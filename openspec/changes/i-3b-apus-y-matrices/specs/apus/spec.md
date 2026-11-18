# Spec Delta: apus

## Purpose

Define la composición técnica de un proyecto: el APU vinculado 1:1 con un ítem del EDT, su composición de
insumos, la composición y reutilización de Matrices, la copia de APU y de Matriz, el conteo de uso de cada
insumo, la vista previa de costo directo y AIU del APU, la autoría y las reglas de acceso. El cálculo total
del proyecto, la cotización y las aprobaciones pertenecen a etapas posteriores.

## ADDED Requirements

### Requirement: REQ-APUS-01 Un APU por ítem del proyecto

El sistema DEBE (MUST) registrar cada APU como registro propio de un proyecto y vincularlo a un único ítem
del EDT de ese proyecto; DEBE (MUST) rechazar el alta sin ítem, con un ítem de otro proyecto o con un ítem
que ya tiene APU, y DEBE (MUST) impedir que el APU cambie de proyecto o de ítem. El APU no almacena código y
su baja ocurre con la del ítem (`REQ-EDT-21`). Citas: RF-07; BR-016; D-05, D-16, D-31; UC-05.

#### Scenario: SC-APUS-01 Alta vinculada a un ítem libre

- **Dado** un Director con pertenencia al proyecto y un ítem del EDT de ese proyecto sin APU
- **Cuando** registra el APU con descripción y unidad válidas
- **Entonces** el sistema crea el APU en el proyecto y el ítem queda vinculado a ese único APU

#### Scenario: SC-APUS-02 Alta con un ítem que ya tiene APU

- **Dado** un ítem del EDT que ya está vinculado a un APU
- **Cuando** un actor autorizado intenta crear otro APU para ese ítem
- **Entonces** el sistema rechaza la operación con `409` y el APU existente no cambia

#### Scenario: SC-APUS-03 Alta con un ítem de otro proyecto u otra compañía

- **Dado** un ítem del EDT de otro proyecto
- **Cuando** un actor autorizado intenta crear el APU en el proyecto activo con ese ítem
- **Entonces** el sistema rechaza la operación sin revelar si el ítem existe

#### Scenario: SC-APUS-04 Intento de mover el APU

- **Dado** un APU vinculado a un ítem
- **Cuando** un actor autorizado envía un cambio de proyecto o de ítem
- **Entonces** el sistema rechaza la solicitud y el vínculo original se conserva

#### Scenario: SC-APUS-05 Código del ítem en la consulta del APU

- **Dado** un APU vinculado a un ítem con código `1.1.1`
- **Cuando** un actor con lectura consulta el detalle del APU
- **Entonces** el sistema devuelve el código y la cantidad de obra del ítem junto con el APU

### Requirement: REQ-APUS-02 Campos del APU y validación

El sistema DEBE (MUST) exigir `description` no vacía y `unit` de 1 a 5 caracteres, y permitir editar ambos
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

- **Dado** un actor autorizado que envía la unidad vacía o de más de 5 caracteres, o la descripción vacía
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

### Requirement: REQ-APUS-03 Estado del APU por completitud

El sistema DEBE (MUST) derivar `status` como `COMPLETO` cuando existan descripción, unidad y al menos una
línea de composición, y `FALTA_INFORMACION` en caso contrario. Una línea de
Matriz cuenta solo si la Matriz tiene unidad y componentes; si está incompleta, el APU queda en
`FALTA_INFORMACION`. El cambio de composición o de unidad de una Matriz DEBE (MUST) recalcular los APUs que
la usan. Citas: RF-07; `05 §3`; `D-48`.

#### Scenario: SC-APUS-11 Alta sin composición

- **Dado** un actor autorizado que registra un APU sin líneas de composición
- **Cuando** confirma el alta
- **Entonces** el APU queda en estado `FALTA_INFORMACION`

#### Scenario: SC-APUS-12 La primera línea completa el APU

- **Dado** un APU en estado `FALTA_INFORMACION` con descripción y unidad válidas
- **Cuando** se agrega su primera línea de composición
- **Entonces** el APU pasa a estado `COMPLETO`

#### Scenario: SC-APUS-13 El retiro de la última línea lo deja incompleto

- **Dado** un APU en estado `COMPLETO` con una sola línea de composición
- **Cuando** se retira esa línea
- **Entonces** el APU vuelve a `FALTA_INFORMACION`

#### Scenario: SC-APUS-14 La edición de los campos descriptivos recalcula el estado

- **Dado** un APU `COMPLETO`
- **Cuando** un actor autorizado edita la descripción o la unidad sin alterar los campos obligatorios
- **Entonces** el estado se recalcula y permanece `COMPLETO`

#### Scenario: SC-APUS-84 Una Matriz completa el APU

- **Dado** un APU con descripción y unidad válidas y sin líneas
- **Cuando** se agrega una línea que referencia una Matriz completa del proyecto
- **Entonces** el APU pasa a estado `COMPLETO`

#### Scenario: SC-APUS-85 La vista previa sin precio queda incompleta

- **Dado** un APU con una línea cuyo insumo no tiene precio unitario
- **Cuando** se consulta su vista previa
- **Entonces** la vista indica `FALTA_INFORMACION` y no devuelve costos

#### Scenario: SC-APUS-96 Una Matriz incompleta deja el APU incompleto

- **Dado** un APU sin líneas y una Matriz del proyecto sin componentes
- **Cuando** se agrega una línea que referencia esa Matriz
- **Entonces** el APU permanece en `FALTA_INFORMACION`

#### Scenario: SC-APUS-97 Completar la Matriz recalcula el APU

- **Dado** un APU con una línea que referencia una Matriz sin componentes
- **Cuando** se agrega el primer componente de la Matriz
- **Entonces** el APU pasa a `COMPLETO`, y al retirar el último componente vuelve a `FALTA_INFORMACION`

#### Scenario: SC-APUS-100 La unidad de la Matriz recalcula el APU

- **Dado** un APU con una línea que referencia una Matriz con componentes pero sin unidad
- **Cuando** se edita la Matriz y se le asigna unidad
- **Entonces** el APU pasa a `COMPLETO`, y si se retira la unidad vuelve a `FALTA_INFORMACION`

### Requirement: REQ-APUS-04 Composición del APU

El sistema DEBE (MUST) permitir agregar, editar y retirar líneas de composición con `consumption` mayor que
cero y `wastePercentage` entre 0 y 100, con valor por defecto 0, y DEBE (MUST) exigir que el insumo
pertenezca al mismo proyecto y esté activo. Citas: RF-07; `02 §5`; `05 §3`.

#### Scenario: SC-APUS-15 Alta de una línea de composición

- **Dado** un APU y un insumo activo del mismo proyecto
- **Cuando** un actor autorizado agrega la línea con consumo y desperdicio válidos
- **Entonces** el sistema guarda la línea y la devuelve en el detalle del APU

#### Scenario: SC-APUS-16 Consumo igual o menor que cero

- **Dado** un actor autorizado que envía consumo cero o negativo
- **Cuando** confirma el alta o la edición de la línea
- **Entonces** el sistema rechaza la operación con un error de validación y no crea la línea

#### Scenario: SC-APUS-17 Desperdicio fuera de rango

- **Dado** un actor autorizado que envía un desperdicio negativo o mayor que 100
- **Cuando** confirma el alta o la edición de la línea
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-18 Insumo de otro proyecto u otra compañía

- **Dado** un insumo que pertenece a otro proyecto
- **Cuando** un actor autorizado intenta usarlo como línea del APU
- **Entonces** el sistema rechaza la operación sin revelar si el insumo existe

#### Scenario: SC-APUS-19 Insumo inactivo

- **Dado** un insumo del proyecto dado de baja lógica
- **Cuando** un actor autorizado intenta usarlo en la composición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-20 Edición y retiro de una línea

- **Dado** un APU con una línea de composición
- **Cuando** un actor autorizado edita su consumo o la retira
- **Entonces** el sistema aplica el cambio y recalcula el estado del APU

### Requirement: REQ-APUS-05 Listado de APUs de otros proyectos

El sistema DEBE (MUST) listar APUs de otros proyectos de la compañía, excluyendo el de la ruta, con búsqueda
parcial por descripción, por código del ítem (`wbs_items`) y filtros por departamento y municipio del proyecto
de origen, con paginación; cada APU DEBE (MUST) incluir identificador, código, descripción, unidad, costo
directo unitario, proyecto y ubicación de origen, sin cantidad de obra ni AIU. Citas: RF-07; D-30, D-31,
D-101.

#### Scenario: SC-APUS-21 APU de otro proyecto

- **Dado** un APU de otro proyecto de la misma compañía
- **Cuando** un actor con lectura consulta el listado desde el proyecto activo
- **Entonces** el listado incluye ese APU con su código y su ubicación

#### Scenario: SC-APUS-22 Exclusión del proyecto activo

- **Dado** un APU del proyecto activo
- **Cuando** se consulta el listado de APUs de otros proyectos
- **Entonces** el APU del proyecto activo no aparece en el resultado

#### Scenario: SC-APUS-23 Búsqueda y paginación

- **Dado** varios APUs de otros proyectos que coinciden con un término
- **Cuando** un actor con lectura busca y pagina
- **Entonces** el sistema devuelve solo los APUs de la compañía que coinciden, con `page`, `pageSize` y total

#### Scenario: SC-APUS-24 Sin cantidad de obra

- **Dado** un APU de otro proyecto con cantidad de obra registrada
- **Cuando** se consulta el listado
- **Entonces** el APU no expone `quantityWork`

#### Scenario: SC-APUS-25 APU de otra compañía

- **Dado** un APU de un proyecto de otra compañía
- **Cuando** se consulta el listado
- **Entonces** el APU no aparece y no se revela su existencia

#### Scenario: SC-APUS-94 Búsqueda por departamento del proyecto de origen

- **Dado** APUs de proyectos de origen ubicados en departamentos distintos
- **Cuando** se consulta el listado con un `departmentId`
- **Entonces** solo aparecen los APUs cuyo proyecto de origen pertenece a ese departamento

#### Scenario: SC-APUS-95 Búsqueda por municipio del proyecto de origen

- **Dado** APUs de proyectos de origen ubicados en municipios distintos
- **Cuando** se consulta el listado con un `municipalityId`
- **Entonces** solo aparecen los APUs cuyo proyecto de origen pertenece a ese municipio

### Requirement: REQ-APUS-06 Composición de una Matriz

El sistema DEBE (MUST) permitir consultar, agregar, editar y retirar los componentes de un insumo de tipo
Matriz con la misma semántica de consumo y desperdicio del APU, y DEBE (MUST) exigir que la Matriz y sus
componentes pertenezcan al mismo proyecto. La Matriz queda `FALTA_INFORMACION` sin unidad o sin componentes,
y `POR_COTIZAR` con ambos. Citas: RF-09; BR-008; D-48; UC-05.

#### Scenario: SC-APUS-26 Alta de un componente

- **Dado** un insumo de tipo Matriz y un insumo activo del mismo proyecto
- **Cuando** un actor autorizado agrega el componente con consumo y desperdicio válidos
- **Entonces** el sistema guarda el componente y lo devuelve al consultar la composición

#### Scenario: SC-APUS-27 Insumo que no es de tipo Matriz

- **Dado** un insumo de tipo Material
- **Cuando** un actor autorizado intenta usarlo como Matriz para definir su composición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-28 Componente de otro proyecto

- **Dado** un insumo de otro proyecto
- **Cuando** un actor autorizado intenta usarlo como componente de la Matriz
- **Entonces** el sistema rechaza la operación sin revelar si el insumo existe

#### Scenario: SC-APUS-29 Consumo o desperdicio inválidos

- **Dado** un actor autorizado que envía consumo menor o igual que cero o desperdicio fuera de 0–100
- **Cuando** confirma la operación sobre la composición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-30 Retiro de un componente

- **Dado** una Matriz con dos componentes
- **Cuando** un actor autorizado retira uno de ellos
- **Entonces** el sistema elimina solo ese componente y conserva el otro

### Requirement: REQ-APUS-07 Reutilización y no anidamiento de Matrices

El sistema DEBE (MUST) permitir que una Matriz se use como insumo en varios APUs y en varias líneas del
mismo proyecto, y DEBE (MUST) rechazar que una Matriz contenga otra Matriz o se contenga a sí misma.
Citas: RF-09; BR-008; R-11.

#### Scenario: SC-APUS-31 La misma Matriz en dos APUs

- **Dado** una Matriz del proyecto usada en un APU
- **Cuando** otro APU del mismo proyecto la incluye en su composición
- **Entonces** el sistema acepta la línea y la Matriz conserva una sola composición

#### Scenario: SC-APUS-32 Componente de tipo Matriz

- **Dado** un insumo de tipo Matriz y otro insumo también de tipo Matriz del mismo proyecto
- **Cuando** un actor autorizado intenta agregar el segundo como componente del primero
- **Entonces** el sistema rechaza la operación por anidamiento

#### Scenario: SC-APUS-33 Autorreferencia

- **Dado** un insumo de tipo Matriz
- **Cuando** un actor autorizado intenta agregarlo como componente de sí mismo
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-34 Matriz de otro proyecto

- **Dado** una Matriz de otro proyecto
- **Cuando** un actor autorizado intenta usarla como línea de un APU del proyecto activo
- **Entonces** el sistema rechaza la operación sin revelar si la Matriz existe

### Requirement: REQ-APUS-08 Copia de una Matriz sin validación

El sistema DEBE (MUST) copiar una Matriz a otro proyecto de la misma compañía creando una Matriz nueva e
independiente y DEBE (MUST) copiar todos sus componentes y los insumos que los componen como registros
nuevos, sin validar nombres, similitudes ni duplicados en el destino. Citas: RF-09; BR-021; D-16, D-25,
D-29.

#### Scenario: SC-APUS-35 Copia de la Matriz y sus componentes

- **Dado** una Matriz con componentes que ya existen en el proyecto destino
- **Cuando** un actor autorizado confirma la copia
- **Entonces** el sistema crea una Matriz nueva y copia cada componente y su insumo como registros nuevos

#### Scenario: SC-APUS-36 Sin vista previa de reconciliación

- **Dado** una Matriz de origen y un proyecto destino
- **Cuando** el actor solicita la copia
- **Entonces** el sistema no devuelve clasificaciones ni candidatos y no exige decisiones

#### Scenario: SC-APUS-37 Matriz con el mismo nombre en el destino

- **Dado** una Matriz de origen cuyo nombre ya existe en el proyecto destino
- **Cuando** el actor autorizado confirma la copia
- **Entonces** el sistema crea otra Matriz con ese nombre y no bloquea la operación

#### Scenario: SC-APUS-38 Insumos de la Matriz duplicados

- **Dado** una Matriz cuyos componentes tienen insumos idénticos en el destino
- **Cuando** se copia la Matriz
- **Entonces** el sistema crea insumos nuevos para la Matriz copiada, sin reutilizar los del destino

#### Scenario: SC-APUS-39 Independencia de la copia de la Matriz

- **Dado** una Matriz copiada a otro proyecto
- **Cuando** se cambia la composición en el origen o en el destino
- **Entonces** la otra Matriz conserva su composición original

#### Scenario: SC-APUS-40 Matriz de otra compañía

- **Dado** una Matriz de un proyecto de otra compañía
- **Cuando** un actor autorizado intenta copiarla a su proyecto
- **Entonces** el sistema rechaza la operación sin revelar si la Matriz existe

#### Scenario: SC-APUS-41 El endpoint de copia de Matriz se conserva

- **Dado** una Matriz de un proyecto de la misma compañía
- **Cuando** un actor autorizado usa `POST /projects/{projectId}/matrices/copy`
- **Entonces** el sistema aplica la copia sin validación de nombres ni de insumos

### Requirement: REQ-APUS-09 Copia de un APU desde otro proyecto de la compañía

El sistema DEBE (MUST) copiar un APU de otro proyecto de la misma compañía creando en el proyecto destino un
ítem del EDT nuevo con su código y cantidad de obra, vinculado 1:1 al APU copiado, conservando su composición,
sin aprobación y con el actor como último modificador; DEBE (MUST) rechazar la copia desde otra compañía sin
revelar si el APU existe. El origen puede ser un APU de un proyecto donde el actor no tiene pertenencia.
Citas: RF-07; BR-018; D-13, D-16, D-24.

#### Scenario: SC-APUS-42 Copia exitosa

- **Dado** un APU de otro proyecto de la misma compañía y los datos de un ítem destino libre
- **Cuando** un actor autorizado confirma la copia
- **Entonces** el sistema crea el ítem, un APU nuevo con la misma descripción, unidad y composición, y los vincula

#### Scenario: SC-APUS-43 Código del ítem destino duplicado

- **Dado** un APU de origen y un código de ítem que ya existe en el proyecto destino
- **Cuando** un actor autorizado intenta copiar el APU
- **Entonces** el sistema responde `409 WBS_CODE_DUPLICATED` y no crea el ítem ni el APU

#### Scenario: SC-APUS-44 APU de otra compañía

- **Dado** un APU de un proyecto de otra compañía
- **Cuando** un actor autorizado intenta copiarlo
- **Entonces** el sistema rechaza la operación sin revelar si el APU existe

#### Scenario: SC-APUS-45 Origen de un proyecto sin pertenencia

- **Dado** un APU de un proyecto de la misma compañía donde el actor no tiene `ProjectMember`
- **Cuando** un actor autorizado con escritura en el destino lo copia
- **Entonces** el sistema completa la copia porque solo se exige lectura sobre el origen

#### Scenario: SC-APUS-46 La copia nace sin aprobación

- **Dado** un APU de origen que tiene una aprobación registrada
- **Cuando** se copia al proyecto destino
- **Entonces** el APU copiado queda sin aprobación y con el actor como último modificador

### Requirement: REQ-APUS-10 Reconciliación de insumos simples al copiar un APU

El sistema DEBE (MUST) comparar los insumos simples del APU de origen con los del proyecto destino y
clasificarlos como existente, similar o inexistente; DEBE (MUST) agregar los inexistentes y exigir la
decisión del usuario para los demás. La vista previa DEBE (MUST) devolver por candidato la unidad, el precio
unitario y su comparativo con el origen; `EXACT` se determina por la descripción normalizada. Las Matrices no
se comparan (`REQ-APUS-08`). Citas: RF-08; D-25.

#### Scenario: SC-APUS-47 Insumo inexistente en el destino

- **Dado** un APU de origen con un insumo simple que no existe en el proyecto destino
- **Cuando** se ejecuta la copia
- **Entonces** el sistema agrega el insumo al destino sin pedir decisión y la línea apunta al insumo nuevo

#### Scenario: SC-APUS-48 Insumo similar en el destino

- **Dado** un APU de origen con un insumo simple cuya descripción coincide parcialmente con uno del destino
- **Cuando** el actor autorizado solicita la vista previa de la copia
- **Entonces** el sistema marca el insumo como similar y devuelve los candidatos del destino con su unidad,
  su precio unitario y el comparativo frente al insumo de origen

#### Scenario: SC-APUS-101 El match exacto compara unidad y precio unitario

- **Dado** un APU de origen con un insumo simple cuya descripción coincide con uno del destino, pero cuya
  unidad o precio unitario difieren
- **Cuando** el actor autorizado solicita la vista previa de la copia
- **Entonces** el sistema lo clasifica como existente y devuelve, por candidato, `unitMatches`,
  `unitPriceMatches` y `unitPriceDifference` calculado como candidato menos origen

#### Scenario: SC-APUS-49 Decisión de usar el insumo existente

- **Dado** un insumo simple marcado como similar o existente en el destino
- **Cuando** el actor decide usar el insumo existente
- **Entonces** la línea del APU copiado referencia ese insumo y conserva su precio unitario

#### Scenario: SC-APUS-50 Decisión de conservar el insumo de origen

- **Dado** un insumo simple marcado como similar o existente en el destino
- **Cuando** el actor decide conservar el insumo de origen
- **Entonces** el sistema crea un insumo nuevo en el destino con el valor de origen y registra su procedencia

#### Scenario: SC-APUS-51 Copia sin resolver un insumo simple con coincidencia

- **Dado** un APU de origen con un insumo simple que existe o es similar en el destino
- **Cuando** el actor confirma la copia sin resolverlo
- **Entonces** el sistema rechaza la operación con un error de validación y no crea el APU

#### Scenario: SC-APUS-52 La reconciliación no modifica el origen

- **Dado** un insumo del proyecto de origen involucrado en una copia
- **Cuando** el actor decide usar el insumo existente en el destino
- **Entonces** el insumo de origen conserva sus datos, su precio y su estado

### Requirement: REQ-APUS-11 Independencia de la copia

El sistema DEBE (MUST) crear el APU y los insumos copiados como registros independientes, de modo que los
cambios posteriores de composición, precio o datos no se propaguen en ningún sentido. Citas: RF-07, RF-08;
R-17; D-29.

#### Scenario: SC-APUS-53 Cambio de composición en el destino

- **Dado** un APU copiado del proyecto A al proyecto B
- **Cuando** se agrega o retira una línea en el APU del proyecto B
- **Entonces** el APU del proyecto A conserva su composición original

#### Scenario: SC-APUS-54 Cambio de precio en el origen

- **Dado** un insumo copiado del proyecto A al proyecto B
- **Cuando** se cambia el precio del insumo en el proyecto A
- **Entonces** el insumo del proyecto B conserva el precio con el que se copió

#### Scenario: SC-APUS-55 Retiro de una línea en el origen

- **Dado** un APU copiado del proyecto A al proyecto B
- **Cuando** se retira una línea en el APU del proyecto A
- **Entonces** el APU del proyecto B conserva esa línea

### Requirement: REQ-APUS-12 Conteo de uso de insumos

El sistema DEBE (MUST) calcular el conteo de uso de cada insumo del proyecto como la suma, sobre los ítems
del proyecto, de `consumption × (1 + waste_percentage/100) × quantity_work`, y DEBE (MUST) devolver cero
para los insumos que no participan en ninguna composición. El reporte DEBE (MUST) agrupar por `resourceType`
y devolver por insumo `unitPrice`, `quantity` y `totalPrice`, además del `subtotal` por tipo y el
`totalCost`. Citas: RF-22; R-21; D-33, D-121.

#### Scenario: SC-APUS-56 Insumo usado en un ítem

- **Dado** un insumo con consumo `2` en el APU de un ítem con cantidad de obra `10`
- **Cuando** se consulta el conteo de uso del proyecto
- **Entonces** el insumo devuelve un conteo de `20`

#### Scenario: SC-APUS-57 Insumo usado en dos APUs

- **Dado** un insumo con consumo `1` en un APU de cantidad `5` y consumo `3` en otro APU de cantidad `2`
- **Cuando** se consulta el conteo de uso
- **Entonces** el insumo devuelve un conteo de `11`

#### Scenario: SC-APUS-58 El desperdicio interviene

- **Dado** un insumo con consumo `2`, desperdicio `10` y cantidad de obra `10`
- **Cuando** se consulta el conteo de uso
- **Entonces** el conteo es `22` e incluye el desperdicio

#### Scenario: SC-APUS-59 Insumo sin uso

- **Dado** un insumo que no participa en ninguna composición del proyecto
- **Cuando** se consulta el conteo de uso
- **Entonces** el sistema devuelve conteo `0` para ese insumo

### Requirement: REQ-APUS-13 Conteo de uso a través de una Matriz

El sistema DEBE (MUST) sumar al conteo de cada componente de una Matriz el producto
`consumption de la Matriz × (1 + desperdicio de la Matriz/100) × consumption del componente ×
(1 + desperdicio del componente/100) × quantity_work`, expandiendo un solo nivel, y DEBE (MUST) conservar
además el conteo propio de la Matriz. Citas: RF-22; R-21; BR-008; UC-05; D-33.

#### Scenario: SC-APUS-60 Matriz con dos componentes

- **Dado** una Matriz con componentes de consumo `2` y `3`, usada con consumo `4` en un ítem de cantidad
  `10` sin desperdicio
- **Cuando** se consulta el conteo de uso
- **Entonces** los componentes devuelven `80` y `120`, y la Matriz devuelve `40`

#### Scenario: SC-APUS-61 Matriz usada en dos APUs

- **Dado** una Matriz usada con consumo `1` en dos APUs con cantidades `5` y `2`
- **Cuando** se consulta el conteo de uso
- **Entonces** cada componente devuelve la suma de sus consumos multiplicada por `7`

#### Scenario: SC-APUS-62 La expansión es de un solo nivel

- **Dado** una Matriz cuyos componentes son insumos simples
- **Cuando** se consulta el conteo de uso
- **Entonces** el sistema expande esos componentes una sola vez y no busca Matrices anidadas

#### Scenario: SC-APUS-98 Desperdicio en la línea y en el componente

- **Dado** una Matriz con desperdicio `5` usada con consumo `4` y un componente de consumo `2` con
  desperdicio `10`, en un ítem de cantidad `10`
- **Cuando** se consulta el conteo de uso
- **Entonces** el componente devuelve `92,4` y la Matriz `42`

#### Scenario: SC-APUS-102 Reporte agrupado con precio unitario y total

- **Dado** un proyecto con insumos de materiales y mano de obra usados en APUs, con precio unitario
- **Cuando** se consulta el conteo de uso
- **Entonces** el reporte agrupa los insumos por `resourceType`, cada insumo devuelve `unitPrice`,
  `quantity` y `totalPrice`, y cada grupo devuelve su `subtotal` y el reporte su `totalCost`

### Requirement: REQ-APUS-14 Autoría de la última modificación

El sistema DEBE (MUST) registrar en el APU el usuario y la fecha UTC de la última modificación de sus datos
o de su composición, fijados por el servidor; la edición de la composición de una Matriz DEBE (MUST)
actualizar la autoría del insumo Matriz. En la copia, DEBE (MUST) registrar al actor como último modificador
del APU, de la Matriz y de cada insumo copiado. Citas: RF-14; D-08.

#### Scenario: SC-APUS-63 El alta registra la autoría

- **Dado** un actor autorizado que crea un APU
- **Cuando** consulta el APU
- **Entonces** el último modificador es ese actor y la fecha es la del alta en UTC

#### Scenario: SC-APUS-64 La composición actualiza la autoría

- **Dado** un APU creado por un actor y una línea agregada por otro
- **Cuando** se consulta el APU
- **Entonces** el último modificador es el actor que agregó la línea

#### Scenario: SC-APUS-65 El cliente no puede fijar la autoría

- **Dado** un actor autorizado que envía un usuario o una fecha distintos en la solicitud
- **Cuando** confirma la operación
- **Entonces** el sistema conserva la autoría que fija el servidor

#### Scenario: SC-APUS-66 La copia registra al actor

- **Dado** un APU copiado junto con una Matriz y sus insumos
- **Cuando** se consultan la Matriz y los insumos copiados
- **Entonces** el último modificador es el actor que realizó la copia

### Requirement: REQ-APUS-15 Concurrencia optimista

El sistema DEBE (MUST) exigir `rowVersion` en la edición del APU y en cualquier cambio de composición del
APU o de la Matriz, y DEBE (MUST) responder `409` sin aplicar el cambio cuando la versión no coincida con la
vigente. Citas: D-50, D-97.

#### Scenario: SC-APUS-67 Edición con la versión vigente

- **Dado** un APU y un actor autorizado con su `rowVersion` vigente
- **Cuando** edita el APU o su composición
- **Entonces** el sistema aplica el cambio y devuelve la nueva versión

#### Scenario: SC-APUS-68 Edición con una versión desactualizada

- **Dado** un APU modificado por otro actor después de que se obtuvo su `rowVersion`
- **Cuando** se envía una edición con la versión anterior
- **Entonces** el sistema responde `409` y conserva el estado vigente

#### Scenario: SC-APUS-69 Cambio de composición sin versión

- **Dado** una solicitud de alta, edición o retiro de línea sin `rowVersion`
- **Cuando** un actor autorizado la envía
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-70 Composición de la Matriz con versión desactualizada

- **Dado** un insumo Matriz modificado por otro actor después de obtenerse su `rowVersion`
- **Cuando** se envía un cambio de composición con la versión anterior
- **Entonces** el sistema responde `409` y no altera la composición

### Requirement: REQ-APUS-16 Escritura de APUs y matrices

El sistema DEBE (MUST) permitir crear, editar, componer y copiar APUs y Matrices solo al Director y al
Presupuestador que tengan `ProjectMember` activo en el proyecto del APU; DEBE (MUST) denegar la escritura al
Administrador y al Auditor. Citas: RF-07; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-APUS-71 El Director con pertenencia escribe

- **Dado** un Director con `ProjectMember` activo en el proyecto
- **Cuando** crea, edita, compone o copia un APU o una Matriz
- **Entonces** el sistema aplica la operación

#### Scenario: SC-APUS-72 El Presupuestador con pertenencia escribe

- **Dado** un Presupuestador con `ProjectMember` activo en el proyecto
- **Cuando** crea, edita, compone o copia un APU o una Matriz
- **Entonces** el sistema aplica la operación

#### Scenario: SC-APUS-73 El Presupuestador sin pertenencia intenta escribir

- **Dado** un Presupuestador de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta crear o modificar un APU o una Matriz
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-APUS-74 El Administrador intenta escribir

- **Dado** un Administrador autenticado
- **Cuando** intenta crear, editar, componer o copiar un APU o una Matriz
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-APUS-75 El Auditor intenta escribir

- **Dado** un Auditor asignado al proyecto
- **Cuando** intenta crear, editar, componer o copiar un APU o una Matriz
- **Entonces** el sistema rechaza la operación con `403`

#### Scenario: SC-APUS-91 El Director sin pertenencia intenta escribir

- **Dado** un Director de la compañía dueña del proyecto sin `ProjectMember` activo
- **Cuando** intenta crear, editar o copiar un APU o una Matriz
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-APUS-17 Lectura de APUs, matrices y conteo de uso

El sistema DEBE (MUST) permitir consultar APUs, Matrices y conteo de uso al Director y al Presupuestador en
los proyectos de su compañía, al Auditor solo en sus proyectos asignados y al Administrador en cualquier
proyecto en solo lectura; la consulta de APUs de otros proyectos no habilita la lectura del EDT de origen.
Citas: RF-07, RF-22; D-34, D-83, D-84.

#### Scenario: SC-APUS-76 El Director consulta

- **Dado** un Director autenticado en un proyecto de su compañía
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información del proyecto

#### Scenario: SC-APUS-77 El Presupuestador consulta APUs de la compañía

- **Dado** un Presupuestador autenticado
- **Cuando** consulta APUs de otros proyectos de la compañía
- **Entonces** el sistema devuelve el listado y no el EDT de esos proyectos

#### Scenario: SC-APUS-78 El Administrador consulta en solo lectura

- **Dado** un Administrador autenticado y un proyecto de cualquier compañía
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información y no permite modificarla

#### Scenario: SC-APUS-79 El Auditor asignado consulta

- **Dado** un Auditor asignado al proyecto
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-APUS-80 El Auditor no asignado intenta consultar

- **Dado** un Auditor de la misma compañía sin pertenencia al proyecto
- **Cuando** intenta consultar APUs, Matrices o conteo de uso
- **Entonces** el sistema rechaza la operación con `403`

### Requirement: REQ-APUS-18 Aislamiento entre compañías

El sistema DEBE (MUST) impedir consultar, copiar o modificar APUs y Matrices de un proyecto de otra
compañía, sin revelar si el APU, la Matriz o el proyecto existen. Citas: R-10, R-14; D-02, D-36.

#### Scenario: SC-APUS-81 Consulta de un APU de otra compañía

- **Dado** un usuario de la compañía A y un APU de un proyecto de la compañía B
- **Cuando** solicita el APU por su identificador
- **Entonces** el sistema rechaza la solicitud sin revelar si el APU existe

#### Scenario: SC-APUS-82 Listado de un proyecto de otra compañía

- **Dado** un usuario de la compañía A
- **Cuando** solicita el listado de APUs de un proyecto de la compañía B
- **Entonces** el sistema rechaza la solicitud sin revelar si el proyecto existe

#### Scenario: SC-APUS-83 Composición de una Matriz de otra compañía

- **Dado** un usuario de la compañía A y una Matriz de la compañía B
- **Cuando** solicita su composición o intenta modificarla
- **Entonces** el sistema rechaza la solicitud sin revelar si la Matriz existe

### Requirement: REQ-APUS-19 Vista previa de costo del APU

El detalle del APU DEBE (MUST) devolver `description`, `resources` agrupado por `resourceType` con `subtotal`
por insumo y, fuera de `resources`, `directCost`, `indirectCosts` y `total`. El costo directo es
`Σ(cantidad efectiva × precio unitario)`, con `cantidad efectiva = ROUND(consumo ×
(1 + desperdicio/100), quantityDecimals)`; sin precio los costos son nulos y el APU se informa
`FALTA_INFORMACION`. Citas: RF-07, RF-10; D-06, D-07, D-33, D-121.

#### Scenario: SC-APUS-86 Costo directo y AIU del APU propio

- **Dado** un APU completo del proyecto con insumos con precio y un AIU configurado
- **Cuando** se consulta su vista previa
- **Entonces** el sistema devuelve `directCost`, `indirectCosts` con los tres componentes y `total` fuera del
  listado de insumos

#### Scenario: SC-APUS-87 Insumos agrupados por tipo

- **Dado** un APU con insumos de materiales, mano de obra y equipos
- **Cuando** se consulta su vista previa
- **Entonces** `resources` devuelve un arreglo por tipo con sus insumos y el `subtotal` de cada insumo, y su
  suma coincide con `directCost`

#### Scenario: SC-APUS-88 Línea de Matriz en el APU

- **Dado** un APU con una línea que referencia una Matriz
- **Cuando** se consulta su vista previa
- **Entonces** la Matriz aparece en el arreglo del tipo correspondiente con su `subtotal` y su
  `matrixResourceId`, sin expandir sus componentes

#### Scenario: SC-APUS-89 APU de otro proyecto

- **Dado** un APU de otro proyecto de la compañía
- **Cuando** se consulta su detalle desde el proyecto activo
- **Entonces** `indirectCosts` usa el AIU del proyecto activo y el ítem no expone `quantityWork`

#### Scenario: SC-APUS-90 Insumo sin precio en la vista previa

- **Dado** un APU con una línea cuyo insumo no tiene precio unitario
- **Cuando** se consulta su vista previa
- **Entonces** el APU se muestra en `FALTA_INFORMACION`, el `subtotal` del insumo es nulo y `directCost`,
  `indirectCosts` y `total` son nulos

### Requirement: REQ-APUS-20 Costo directo de la Matriz

El sistema DEBE (MUST) devolver en la composición de una Matriz `description`, `resources` agrupado por
`resourceType` con `subtotal` por insumo y `directCost` fuera del listado, calculado como
`Σ(consumo × (1 + desperdicio/100) × precio unitario)`; NO DEBE (MUST NOT) incluir `indirectCosts` ni AIU.
Citas: RF-09, RF-10; BR-008; D-48.

#### Scenario: SC-APUS-92 Costo directo de una Matriz

- **Dado** una Matriz con componentes de materiales y mano de obra con precio
- **Cuando** se consulta su composición
- **Entonces** `resources` agrupa los insumos por tipo con su `subtotal` y devuelve `directCost` sin
  `indirectCosts`

#### Scenario: SC-APUS-93 Matriz sin precio completo

- **Dado** una Matriz con un componente sin precio unitario
- **Cuando** se consulta su composición
- **Entonces** el sistema indica `FALTA_INFORMACION`, el `subtotal` del componente es nulo y `directCost` es
  nulo

#### Scenario: SC-APUS-99 La cantidad efectiva aplica el desperdicio

- **Dado** un APU con una línea de consumo `2`, desperdicio `10` y precio unitario `100`
- **Cuando** se consulta su vista previa
- **Entonces** la cantidad efectiva es `2,2`, el `subtotal` de la línea es `220` y `directCost` es `220`

### Requirement: REQ-APUS-21 Alta y edición unificadas con el ítem del EDT

El sistema DEBE (MUST) crear el APU únicamente junto con un ítem del EDT en
`POST /projects/{projectId}/wbs/items`, con `description` y `unit` obligatorios, `resources` opcionales y
vínculo 1:1 transaccional. DEBE (MUST) editar ambos en `PUT /projects/{projectId}/wbs/items/{itemId}`:
`quantityWork` obligatorio, `code` opcional y bloque `apu` opcional; `resources` reemplaza la composición
completa. No existen altas ni ediciones individuales de APU. Citas: RF-07; D-16, D-31.

#### Scenario: SC-APUS-103 Alta conjunta de ítem y APU

- **Dado** un actor autorizado con un código libre y el bloque `apu`
- **Cuando** registra el ítem en `POST /wbs/items`
- **Entonces** el sistema crea ítem, APU y líneas, y los vincula en una sola transacción

#### Scenario: SC-APUS-104 Edición parcial del APU

- **Dado** un APU existente y una solicitud con `apu.description` pero sin `resources`
- **Cuando** se envía `PUT /wbs/items/{itemId}`
- **Entonces** solo cambia la descripción y la composición se conserva

#### Scenario: SC-APUS-105 Reemplazo completo de recursos

- **Dado** un APU con dos líneas y una solicitud con una línea existente y una nueva
- **Cuando** se envía el bloque `apu.resources`
- **Entonces** la línea existente se actualiza, la nueva se crea y la ausente se elimina

#### Scenario: SC-APUS-106 Copia con ítem nuevo

- **Dado** un APU de otro proyecto y un `target` con código, capítulo y cantidad
- **Cuando** se copia el APU
- **Entonces** el sistema crea el ítem destino, el APU copiado y el vínculo 1:1

### Requirement: REQ-APUS-22 Sin rendimiento en el APU

El APU NO DEBE (MUST NOT) almacenar ni exponer `yield`. La cantidad efectiva del costo es
`ROUND(consumo × (1 + desperdicio/100), quantityDecimals)` y el detalle no incluye rendimiento. Citas: RF-07,
RF-10; D-06, D-121.

#### Scenario: SC-APUS-107 El detalle no expone rendimiento

- **Dado** un APU existente
- **Cuando** se consulta su detalle
- **Entonces** la respuesta no contiene `yield` y sí la cantidad de obra del ítem cuando es del proyecto

#### Scenario: SC-APUS-108 El costo no usa rendimiento

- **Dado** una línea con consumo `2`, desperdicio `0` y precio `100`
- **Cuando** se calcula la vista previa
- **Entonces** la cantidad efectiva es `2` y el subtotal es `200`

