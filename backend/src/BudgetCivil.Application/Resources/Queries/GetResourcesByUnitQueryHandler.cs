using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Queries;

public sealed class GetResourcesByUnitQueryHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository)
{
    public async Task<IReadOnlyList<ResourceDto>> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        string? shortUnit,
        CancellationToken cancellationToken = default)
    {
        await resourceAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var normalized = shortUnit?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > Resource.UnitMaxLength)
        {
            throw new ValidationAppException("shortUnit is invalid.", "VALIDATION_ERROR");
        }

        var resources = await resourceRepository.ListByUnitAsync(
            projectId,
            normalized,
            cancellationToken);

        return resources.Select(ResourceMapper.ToDto).ToList();
    }
}