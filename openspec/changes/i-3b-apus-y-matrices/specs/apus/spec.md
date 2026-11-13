# Spec Delta: apus

## Purpose

Define la composición técnica de un proyecto: el APU vinculado 1:1 con un ítem del EDT, su composición de
insumos, la composición y reutilización de Matrices, la copia de APU y de Matriz con reconciliación de
insumos, el conteo de uso de cada insumo, la autoría y las reglas de acceso. El cálculo de costos, la
cotización y las aprobaciones pertenecen a etapas posteriores.

## ADDED Requirements

### Requirement: REQ-APUS-01 Un APU por ítem del proyecto

El sistema DEBE (MUST) registrar cada APU como registro propio de un proyecto y vincularlo a un único ítem
del EDT de ese proyecto; DEBE (MUST) rechazar el alta sin ítem, con un ítem de otro proyecto o con un ítem
que ya tiene APU, y DEBE (MUST) impedir que el APU cambie de proyecto o de ítem. El APU no almacena
código. Citas: RF-07; BR-016; D-05, D-16, D-31; UC-05.

#### Scenario: SC-APUS-01 Alta vinculada a un ítem libre

- **Dado** un Director autenticado, un proyecto de su compañía y un ítem del EDT de ese proyecto sin APU
- **Cuando** registra el APU con descripción, unidad y rendimiento válidos
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

El sistema DEBE (MUST) exigir `description` y `unit` no vacías y `yield` mayor que cero, y DEBE (MUST)
permitir editar esos tres campos sin alterar el vínculo con el ítem. `status`, el vínculo y la autoría son
de solo lectura para el cliente. Citas: RF-07; BR-016; D-31; `02 §5`; `05 §3`.

#### Scenario: SC-APUS-06 Alta con los campos obligatorios

- **Dado** un actor autorizado y un ítem libre
- **Cuando** registra el APU con descripción, unidad y rendimiento mayor que cero
- **Entonces** el sistema guarda los tres valores y los devuelve al consultar el APU

#### Scenario: SC-APUS-07 Rendimiento no positivo

- **Dado** un actor autorizado que envía rendimiento cero o negativo
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación y no modifica el APU

#### Scenario: SC-APUS-08 Unidad o descripción vacías

- **Dado** un actor autorizado que envía la unidad o la descripción vacías
- **Cuando** confirma el alta o la edición
- **Entonces** el sistema rechaza la operación con un error de validación

#### Scenario: SC-APUS-09 Edición de los campos descriptivos

- **Dado** un APU existente y un actor autorizado con la versión vigente
- **Cuando** edita la descripción, la unidad o el rendimiento
- **Entonces** el sistema guarda los cambios y conserva el vínculo con el ítem

#### Scenario: SC-APUS-10 El cliente intenta fijar estado o vínculo

- **Dado** un actor autorizado que envía `status`, `projectId` o `wbsItemId` en una edición
- **Cuando** confirma la operación
- **Entonces** el sistema no aplica esos valores y conserva el estado y el vínculo calculados

### Requirement: REQ-APUS-03 Estado del APU por completitud

El sistema DEBE (MUST) derivar `status` como `COMPLETO` cuando existan descripción, unidad, rendimiento
mayor que cero y al menos una línea de composición, y `FALTA_INFORMACION` en caso contrario; DEBE (MUST)
recalcularlo en cada alta, edición y cambio de composición. Citas: RF-07; `05 §3`.

#### Scenario: SC-APUS-11 Alta sin composición

- **Dado** un actor autorizado que registra un APU sin líneas de composición
- **Cuando** confirma el alta
- **Entonces** el APU queda en estado `FALTA_INFORMACION`

#### Scenario: SC-APUS-12 La primera línea completa el APU

- **Dado** un APU en estado `FALTA_INFORMACION` con descripción, unidad y rendimiento válidos
- **Cuando** se agrega su primera línea de composición
- **Entonces** el APU pasa a estado `COMPLETO`

#### Scenario: SC-APUS-13 El retiro de la última línea lo deja incompleto

- **Dado** un APU en estado `COMPLETO` con una sola línea de composición
- **Cuando** se retira esa línea
- **Entonces** el APU vuelve a `FALTA_INFORMACION`

#### Scenario: SC-APUS-14 La edición del rendimiento recalcula el estado

- **Dado** un APU `COMPLETO`
- **Cuando** un actor autorizado edita el APU sin alterar los campos obligatorios
- **Entonces** el estado se recalcula y permanece `COMPLETO`

### Requirement: REQ-APUS-04 Composición del APU

