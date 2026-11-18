using BudgetCivil.Application.Apus.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ApuProjectAccessReader(BudgetCivilDbContext dbContext) : IApuProjectAccessReader
{
    public async Task<ApuProjectAccess?> GetAccessAsync(
        Guid projectId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(existing => existing.Id == projectId)
            .Select(existing => new { existing.Id, existing.CompanyId })
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

        return new ApuProjectAccess(project.Id, project.CompanyId, isMember);
    }
}
