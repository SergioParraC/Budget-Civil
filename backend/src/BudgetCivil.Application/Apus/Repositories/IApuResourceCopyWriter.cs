using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Apus.Repositories;

/// <summary>
/// Resultado de copiar una Matriz: el nuevo insumo y el mapeo entre cada insumo componente del origen y
/// su copia en el destino (`D9`, `REQ-APUS-08`).
/// </summary>
public sealed record MatrixCopyResult(
    Guid MatrixResourceId,
    IReadOnlyDictionary<Guid, Guid> ComponentResourceMap);

/// <summary>
/// Escritura de insumos y Matrices copiadas dentro del mismo `DbContext` y la misma transacción
/// (`D9`, `D-54`): `M-05` nunca escribe `resources` por su cuenta.
/// </summary>
public interface IApuResourceCopyWriter
{
    Task<Resource?> GetResourceAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<Resource> CopyResourceAsync(
        Resource source,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<MatrixCopyResult> CopyMatrixAsync(
        Guid sourceMatrixResourceId,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AddResourceAsync(Resource resource, CancellationToken cancellationToken = default);
}
