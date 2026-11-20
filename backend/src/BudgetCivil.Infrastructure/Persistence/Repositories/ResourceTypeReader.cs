using BudgetCivil.Application.Resources.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ResourceTypeReader(BudgetCivilDbContext dbContext) : IResourceTypeReader
{
    public async Task<IReadOnlyList<ResourceTypeOption>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResourceTypes
            .AsNoTracking()
            .Where(resourceType => resourceType.IsActive)
            .Select(resourceType => new ResourceTypeOption(resourceType.Id, resourceType.Code, resourceType.Name))
            .ToListAsync(cancellationToken);
    }
}