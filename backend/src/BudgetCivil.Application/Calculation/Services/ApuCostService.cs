using BudgetCivil.Application.Calculation.Repositories;

namespace BudgetCivil.Application.Calculation.Services;

/// <summary>Costo derivado de un ítem del EDT (`D1`, `REQ-EDT-23`).</summary>
public sealed record ProjectItemCost(
    Guid ItemId,
    Guid? ApuId,
    decimal QuantityWork,
    Guid? WbsChapterId,
    int OrderIndex,
    decimal? UnitCost,
    decimal? DirectCost,
    bool HasApu,
    bool IsApuComplete);

/// <summary>
/// Snapshot de costos del proyecto reutilizable por el árbol y por el presupuesto (`D5`, `D6`). El cálculo
/// se deriva en cada lectura desde los APUs, las cantidades y la configuración (`REQ-CALCULO-05`).
/// </summary>
public sealed record ProjectCostSnapshot(
    Guid ProjectId,
    Guid CompanyId,
    CostSettings Settings,
    decimal? BudgetLimit,
    ProjectCostCurrency? BaseCurrency,
    IReadOnlyList<ProjectItemCost> Items,
    IReadOnlyDictionary<Guid, ApuCostResult> ApuCosts)
{
    /// <summary>Costo directo unitario de un APU o nulo si no está completo.</summary>
    public decimal? GetApuUnitCost(Guid? apuId)
    {
        return apuId.HasValue && ApuCosts.TryGetValue(apuId.Value, out var cost)
            ? cost.DirectCost
            : null;
    }
}

/// <summary>
/// Calcula el costo de cada APU del proyecto con la fórmula única de `E-3b` (`D1`, `D-121`) y devuelve el
/// snapshot que consumen el árbol y el presupuesto. No persiste costos (`REQ-CALCULO-05`).
/// </summary>
public sealed class ApuCostService(IApuCostReader apuCostReader)
{
    public async Task<ProjectCostSnapshot?> GetProjectSnapshotAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var data = await apuCostReader.GetProjectAsync(projectId, cancellationToken);
        if (data is null)
        {
            return null;
        }

        var apuCosts = data.ApuLines.ToDictionary(
            entry => entry.Key,
            entry => ApuCostCalculator.CalculateApu(entry.Value, data.Settings));

        var items = data.Items
            .Select(item =>
            {
                var hasApu = item.ApuId.HasValue;
                var apuCost = hasApu && apuCosts.TryGetValue(item.ApuId!.Value, out var found)
                    ? found
                    : null;

                var unitCost = apuCost?.DirectCost;
                var directCost = unitCost.HasValue
                    ? ApuCostCalculator.Round(
                        item.QuantityWork * unitCost.Value,
                        data.Settings.RoundingDecimals)
                    : (decimal?)null;

                return new ProjectItemCost(
                    item.ItemId,
                    item.ApuId,
                    item.QuantityWork,
                    item.WbsChapterId,
                    item.OrderIndex,
                    unitCost,
                    directCost,
                    hasApu,
                    apuCost?.IsComplete ?? false);
            })
            .ToList();

        return new ProjectCostSnapshot(
            data.ProjectId,
            data.CompanyId,
            data.Settings,
            data.BudgetLimit,
            data.BaseCurrency,
            items,
            apuCosts);
    }
}
