namespace BudgetCivil.Application.Calculation;

/// <summary>
/// Fórmula única de la vista previa de costo (`D8`, `REQ-APUS-19`, `REQ-APUS-20`, `D-121`). No persiste
/// costos y la reutilizan el detalle del APU, el listado de APUs de otros proyectos y la vista de la Matriz.
/// </summary>
public static class ApuCostCalculator
{
    private static readonly string[] TypeOrder =
    [
        "MATERIAL",
        "LABOR",
        "TOOL",
        "EQUIPMENT",
        "AUXILIARY",
        "WORK",
        "TRANSPORT",
        "MATRIX"
    ];

    /// <summary>
    /// Calcula la vista previa del APU. La cantidad efectiva de cada línea es
    /// <c>ROUND(consumo × (1 + desperdicio/100), quantityDecimals)</c> y su valor
    /// <c>ROUND(cantidad efectiva × precio unitario, roundingDecimals)</c>; una línea de Matriz usa como
    /// precio el costo directo unitario de la Matriz (`D8`).
    /// </summary>
    public static ApuCostResult CalculateApu(
        IReadOnlyList<ApuCostLineInput> lines,
        CostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(settings);

        var items = new List<(string Type, CostItemResult Item)>();
        var isComplete = lines.Count > 0;

        foreach (var line in lines)
        {
            var quantity = Round(
                line.Consumption * (1m + line.WastePercentage / 100m),
                settings.QuantityDecimals);

            var unitPrice = line.IsMatrix
                ? CalculateMatrixUnitPrice(line.MatrixComponents, settings)
                : line.UnitPrice;

            decimal? subtotal = unitPrice.HasValue
                ? Round(quantity * unitPrice.Value, settings.RoundingDecimals)
                : null;

            isComplete &= subtotal.HasValue;

            items.Add((NormalizeType(line.ResourceTypeCode), new CostItemResult(
                line.LineId,
                line.ResourceId,
                line.Description,
                line.Unit,
                unitPrice,
                line.Consumption,
                quantity,
                line.WastePercentage,
                subtotal,
                line.IsMatrix ? line.ResourceId : null)));
        }

        var resources = Group(items);
        decimal? directCost = isComplete
            ? Round(resources.Sum(group => group.Items.Sum(item => item.Subtotal ?? 0m)), settings.RoundingDecimals)
            : null;

        var indirectCosts = CalculateIndirectCosts(directCost, settings);
        var total = directCost.HasValue && indirectCosts.Total.HasValue
            ? Round(directCost.Value + indirectCosts.Total.Value, settings.RoundingDecimals)
            : (decimal?)null;

        return new ApuCostResult(resources, directCost, indirectCosts, total, isComplete);
    }

    /// <summary>
    /// Costo directo de una Matriz: <c>ROUND(Σ(consumo × (1 + desperdicio/100) × precio), roundingDecimals)</c>.
    /// No aplica AIU (`REQ-APUS-20`).
    /// </summary>
    public static MatrixCostResult CalculateMatrix(
        IReadOnlyList<MatrixComponentInput> components,
        CostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(settings);

        var items = new List<(string Type, CostItemResult Item)>();
        var isComplete = components.Count > 0;

        foreach (var component in components)
        {
            var quantity = Round(
                component.Consumption * (1m + component.WastePercentage / 100m),
                settings.QuantityDecimals);

            decimal? subtotal = component.UnitPrice.HasValue
                ? Round(quantity * component.UnitPrice.Value, settings.RoundingDecimals)
                : null;

            isComplete &= subtotal.HasValue;

            items.Add((NormalizeType(component.ResourceTypeCode), new CostItemResult(
                component.ResourceId,
                component.ResourceId,
                component.Description,
                component.Unit,
                component.UnitPrice,
                component.Consumption,
                quantity,
                component.WastePercentage,
                subtotal,
                null)));
        }

        var resources = Group(items);
        decimal? directCost = isComplete
            ? Round(resources.Sum(group => group.Items.Sum(item => item.Subtotal ?? 0m)), settings.RoundingDecimals)
            : null;

        return new MatrixCostResult(resources, directCost, isComplete);
    }

    public static decimal? CalculateMatrixUnitPrice(
        IReadOnlyList<MatrixComponentInput>? components,
        CostSettings settings)
    {
        if (components is null)
        {
            return null;
        }

        return CalculateMatrix(components, settings).DirectCost;
    }

    public static IndirectCostsResult CalculateIndirectCosts(decimal? directCost, CostSettings settings)
    {
        var administration = new AiuComponentResult(
            settings.AiuAdminPercent,
            directCost.HasValue
                ? Round(directCost.Value * settings.AiuAdminPercent / 100m, settings.RoundingDecimals)
                : null);

        var unforeseen = new AiuComponentResult(
            settings.AiuUnforeseenPercent,
            directCost.HasValue
                ? Round(directCost.Value * settings.AiuUnforeseenPercent / 100m, settings.RoundingDecimals)
                : null);

        var profit = new AiuComponentResult(
            settings.AiuProfitPercent,
            directCost.HasValue
                ? Round(directCost.Value * settings.AiuProfitPercent / 100m, settings.RoundingDecimals)
                : null);

        decimal? total = directCost.HasValue
            ? Round(
                administration.Amount!.Value + unforeseen.Amount!.Value + profit.Amount!.Value,
                settings.RoundingDecimals)
            : null;

        return new IndirectCostsResult(administration, unforeseen, profit, total);
    }

    public static decimal Round(decimal value, int decimals)
    {
        var normalizedDecimals = Math.Clamp(decimals, 0, 10);
        return decimal.Round(value, normalizedDecimals, MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<CostGroupResult> Group(List<(string Type, CostItemResult Item)> items)
    {
        return items
            .GroupBy(entry => entry.Type)
            .OrderBy(group => Array.IndexOf(TypeOrder, group.Key) is var index && index >= 0
                ? index
                : TypeOrder.Length)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CostGroupResult(
                group.Key,
                group.Select(entry => entry.Item).ToList()))
            .ToList();
    }

    private static string NormalizeType(string? resourceTypeCode)
    {
        return string.IsNullOrWhiteSpace(resourceTypeCode)
            ? string.Empty
            : resourceTypeCode.Trim().ToUpperInvariant();
    }
}
