# Tasks

## 1. Permisos y modelos

- [x] 1.1 Agregar `wbsRead`, `wbsWrite`, `apusRead`, `apusWrite`, `resourcesRead`, `resourcesWrite`, `suppliersRead` y `catalogsWrite` a `core/auth/permissions.ts` y verificar que `ng build` compila sin errores de tipos
- [x] 1.2 Crear `core/models/wbs.model.ts` con los tipos del árbol, la búsqueda, la actividad, la validación de código y los comandos de capítulo, ítem, movimiento y reordenamiento, verificando que cada campo coincide con `WbsDtos.cs` y `WbsItemApuDtos.cs`
- [x] 1.3 Crear `core/models/apu.model.ts` con el detalle del APU, los grupos de recursos, los costos indirectos y la composición de la Matriz, verificando los nombres contra `ApuDtos.cs`
- [x] 1.4 Crear `core/models/resource.model.ts` y `core/models/unit.model.ts` con insumo, tipos, sugerencias, cotización masiva, componentes de Matriz y unidades, verificando contra `ResourceDto.cs`, `ChangeResourceQuotationStatusCommand.cs` y `UnitDto.cs`

## 2. Servicios HTTP de EDT, APU, insumos, matrices y unidades

- [x] 2.1 Crear `WbsService` con árbol, búsqueda, actividad, validación de código, capítulos, ítems, `move` y `reorder`; verificar que cada método apunta a la ruta y al permiso del mapa de `design.md` D14
- [x] 2.2 Crear `ApuService` con `GET /apus`, `GET /apus/{apuId}` y las cuatro operaciones de componentes de Matriz; verificar los tipos de respuesta contra el contrato
- [x] 2.3 Crear `ResourceService` con listado, detalle, alta, edición, baja, copia, sugerencias, `by-unit`, tipos y cotización masiva; verificar que los filtros vacíos no se envían como parámetros
- [x] 2.4 Crear `UnitService` y `SupplierService`, y el helper `project-context.ts` que valida el proyecto de la ruta y produce el estado “Proyecto no disponible”; verificar que `ng build` compila

## 3. Shell, rutas y accesos del proyecto

- [x] 3.1 Declarar en `app.routes.ts` las rutas `/proyectos/:id/edt`, `/proyectos/:id/edt/items/nuevo`, `/proyectos/:id/edt/items/:itemId/apu` y `/proyectos/:id/insumos` con `loadComponent` y los `permissionGuard` definidos; verificar la navegación directa por URL
- [x] 3.2 Agregar la tarjeta “Módulos del proyecto” en `proyecto-detalle.html` con los accesos “EDT” e “Insumos” visibles según permiso; verificar que no se agregan ítems a `navGroups` de `main-layout.ts` ni al dashboard y que `/apu` sigue igual
- [x] 3.3 Implementar el estado de proyecto no disponible en las tres pantallas (`404 PROJECT_NOT_FOUND` y `403`) con enlace a `/proyectos`; verificar con un `projectId` inexistente

## 4. Árbol del EDT y alta de capítulo

- [x] 4.1 Construir `Edt` con `p-treeTable`, las columnas de `REQ-EDT-25` y los estados de carga, vacío y error con reintento; verificar sobre un proyecto con capítulos, subcapítulos e ítems raíz
- [x] 4.2 Implementar `chapter-form-dialog` con código validado al salir del campo, nombre y padre (raíz o capítulo), refresco y selección del nodo creado; verificar un alta en raíz y otra dentro de un capítulo
- [x] 4.3 Mostrar `unitCost`, `directCost` y el estado del APU como etiqueta de solo lectura, con “—” y ayuda cuando son nulos; verificar con un ítem sin APU y con un APU incompleto
- [x] 4.4 Implementar la búsqueda con `GET /wbs/search` (resultados sin costos) y la tarjeta de actividad con ocultamiento ante `403`; verificar la expansión y el resaltado del nodo elegido

## 5. Arrastrar y soltar y acciones de movimiento

