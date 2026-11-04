namespace BudgetCivil.Infrastructure.Catalogs;

public interface IResourceTypeSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
