using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.UpdateWbsItem;

public sealed class UpdateWbsItemCommand
{
    public string? Code { get; init; }

    public decimal QuantityWork { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class UpdateWbsItemCommandValidator : AbstractValidator<UpdateWbsItemCommand>
{
    public UpdateWbsItemCommandValidator()
    {
        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

/// <summary>
/// Edición de código y cantidad; el traslado se hace con `move` (`D-118`, `SC-EDT-27`, `SC-EDT-37`…`39`).
/// </summary>
public sealed class UpdateWbsItemCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<UpdateWbsItemCommand> validator)
{
    public async Task<WbsNodeDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid itemId,
        UpdateWbsItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var code = WbsRules.EnsureValidCode(command.Code);
        var quantityWork = WbsRules.EnsureValidQuantity(command.QuantityWork);
        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var item = await wbsRepository.GetItemAsync(projectId, itemId, cancellationToken)
            ?? throw new NotFoundAppException("El ítem no existe en el proyecto.", "WBS_NOT_FOUND");

        WbsRules.EnsureCurrentRowVersion(item.RowVersion, requestedRowVersion);
        wbsRepository.SetOriginalRowVersion(item, requestedRowVersion);

        if (await wbsRepository.CodeExistsAsync(projectId, code, item.CodeId, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var utcNow = DateTime.UtcNow;
        item.Code.ChangeCode(code);
        item.ChangeQuantity(quantityWork);
        item.TouchAuthor(actorUserId, utcNow);

        await wbsRepository.UpsertActivityAsync(projectId, actorUserId, utcNow, cancellationToken);
        await wbsRepository.SaveChangesAsync(cancellationToken);

        return WbsMapper.ToItemNode(item);
    }

    private async Task ValidateAsync(
        UpdateWbsItemCommand command,
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