- [x] 5.1 Integrar `CdkDropList`/`CdkDrag` con un contenedor por capítulo y la zona de raíz, `enterPredicate` por tipo de nodo y subárbol, y el texto de rechazo del subárbol propio; verificar que un ítem nunca acepta hijos
- [x] 5.2 Calcular `targetParentChapterId` y `targetIndex` para soltar sobre un capítulo, entre filas y en la raíz, y enviar `POST /wbs/move` o `POST /wbs/reorder` según el caso; verificar el índice después de retirar el nodo
- [x] 5.3 Implementar `move-node-dialog` con destino y posición, excluyendo el propio subárbol, y las acciones “Subir” y “Bajar” con los extremos deshabilitados; verificar que el arrastre no es la única vía
- [x] 5.4 Aplicar el movimiento de forma optimista, revertirlo ante rechazo y recargar `GET /wbs` tras `200`; verificar el orden y los costos de los capítulos de origen y destino

## 6. Alertas de conflicto y mapa de mensajes

- [x] 6.1 Ampliar `CODE_MESSAGES` de `core/http/api-error.ts` con todos los códigos de la tabla de `design.md` D6 y verificar que cada mensaje está en español y no expone el código como texto principal
- [x] 6.2 Crear `conflict-dialog` con título, mensaje, botón “Entendido”, acción “Actualizar árbol” cuando aplica y detalle técnico plegable con código y `traceId`; verificar los escenarios SC-EDT-121…127 de `REQ-EDT-29`
- [x] 6.3 Conectar el diálogo a las operaciones de alta, edición, movimiento, reordenamiento y baja del EDT; verificar que `WBS_MOVE_CYCLE` conserva el árbol y resalta el nodo y que `WBS_CONCURRENCY_CONFLICT` refresca

## 7. Reordenamiento, edición y bajas del EDT

- [x] 7.1 Implementar `item-form-dialog` con cantidad de obra obligatoria, código opcional y bloque APU opcional, enviando `rowVersion` y `apuRowVersion`; verificar que sin `APUS_WRITE` no se envía el objeto `apu`
- [x] 7.2 Implementar “Abrir APU” hacia `/proyectos/:id/edt/items/:itemId/apu` y el acceso “Nuevo APU” solo con `WBS_WRITE` y `APUS_WRITE`; verificar la navegación y el ocultamiento por permiso
- [x] 7.3 Implementar la baja de ítem y de capítulo con confirmación explícita, advertencia de la eliminación del APU y del capítulo vacío, y `rowVersion` en la query; verificar `204`, `WBS_NODE_HAS_CHILDREN` y `WBS_CHAPTER_HAS_APU`
- [x] 7.4 Verificar que el árbol se refresca tras cada alta, edición, baja, movimiento y reordenamiento y que ningún código se renumera

## 8. Página “Nuevo APU”

- [x] 8.1 Crear `ApuCreate` con título, subtítulo y el bloque EDT (código validado, ubicación con valor inicial desde la fila de origen, cantidad de obra mayor que cero); verificar el alta de un ítem raíz y de un ítem dentro de un capítulo
- [x] 8.2 Crear `composition-editor` con selector de insumos del proyecto, filtro “Solo insumos con la unidad del APU” sobre `GET /resources/by-unit/{shortUnit}` y validación de consumo y desperdicio; verificar que se admiten cero líneas con el aviso de `FALTA_INFORMACION`
- [x] 8.3 Enviar una sola llamada a `POST /wbs/items` con `code`, `wbsChapterId`, `quantityWork` y `apu` (`description`, `unit`, `resources`), mostrar el resumen devuelto e invalidar el árbol; verificar con y sin líneas
- [x] 8.4 Verificar que la página no abre sin `WBS_WRITE` o `APUS_WRITE` y que los errores de código, cantidad e insumo se muestran sin perder los datos del formulario

## 9. Página de edición del APU

