using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContextFactory : IDesignTimeDbContextFactory<BudgetCivilDbContext>
{
    public BudgetCivilDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetCivilDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=BudgetCivilDb.DesignTime;Trusted_Connection=True;TrustServerCertificate=True");

        return new BudgetCivilDbContext(optionsBuilder.Options);
    }
}
