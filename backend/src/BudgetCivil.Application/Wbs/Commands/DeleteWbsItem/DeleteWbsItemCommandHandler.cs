using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.DeleteWbsItem;

public sealed class DeleteWbsItemCommand
{
    public string? RowVersion { get; init; }
}

public sealed class DeleteWbsItemCommandValidator : AbstractValidator<DeleteWbsItemCommand>
{
    public DeleteWbsItemCommandValidator()
    {
        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

/// <summary>
/// Baja física del ítem y su código; el APU no se elimina y queda desvinculado (`SC-EDT-51`,
/// `SC-EDT-55`, `D-119`). Las posiciones de los hermanos se compactan.
/// </summary>
public sealed class DeleteWbsItemCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<DeleteWbsItemCommand> validator)
{
    public async Task ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid itemId,
        DeleteWbsItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var item = await wbsRepository.GetItemAsync(projectId, itemId, cancellationToken)
            ?? throw new NotFoundAppException("El ítem no existe en el proyecto.", "WBS_NOT_FOUND");

        WbsRules.EnsureCurrentRowVersion(item.RowVersion, requestedRowVersion);
        wbsRepository.SetOriginalRowVersion(item, requestedRowVersion);

        var parentChapterId = item.WbsChapterId;
        var code = item.Code;
        var utcNow = DateTime.UtcNow;

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            wbsRepository.RemoveItem(item);
            wbsRepository.RemoveCode(code);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);

            await wbsRepository.CompactAsync(projectId, parentChapterId, transactionCancellationToken);
            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }

    private async Task ValidateAsync(
        DeleteWbsItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", result.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, WbsRules.ValidationError);
    }
}
