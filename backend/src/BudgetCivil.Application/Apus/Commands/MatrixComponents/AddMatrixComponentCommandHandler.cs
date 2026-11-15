using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Apus;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.MatrixComponents;

/// <summary>Alta de un componente de Matriz (`REQ-APUS-06`, `SC-APUS-26`…`29`, `D4`).</summary>
public sealed class AddMatrixComponentCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IMatrixApuStatusUpdater matrixApuStatusUpdater,
    MatrixDetailService matrixDetailService,
    IValidator<AddMatrixComponentCommand> validator)
{
    public async Task<MatrixDetailResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid matrixResourceId,
        AddMatrixComponentCommand command,
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

        await MatrixComponentRules.EnsureComponentAsync(
            apuRepository,
            projectId,
            matrixResourceId,
            command.ComponentResourceId,
            cancellationToken);

        MatrixComponent component;
        try
        {
            component = MatrixComponent.Create(
                matrixResourceId,
                command.ComponentResourceId,
                command.Consumption,
                command.WastePercentage);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ApuRules.ValidationError);
        }

        await apuRepository.AddMatrixComponentAsync(component, cancellationToken);
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
