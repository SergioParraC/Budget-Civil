namespace BudgetCivil.Application.Projects.Repositories;

/// <summary>
/// Conteo de insumos pendientes de cotización para la precondición de `REQ-PROYECTOS-05` (`D-13`). Vive en
/// `Application.Projects` para no acoplar la transición del proyecto al módulo de insumos.
/// </summary>
public interface IProjectQuotationStatusReader
{
    Task<int> CountPendingResourcesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
