using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Repositories;

public interface ICompanyMembershipRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<UserCompanyMembership?> GetActiveMembershipByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserCompanyMembership?> GetActiveMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(UserCompanyMembership membership, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
