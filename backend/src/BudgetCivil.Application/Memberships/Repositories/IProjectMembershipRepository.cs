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

    Task<ProjectUserMembership?> GetActiveMembershipByProjectAndUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    Task<ProjectUserMembership?> GetActiveMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(ProjectUserMembership membership, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
