using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;

namespace BudgetCivil.Application.Resources.Queries;

public sealed class GetResourceByIdQueryHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository)
{
    public async Task<ResourceDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        await resourceAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var resource = await resourceRepository.GetByIdAsync(projectId, resourceId, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found.", "RESOURCE_NOT_FOUND");

        return ResourceMapper.ToDto(resource);
    }
}
