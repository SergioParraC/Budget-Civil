using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.MatrixComponents;

/// <summary>Retiro físico de un componente (`REQ-APUS-06`, `SC-APUS-30`).</summary>
public sealed class RemoveMatrixComponentCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IMatrixApuStatusUpdater matrixApuStatusUpdater,
    IValidator<RemoveMatrixComponentCommand> validator)
{
    public async Task ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid matrixResourceId,
        Guid componentId,
        RemoveMatrixComponentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" | ", result.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, ApuRules.ValidationError);
        }

        await apuAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);
        await MatrixComponentRules.EnsureMatrixAsync(
            apuRepository,
            projectId,
            matrixResourceId,
            command.RowVersion,
            cancellationToken);

        var component = await apuRepository.GetMatrixComponentAsync(
            projectId,
            matrixResourceId,
            componentId,
            cancellationToken)
            ?? throw new NotFoundAppException(
                "El componente no existe en la Matriz.",
                ApuRules.ApuResourceNotFound);

        apuRepository.RemoveMatrixComponent(component);

        await MatrixComponentRules.PersistAsync(
            apuRepository,
            matrixApuStatusUpdater,
            projectId,
            matrixResourceId,
            actorUserId,
            DateTime.UtcNow,
            cancellationToken);
    }
}
