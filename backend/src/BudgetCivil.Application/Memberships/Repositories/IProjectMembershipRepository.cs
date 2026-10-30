using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Repositories;

public interface IProjectMembershipRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<bool> IsUserAffiliatedToCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);

    Task<ProjectMember?> GetMembershipByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ProjectMember?> GetMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra la pertenencia. Devuelve <c>false</c> cuando ya existe una fila para el par
    /// (`project_id`, `user_id`), incluida la carrera entre dos solicitudes simultáneas (`D-98`).
    /// </summary>
    Task<bool> TryAddMembershipAsync(ProjectMember member, CancellationToken cancellationToken = default);

    Task RemoveMembershipAsync(ProjectMember member, CancellationToken cancellationToken = default);
}
