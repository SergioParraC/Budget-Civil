using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.CopyMatrix;

public sealed class CopyMatrixCommand
{
    public Guid SourceMatrixResourceId { get; init; }
}

public sealed class CopyMatrixCommandValidator : AbstractValidator<CopyMatrixCommand>
{
    public CopyMatrixCommandValidator()
    {
        RuleFor(command => command.SourceMatrixResourceId)
            .NotEqual(Guid.Empty);
    }
}

/// <summary>
/// Copia de una Matriz sin validación de nombres, similitudes ni duplicados: crea una Matriz nueva, sus
/// componentes y los insumos que los componen como registros nuevos (`REQ-APUS-08`, `SC-APUS-35`…`41`).
/// </summary>
public sealed class CopyMatrixCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IApuResourceCopyWriter apuResourceCopyWriter,
    IValidator<CopyMatrixCommand> validator)
{
    public async Task<CopyMatrixResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetProjectId,
        CopyMatrixCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" | ", result.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, ApuRules.ValidationError);
        }

        var (_, targetAccess) = await apuAccessService.EnsureWriteAsync(
            actorUserId,
            targetProjectId,
            cancellationToken);

        var source = await apuResourceCopyWriter.GetResourceAsync(
            command.SourceMatrixResourceId,
            cancellationToken)
            ?? throw new NotFoundAppException("La Matriz no existe.", ApuRules.MatrixNotFound);

        if (!string.Equals(source.ResourceType?.Code, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("El insumo no es de tipo Matriz.", ApuRules.MatrixTypeInvalid);
        }

        var sourceAccess = await apuAccessService.GetProjectAccessForActorAsync(
            actorUserId,
            source.ProjectId,
            cancellationToken);

        if (sourceAccess is null || sourceAccess.CompanyId != targetAccess.CompanyId)
        {
            // No se revela la existencia de una Matriz de otra compañía (`SC-APUS-40`).
            throw new NotFoundAppException("La Matriz no existe.", ApuRules.MatrixNotFound);
        }

        ApuAccessService.EnsureCanRead(
            await apuAccessService.GetActorAsync(actorUserId, cancellationToken),
            sourceAccess);

        MatrixCopyResult copy = null!;
        await apuRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            copy = await apuResourceCopyWriter.CopyMatrixAsync(
                source.Id,
                targetProjectId,
                actorUserId,
                DateTime.UtcNow,
                transactionCancellationToken);

            await apuRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return new CopyMatrixResponse { MatrixResourceId = copy.MatrixResourceId };
    }
}
