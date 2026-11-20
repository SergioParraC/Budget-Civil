using BudgetCivil.Application.Catalogs.Dtos;
using BudgetCivil.Application.Catalogs.Repositories;

namespace BudgetCivil.Application.Catalogs.Queries;

public sealed class GetUnitsQueryHandler(IMeasurementUnitRepository repository)
{
    public async Task<IReadOnlyList<UnitDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var units = await repository.ListAsync(cancellationToken);

        return units
            .OrderBy(unit => unit.ShortUnit, StringComparer.OrdinalIgnoreCase)
            .ThenBy(unit => unit.Id)
            .Select(unit => new UnitDto
            {
                Id = unit.Id,
                ShortUnit = unit.ShortUnit,
                Unit = unit.Unit
            })
            .ToList();
    }
}