El sistema DEBE (MUST) permitir agregar, editar y retirar líneas de composición con `consumption` mayor
que cero y `wastePercentage` entre 0 y 100, y DEBE (MUST) exigir que el insumo pertenezca al mismo
proyecto y esté activo. Citas: RF-07; `02 §5`; `05 §3`.

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

### Requirement: REQ-APUS-05 Listado y búsqueda de APUs

El sistema DEBE (MUST) listar los APUs del proyecto con búsqueda parcial por descripción o por código del
ítem, filtro por estado y paginación `page`/`pageSize`, sin cruzar proyectos, y DEBE (MUST) devolver una
lista vacía cuando no haya coincidencias. Citas: RF-07; D-30, D-31, D-101.

#### Scenario: SC-APUS-21 Búsqueda por descripción

- **Dado** un proyecto con un APU cuya descripción contiene «excavación»
- **Cuando** un actor con lectura busca «excavación»
- **Entonces** el resultado incluye ese APU

#### Scenario: SC-APUS-22 Búsqueda por código del ítem

- **Dado** un APU vinculado a un ítem con código `1.1`
- **Cuando** un actor con lectura busca «1.1»
- **Entonces** el resultado incluye ese APU

#### Scenario: SC-APUS-23 Filtro por estado y paginación

- **Dado** un proyecto con más APUs que el tamaño de página solicitado
- **Cuando** un actor con lectura filtra por estado y pagina
- **Entonces** el sistema devuelve solo los APUs de ese estado, con `page`, `pageSize` y el total

#### Scenario: SC-APUS-24 Sin coincidencias

- **Dado** un proyecto sin APUs que coincidan con la búsqueda
- **Cuando** un actor con lectura busca un término inexistente
- **Entonces** el sistema devuelve una lista vacía y no un error

#### Scenario: SC-APUS-25 El listado no cruza proyectos

- **Dado** dos proyectos de la misma compañía
- **Cuando** se lista uno de ellos
- **Entonces** el resultado no incluye APUs del otro proyecto


### Requirement: REQ-APUS-06 Composición de una Matriz

El sistema DEBE (MUST) permitir consultar, agregar, editar y retirar los componentes de un insumo de tipo
Matriz con la misma semántica de consumo y desperdicio del APU, y DEBE (MUST) exigir que la Matriz y sus
componentes pertenezcan al mismo proyecto. La Matriz queda `FALTA_INFORMACION` sin unidad o sin
componentes, y `POR_COTIZAR` con ambos. Citas: RF-09; BR-008; D-48; UC-05.

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


### Requirement: REQ-APUS-08 Copia de una Matriz con reconciliación

El sistema DEBE (MUST) copiar una Matriz a otro proyecto de la misma compañía creando una Matriz nueva e
independiente y reconciliando cada componente con los insumos del destino; DEBE (MUST) indicar por
componente si existe uno igual, uno similar o ninguno, y DEBE (MUST) exigir la decisión del usuario en los
dos primeros casos. Citas: RF-09; BR-021; D-16, D-25, D-29.

#### Scenario: SC-APUS-35 Copia con componentes inexistentes en el destino

- **Dado** una Matriz con componentes que no existen en el proyecto destino
- **Cuando** un actor autorizado confirma la copia sin decisiones pendientes
- **Entonces** el sistema crea la Matriz y copia cada componente como insumo nuevo e independiente

#### Scenario: SC-APUS-36 Vista previa de la reconciliación

- **Dado** una Matriz de origen y un proyecto destino
- **Cuando** el actor autorizado solicita la vista previa de la copia
- **Entonces** el sistema devuelve por componente la clasificación y los candidatos del destino, sin crear nada

#### Scenario: SC-APUS-37 Componente similar con decisión de usar el existente

- **Dado** una Matriz cuyo componente tiene un insumo similar en el destino
- **Cuando** el actor decide usar el insumo existente
- **Entonces** la Matriz copiada referencia el insumo del destino y conserva su precio

#### Scenario: SC-APUS-38 Componente similar con decisión de conservar el origen

- **Dado** una Matriz cuyo componente tiene un insumo similar en el destino
- **Cuando** el actor decide conservar el insumo de origen
- **Entonces** el sistema crea un insumo nuevo en el destino con el valor de origen y la procedencia registrada

#### Scenario: SC-APUS-39 Falta de decisión en un componente con coincidencia

- **Dado** una Matriz con un componente que existe o es similar en el destino
- **Cuando** el actor confirma la copia sin resolver ese componente
- **Entonces** el sistema rechaza la operación con un error de validación y no crea nada

#### Scenario: SC-APUS-40 Independencia de la copia de la Matriz

