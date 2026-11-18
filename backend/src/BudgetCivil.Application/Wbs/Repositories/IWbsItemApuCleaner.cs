namespace BudgetCivil.Application.Wbs.Repositories;

/// <summary>
/// Limpieza del APU cuando se elimina un ítem del EDT (`REQ-EDT-21`, `D3`, `D-119`). Vive en `M-03` y se
/// implementa en `Infrastructure` para que `Application/Wbs` no dependa de EF Core.
/// </summary>
public interface IWbsItemApuCleaner
{
    /// <summary>Marca para eliminar los APUs vinculados a los ítems indicados; los insumos se conservan.</summary>
    Task<int> DeleteApusForItemsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si algún ítem del subárbol del capítulo tiene APU (`WBS_CHAPTER_HAS_APU`).</summary>
    Task<bool> HasApusInSubtreeAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default);
}
