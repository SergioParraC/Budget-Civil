namespace BudgetCivil.Application.Calculation;

public sealed record UsageDirectLine(Guid ResourceId, decimal Consumption, decimal WastePercentage, decimal QuantityWork);

public sealed record UsageMatrixLine(Guid MatrixResourceId, decimal Consumption, decimal WastePercentage, decimal QuantityWork);

public sealed record UsageMatrixComponent(
    Guid MatrixResourceId,
    Guid ComponentResourceId,
    decimal Consumption,
    decimal WastePercentage);

/// <summary>
/// Conteo de uso de insumos (`REQ-APUS-12`, `REQ-APUS-13`, `R-21`, `D-33`): consumo efectivo por cantidad
/// de obra, con desperdicio en la línea y en el componente, sin `yield` y con expansión de un solo nivel.
/// </summary>
public static class ResourceUsageCalculator
{
    public static IReadOnlyDictionary<Guid, decimal> Calculate(
        IReadOnlyList<UsageDirectLine> directLines,
        IReadOnlyList<UsageMatrixLine> matrixLines,
        IReadOnlyList<UsageMatrixComponent> components,
        int quantityDecimals = 10)
    {
        ArgumentNullException.ThrowIfNull(directLines);
        ArgumentNullException.ThrowIfNull(matrixLines);
        ArgumentNullException.ThrowIfNull(components);

        var usage = new Dictionary<Guid, decimal>();

        foreach (var line in directLines)
        {
            var quantity = line.Consumption *
                           (1m + line.WastePercentage / 100m) *
                           line.QuantityWork;

            Add(usage, line.ResourceId, quantity);
        }

        var componentsByMatrix = components.ToLookup(component => component.MatrixResourceId);

        foreach (var matrixLine in matrixLines)
        {
            var matrixQuantity = matrixLine.Consumption *
                                 (1m + matrixLine.WastePercentage / 100m) *
                                 matrixLine.QuantityWork;

            // La Matriz conserva además su consumo directo.
            Add(usage, matrixLine.MatrixResourceId, matrixQuantity);

            foreach (var component in componentsByMatrix[matrixLine.MatrixResourceId])
            {
                var componentQuantity = matrixQuantity *
                                        component.Consumption *
                                        (1m + component.WastePercentage / 100m);

                Add(usage, component.ComponentResourceId, componentQuantity);
            }
        }

        return usage.ToDictionary(
            entry => entry.Key,
            entry => ApuCostCalculator.Round(entry.Value, quantityDecimals));
    }

    private static void Add(IDictionary<Guid, decimal> usage, Guid resourceId, decimal quantity)
    {
        usage[resourceId] = usage.TryGetValue(resourceId, out var current)
            ? current + quantity
            : quantity;
    }
}