- **Dado** una Matriz copiada a otro proyecto
- **Cuando** se cambia la composición en el origen o en el destino
- **Entonces** la otra Matriz conserva su composición original

#### Scenario: SC-APUS-41 Matriz de otra compañía

- **Dado** una Matriz de un proyecto de otra compañía
- **Cuando** un actor autorizado intenta copiarla a su proyecto
- **Entonces** el sistema rechaza la operación sin revelar si la Matriz existe

### Requirement: REQ-APUS-09 Copia de un APU desde otro proyecto de la compañía

El sistema DEBE (MUST) copiar un APU de otro proyecto de la misma compañía hacia un ítem libre del
proyecto destino, conservando su composición, sin aprobación y con el actor como último modificador; DEBE
(MUST) rechazar la copia desde otra compañía sin revelar si el APU existe. Citas: RF-07; BR-018; D-13,
D-16, D-24.

#### Scenario: SC-APUS-42 Copia exitosa

- **Dado** un APU de un proyecto de la misma compañía y un ítem libre del proyecto destino
- **Cuando** un actor autorizado confirma la copia
- **Entonces** el sistema crea un APU nuevo en el destino con la misma descripción, unidad, rendimiento y composición

#### Scenario: SC-APUS-43 Ítem destino ya vinculado

- **Dado** un ítem del proyecto destino que ya tiene un APU
- **Cuando** un actor autorizado intenta copiar un APU hacia ese ítem
- **Entonces** el sistema rechaza la operación con `409` y conserva el APU del ítem

#### Scenario: SC-APUS-44 APU de otra compañía

- **Dado** un APU de un proyecto de otra compañía
- **Cuando** un actor autorizado intenta copiarlo
- **Entonces** el sistema rechaza la operación sin revelar si el APU existe

#### Scenario: SC-APUS-45 Origen en cualquier estado del proyecto

- **Dado** un APU de un proyecto de la misma compañía en estado `FINALIZADO`
- **Cuando** un actor autorizado lo copia a un ítem libre del destino
- **Entonces** el sistema completa la copia porque el estado del origen no restringe la operación

#### Scenario: SC-APUS-46 La copia nace sin aprobación

- **Dado** un APU de origen que tiene una aprobación registrada
- **Cuando** se copia al proyecto destino
- **Entonces** el APU copiado queda sin aprobación y con el actor como último modificador


### Requirement: REQ-APUS-10 Reconciliación de insumos al incorporar un APU

El sistema DEBE (MUST) comparar los insumos del APU de origen con los del proyecto destino y clasificar
cada uno como existente, similar o inexistente; DEBE (MUST) agregar los inexistentes y DEBE (MUST) exigir
la decisión del usuario para los demás, sin decidir por él. Citas: RF-08; D-25.

#### Scenario: SC-APUS-47 Insumo inexistente en el destino

- **Dado** un APU de origen con un insumo que no existe en el proyecto destino
- **Cuando** se ejecuta la copia
- **Entonces** el sistema agrega el insumo al destino sin pedir decisión y la línea apunta al insumo nuevo

#### Scenario: SC-APUS-48 Insumo similar en el destino

- **Dado** un APU de origen con un insumo cuya descripción coincide parcialmente con uno del destino
- **Cuando** el actor autorizado solicita la vista previa de la copia
- **Entonces** el sistema marca el insumo como similar y devuelve los candidatos del destino

#### Scenario: SC-APUS-49 Decisión de usar el insumo existente

- **Dado** un insumo marcado como similar o existente en el destino
- **Cuando** el actor decide usar el insumo existente
- **Entonces** la línea del APU copiado referencia ese insumo y conserva su precio unitario

#### Scenario: SC-APUS-50 Decisión de conservar el insumo de origen

- **Dado** un insumo marcado como similar o existente en el destino
- **Cuando** el actor decide conservar el insumo de origen
- **Entonces** el sistema crea un insumo nuevo en el destino con el valor de origen y registra su procedencia

#### Scenario: SC-APUS-51 Copia sin resolver un insumo con coincidencia

- **Dado** un APU de origen con un insumo que existe o es similar en el destino
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
del proyecto, de `consumption × quantity_work`, sin aplicar `waste_percentage`, y DEBE (MUST) devolver
cero para los insumos que no participan en ninguna composición. Citas: RF-22; R-21; D-33.

#### Scenario: SC-APUS-56 Insumo usado en un ítem

- **Dado** un insumo con consumo `2` en el APU de un ítem con cantidad de obra `10`
- **Cuando** se consulta el conteo de uso del proyecto
- **Entonces** el insumo devuelve un conteo de `20`

#### Scenario: SC-APUS-57 Insumo usado en dos APUs

