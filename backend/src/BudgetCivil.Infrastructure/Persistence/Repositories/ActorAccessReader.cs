using BudgetCivil.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ActorAccessReader(BudgetCivilDbContext dbContext) : IActorAccessReader
{
    public async Task<ActorAccessSnapshot?> GetSnapshotAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(userAccount => userAccount.Id == actorUserId)
            .Select(userAccount => new
            {
                userAccount.Id,
                userAccount.CompanyId,
                userAccount.RoleId,
                RoleCode = userAccount.Role.Code,
                userAccount.AccountEnabled,
                userAccount.AccountLocked,
                RoleIsActive = userAccount.Role.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        bool? companyIsActive = null;
        if (user.CompanyId.HasValue)
        {
            companyIsActive = await dbContext.Companies
                .AsNoTracking()
                .Where(company => company.Id == user.CompanyId.Value)
                .Select(company => (bool?)company.IsActive)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var grantedPermissionKeys = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission =>
                rolePermission.RoleId == user.RoleId &&
                rolePermission.IsActive &&
                rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionKey)
            .ToListAsync(cancellationToken);

        return new ActorAccessSnapshot(
            user.Id,
            user.CompanyId,
            user.RoleId,
            user.RoleCode,
            user.AccountEnabled,
            user.AccountLocked,
            user.RoleIsActive,
            companyIsActive,
            grantedPermissionKeys);
    }
}
