using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContextFactory : IDesignTimeDbContextFactory<BudgetCivilDbContext>
{
    public BudgetCivilDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetCivilDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=BudgetCivilDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=False;Application Name=BudgetCivil.Api");

        return new BudgetCivilDbContext(optionsBuilder.Options);
    }
}
