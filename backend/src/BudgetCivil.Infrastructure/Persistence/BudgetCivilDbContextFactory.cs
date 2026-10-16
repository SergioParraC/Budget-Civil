using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContextFactory : IDesignTimeDbContextFactory<BudgetCivilDbContext>
{
    public BudgetCivilDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetCivilDbContext>();
        optionsBuilder.UseSqlServer(
            "Data Source=LAPTOP-ASUS-F15\\SQLEXPRESS;Initial Catalog=BudgetCivilDb;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=SQL Server Management Studio;Command Timeout=0");

        return new BudgetCivilDbContext(optionsBuilder.Options);
    }
}
