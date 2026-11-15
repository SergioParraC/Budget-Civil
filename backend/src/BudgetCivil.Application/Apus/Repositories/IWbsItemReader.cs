namespace BudgetCivil.Application.Apus.Repositories;

public sealed record WbsItemInfo(
    Guid ItemId,
    Guid ProjectId,
    string Code,
    decimal QuantityWork,
    Guid? ApuId);

/// <summary>
/// Lectura del EDT para `M-05` (`D3`, `D-54`): ítem por proyecto, ítem por APU y cantidades del proyecto,
/// sin exponer `DbContext` ni repositorios de `M-03`.
/// </summary>
public interface IWbsItemReader
{
    Task<WbsItemInfo?> GetItemAsync(
        Guid projectId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<WbsItemInfo?> GetItemByApuAsync(
        Guid apuId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, WbsItemInfo>> GetItemsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
