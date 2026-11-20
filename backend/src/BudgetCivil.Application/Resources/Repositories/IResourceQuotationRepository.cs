using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Repositories;

public enum ResourceQuotationScope
{
    Selection = 1,
    Filter = 2,
    All = 3
}

/// <summary>Filtros del listado reutilizados por el cambio masivo (`D-101`, `D-11`).</summary>
public sealed record ResourceQuotationFilter(
    string? Description,
    Guid? ResourceTypeId,
    QuotationStatus? QuotationStatus,
    string? Brand,
    Guid? MunicipalityId,
    decimal? MinPrice,
    decimal? MaxPrice);

/// <summary>
/// Insumo resuelto dentro de la transacción con los datos que deciden su cotización (`REQ-COTIZACION-02`).
/// `IsComplete` replica `Resource.IsComplete`; la Matriz se evalúa por unidad y componentes (`REQ-APUS-06`).
/// </summary>
public sealed record ResourceQuotationCandidate(
    Guid ResourceId,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    string? Brand,
    Guid? SupplierId,
    QuotationStatus QuotationStatus,
    bool IsComplete,
    bool IsMatrix,
    int MatrixComponentCount);

/// <summary>
/// Resolución set-based y escritura del cambio masivo de cotización (`REQ-COTIZACION-01`, `D-12`).
/// </summary>
public interface IResourceQuotationRepository
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceQuotationCandidate>> GetByIdsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> resourceIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceQuotationCandidate>> GetByFilterAsync(
        Guid projectId,
        ResourceQuotationFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceQuotationCandidate>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como `COTIZADO` solo las filas indicadas, escribe su autoría y devuelve cuántas cambiaron
    /// (`REQ-COTIZACION-05`, `D-12`).
    /// </summary>
    Task<int> MarkQuotedAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> resourceIds,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
