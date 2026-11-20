using BudgetCivil.Application.Calculation.Services;

namespace BudgetCivil.Application.Calculation;

/// <summary>Resultado del presupuesto del proyecto (`REQ-CALCULO-01`…`03`, `D-94`).</summary>
public sealed record ProjectBudgetResult(
    decimal? DirectCost,
    IndirectCostsResult IndirectCosts,
    decimal? Total,
    bool IsComplete,
    int ItemCount,
    int ItemsWithoutApu,
    int ItemsWithoutPrice,
    decimal? BudgetLimit,
    bool IsOverBudget);

/// <summary>
/// Agrega el costo directo del proyecto, aplica el AIU una sola vez y evalúa el tope (`D1`, `D7`, `RF-17`).
/// No persiste costos y reutiliza `ApuCostCalculator` para el AIU y el redondeo (`D-121`).
/// </summary>
public static class ProjectBudgetCalculator
{
    public static ProjectBudgetResult Calculate(ProjectCostSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var items = snapshot.Items;
        var itemsWithoutApu = items.Count(item => !item.ApuId.HasValue);
        var itemsWithoutPrice = items.Count(item => item.ApuId.HasValue && !item.IsApuComplete);
        var isComplete = itemsWithoutApu == 0 && itemsWithoutPrice == 0;

        decimal? directCost = null;
        if (isComplete)
        {
            directCost = items.Count == 0
                ? 0m
                : ApuCostCalculator.Round(
                    items.Sum(item => item.DirectCost ?? 0m),
                    snapshot.Settings.RoundingDecimals);
        }

        var indirectCosts = ApuCostCalculator.CalculateIndirectCosts(directCost, snapshot.Settings);
        var total = directCost.HasValue && indirectCosts.Total.HasValue
            ? ApuCostCalculator.Round(
                directCost.Value + indirectCosts.Total.Value,
                snapshot.Settings.RoundingDecimals)
            : (decimal?)null;

        var isOverBudget = total.HasValue &&
            snapshot.BudgetLimit.HasValue &&
            total.Value > snapshot.BudgetLimit.Value;

        return new ProjectBudgetResult(
            directCost,
            indirectCosts,
            total,
            isComplete,
            items.Count,
            itemsWithoutApu,
            itemsWithoutPrice,
            snapshot.BudgetLimit,
            isOverBudget);
    }
}
