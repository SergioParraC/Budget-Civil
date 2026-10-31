using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration.Infrastructure;

/// <summary>
/// `D-66`: la integración se ejecuta manualmente contra SQL Server local, nunca con `InMemory` ni
/// `WebApplicationFactory` (`D-106`). La cadena se toma de `BUDGETCIVIL_TEST_CONNECTION`.
/// </summary>
public static class SqlServerTestDatabase
{
    public const string ConnectionStringVariable = "BUDGETCIVIL_TEST_CONNECTION";

    public static string ResolveConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Define la variable de entorno {ConnectionStringVariable} con la cadena de conexión a una " +
                "base SQL Server local vacía antes de ejecutar las pruebas de integración (runbook §2).");
        }

        return connectionString;
    }

    public static BudgetCivilDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<BudgetCivilDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BudgetCivilDbContext(options);
    }
}
