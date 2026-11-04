namespace BudgetCivil.Application.Common.Startup;

/// <summary>
/// Comprueba el estado del esquema antes de ejecutar los `seed` de arranque, sin que la API dependa de
/// EF Core ni de `BudgetCivilDbContext` (`PLAN §3.4.6`).
/// </summary>
public interface IStartupDatabaseValidator
{
    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}
