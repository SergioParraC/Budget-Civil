# unidades Specification

## Purpose

Define el catálogo global de unidades de medida que el usuario puede crear y consultar para usarlo en APUs,
insumos y actividades futuras, sin convertir la unidad en una clave foránea: el APU y el insumo guardan la
sigla textual en su propio atributo `unit`.

## Requirements

### Requirement: REQ-UNIDADES-01 Catálogo global de unidades

El sistema DEBE (MUST) mantener un catálogo global de unidades de medida con `id` numérico autoincremental,
`short_unit` de 1 a 10 caracteres y `unit` de 1 a 100 caracteres; `short_unit` DEBE (MUST) ser único sin
distinguir mayúsculas y el catálogo NO DEBE (MUST NOT) tener clave foránea desde APU o insumo. Citas: RF-05,
RF-07; D-16, D-31; `05 §3`.

#### Scenario: SC-UNIDADES-01 Catálogo con tres atributos

- **Dado** un usuario autorizado que registra la sigla `M2` y la unidad `Metro cuadrado`
- **Cuando** el sistema guarda la unidad
- **Entonces** la unidad queda con un `id` numérico, `shortUnit = M2` y `unit = Metro cuadrado`

#### Scenario: SC-UNIDADES-02 Sigla duplicada sin distinguir mayúsculas

- **Dado** un catálogo con la sigla `M2`
- **Cuando** se intenta registrar la sigla `m2`
- **Entonces** el sistema responde `409 UNIT_ALREADY_EXISTS` y conserva la unidad existente

#### Scenario: SC-UNIDADES-03 Longitud fuera de rango

