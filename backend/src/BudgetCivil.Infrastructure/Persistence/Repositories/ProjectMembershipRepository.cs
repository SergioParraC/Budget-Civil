using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ProjectMembershipRepository(BudgetCivilDbContext dbContext)
    : BaseFeature002Repository(dbContext), IProjectMembershipRepository
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

    public Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return DbContext.Projects
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public Task<bool> IsUserAffiliatedToCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return DbContext.UserAccounts.AnyAsync(
            user => user.Id == userId && user.CompanyId == companyId,
            cancellationToken);
    }

    public Task<ProjectMember?> GetMembershipByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == projectId && member.UserId == userId,
            cancellationToken);
    }

    public Task<ProjectMember?> GetMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        return DbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.Id == membershipId,
            cancellationToken);
    }

    public async Task<bool> TryAddMembershipAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        DbContext.ProjectMembers.Add(member);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // La unicidad (project_id, user_id) la garantiza la base; la carrera se traduce en conflicto.
            DbContext.Entry(member).State = EntityState.Detached;
            return false;
        }
    }

    public async Task RemoveMembershipAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        DbContext.ProjectMembers.Remove(member);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
