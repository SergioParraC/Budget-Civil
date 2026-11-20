# Cambio: paginas-edicion-edt-apu-y-insumos

## Why

El contrato v1 ya expone todo lo necesario para trabajar el EDT, los APUs y los insumos del proyecto, pero el
cliente Angular no tiene pantallas para ello: hoy solo existen la ficha del proyecto, el listado de proyectos y
el catálogo público `/apu`, que es de solo lectura (`APU_PUBLIC_READ`). Sin interfaz, un Director o un
Presupuestador no puede crear capítulos ni ítems, componer un APU ni administrar los insumos de su proyecto, de
modo que los módulos de `E-2b`, `E-3a`, `E-3b` y `E-4a` solo son usables por API. Este cambio abre la etapa de
interfaz para esos tres módulos sin tocar el backend ni el contrato (`D-109`).

## What Changes

- Se agregan tres módulos del cliente Angular, **todos dentro del contexto de un proyecto**:
  - **EDT en una sola pantalla** (`/proyectos/:id/edt`): árbol único que mezcla capítulos e ítems raíz, con alta
    de capítulo, alta y edición de ítem, baja controlada, búsqueda, tarjeta de actividad, **arrastrar y soltar**
    más acciones explícitas de movimiento, y **alertas en español que explican la causa** de cada conflicto.
  - **Alta y edición de APU en páginas propias**: `/proyectos/:id/edt/items/nuevo` crea el ítem del EDT y su APU
    en una sola llamada a `POST /wbs/items` (no existe alta individual de APU) y
    `/proyectos/:id/edt/items/:itemId/apu` es la página de edición del APU del ítem.
  - **Insumos del proyecto** (`/proyectos/:id/insumos`): listado paginado y filtrado en servidor; alta, detalle,
    edición, copia, cotización masiva y baja **en modales**, incluida la composición de una Matriz y sus
    componentes en submodales.
- Se agregan accesos “EDT” e “Insumos” en la ficha del proyecto. **No** se crean entradas nuevas en el menú
  lateral ni en el dashboard, y el catálogo público `/apu` queda intacto.
- Se agregan los permisos `wbsRead`, `wbsWrite`, `apusRead`, `apusWrite`, `resourcesRead`, `resourcesWrite`,
  `suppliersRead` y `catalogsWrite` al mapa de permisos del cliente. Las acciones de escritura se **ocultan**
  cuando el permiso no está en el token.
- El cliente **no replica fórmulas ni estados**: costos, subtotales, AIU, estado del APU y estado de cotización
  se presentan tal como los devuelve el servidor; los valores nulos se muestran como “—”.
- Se amplía el mapa de mensajes de error en español con los códigos `WBS_*`, `APU_*`, `MATRIX_*`, `RESOURCE_*`,
  `UNIT_*` y `PROJECT_NOT_FOUND`, conservando el código y el `traceId` solo como detalle técnico.
- Se mantiene la identidad visual vigente: tema PrimeNG **Aura**, variables de `styles.scss`, estructura del
  layout y convención de clases bloque/elemento de `proyecto-detalle.scss`.
- **Sin cambios de backend, de contrato v1 ni de base de datos.** Solo se consumen los endpoints existentes.

**Fuera de alcance:** cambios en `backend/` o en el contrato v1; copia de APU entre proyectos y aprobaciones;
presupuesto del proyecto (`GET /budget`), alertas de tope y cierre; catálogo público `/apu` y autenticación;
anidamiento de Matrices; exportación/importación; pruebas automatizadas de frontend (el proyecto no tiene suite
de pruebas del cliente).

## Capabilities

### New Capabilities

Ninguna. El cambio no introduce capacidades nuevas: la interfaz expone comportamiento ya especificado.

### Modified Capabilities

- `edt`: se agregan requisitos de la pantalla única del EDT (árbol, alta de capítulo, arrastrar y soltar,
  acciones explícitas de movimiento, alertas de conflicto en español, reordenamiento, bajas, búsqueda, actividad,
  presentación de costos y permisos de la interfaz).
- `apus`: se agregan requisitos de la página de alta del APU con su ítem del EDT y de la página de edición del
  APU (composición, omisión de `resources` sin cambios, conflictos de concurrencia y permisos).
- `insumos`: se agregan requisitos del listado y de los modales de alta, detalle, edición, copia, cotización
  masiva, baja y composición de Matriz con submodales.
