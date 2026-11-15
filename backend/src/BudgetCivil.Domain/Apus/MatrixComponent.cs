using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Domain.Apus;

/// <summary>
/// Componente de una Matriz (`REQ-APUS-06`, `REQ-APUS-07`, `BR-008`, `D-48`). La Matriz es un
/// <see cref="Resource"/> de tipo `MATRIX`; sus componentes nunca son otra Matriz y nunca son ella misma.
/// </summary>
public sealed class MatrixComponent
{
    private MatrixComponent()
    {
    }

    public Guid Id { get; private set; }

    public Guid MatrixResourceId { get; private set; }

    public Guid ComponentResourceId { get; private set; }

    public decimal Consumption { get; private set; }

    public decimal WastePercentage { get; private set; }

    public Resource ComponentResource { get; private set; } = null!;

    public static MatrixComponent Create(
        Guid matrixResourceId,
        Guid componentResourceId,
        decimal consumption,
        decimal wastePercentage = 0m)
    {
        if (matrixResourceId == Guid.Empty)
        {
            throw new InvalidOperationException("Matrix resource is required.");
        }

        if (componentResourceId == Guid.Empty)
        {
            throw new InvalidOperationException("Matrix component is required.");
        }

        if (matrixResourceId == componentResourceId)
        {
            throw new InvalidOperationException("A matrix cannot contain itself.");
        }

        return new MatrixComponent
        {
            Id = Guid.NewGuid(),
            MatrixResourceId = matrixResourceId,
            ComponentResourceId = componentResourceId,
            Consumption = ApuResource.NormalizeConsumption(consumption),
            WastePercentage = ApuResource.NormalizeWastePercentage(wastePercentage)
        };
    }

    /// <summary>Cambia el consumo y el desperdicio del componente (`SC-APUS-29`).</summary>
    public void Change(decimal consumption, decimal wastePercentage)
    {
        Consumption = ApuResource.NormalizeConsumption(consumption);
        WastePercentage = ApuResource.NormalizeWastePercentage(wastePercentage);
    }
}