- **Dado** un insumo con consumo `1` en un APU de cantidad `5` y consumo `3` en otro APU de cantidad `2`
- **Cuando** se consulta el conteo de uso
- **Entonces** el insumo devuelve un conteo de `11`

#### Scenario: SC-APUS-58 El desperdicio no interviene

- **Dado** un insumo con consumo `2`, desperdicio `10` y cantidad de obra `10`
- **Cuando** se consulta el conteo de uso
- **Entonces** el conteo es `20` y no incluye el desperdicio

#### Scenario: SC-APUS-59 Insumo sin uso

- **Dado** un insumo que no participa en ninguna composición del proyecto
- **Cuando** se consulta el conteo de uso
- **Entonces** el sistema devuelve conteo `0` para ese insumo

### Requirement: REQ-APUS-13 Conteo de uso a través de una Matriz

El sistema DEBE (MUST) sumar al conteo de cada componente de una Matriz el producto
`consumption de la Matriz × consumption del componente × quantity_work`, expandiendo un solo nivel, y DEBE
(MUST) conservar además el conteo propio de la Matriz. Citas: RF-22; R-21; BR-008; UC-05; D-33.

#### Scenario: SC-APUS-60 Matriz con dos componentes

- **Dado** una Matriz con componentes de consumo `2` y `3`, usada con consumo `4` en un ítem de cantidad `10`
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


### Requirement: REQ-APUS-14 Autoría de la última modificación

El sistema DEBE (MUST) registrar en el APU el usuario y la fecha UTC de la última modificación de sus datos
o de su composición, fijados por el servidor; la edición de la composición de una Matriz DEBE (MUST)
actualizar la autoría del insumo Matriz. Citas: RF-14; D-08.

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

#### Scenario: SC-APUS-66 La composición de la Matriz actualiza la autoría del insumo

- **Dado** un insumo de tipo Matriz
- **Cuando** un actor autorizado agrega o retira un componente
- **Entonces** la autoría del insumo Matriz queda actualizada con ese actor y la fecha UTC

### Requirement: REQ-APUS-15 Concurrencia optimista

El sistema DEBE (MUST) exigir `rowVersion` en la edición del APU y en cualquier cambio de composición del
APU o de la Matriz, y DEBE (MUST) responder `409` sin aplicar el cambio cuando la versión no coincida con
la vigente. Citas: D-50, D-97.

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

El sistema DEBE (MUST) permitir crear, editar, componer y copiar APUs y Matrices al Director de proyectos
en los proyectos de su compañía y al Presupuestador en sus proyectos asignados, y DEBE (MUST) denegar la
escritura al Administrador y al Auditor. Citas: RF-07; D-34, D-83, D-84; matriz `1.3.2`.

#### Scenario: SC-APUS-71 El Director escribe en su compañía

- **Dado** un Director autenticado en un proyecto de su compañía
- **Cuando** crea, edita, compone o copia un APU o una Matriz
- **Entonces** el sistema aplica la operación

#### Scenario: SC-APUS-72 El Presupuestador asignado escribe

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** crea, edita, compone o copia un APU o una Matriz
- **Entonces** el sistema aplica la operación

#### Scenario: SC-APUS-73 El Presupuestador no asignado intenta escribir

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

### Requirement: REQ-APUS-17 Lectura de APUs, matrices y conteo de uso

El sistema DEBE (MUST) permitir consultar APUs, composiciones, Matrices y conteo de uso al Director en los
proyectos de su compañía, al Presupuestador y al Auditor en sus proyectos asignados, y al Administrador en
cualquier proyecto; el Administrador y el Auditor solo leen. Citas: RF-07, RF-22; D-34, D-83, D-84.

#### Scenario: SC-APUS-76 El Director consulta

- **Dado** un Director autenticado en un proyecto de su compañía
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información del proyecto

#### Scenario: SC-APUS-77 El Presupuestador asignado consulta

- **Dado** un Presupuestador asignado al proyecto
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información del proyecto

#### Scenario: SC-APUS-78 El Administrador consulta en solo lectura

- **Dado** un Administrador autenticado y un proyecto de cualquier compañía
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información y no permite modificarla

#### Scenario: SC-APUS-79 El Auditor asignado consulta

- **Dado** un Auditor asignado al proyecto
- **Cuando** consulta APUs, Matrices y conteo de uso
- **Entonces** el sistema devuelve la información en solo lectura

#### Scenario: SC-APUS-80 El Auditor o el Presupuestador no asignado intenta consultar

- **Dado** un Presupuestador o un Auditor de la misma compañía sin pertenencia al proyecto
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

