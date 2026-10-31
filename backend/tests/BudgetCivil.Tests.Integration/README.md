# Pruebas de integración (`D-66`, `D-106`)

Se ejecutan **manualmente** contra una base SQL Server local. No usan `EF Core InMemory` ni
`WebApplicationFactory` (`D-106`); el arranque de la API tampoco aplica migraciones (`D-40`).

## Requisitos

1. Base **vacía** dedicada a la verificación, por ejemplo `BudgetCivilDb_Integration`.
2. Cadena de conexión en la variable de entorno `BUDGETCIVIL_TEST_CONNECTION`.

```powershell
$env:BUDGETCIVIL_TEST_CONNECTION = "Data Source=.\SQLEXPRESS;Initial Catalog=BudgetCivilDb_Integration;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
dotnet test tests\BudgetCivil.Tests.Integration
```

El `DatabaseFixture` aplica la baseline (`202610300438_e1b_reconciliacion_baseline`) y ejecuta el `seed`
de seguridad antes de las pruebas. Si la variable no está definida, la ejecución falla con un mensaje
explícito en lugar de saltarse las pruebas en silencio.

## Cobertura automatizada

| Identificador | Prueba | Qué comprueba |
|---|---|---|
| `T-I-24` (baseline) | `BaselineSchemaTests.La_baseline_esta_aplicada...` | La baseline está aplicada, existen las tablas del modelo reconciliado y no quedan tablas de membresía ni de perfil. |
| `T-I-24` (tarjetas) | `ProjectConcurrencyTests` | `rowversion` en `Project`: la actualización con la versión anterior falla y la vigente avanza la versión. |
| `T-I-25` | `HistoryPersistenceTests` | Cada cambio de estado y de visibilidad inserta su registro con actor, fecha y motivo. |
| `T-I-23` | `SeedIdempotencyTests` | El `seed` es idempotente y la matriz de permisos coincide con `DEC-21`, `DEC-22` y `D-86`. |
| `T-I-28` | `UserModelTests` | Dos cuentas comparten número de documento; correo y usuario siguen siendo únicos. |
| `T-I-29` | `MembershipPersistenceTests` | `User.company_id` con asignación concurrente única y `ProjectMember` único por (`project_id`, `user_id`). |
| `T-I-30` | `ProjectConfigurationPersistenceTests` | El proyecto se crea con una sola configuración y la segunda se rechaza. |

Las verificaciones de API (`T-I-22`, `T-I-26`, `T-I-27`, `T-I-31`, `T-I-32`, `T-S-07`…`T-S-12`) se
ejecutan manualmente con Swagger y se registran en el checklist de la etapa (`T026`), porque
`WebApplicationFactory` está prohibido (`D-106`).