- [x] 9.1 Crear `ApuEdit` que cargue `GET /wbs` para ubicar el ítem y `GET /apus/{apuId}` para el detalle, con estados de carga, error y “Este ítem no tiene APU”; verificar ambos casos
- [x] 9.2 Mostrar las secciones de ítem, APU, composición y costos del servidor (`directCost`, `indirectCosts`, `total`) con “—” en los nulos; verificar que ningún importe se calcula en el navegador
- [x] 9.3 Guardar con `PUT /wbs/items/{itemId}` enviando `quantityWork`, `rowVersion` y `apu` con `apuRowVersion`, y `apu.resources` solo cuando la composición cambió; verificar con `compositionDirty` en falso y en verdadero
- [x] 9.4 Manejar `409 APU_CONCURRENCY_CONFLICT`, `409 WBS_CONCURRENCY_CONFLICT`, `APU_RESOURCE_NOT_FOUND` y `APU_RESOURCE_PROJECT_MISMATCH` con mensaje en español, refresco y datos escritos conservados
- [x] 9.5 Implementar “Volver al EDT”, “Guardar” y “Descartar cambios” con confirmación cuando hay cambios sin guardar; verificar el bloqueo de doble envío con `saving`
- [x] 9.6 Habilitar el código del ítem en la página (`SC-APUS-142`): validarlo con `GET /wbs/codes/validate` enviando `excludeNodeId`, enviar `code` en `PUT /wbs/items/{itemId}` solo cuando cambió y mostrar `WBS_CODE_DUPLICATED`/`WBS_CODE_INVALID` sin perder los datos

## 10. Listado de insumos con filtros

- [x] 10.1 Crear `ResourceList` con `p-table`, paginación y orden en servidor, columnas de `REQ-INSUMOS-23` y estados de carga, vacío y error; verificar con más de una página de insumos
- [x] 10.2 Implementar los filtros de búsqueda, tipo, estado, marca, municipio y rango de precio con botón “Limpiar”, validación del rango y conservación al volver de un modal; verificar que cada cambio vuelve a la página 1
- [x] 10.3 Verificar que las acciones “Nuevo insumo”, “Editar”, “Duplicar/Copiar”, “Eliminar”, “Cotizar seleccionados” y “Cotización masiva” se ocultan sin `RESOURCES_WRITE`

## 11. Modal de alta y edición de insumo, sugerencias y unidades

- [x] 11.1 Crear `resource-form-dialog` con las secciones de identificación, costo y abastecimiento, y detalles, y la cascada país/departamento/municipio con la ubicación del proyecto como valor inicial; verificar alta y edición con `rowVersion`
- [x] 11.2 Agregar las validaciones locales del insumo y el manejo de `VALIDATION_ERROR`, `RESOURCE_TYPE_INVALID`, `MUNICIPALITY_INVALID` y `409 RESOURCE_CONCURRENCY_CONFLICT`; verificar que el modal conserva los datos
- [x] 11.3 Integrar las sugerencias de `GET /resources/suggestions` al escribir descripción o clave, sin bloquear el guardado y sin consultarlas sin `RESOURCES_WRITE`
- [x] 11.4 Crear `unit-create-dialog` y el selector de unidad del APU con `GET /catalogs/units`, texto libre y la opción “Nueva unidad” solo con `CATALOGS_WRITE`; verificar `409 UNIT_ALREADY_EXISTS` y `400 UNIT_VALIDATION_ERROR`
- [x] 11.5 Integrar el selector de proveedor con `GET /suppliers/options` y ocultarlo sin `SUPPLIERS_READ`; verificar que el insumo se guarda sin proveedor
- [x] 11.6 Mostrar `quotationStatus` como etiqueta de solo lectura en listado, detalle y edición; verificar que no existe control para cambiarlo desde el formulario

## 12. Modal de detalle y composición de Matriz con submodales

- [x] 12.1 Crear `resource-detail-dialog` con los datos completos, las acciones por permiso y el caso `404 RESOURCE_NOT_FOUND`; verificar con un insumo simple y con un lector sin escritura
- [x] 12.2 Mostrar la composición de una Matriz con `GET /matrices/{id}/components` y el `directCost` del servidor, con aviso y ocultamiento cuando falta `APUS_READ`; verificar los tres estados de permiso
- [x] 12.3 Crear `matrix-component-dialog` para agregar y editar componentes con consumo y desperdicio válidos, `rowVersion` de la Matriz y solo insumos simples del proyecto; verificar que la Matriz propia y las demás Matrices no aparecen
- [x] 12.4 Implementar “Quitar componente” con confirmación y manejo de `MATRIX_CONCURRENCY_CONFLICT`, `MATRIX_NESTING_FORBIDDEN`, `MATRIX_TYPE_INVALID` y `APU_RESOURCE_NOT_FOUND`; verificar el refresco de la composición
- [x] 12.5 Encadenar el alta: al guardar un insumo con `resourceTypeCode === 'MATRIX'`, cerrar el formulario y abrir el detalle en modo composición; verificar con y sin `APUS_WRITE`

