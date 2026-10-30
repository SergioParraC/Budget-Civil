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

    public async Task<bool> TryAssignCompanyAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbContext.UserAccounts
            .Where(user => user.Id == userId && user.CompanyId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.CompanyId, (Guid?)companyId)
                    .SetProperty(user => user.UpdatedAtUtc, utcNow),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryUnassignCompanyAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbContext.UserAccounts
            .Where(user => user.Id == userId && user.CompanyId == companyId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.CompanyId, (Guid?)null)
                    .SetProperty(user => user.UpdatedAtUtc, utcNow),
                cancellationToken);

        return affectedRows == 1;
    }
}
