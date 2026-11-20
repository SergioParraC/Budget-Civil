using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Queries;

public sealed class ResourceTypeOptionDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class GetResourceTypesQueryHandler(
    ResourceAccessService resourceAccessService,
    IResourceTypeReader resourceTypeReader)
{
    public async Task<IReadOnlyList<ResourceTypeOptionDto>> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await resourceAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var types = await resourceTypeReader.ListActiveAsync(cancellationToken);
        var catalogOrder = ResourceTypeCatalog.Definitions
            .Select((definition, index) => (definition.Id, Index: index))
            .ToDictionary(item => item.Id, item => item.Index);

        return types
            .OrderBy(type => catalogOrder.TryGetValue(type.Id, out var index) ? index : int.MaxValue)
            .ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .Select(type => new ResourceTypeOptionDto { Id = type.Id, Name = type.Name })
            .ToList();
    }
}