- **Dado** un usuario autorizado que envía una sigla vacía o de más de 10 caracteres, o una unidad vacía o
  de más de 100 caracteres
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 UNIT_VALIDATION_ERROR` y no crea la unidad

#### Scenario: SC-UNIDADES-04 El APU y el insumo guardan la sigla textual

- **Dado** un catálogo con `M2` y un APU o un insumo que usan esa sigla
- **Cuando** se guardan el APU o el insumo
- **Entonces** el valor `M2` queda en el atributo `unit` de la tabla correspondiente y no se guarda el `id`
  del catálogo

#### Scenario: SC-UNIDADES-05 Unidad fuera del catálogo

- **Dado** un APU o un insumo cuya unidad no existe en el catálogo
- **Cuando** se guarda el registro
- **Entonces** el sistema acepta la unidad textual y no la rechaza por no estar en el catálogo

### Requirement: REQ-UNIDADES-02 Alta de una unidad

El sistema DEBE (MUST) permitir crear una unidad en `POST /api/v1/catalogs/units` con `shortUnit` y `unit`,
recortar los espacios de ambos valores y devolver `201` con `{ id, shortUnit, unit }`; DEBE (MUST) responder
`400 UNIT_VALIDATION_ERROR` ante datos inválidos y `409 UNIT_ALREADY_EXISTS` ante una sigla repetida. Citas:
RF-05, RF-07; D-16.

#### Scenario: SC-UNIDADES-06 Alta válida

- **Dado** un usuario autorizado y un catálogo sin la sigla `M2`
- **Cuando** envía `shortUnit = M2` y `unit = Metro cuadrado`
- **Entonces** el sistema responde `201` con el `id`, `shortUnit` y `unit` creados

#### Scenario: SC-UNIDADES-07 Recorte de espacios

- **Dado** un usuario autorizado que envía `shortUnit = " M2 "` y `unit = " Metro cuadrado "`
- **Cuando** confirma el alta
- **Entonces** el sistema guarda y devuelve ambos valores sin espacios en los extremos

#### Scenario: SC-UNIDADES-08 Datos inválidos

- **Dado** un usuario autorizado que envía una sigla o una unidad vacía o fuera de la longitud permitida
- **Cuando** confirma el alta
- **Entonces** el sistema responde `400 UNIT_VALIDATION_ERROR` y no crea la unidad

#### Scenario: SC-UNIDADES-09 Sigla repetida

- **Dado** un catálogo con la sigla `M2`
- **Cuando** se intenta crear otra unidad con `M2` o `m2`
- **Entonces** el sistema responde `409 UNIT_ALREADY_EXISTS`

### Requirement: REQ-UNIDADES-03 Consulta del catálogo de unidades

El sistema DEBE (MUST) devolver en `GET /api/v1/catalogs/units` todas las unidades con
`{ id, shortUnit, unit }`, ordenadas por `shortUnit`; en este incremento NO DEBE (MUST NOT) paginar el
catálogo. Citas: RF-05, RF-07; D-16.

#### Scenario: SC-UNIDADES-10 Catálogo con unidades

- **Dado** un catálogo con `M2`, `ML` y `UND`
- **Cuando** un actor autorizado consulta el catálogo
- **Entonces** el sistema devuelve las tres unidades con su `id`, `shortUnit` y `unit`

#### Scenario: SC-UNIDADES-11 Catálogo vacío

- **Dado** un catálogo sin unidades
- **Cuando** se consulta el catálogo
- **Entonces** el sistema devuelve una colección vacía

#### Scenario: SC-UNIDADES-12 Orden por sigla

- **Dado** un catálogo con `UND`, `M2` y `ML`
- **Cuando** se consulta el catálogo
- **Entonces** el sistema devuelve las unidades ordenadas por `shortUnit`

### Requirement: REQ-UNIDADES-04 Autorización del catálogo

El sistema DEBE (MUST) permitir la consulta del catálogo a los cuatro roles con `CATALOGS_READ` y la
creación solo al Administrador, al Director y al Presupuestador con `CATALOGS_WRITE`; el Auditor NO DEBE
(MUST NOT) crear unidades. Citas: RF-05; matriz `1.3.2`.

#### Scenario: SC-UNIDADES-13 Los roles autorizados crean unidades

- **Dado** un Administrador, un Director o un Presupuestador autenticado
- **Cuando** crea una unidad con datos válidos
- **Entonces** el sistema la guarda en el catálogo

#### Scenario: SC-UNIDADES-14 El Auditor intenta crear una unidad

- **Dado** un Auditor autenticado
- **Cuando** intenta crear una unidad
- **Entonces** el sistema responde `403`

#### Scenario: SC-UNIDADES-15 Un actor sin CATALOGS_WRITE intenta crear

- **Dado** un usuario autenticado sin el permiso `CATALOGS_WRITE`
- **Cuando** intenta crear una unidad
- **Entonces** el sistema responde `403`

### Requirement: REQ-UNIDADES-05 La unidad no catalogada permanece en el APU o insumo

El sistema NO DEBE (MUST NOT) insertar ni actualizar registros de `measurement_units` cuando se registra un
APU o un insumo con una unidad que no existe en el catálogo; en ese caso el valor textual DEBE (MUST)
guardarse únicamente en el atributo `unit` del APU o del insumo que se crea. Citas: RF-05, RF-07; D-16,
D-31; `05 §3`.

#### Scenario: SC-UNIDADES-16 Unidad del APU fuera del catálogo

- **Dado** un catálogo que solo contiene `M2`
- **Cuando** un usuario autorizado registra un APU con la unidad `GL`
- **Entonces** el APU queda guardado con `unit = GL` y el catálogo sigue conteniendo solo `M2`

#### Scenario: SC-UNIDADES-17 Unidad del insumo fuera del catálogo

- **Dado** un catálogo que solo contiene `M2`
- **Cuando** un usuario autorizado registra un insumo con la unidad `GL`
- **Entonces** el insumo queda guardado con `unit = GL` y no se crea la unidad `GL` en el catálogo

#### Scenario: SC-UNIDADES-18 Alta explícita de la unidad usada

- **Dado** un proyecto con un insumo cuya unidad `GL` no está en el catálogo
- **Cuando** se consulta el catálogo y después se crea `GL` con `POST /api/v1/catalogs/units`
- **Entonces** `GL` no aparece antes del alta y aparece únicamente después de crearla explícitamente

### Requirement: REQ-UNIDADES-06 Alta de unidad desde el formulario de APU e insumo

El cliente DEBE (MUST) ofrecer el alta de una unidad desde el formulario de APU y de insumo cuando el token incluye
`CATALOGS_WRITE`, llamando `POST /catalogs/units` y dejando la nueva sigla seleccionada. Citas: RF-05, RF-07; D-16;
`REQ-UNIDADES-02`, `REQ-UNIDADES-04`.

#### Scenario: SC-UNIDADES-19 Alta desde el formulario del APU

- **Dado** un usuario con `CATALOGS_WRITE` en la página de alta o edición de un APU
- **Cuando** elige “Nueva unidad”, escribe `shortUnit = M2` y `unit = Metro cuadrado`, y el servidor responde `201`
- **Entonces** el submodal se cierra, la sigla `M2` queda seleccionada en el campo `unit` del APU y el formulario
  conserva los demás datos escritos

#### Scenario: SC-UNIDADES-20 Alta desde el formulario del insumo

- **Dado** un usuario con `CATALOGS_WRITE` en el modal de alta o edición de un insumo
- **Cuando** crea la unidad `GL` desde el submodal
- **Entonces** el cliente deja `GL` como unidad del insumo y el insumo se puede guardar con ese valor

#### Scenario: SC-UNIDADES-21 Sigla duplicada

- **Dado** un catálogo que ya contiene `M2`
- **Cuando** el servidor responde `409 UNIT_ALREADY_EXISTS`
- **Entonces** el cliente muestra “Ya existe una unidad con esa sigla. Elige otra o usa la existente.” y mantiene el
  submodal abierto con los valores escritos

#### Scenario: SC-UNIDADES-22 Datos inválidos de la unidad

- **Dado** un submodal con sigla vacía, sigla de más de 10 caracteres, unidad vacía o unidad de más de 100 caracteres
- **Cuando** el cliente valida o el servidor responde `400 UNIT_VALIDATION_ERROR`
- **Entonces** muestra el mensaje en español correspondiente y no crea la unidad

#### Scenario: SC-UNIDADES-23 Alta de unidad sin permiso

- **Dado** un usuario sin `CATALOGS_WRITE`
- **Cuando** abre el selector de unidad del APU o del insumo
- **Entonces** la opción “Nueva unidad” no se muestra y el formulario sigue aceptando una unidad escrita a mano

#### Scenario: SC-UNIDADES-24 Catálogo disponible en los siguientes formularios

- **Dado** una unidad creada desde el formulario
- **Cuando** el usuario abre de nuevo el selector de unidad
- **Entonces** el cliente vuelve a consultar `GET /catalogs/units` y muestra la unidad recién creada entre las
  opciones
