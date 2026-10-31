using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Repositories;

public interface ICompanyMembershipRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyWithDetailsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra el vínculo en <c>users_company</c>. Si el usuario ya tuvo un vínculo con la misma
    /// compañía se reactiva esa fila (conservando la fecha de vinculación original); en caso contrario
    /// se inserta una vinculación nueva. Devuelve <c>false</c> cuando otra solicitud dejó al usuario
    /// con una compañía vigente (regla `BR-001` garantizada por el índice único filtrado).
    /// </summary>
    Task<bool> AssignAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cierra el vínculo vigente del usuario con la compañía indicada, conservando la fila para el
    /// historial. Devuelve <c>false</c> si no había un vínculo vigente.
    /// </summary>
    Task<bool> DeactivateAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina las pertenencias del usuario a proyectos de esa compañía (`project_members`).
    /// </summary>
    Task<int> RemoveProjectMembershipsAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);
}
