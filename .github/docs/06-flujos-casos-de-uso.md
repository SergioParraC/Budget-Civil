# Flujos y casos de uso

> Documento actualizado el 27/10/2026. Actores y reglas según `PLAN_INGENIERIA.md` (v0.26).

## 1. Actores
| Actor | Ámbito | Responsabilidad |
|---|---|---|
| Administrador | Plataforma, sin compañía | Usuarios, roles y compañías; lectura de proyectos e insumos |
| Director de proyectos | Una compañía y sus proyectos | Configuración, asignaciones, modo de firma y cierre |
| Presupuestador | Proyectos asignados | EDT, insumos, APUs, matrices y cotización |
| Auditor | Proyectos asignados | Lectura y aprobación de APUs cuando el proyecto lo exige |

## 2. Casos de uso
| ID | Caso de uso | Actores |
|---|---|---|
| UC-01 | Administrar usuarios, roles y compañías | Administrador, Director |
| UC-02 | Configurar proyecto | Director |
| UC-03 | Estructurar la EDT | Director, Presupuestador |
| UC-04 | Gestionar insumos | Director, Presupuestador |
| UC-05 | Crear o copiar APU y reconciliar insumos | Director, Presupuestador |
| UC-06 | Calcular, cotizar y alertar | Sistema, Director, Presupuestador |
| UC-07 | Aprobar y cerrar el presupuesto | Director, Presupuestador, Auditor |
| UC-08 | Consultar el catálogo público | Cualquiera (anónimo) |

## 3. UC-01 Administrar usuarios, roles y compañías
- **Objetivo:** gestionar cuentas, roles y pertenencia a compañías.
- **Actor principal:** Administrador; el Director participa en su compañía.
- **Precondiciones:** sesión con JWT válido.
- **Postcondiciones:** usuario creado, rol cambiado o compañía asignada.
- **Flujo principal:**
  1. El Administrador crea la compañía o selecciona una existente.
  2. Crea el usuario con correo, nombre y rol.
  3. El sistema valida correo único y `companyId` (nulo solo para Administrador).
  4. El Director busca usuarios y los asigna a su compañía o a sus proyectos.
  5. Se envían los correos de creación y recuperación.
- **Alternos:** cambio de rol global por el Administrador; retiro de un usuario de la compañía por el Director.
- **Errores:** correo duplicado; usuario ya asignado a otra compañía; rol inexistente.

## 4. UC-02 Configurar proyecto
- **Objetivo:** definir divisa, AIU, tope, modo de firma, requisito de auditor y visibilidad.
- **Actor:** Director.
- **Precondiciones:** compañía propia creada.
- **Postcondiciones:** proyecto con configuración única.
- **Flujo:**
  1. El Director crea el proyecto.
  2. Define `currency` ISO, `aiuAdmin`, `aiuContingency`, `aiuProfit`, `budgetLimit`, `signatureMode`, `requiresAuditor` e `isPublic`.
  3. El sistema valida rangos y guarda la configuración.
- **Alternos:** cambio de modo de firma antes de la primera aprobación.
- **Errores:** divisa inválida; AIU fuera de 0–100; configuración duplicada.

## 5. UC-03 Estructurar la EDT
- **Objetivo:** crear capítulos e ítems con códigos únicos.
- **Actores:** Director y Presupuestador.
- **Precondiciones:** proyecto existente y no finalizado.
- **Postcondiciones:** árbol con numeración y códigos válidos.
- **Flujo:**
  1. Se crea un capítulo; el sistema propone el número según la posición.
  2. Se agregan subcapítulos sin límite de profundidad.
  3. Se crea un ítem con cantidad de obra y APU.
  4. Se registra el código en `WbsCode`; el sistema valida unicidad por proyecto.
  5. Se puede buscar por código de ítem o nombre de capítulo.
- **Alternos:** el usuario sobrescribe el código; los hijos heredan el prefijo (`CIM.1`).
- **Errores:** código duplicado; cantidad ≤ 0; padre de otro proyecto.

## 6. UC-04 Gestionar insumos
- **Objetivo:** registrar, listar, filtrar y copiar insumos del proyecto.
- **Actores:** Director y Presupuestador.
- **Precondiciones:** proyecto con catálogo de tipos cargado.
- **Postcondiciones:** insumo creado o copiado, con estado de cotización calculado.
- **Flujo:**
  1. Se crea el insumo con descripción, tipo, unidad, precio, marca y datos opcionales.
  2. El sistema evalúa los cuatro campos obligatorios y asigna `FALTA_INFORMACION` o `POR_COTIZAR`.
  3. Se listan los insumos con filtros por tipo, estado, marca, descripción, ciudad y precio.
  4. Se puede copiar un insumo desde otro proyecto de la compañía; la copia es independiente.
  5. Al copiar un APU se sugiere un insumo similar y el usuario decide.
- **Alternos:** cambio de estado a `COTIZADO`; edición de precio y marca.
- **Errores:** tipo inexistente; precio negativo; insumo de otro proyecto.

