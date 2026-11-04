using BudgetCivil.Domain.Resources;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetCivil.Infrastructure.Catalogs;

public sealed class ResourceTypeSeeder(
    BudgetCivilDbContext dbContext,
    ILogger<ResourceTypeSeeder> logger) : IResourceTypeSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingTypes = await dbContext.ResourceTypes
            .ToDictionaryAsync(resourceType => resourceType.Code, cancellationToken);

        foreach (var definition in ResourceTypeCatalog.Definitions)
        {
            if (existingTypes.TryGetValue(definition.Code, out var resourceType))
            {
                resourceType.Name = definition.Name;
                resourceType.IsActive = true;
                continue;
            }

            dbContext.ResourceTypes.Add(new ResourceType
            {
                Id = definition.Id,
                Code = definition.Code,
                Name = definition.Name,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Resource type seed completed with {Count} types", ResourceTypeCatalog.Definitions.Count);
    }
}
