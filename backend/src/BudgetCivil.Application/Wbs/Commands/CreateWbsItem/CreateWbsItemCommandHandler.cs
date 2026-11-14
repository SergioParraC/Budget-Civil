using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.CreateWbsItem;

public sealed class CreateWbsItemCommand
{
    public string? Code { get; init; }

    public Guid? WbsChapterId { get; init; }

    public decimal QuantityWork { get; init; }
}

public sealed class CreateWbsItemCommandValidator : AbstractValidator<CreateWbsItemCommand>
{
    public CreateWbsItemCommandValidator()
    {
        RuleFor(command => command.WbsChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.WbsChapterId.HasValue);
    }
}

/// <summary>
/// Alta de ítem con código manual y cantidad de obra, dentro de un capítulo o en la raíz
/// (`REQ-EDT-02`, `REQ-EDT-07`, `SC-EDT-06`, `SC-EDT-25`). El APU no se acepta en este incremento.
/// </summary>
public sealed class CreateWbsItemCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<CreateWbsItemCommand> validator)
{
    public async Task<WbsNodeDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CreateWbsItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var code = WbsRules.EnsureValidCode(command.Code);
        var quantityWork = WbsRules.EnsureValidQuantity(command.QuantityWork);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        if (command.WbsChapterId.HasValue)
        {
            var parent = await wbsRepository.GetChapterAsync(
                projectId,
                command.WbsChapterId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new ValidationAppException(
                    "El capítulo indicado no existe en el proyecto.",
                    WbsRules.ValidationError);
            }
        }

        if (await wbsRepository.CodeExistsAsync(projectId, code, null, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var utcNow = DateTime.UtcNow;
        var orderIndex = await wbsRepository.CountChildrenAsync(
            projectId,
            command.WbsChapterId,
            cancellationToken);

        WbsItem item;
        try
        {
            var wbsCode = WbsCode.Create(projectId, code, WbsNodeType.Item);
            item = WbsItem.Create(
                projectId,
                command.WbsChapterId,
                wbsCode,
                quantityWork,
                orderIndex,
                actorUserId,
                utcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, WbsRules.ValidationError);
        }

        await wbsRepository.AddItemAsync(item, cancellationToken);
        await wbsRepository.UpsertActivityAsync(projectId, actorUserId, utcNow, cancellationToken);
        await wbsRepository.SaveChangesAsync(cancellationToken);

        return WbsMapper.ToItemNode(item);
    }

    private async Task ValidateAsync(
        CreateWbsItemCommand command,
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
