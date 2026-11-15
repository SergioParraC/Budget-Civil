using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Apus.Services;

public sealed record ResourceUsageReportItem(
    Guid ResourceId,
    string Description,
    string? Unit,
    decimal Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice);

public sealed record ResourceUsageReportGroup(
    string ResourceType,
    string ResourceTypeName,
    decimal? Subtotal,
    IReadOnlyList<ResourceUsageReportItem> Items);

public sealed record ResourceUsageReport(
    IReadOnlyList<ResourceUsageReportGroup> Resources,
    decimal? TotalCost);

/// <summary>
/// Arma el reporte del conteo de uso (`REQ-APUS-12`, `D-33`): agrupa por tipo de insumo, discrimina cada
/// insumo con su cantidad física, precio unitario y total, y suma subtotales por tipo y total general.
/// El total por insumo es `ROUND(cantidad × precio unitario, roundingDecimals)`; si falta el precio, el
/// total y el subtotal del tipo quedan nulos (misma semántica que `D8`).
/// </summary>
public static class ResourceUsageReportService
{
    private static readonly IReadOnlyDictionary<string, int> TypeOrder =
        ResourceTypeCatalog.Definitions
            .Select((definition, index) => (definition.Code, index))
            .ToDictionary(
                entry => entry.Code,
                entry => entry.index,
                StringComparer.OrdinalIgnoreCase);

    public static ResourceUsageReport Build(
        IReadOnlyList<ResourceUsageEntry> resources,
        IReadOnlyDictionary<Guid, decimal> usage,
        int roundingDecimals)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(usage);

        var groups = resources
            .GroupBy(resource => resource.ResourceTypeCode, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var items = group
                    .OrderBy(resource => resource.Description, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(resource => resource.ResourceId)
                    .Select(resource =>
                    {
                        var quantity = usage.TryGetValue(resource.ResourceId, out var counted)
                            ? counted
                            : 0m;

                        var totalPrice = resource.UnitPrice.HasValue
                            ? ApuCostCalculator.Round(
                                quantity * resource.UnitPrice.Value,
                                roundingDecimals)
                            : (decimal?)null;

                        return new ResourceUsageReportItem(
                            resource.ResourceId,
                            resource.Description,
                            resource.Unit,
                            quantity,
                            resource.UnitPrice,
                            totalPrice);
                    })
                    .ToList();

                var subtotal = items.All(item => item.TotalPrice.HasValue)
                    ? ApuCostCalculator.Round(
                        items.Sum(item => item.TotalPrice!.Value),
                        roundingDecimals)
                    : (decimal?)null;

                return new ResourceUsageReportGroup(
                    group.Key,
                    group.First().ResourceTypeName,
                    subtotal,
                    items);
            })
            .OrderBy(group => TypeOrder.TryGetValue(group.ResourceType, out var index)
                ? index
                : int.MaxValue)
            .ThenBy(group => group.ResourceType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCost = groups.All(group => group.Subtotal.HasValue)
            ? ApuCostCalculator.Round(
                groups.Sum(group => group.Subtotal!.Value),
                roundingDecimals)
            : (decimal?)null;

        return new ResourceUsageReport(groups, totalCost);
    }
}
