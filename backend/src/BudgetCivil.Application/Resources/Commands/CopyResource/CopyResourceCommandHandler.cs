using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Commands.CopyResource;

public sealed class CopyResourceCommand
{
    public Guid SourceResourceId { get; init; }
}

public sealed class CopyResourceCommandValidator : AbstractValidator<CopyResourceCommand>
{
    public CopyResourceCommandValidator()
    {
        RuleFor(command => command.SourceResourceId)
            .NotEqual(Guid.Empty);
    }
}

public sealed class CopyResourceCommandHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository,
    IValidator<CopyResourceCommand> validator)
{
    public async Task<ResourceDto> ExecuteAsync(
        Guid actorUserId,
        Guid targetProjectId,
        CopyResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, "VALIDATION_ERROR");
        }

        var (actor, targetAccess) = await resourceAccessService.EnsureWriteAsync(
            actorUserId,
            targetProjectId,
            cancellationToken);

        var source = await resourceRepository.GetByIdAsync(command.SourceResourceId, cancellationToken)
            ?? throw new NotFoundAppException("Source resource was not found.", "RESOURCE_NOT_FOUND");

        var sourceAccess = await resourceAccessService.GetProjectAccessForActorAsync(
            actorUserId,
            source.ProjectId,
            cancellationToken)
            ?? throw new NotFoundAppException("Source resource was not found.", "RESOURCE_NOT_FOUND");

        if (sourceAccess.CompanyId != targetAccess.CompanyId)
        {
            // No se revela la existencia de un insumo de otra compañía (`REQ-INSUMOS-15`).
            throw new NotFoundAppException("Source resource was not found.", "RESOURCE_NOT_FOUND");
        }

        ResourceAccessService.EnsureCanRead(actor, sourceAccess);

        Resource copy;
        try
        {
            copy = source.CopyTo(targetProjectId, actorUserId, DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, "VALIDATION_ERROR");
        }

        await resourceRepository.AddAsync(copy, cancellationToken);
        await resourceRepository.SaveChangesAsync(cancellationToken);

        var stored = await resourceRepository.GetByIdAsync(targetProjectId, copy.Id, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found after copy.", "RESOURCE_NOT_FOUND");

        return ResourceMapper.ToDto(stored);
    }
}
