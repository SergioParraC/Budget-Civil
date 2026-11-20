using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;

namespace BudgetCivil.Application.Resources.Commands.DeleteResource;

/// <summary>
/// Baja física de un insumo (`REQ-INSUMOS-18`…`REQ-INSUMOS-20`, `D-49` revisada). Si el insumo está
/// referenciado por un APU, una Matriz o una copia, se rechaza con `409 RESOURCE_IN_USE`.
/// </summary>
public sealed class DeleteResourceCommandHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository)
{
    public async Task ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        await resourceAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var resource = await resourceRepository.GetByIdAsync(projectId, resourceId, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found.", "RESOURCE_NOT_FOUND");

        if (await resourceRepository.HasReferencesAsync(resource.Id, cancellationToken))
        {
            throw new ConflictAppException(
                "Resource is in use and cannot be deleted.",
                "RESOURCE_IN_USE");
        }

        await resourceRepository.DeleteAsync(resource, cancellationToken);
    }
}