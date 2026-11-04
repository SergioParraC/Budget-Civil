using BudgetCivil.Application.Common.Startup;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class StartupDatabaseValidator(BudgetCivilDbContext dbContext) : IStartupDatabaseValidator
{
    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        return (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
    }
}
