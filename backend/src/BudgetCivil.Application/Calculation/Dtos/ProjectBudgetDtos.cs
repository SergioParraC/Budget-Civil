using BudgetCivil.Application.Calculation.Repositories;
using BudgetCivil.Application.Calculation.Services;

namespace BudgetCivil.Application.Calculation.Dtos;

/// <summary>Presupuesto del proyecto con costo directo, AIU discriminado y tope (`D4`, `REQ-CALCULO-01`…`03`).</summary>
public sealed class ProjectBudgetResponse
{
    public Guid ProjectId { get; init; }

    public ProjectBudgetCurrencyDto? BaseCurrency { get; init; }

    public int RoundingDecimals { get; init; }

    public int QuantityDecimals { get; init; }

    public decimal? DirectCost { get; init; }

    public ProjectBudgetIndirectCostsDto IndirectCosts { get; init; } = new();

    public decimal? Total { get; init; }

    public decimal? BudgetLimit { get; init; }

    public bool IsOverBudget { get; init; }

    public bool IsComplete { get; init; }

    public int ItemCount { get; init; }

    public int ItemsWithoutApu { get; init; }

    public int ItemsWithoutPrice { get; init; }
}

public sealed class ProjectBudgetCurrencyDto
{
    public Guid Id { get; init; }

    public string IsoCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class ProjectBudgetIndirectCostsDto
{
    public ProjectBudgetAiuComponentDto Administration { get; init; } = new();

    public ProjectBudgetAiuComponentDto Unforeseen { get; init; } = new();

    public ProjectBudgetAiuComponentDto Profit { get; init; } = new();

    public decimal? Total { get; init; }
}

public sealed class ProjectBudgetAiuComponentDto
{
    public decimal Percent { get; init; }

    public decimal? Amount { get; init; }
}

public static class ProjectBudgetMapper
{
    public static ProjectBudgetResponse ToResponse(
        ProjectBudgetResult budget,
        ProjectCostSnapshot snapshot) => new()
    {
        ProjectId = snapshot.ProjectId,
        BaseCurrency = snapshot.BaseCurrency is null
            ? null
            : new ProjectBudgetCurrencyDto
            {
                Id = snapshot.BaseCurrency.Id,
                IsoCode = snapshot.BaseCurrency.IsoCode,
                Name = snapshot.BaseCurrency.Name
            },
        RoundingDecimals = snapshot.Settings.RoundingDecimals,
        QuantityDecimals = snapshot.Settings.QuantityDecimals,
        DirectCost = budget.DirectCost,
        IndirectCosts = new ProjectBudgetIndirectCostsDto
        {
            Administration = ToComponent(budget.IndirectCosts.Administration),
            Unforeseen = ToComponent(budget.IndirectCosts.Unforeseen),
            Profit = ToComponent(budget.IndirectCosts.Profit),
            Total = budget.IndirectCosts.Total
        },
        Total = budget.Total,
        BudgetLimit = budget.BudgetLimit,
        IsOverBudget = budget.IsOverBudget,
        IsComplete = budget.IsComplete,
        ItemCount = budget.ItemCount,
        ItemsWithoutApu = budget.ItemsWithoutApu,
        ItemsWithoutPrice = budget.ItemsWithoutPrice
    };

    private static ProjectBudgetAiuComponentDto ToComponent(AiuComponentResult component) => new()
    {
        Percent = component.Percent,
        Amount = component.Amount
    };
}
