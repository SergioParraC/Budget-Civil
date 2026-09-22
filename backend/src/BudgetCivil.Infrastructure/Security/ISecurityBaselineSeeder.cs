namespace BudgetCivil.Infrastructure.Security;

public interface ISecurityBaselineSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
