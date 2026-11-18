using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.WbsItemApu;

public sealed class CreateWbsItemCommand
{
    public string? Code { get; init; }

    public Guid? WbsChapterId { get; init; }

    public decimal QuantityWork { get; init; }

    public CreateWbsItemApuDraft? Apu { get; init; }
}

public sealed class CreateWbsItemApuDraft
{
    public string Description { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public IReadOnlyList<WbsItemApuResourceDraft> Resources { get; init; } = [];
}

public sealed class WbsItemApuResourceDraft
{
    public Guid ResourceId { get; init; }

    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }
}

public sealed class CreateWbsItemCommandValidator : AbstractValidator<CreateWbsItemCommand>
{
    public CreateWbsItemCommandValidator()
    {
        RuleFor(command => command.WbsChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.WbsChapterId.HasValue);

        RuleFor(command => command.QuantityWork)
            .GreaterThan(0m);

        RuleFor(command => command.Apu)
            .NotNull();

        When(command => command.Apu is not null, () =>
        {
            RuleFor(command => command.Apu!.Description)
                .NotEmpty();

            RuleFor(command => command.Apu!.Unit)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(Apu.UnitMaxLength);

            RuleForEach(command => command.Apu!.Resources).ChildRules(resource =>
            {
                resource.RuleFor(line => line.ResourceId)
                    .NotEqual(Guid.Empty);

                resource.RuleFor(line => line.Consumption)
                    .GreaterThan(0m);

                resource.RuleFor(line => line.WastePercentage)
                    .InclusiveBetween(0m, 100m);
            });
        });
    }
}

/// <summary>
/// Alta unificada de ítem EDT + APU (`REQ-EDT-22`, `REQ-APUS-21`): crea el código, el ítem, el APU, sus
/// líneas y el vínculo 1:1 en una sola transacción. Los recursos pueden ser cero; en ese caso el APU queda
/// en `FALTA_INFORMACION`.
/// </summary>
public sealed class CreateWbsItemCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IApuRepository apuRepository,
    ApuDetailService apuDetailService,
    IValidator<CreateWbsItemCommand> validator)
{
    public async Task<WbsItemApuResponse> ExecuteAsync(
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

        Apu apu;
        WbsItem item;
        try
        {
            apu = Apu.Create(
                projectId,
                command.Apu!.Description,
                command.Apu.Unit,
                actorUserId,
                utcNow);

            foreach (var draft in command.Apu.Resources)
            {
                var resource = await apuRepository.GetProjectResourceAsync(
                    projectId,
                    draft.ResourceId,
                    cancellationToken)
                    ?? throw new NotFoundAppException(
                        "El insumo no existe en el proyecto.",
                        ApuRules.ApuResourceNotFound);

                apu.AddLine(
                    resource.Id,
                    draft.Consumption,
                    draft.WastePercentage,
                    actorUserId,
                    utcNow);
            }

            item = WbsItem.Create(
                projectId,
                command.WbsChapterId,
                WbsCode.Create(projectId, code, WbsNodeType.Item),
                quantityWork,
                orderIndex,
                actorUserId,
                utcNow);

            item.LinkApu(apu.Id);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, WbsRules.ValidationError);
        }

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await apuRepository.AddAsync(apu, transactionCancellationToken);
            await wbsRepository.AddItemAsync(item, transactionCancellationToken);
            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);

            var payload = await apuRepository.GetCostPayloadAsync(
                projectId,
                apu.Id,
                transactionCancellationToken);

            ApuDetailService.RecalculateStatus(apu, payload?.Lines ?? []);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        var detail = await apuDetailService.BuildAsync(projectId, apu, cancellationToken);
        var summary = new ApuSummary(
            apu.Id,
            ApuStatusCodes.ToCode(apu.Status),
            apu.Description,
            apu.Unit,
            Convert.ToBase64String(apu.RowVersion));

        return WbsItemApuMapper.ToResponse(WbsMapper.ToItemNode(item, summary), detail);
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

