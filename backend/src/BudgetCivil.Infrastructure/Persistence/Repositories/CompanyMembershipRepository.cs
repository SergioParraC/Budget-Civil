using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class CompanyMembershipRepository(BudgetCivilDbContext dbContext)
    : BaseFeature002Repository(dbContext), ICompanyMembershipRepository
{
    public Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    public Task<UserAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    public Task<UserCompanyMembership?> GetActiveMembershipByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserCompanyMemberships
            .SingleOrDefaultAsync(membership => membership.UserId == userId && membership.IsActive, cancellationToken);
    }

    public Task<UserCompanyMembership?> GetActiveMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserCompanyMemberships
            .SingleOrDefaultAsync(membership => membership.Id == membershipId && membership.IsActive, cancellationToken);
    }

    public Task AddMembershipAsync(UserCompanyMembership membership, CancellationToken cancellationToken = default)
    {
        DbContext.UserCompanyMemberships.Add(membership);
        return Task.CompletedTask;
    }

    public new Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
