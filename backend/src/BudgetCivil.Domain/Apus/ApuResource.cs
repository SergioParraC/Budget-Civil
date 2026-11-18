using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Domain.Apus;

/// <summary>
/// Línea de composición del APU (`REQ-APUS-04`, `D-48`): consume un insumo del mismo proyecto con un
/// desperdicio opcional. La línea no lleva autoría propia; el APU registra la del último cambio.
/// </summary>
public sealed class ApuResource
{
    public const decimal MaxWastePercentage = 100m;

    private ApuResource()
    {
    }

    public Guid Id { get; private set; }

    public Guid ApuId { get; private set; }

    public Guid ResourceId { get; private set; }

    public decimal Consumption { get; private set; }

    public decimal WastePercentage { get; private set; }

    public Resource Resource { get; private set; } = null!;

    public static ApuResource Create(
        Guid apuId,
        Guid resourceId,
        decimal consumption,
        decimal wastePercentage = 0m)
    {
        if (apuId == Guid.Empty)
        {
            throw new InvalidOperationException("APU is required.");
        }

        if (resourceId == Guid.Empty)
        {
            throw new InvalidOperationException("APU resource is required.");
        }

        return new ApuResource
        {
            Id = Guid.NewGuid(),
            ApuId = apuId,
            ResourceId = resourceId,
            Consumption = NormalizeConsumption(consumption),
            WastePercentage = NormalizeWastePercentage(wastePercentage)
        };
    }

    /// <summary>Cambia el consumo y el desperdicio de la línea (`SC-APUS-20`).</summary>
    public void Change(decimal consumption, decimal wastePercentage)
    {
        Consumption = NormalizeConsumption(consumption);
        WastePercentage = NormalizeWastePercentage(wastePercentage);
    }

    /// <summary>El consumo debe ser mayor que cero (`SC-APUS-14`, `SC-APUS-29`).</summary>
    public static decimal NormalizeConsumption(decimal consumption)
    {
        if (consumption <= 0m)
        {
            throw new InvalidOperationException("APU resource consumption is invalid.");
        }

        return consumption;
    }

    /// <summary>El desperdicio es un porcentaje entre 0 y 100 y su valor por defecto es 0 (`D-48`).</summary>
    public static decimal NormalizeWastePercentage(decimal wastePercentage)
    {
        if (wastePercentage is < 0m or > MaxWastePercentage)
        {
            throw new InvalidOperationException("APU resource waste percentage is invalid.");
        }

        return wastePercentage;
    }
}