## 13. Copia, cotización masiva y baja

- [x] 13.1 Crear `resource-copy-dialog` con selección de proyecto de origen desde `GET /projects`, búsqueda de insumos del origen y `POST /resources/copy`; verificar el origen Matriz (`RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION`) y el origen sin acceso
- [x] 13.2 Crear `quotation-dialog` con los tres alcances (`SELECTION`, `FILTER`, `ALL`), confirmación con el número de insumos y resumen de `requested`, `updated`, `alreadyQuoted`, `skipped`, `pendingResources` y `allQuoted`; verificar el refresco posterior
- [x] 13.3 Implementar la baja con confirmación, la advertencia de componentes para una Matriz, el manejo de `409 RESOURCE_IN_USE`, `404 RESOURCE_NOT_FOUND` y la cancelación sin cambios; verificar que el listado no cambia cuando la baja se rechaza
- [x] 13.4 Implementar “Cotizar seleccionados” desde las filas marcadas, sin habilitar el envío con una selección vacía

## 14. Estados, errores y accesibilidad

- [x] 14.1 Unificar los estados de carga con esqueleto o `loading` de PrimeNG, el bloqueo de doble envío con `saving` y los mensajes de éxito con `MessageService` en las cinco pantallas; verificar que no hay dos envíos simultáneos
- [x] 14.2 Verificar que todas las llamadas usan `resolveApiError` y que un `403` inesperado produce el mensaje en español sin dejar la pantalla bloqueada
- [x] 14.3 Revisar etiquetas asociadas, foco visible, cierre con `Esc` de los modales, retorno de foco y anuncio `aria-live` del destino de arrastre; verificar la navegación completa por teclado
- [x] 14.4 Verificar el diseño entre 320 px y 1920 px con desplazamiento horizontal de tablas y las rejillas existentes, sin introducir otra paleta ni otra librería de UI
- [x] 14.5 Ejecutar `ng build` en `Frontend/` y confirmar que compila sin errores, que `package.json` no gana dependencias nuevas y que el tema Aura sigue configurado en `app.config.ts`

## 15. Checklist manual de verificación

- [x] 15.1 Recorrer los escenarios de `specs/edt/spec.md` (SC-EDT-101…147) sobre un proyecto con capítulos, subcapítulos, ítems raíz e ítems con APU; registrar el resultado por escenario
- [x] 15.2 Recorrer los escenarios de `specs/apus/spec.md` (SC-APUS-111…141) con altas de APU con y sin composición y una edición con conflicto simulado en dos sesiones
- [x] 15.3 Recorrer los escenarios de `specs/insumos/spec.md` (SC-INSUMOS-84…134) con un insumo simple, una Matriz con componentes, la copia desde otro proyecto y la cotización masiva en los tres modos
- [x] 15.4 Recorrer los escenarios de `specs/unidades/spec.md` (SC-UNIDADES-19…24) creando una unidad desde el formulario del APU y del insumo
- [x] 15.5 Verificar con usuarios de cada rol (Administrador, Director, Presupuestador asignado, Presupuestador no asignado y Auditor) que las acciones visibles coinciden con sus permisos y que `/apu` no cambió

## Workflow follow-up

- Ejecutar `openspec validate paginas-edicion-edt-apu-y-insumos --strict` y confirmar que pasa sin errores antes de archivar.
- Registrar la verificación manual del checklist 15 en el cierre de la etapa de interfaz.
- Al archivar, anotar en `PLAN_INGENIERIA.md` que `D-43` queda superado por la implementación real con PrimeNG y tema Aura.
- Confirmar que `openspec/project.md §8` conserva las capacidades existentes sin agregar una nueva para la interfaz.