## 7. UC-05 Crear o copiar APU y reconciliar insumos
- **Objetivo:** definir la composición técnica de un APU y vincularlo al ítem.
- **Actores:** Director y Presupuestador.
- **Precondiciones:** ítem de la EDT e insumos disponibles.
- **Postcondiciones:** APU 1:1 con el ítem, sin aprobación y con el editor como último modificador.
- **Flujo:**
  1. Se crea el APU con descripción, unidad y rendimiento > 0.
  2. Se agregan insumos con consumo y desperdicio.
  3. Se puede vincular una Matriz; se expande un solo nivel.
  4. Se puede copiar un APU desde otro proyecto de la compañía o del catálogo público.
  5. Al copiar se comparan los insumos con el destino: los inexistentes se agregan y los similares se sugieren.
  6. El usuario decide usar el insumo existente o conservar el de origen.
- **Alternos:** copia de una Matriz, que queda independiente y exige reconciliar sus componentes.
- **Errores:** rendimiento ≤ 0; anidamiento de matrices; copia desde otra compañía; APU ya vinculado a otro ítem.

## 8. UC-06 Calcular, cotizar y alertar
- **Objetivo:** calcular costos, cotizar insumos y alertar sobre el tope.
- **Actores:** Sistema, Director y Presupuestador.
- **Precondiciones:** APUs con composición e insumos.
- **Postcondiciones:** costos recalculados, insumos cotizados y alerta evaluada.
- **Flujo:**
  1. El motor calcula el costo directo del APU y el total del ítem.
  2. Se aplica el AIU del proyecto.
  3. Se calcula el conteo de uso por insumo.
  4. El usuario marca insumos como `COTIZADO` de forma masiva por selección, filtro o totalidad.
  5. Si el costo supera el tope, se muestra la alerta.
- **Alternos:** recálculo tras cambiar precio, consumo o rendimiento.
- **Errores:** datos incompletos (`FALTA_INFORMACION`); rendimiento inválido.

## 9. UC-07 Aprobar y cerrar el presupuesto
- **Objetivo:** aprobar cada APU según el modo de firma y cerrar el proyecto.
- **Actores:** Presupuestador, Auditor y Director.
- **Precondiciones:** APUs calculados y todos los insumos cotizados.
- **Postcondiciones:** un registro de aprobación por APU y proyecto en `FINALIZADO`.
- **Flujo:**
  1. El Presupuestador firma los APUs que le corresponden, según el modo.
  2. En el modo con auditor, el Auditor aprueba cada APU.
  3. El Director verifica que todos los APUs tengan la aprobación exigida.
  4. Si todo está cotizado y aprobado, ejecuta el cierre.
  5. El sistema registra la aprobación final y cambia el estado.
- **Alternos:** edición de un APU aprobado, que elimina su aprobación y exige una nueva.
- **Errores:** insumos sin cotizar; APUs sin aprobación; actor de otra compañía; falta de firma cuando el modo la exige.

## 10. UC-08 Consultar el catálogo público
- **Objetivo:** consultar y copiar composiciones de proyectos públicos.
- **Actor:** cualquiera, sin autenticación.
- **Precondiciones:** proyectos públicos con APUs e insumos.
- **Postcondiciones:** resultados sin autoría ni empresa.
- **Flujo:**
  1. El usuario busca por palabra clave, tipo o rendimiento.
  2. El sistema consulta solo proyectos públicos.
  3. Devuelve APUs e insumos con precios y contactos, sin `companyId`, autor ni firma.
  4. El usuario copia un APU o un insumo; la copia es independiente.
- **Alternos:** sin resultados, devuelve lista vacía.
- **Errores:** ninguno; el acceso es anónimo y limitado por IP.

## 11. Mapeo a requerimientos
| UC | RF | BR |
|---|---|---|
| UC-01 | RF-01, RF-02, RF-18 | BR-001 |
| UC-02 | RF-03 | BR-019, BR-020 |
| UC-03 | RF-04 | BR-015 |
| UC-04 | RF-05, RF-06, RF-16, RF-20 | BR-009, BR-010 |
| UC-05 | RF-07, RF-08, RF-09, RF-22 | BR-008, BR-016, BR-017, BR-018, BR-021 |
| UC-06 | RF-10, RF-11, RF-14, RF-17 | BR-003, BR-007 |
| UC-07 | RF-12, RF-13, RF-21 | BR-011, BR-012, BR-013 |
| UC-08 | RF-15 | BR-005, BR-006, BR-013 |

## 12. Mapeo a pruebas
| UC | Verificación |
|---|---|
| UC-01 | T-I-01, T-S-01, T-S-04 |
| UC-02 | T-I-03 |
| UC-03 | T-U-01, T-I-04 |
| UC-04 | T-U-02, T-I-05, T-I-06, T-I-18, T-U-08 |
| UC-05 | T-U-03, T-U-04, T-U-11, T-I-07, T-I-08, T-I-09, T-I-20 |
| UC-06 | T-U-05, T-U-06, T-U-09, T-I-10, T-I-16 |
| UC-07 | T-U-07, T-U-10, T-I-11, T-I-12, T-I-13, T-I-19 |
| UC-08 | T-I-14, T-S-02, T-S-06 |

