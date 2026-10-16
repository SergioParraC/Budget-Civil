using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public abstract class BaseFeature002Repository(BudgetCivilDbContext dbContext)
{
    protected BudgetCivilDbContext DbContext { get; } = dbContext;

    protected static DateTime UtcNow => DateTime.UtcNow;

    protected Task<UserAccount?> GetActorWithRoleAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
    }

    protected Task<Company?> GetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    protected Task<UserCompanyMembership?> GetActiveCompanyMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserCompanyMemberships
            .SingleOrDefaultAsync(membership => membership.UserId == userId && membership.IsActive, cancellationToken);
    }

    protected Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
