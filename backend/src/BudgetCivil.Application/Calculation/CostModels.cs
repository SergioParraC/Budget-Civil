namespace BudgetCivil.Application.Calculation;

public sealed record ApuCostLineInput(
    Guid LineId,
    Guid ResourceId,
    string ResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal WastePercentage,
    IReadOnlyList<MatrixComponentInput>? MatrixComponents = null)
{
    public bool IsMatrix =>
        string.Equals(ResourceTypeCode, "MATRIX", StringComparison.OrdinalIgnoreCase);
}

public sealed record MatrixComponentInput(
    Guid ResourceId,
    string ResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal WastePercentage);

public sealed record CostItemResult(
    Guid LineId,
    Guid ResourceId,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal Quantity,
    decimal WastePercentage,
    decimal? Subtotal,
    Guid? MatrixResourceId);

public sealed record CostGroupResult(string ResourceType, IReadOnlyList<CostItemResult> Items);

public sealed record AiuComponentResult(decimal Percent, decimal? Amount);

public sealed record IndirectCostsResult(
    AiuComponentResult Administration,
    AiuComponentResult Unforeseen,
    AiuComponentResult Profit,
    decimal? Total);

public sealed record ApuCostResult(
    IReadOnlyList<CostGroupResult> Resources,
    decimal? DirectCost,
    IndirectCostsResult IndirectCosts,
    decimal? Total,
    bool IsComplete);

public sealed record MatrixCostResult(
    IReadOnlyList<CostGroupResult> Resources,
    decimal? DirectCost,
    bool IsComplete);

public sealed record CostSettings(
    int RoundingDecimals,
    int QuantityDecimals,
    decimal AiuAdminPercent,
    decimal AiuUnforeseenPercent,
    decimal AiuProfitPercent);
