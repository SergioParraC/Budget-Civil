using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.Data.SqlClient;
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

    public Task<Company?> GetCompanyWithDetailsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.Companies
            .AsNoTracking()
            .Include(company => company.Country)
            .Include(company => company.Department)
            .Include(company => company.Municipality)
            .SingleOrDefaultAsync(company => company.Id == companyId, cancellationToken);
    }

    public async Task<bool> AssignAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var user = await DbContext.UserAccounts
            .SingleOrDefaultAsync(userAccount => userAccount.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        // Reutiliza la fila histórica de la misma compañía (reactivación) o inserta una nueva.
        CompanyMembershipAssignment.Attach(user, companyId, utcNow);
        user.UpdatedAtUtc = utcNow;

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // El índice único filtrado impide dos vínculos vigentes (`BR-001`) incluso en carrera.
            return false;
        }
    }

    public async Task<bool> DeactivateAsync(
        Guid userId,
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var membership = await DbContext.CompanyMemberships
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == userId &&
                    candidate.CompanyId == companyId &&
                    candidate.AccountEnabled,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        membership.Deactivate(utcNow);

        var user = await DbContext.UserAccounts
            .SingleOrDefaultAsync(userAccount => userAccount.Id == userId, cancellationToken);

        if (user is not null)
        {
            user.UpdatedAtUtc = utcNow;
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<int> RemoveProjectMembershipsAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.ProjectMembers
            .Where(member =>
                member.UserId == userId &&
                DbContext.Projects.Any(project => project.Id == member.ProjectId && project.CompanyId == companyId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
