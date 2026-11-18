# Idea y problema a resolver

> Documento actualizado el 30/10/2026 (reconciliación `E-1b`, `D-74`…`D-111`). La fuente de verdad es `PLAN_INGENIERIA.md`; este documento resume el problema, los actores y el alcance.

## 1. Resumen de la idea
Aplicación web multiempresa para gestionar presupuestos de obra civil. Permite estructurar la EDT, administrar insumos y APUs, calcular el presupuesto, gestionar cotización y aprobación, y publicar composiciones en un catálogo abierto.

El desarrollo sigue Spec-Driven Development con **OpenSpec** (`D-09`). El sistema no asigna precios ni cantidades por IA: automatiza cálculo, integridad y trazabilidad de lo que define el usuario.

## 2. Problema actual
El proceso basado en hojas de cálculo presenta:

1. **Trazabilidad limitada:** no se sabe quién modificó por última vez un APU o un insumo ni quién aprobó. La solución registra el último modificador y una aprobación por APU (`D-08`, `D-52`), no un historial completo.
2. **Aislamiento y duplicidad:** los APUs e insumos se reconstruyen por proyecto; no hay reutilización estructurada ni detección de similitudes (`D-05`, `D-25`).
3. **Cálculo manual:** fórmulas frágiles y redondeos inconsistentes (`X-15` pendiente).
4. **Cierre sin control:** un presupuesto puede cerrarse sin que todo esté cotizado o aprobado (`D-04`, `D-14`).

## 3. Usuarios y actores
| Rol (`role_code`) | Ámbito | Responsabilidad principal |
|---|---|---|
| **Administrador** (`ADMINISTRADOR`) | Plataforma, sin compañía | Crea usuarios y compañías, cambia roles y edita o desactiva cuentas; solo lee proyectos e insumos |
| **Director de proyectos** (`DIRECTOR`) | Una compañía y sus proyectos | Configura compañía y proyectos, asigna usuarios, define el modo de firma y cierra el presupuesto |
| **Presupuestador** (`PRESUPUESTADOR`) | Compañía y proyectos asignados | Construye la EDT, crea insumos, APUs y matrices; cotiza |
| **Auditor** (`AUDITOR`) | Proyectos asignados | Lee la información y aprueba APUs cuando el proyecto lo exige |

Los roles `PROJECT_MANAGER`, `ESTIMATOR` y `READ_ONLY` quedan reemplazados (`D-02`, `D-15`, `D-34`). Los cuatro roles base son **inmutables** y la autorización se decide por `permission_key` activo con política **fail-closed** (`D-82`); la matriz `1.3.2` es referencia funcional, no control.

El **autorregistro público** (`RF-23`, `D-72`, `D-73`) crea una cuenta con rol Presupuestador y sin compañía; el cliente no puede fijar rol, compañía ni proyecto. Antes de operar dentro de una compañía el usuario completa su perfil (residencia y tarjeta profesional) y verifica el correo (`D-80`, `REQ-IDENTIDAD-08`).

## 4. Objetivo del producto
Automatizar el cálculo de presupuestos estructurados, garantizar integridad y trazabilidad mínima, permitir la reutilización de composiciones y ofrecer un catálogo público de referencia.

## 5. Alcance del MVP
| Bloque | Incluye |
|---|---|
| Identidad | Usuarios, roles por permiso, autorregistro público con identidad completa y verificación de correo, asignación a compañía y proyecto, dos correos transaccionales (`D-17`, `D-72`, `D-73`, `D-80`, `D-82`) |
| Compañías y proyectos | Creación, datos de registro, divisa, AIU, tope, modo de firma, requisito de auditor, visibilidad (`D-03`, `D-07`, `D-22`, `D-93`) |
| EDT | Capítulos e ítems recursivos, numeración automática editable, códigos únicos por proyecto, búsqueda por código o nombre (`D-30`, `D-51`) |
| Insumos | Siete tipos, estados de cotización, listado con filtros, sugerencia de unicidad, copia desde otros proyectos (`D-05`, `D-29`) |
| APUs y matrices | APU 1:1 con el ítem, copia desde proyectos o catálogo, reconciliación de insumos, matriz reutilizable (`D-13`, `D-16`, `D-25`, `D-48`) |
| Cálculo | Consumo, costo directo, AIU y redondeo (`D-06`, `D-07`) |
| Cierre | Todo cotizado, aprobación por APU, cierre por el Director (`D-04`, `D-14`, `D-52`) |
| Catálogo | Proyectos públicos, APUs e insumos con todos sus campos, consulta anónima (`D-22`, `D-28`, `D-38`) |
| Alertas | Tope presupuestal (`D-18`) |

## 6. Fuera de alcance
- Asignación automática de precios, cantidades o rendimientos por IA.
- Scraping o comparación de precios de proveedores en tiempo real.
- Ejecución de obra: inventarios, compras, facturación.
- Visores 3D e integración CAD/BIM.
- Historial de cambios y de valores (`E-05`).
- Conversión de divisas (`E-06`).
- Importación y exportación en el MVP (`D-18`).
- Congelamiento o versionado del presupuesto (`E-12`).

## 7. Criterios de éxito
1. Cálculo determinista y único para APU y presupuesto.
2. Última modificación registrada por APU e insumo, y una aprobación por APU cuando el modo lo exige.
3. Cierre bloqueado si falta cotización o aprobación; el Director cierra sin firmar APUs (`D-90`).
4. Catálogo público sin datos de autoría ni empresa, y sin acceso a proyectos privados.
5. Cobertura ≥ 80 % en dominio y motor de cálculo.
6. Autorización por `permission_key` con política fail-closed y roles base inmutables (`D-82`); el documento de identidad no es único (`D-77`).
7. La imagen de firma se persiste en `UserSignature` y nunca se expone en DTO públicos, catálogo ni logs (`D-91`, `R-28`).

## 8. Supuestos y riesgos de negocio
- La publicación expone precios y contactos; el usuario lo acepta al publicar (`D-28`, `RT-11`).
- El auditor puede ser interno o externo; el sistema no evita conflictos de interés (`Q-03`).
- El catálogo no tiene curaduría (`D-12`).
- El equipo es una sola persona y no hay datos históricos del proceso (`D-19`).

## 9. Preguntas abiertas
| ID | Tema | Estado |
|---|---|---|
| `X-15` | Fórmulas exactas de cálculo | Pendiente; bloquea E-4a |
| `D-45` | Proveedor de correo concreto | Se elige al implementar |
| `RT-15` | Edición de SQL Server para producción | Antes de E-6 |
