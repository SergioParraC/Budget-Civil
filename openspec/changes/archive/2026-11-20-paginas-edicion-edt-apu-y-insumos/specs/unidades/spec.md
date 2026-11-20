# Spec Delta: unidades

## ADDED Requirements

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
