using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
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

    public async Task<bool> IsUserAffiliatedToCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var userCompanyLink = await DbContext.UserAccounts.AnyAsync(
            user => user.Id == userId && user.CompanyId == companyId,
            cancellationToken);

        if (userCompanyLink)
        {
            return true;
        }

        return await DbContext.UserCompanyMemberships.AnyAsync(
            membership => membership.UserId == userId && membership.CompanyId == companyId && membership.IsActive,
            cancellationToken);
    }

    public Task<ProjectUserMembership?> GetActiveMembershipByProjectAndUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.ProjectUserMemberships.SingleOrDefaultAsync(
            membership => membership.ProjectId == projectId && membership.UserId == userId && membership.IsActive,
            cancellationToken);
    }

    public Task<ProjectUserMembership?> GetActiveMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        return DbContext.ProjectUserMemberships.SingleOrDefaultAsync(
            membership => membership.Id == membershipId && membership.IsActive,
            cancellationToken);
    }

    public Task AddMembershipAsync(ProjectUserMembership membership, CancellationToken cancellationToken = default)
    {
        DbContext.ProjectUserMemberships.Add(membership);
        return Task.CompletedTask;
    }

    public new Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
