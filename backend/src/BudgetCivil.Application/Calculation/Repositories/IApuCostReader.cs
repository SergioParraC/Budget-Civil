namespace BudgetCivil.Application.Calculation.Repositories;

/// <summary>Ítem del EDT con la cantidad de obra y el APU que necesita el cálculo (`REQ-CALCULO-01`).</summary>
public sealed record ProjectCostItem(
    Guid ItemId,
    Guid? ApuId,
    decimal QuantityWork,
    Guid? WbsChapterId,
    int OrderIndex);

/// <summary>Divisa base del proyecto para el contrato del presupuesto (`D-04`, `REQ-CALCULO-01`).</summary>
public sealed record ProjectCostCurrency(Guid Id, string IsoCode, string Name);

/// <summary>
/// Datos de lectura del presupuesto: ítems del EDT, líneas de APU con Matrices expandidas, `CostSettings`,
/// tope y divisa (`D-06`, `D-121`). La implementa `Infrastructure` con consultas set-based por proyecto.
/// </summary>
public sealed record ProjectCostData(
    Guid ProjectId,
    Guid CompanyId,
    CostSettings Settings,
    decimal? BudgetLimit,
    ProjectCostCurrency? BaseCurrency,
    IReadOnlyList<ProjectCostItem> Items,
    IReadOnlyDictionary<Guid, IReadOnlyList<ApuCostLineInput>> ApuLines);

/// <summary>
/// Puerto de lectura del cálculo (`D5`). No conoce `Application.Apus` ni `Application.Wbs`: entrega los
/// modelos de entrada que ya viven en `Application.Calculation`.
/// </summary>
public interface IApuCostReader
{
    Task<ProjectCostData?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