- `unidades`: se agrega el requisito del alta de unidad desde el formulario de APU e insumo.

## Impact

- **Contrato de API**: sin cambios. Se consumen `GET /projects/{projectId}`, `/wbs` (árbol, `search`, `activity`,
  `codes/validate`, `chapters`, `items`, `reorder`, `move`), `/apus` y `/apus/{apuId}`, `/matrices/*`,
  `/resources`, `/resources/by-unit/{shortUnit}`, `/resources/suggestions`, `/resources/copy`,
  `/resources/quotation-status`, `/resource-types`, `/catalogs/units`, `/catalogs/*` y `/suppliers/options`.
- **Base de datos**: sin cambios ni migraciones.
- **Permisos**: sin permisos nuevos en el backend; el cliente reconoce `WBS_READ`, `WBS_WRITE`, `APUS_READ`,
  `APUS_WRITE`, `RESOURCES_READ`, `RESOURCES_WRITE`, `SUPPLIERS_READ` y `CATALOGS_WRITE`.
- **Código**: `Frontend/src/app/core/auth/permissions.ts`, `Frontend/src/app/core/http/api-error.ts`,
  `Frontend/src/app/app.routes.ts`, `Frontend/src/app/features/proyectos/proyecto-detalle/*` y dos features
  nuevas (`features/wbs`, `features/resources`) con sus modelos y servicios en `core/models` y `core/services`.
- **Estilos**: se reutilizan tema Aura, `styles.scss`, layout y convenciones de `proyecto-detalle.scss`; no se
  agrega otra librería de UI ni otra paleta.
- **Documentación**: al archivar, `openspec/project.md §8` mantiene las capacidades existentes y
  `PLAN_INGENIERIA.md` registra que `D-43` menciona Angular Material mientras el cliente implementado usa
  PrimeNG (`D-109`).

## Criterios de aceptación

1. `openspec validate paginas-edicion-edt-apu-y-insumos --strict` pasa sin errores.
2. Desde `/proyectos/:id` se accede a EDT e Insumos; no hay entradas nuevas en el menú lateral ni en el dashboard
   para Insumos o para crear APUs, y `/apu` queda intacto.
3. El EDT es una única pantalla y permite crear capítulo raíz, subcapítulo, ítem raíz e ítem dentro de capítulo.
4. Se mueve un capítulo con subárbol y un ítem, por arrastrar y soltar y por “Mover a…”, a la raíz o a otro
   capítulo, en una posición concreta.
5. Los destinos inválidos no se ofrecen; si el servidor rechaza el movimiento aparece un modal en español con la
   causa, la interfaz revierte el cambio y el árbol se refresca cuando corresponde.
6. Se reordenan hermanos y ningún código cambia.
7. Se elimina un capítulo vacío; con hijos responde `WBS_NODE_HAS_CHILDREN` y con APUs `WBS_CHAPTER_HAS_APU`, con
   los mensajes definidos; al eliminar un ítem se advierte que se borra su APU.
8. El alta de APU es una página y crea ítem + APU + composición en una sola llamada; sin líneas el APU queda
   `FALTA_INFORMACION`.
9. La edición del APU es una página nueva, envía `rowVersion` y `apuRowVersion`, omite `resources` cuando la
   composición no cambió y maneja `409` sin perder datos.
10. El listado de insumos filtra, ordena y pagina en el servidor.
11. El alta y la edición de insumos se hacen en modales, con validaciones, sugerencias de duplicados y estado de
    cotización calculado por el servidor.
12. El detalle del insumo es un modal; para una Matriz muestra sus componentes y permite agregar, editar y quitar
    en submodales, respetando `APUS_READ`/`APUS_WRITE`.
13. La baja de insumo funciona y se bloquea con `409 RESOURCE_IN_USE` mostrando el motivo; la baja de una Matriz
    advierte que elimina sus componentes.
14. La copia y la cotización masiva funcionan y muestran el resumen del servidor.
15. Un usuario sin `WBS_WRITE`, `APUS_WRITE`, `RESOURCES_WRITE` o `CATALOGS_WRITE` no ve las acciones
    correspondientes y recibe manejo correcto de `403`.
16. Ningún costo, subtotal, AIU ni estado se calcula en Angular; los nulos se muestran como “—”.
17. Se mantienen los estilos actuales (tema Aura, `styles.scss`, layout y convenciones existentes).
18. Textos en español, identificadores en inglés y sin cambios de backend.
