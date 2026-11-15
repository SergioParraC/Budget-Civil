using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Apus.Repositories;

/// <summary>
/// Persistencia del módulo `M-05` (`D-54`, `D-95`): APU, composición, Matrices, costo, conteo de uso y
/// transacciones. La aplicación nunca accede a EF Core (`T-A-04`).
/// </summary>
public interface IApuRepository
{
    Task<Apu?> GetAsync(
        Guid projectId,
        Guid apuId,
        CancellationToken cancellationToken = default);

    /// <summary>Lectura por identificador para la copia desde otro proyecto (`D9`).</summary>
    Task<Apu?> GetByIdAsync(
        Guid apuId,
        CancellationToken cancellationToken = default);

    Task<Apu?> GetByWbsItemAsync(
        Guid projectId,
        Guid wbsItemId,
        CancellationToken cancellationToken = default);

    Task<ApuCostPayload?> GetCostPayloadAsync(
        Guid projectId,
        Guid apuId,
        CancellationToken cancellationToken = default);

    Task<ApuListPage> ListOtherProjectsAsync(
        Guid projectId,
        Guid companyId,
        string? search,
        Guid? departmentId,
        Guid? municipalityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Apu apu, CancellationToken cancellationToken = default);

    /// <summary>Marca el retiro físico de una línea (`D4`).</summary>
    void RemoveLine(ApuResource line);

    Task<Resource?> GetProjectResourceAsync(
        Guid projectId,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SimpleResourceSnapshot>> GetProjectSimpleResourcesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<MatrixStateSnapshot?> GetMatrixStateAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatrixComponentRow>> GetMatrixComponentsAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default);

    Task<MatrixComponent?> GetMatrixComponentAsync(
        Guid projectId,
        Guid matrixResourceId,
        Guid componentId,
        CancellationToken cancellationToken = default);

    Task AddMatrixComponentAsync(
        MatrixComponent component,
        CancellationToken cancellationToken = default);

    void RemoveMatrixComponent(MatrixComponent component);

    Task<IReadOnlyList<MatrixComponentSource>> GetMatrixComponentsForCostAsync(
        Guid matrixResourceId,
        CancellationToken cancellationToken = default);

    Task MarkMatrixChangedAsync(
        Guid matrixResourceId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<ProjectCostSettings?> GetProjectSettingsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<ResourceUsageData> GetUsageDataAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    void SetOriginalRowVersion(Apu apu, byte[] rowVersion);

    void SetOriginalRowVersion(Resource resource, byte[] rowVersion);
}
