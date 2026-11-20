using BudgetCivil.Domain.Catalogs;

namespace BudgetCivil.Application.Catalogs.Repositories;

/// <summary>Acceso al catálogo global de unidades (`REQ-UNIDADES-02`, `REQ-UNIDADES-03`).</summary>
public interface IMeasurementUnitRepository
{
    Task<IReadOnlyList<MeasurementUnit>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> ShortUnitExistsAsync(string shortUnit, CancellationToken cancellationToken = default);

    Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}