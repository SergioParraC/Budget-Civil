# Clarificaciones Resueltas

Fecha de cierre: 2026-09-08
Estado general: RESUELTO

## Decisiones cerradas

| Punto | Categoria | Decision confirmada | Estado |
|---|---|---|---|
| 1 | Modelo de Datos | 1A - Completar diccionario WBS en modelo de datos | Resuelto |
| 2 | Modelo de Datos | 2B - Enums separados con mapeo explicito entre Resource/Apu/Project | Resuelto |
| 3 | Modelo de Datos | 3A - Visibilidad publica con regla AND | Resuelto |
| 4 | Modelo de Datos | 4A - Validacion de FINALIZADO en Domain/Application (sin constraint condicional DB) | Resuelto |
| 5 | Flujo UC/API | 5A - Estandarizar rutas en resources y apu-resources | Resuelto |
| 6 | Flujo UC/API | 6A - PublicApuDTO por whitelist estricta | Resuelto |
| 7 | Flujo UC/API | 7A - Query filter global + bypass controlado solo en lectura publica | Reemplazado por baseline v2 |
| 8 | Flujo UC/API | 8C - Rol de segundo aprobador parametrizable en ProjectConfiguration | Reemplazado por baseline v2 |
| 9 | Reglas de Dominio | 9B - Matriz reusable con expansion de un nivel | Resuelto |
| 10 | Reglas de Dominio | 10A - Cascada y reversion sincrona transaccional | Resuelto |
| 11 | Modelo de Datos | 12A - Moneda unica por proyecto, ISO 4217, sin conversion automatica | Resuelto |
| 12 | Reglas de Dominio | 13A - Formula CD_APU explicita con rendimiento | Resuelto |

## Decisiones de rebaseline v2 (2026-09-08)

| Punto | Categoria | Decision confirmada | Estado |
|---|---|---|---|
| V2-01 | Stack | Angular 20.x como version oficial de frontend | Resuelto |
| V2-02 | Stack | Backend oficial en .NET 10 | Resuelto |
| V2-03 | Seguridad/App | Firma de aprobacion capturada con Signature Pad en frontend | Resuelto |
| V2-04 | Seguridad/App | Aprobacion unica para `FINALIZADO` (sin segunda firma) | Resuelto |
| V2-05 | Infraestructura | Operacion sin TLS/SSL, transporte HTTP | Resuelto |
| V2-06 | Pruebas | Pruebas unitarias solo en backend para calculos y privacidad por empresa | Resuelto |
| V2-07 | Privacidad | Se mantiene `CompanyId` y acceso solo para usuarios de la misma compania en registros privados | Resuelto |
| V2-08 | Modelo de datos | Se eliminan campos de token de firma en `ProjectConfiguration` | Resuelto |

## Notas de implementacion
- No abrir nuevas decisiones fuera de este set sin nuevo ciclo de clarify.
- Cualquier ajuste posterior debe mantener trazabilidad a este archivo.
