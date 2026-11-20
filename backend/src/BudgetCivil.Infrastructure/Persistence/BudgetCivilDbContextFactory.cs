using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContextFactory : IDesignTimeDbContextFactory<BudgetCivilDbContext>
{
    // `dotnet ef` usa esta fabrica en lugar del contenedor de la API, por eso la cadena no se lee de
    // appsettings: primero se usa la variable de entorno (misma clave que
    // `ConnectionStrings:BudgetCivilDatabase`) y, si no existe, la instancia local documentada en
    // docs/runbook-migraciones.md.
    private const string DefaultConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=BudgetCivilDb;Integrated Security=True;Encrypt=True;" +
        "TrustServerCertificate=True;MultipleActiveResultSets=False;Application Name=BudgetCivil.Api";

    public BudgetCivilDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BudgetCivilDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DefaultConnectionString;
        }

        var optionsBuilder = new DbContextOptionsBuilder<BudgetCivilDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BudgetCivilDbContext(optionsBuilder.Options);
    }
}
