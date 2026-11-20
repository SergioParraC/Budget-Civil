using BudgetCivil.Application.Calculation.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class CalculationProjectAccessReader(BudgetCivilDbContext dbContext) : ICalculationProjectAccessReader
{
    public async Task<CalculationProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(existingProject => existingProject.Id == projectId)
            .Select(existingProject => new { existingProject.Id, existingProject.CompanyId })
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return null;
        }

        var isMember = await dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.ProjectId == projectId && member.UserId == actorUserId,
                cancellationToken);

        return new CalculationProjectAccess(project.Id, project.CompanyId, isMember);
    }
}
