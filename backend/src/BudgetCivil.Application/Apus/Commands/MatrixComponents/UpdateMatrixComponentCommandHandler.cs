using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.MatrixComponents;

/// <summary>Edición de un componente con concurrencia optimista (`REQ-APUS-06`, `SC-APUS-70`).</summary>
public sealed class UpdateMatrixComponentCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IMatrixApuStatusUpdater matrixApuStatusUpdater,
    MatrixDetailService matrixDetailService,
    IValidator<UpdateMatrixComponentCommand> validator)
{
    public async Task<MatrixDetailResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid matrixResourceId,
        Guid componentId,
        UpdateMatrixComponentCommand command,
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

        try
        {
            component.Change(command.Consumption, command.WastePercentage);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ApuRules.ValidationError);
        }

        await MatrixComponentRules.PersistAsync(
            apuRepository,
            matrixApuStatusUpdater,
            projectId,
            matrixResourceId,
            actorUserId,
            DateTime.UtcNow,
            cancellationToken);

        return await matrixDetailService.BuildAsync(projectId, matrixResourceId, cancellationToken);
    }
}
