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

public sealed class UpdateWbsItemCommand
{
    public string? Code { get; init; }

    public decimal QuantityWork { get; init; }

    public string? RowVersion { get; init; }

    public UpdateWbsItemApuDraft? Apu { get; init; }
}

public sealed class UpdateWbsItemApuDraft
{
    public string? Description { get; init; }

    public string? Unit { get; init; }

    public string? ApuRowVersion { get; init; }

    public IReadOnlyList<WbsItemApuResourceUpdateDraft>? Resources { get; init; }
}

public sealed class WbsItemApuResourceUpdateDraft
{
    public Guid? LineId { get; init; }

    public Guid ResourceId { get; init; }

    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }
}

public sealed class UpdateWbsItemCommandValidator : AbstractValidator<UpdateWbsItemCommand>
{
    public UpdateWbsItemCommandValidator()
    {
        RuleFor(command => command.RowVersion)
            .NotEmpty();

        RuleFor(command => command.QuantityWork)
            .GreaterThanOrEqualTo(0m);

        When(command => command.Apu is not null, () =>
        {
            RuleFor(command => command.Apu!.ApuRowVersion)
                .NotEmpty();

            RuleFor(command => command.Apu!.Description)
                .NotEmpty()
                .When(command => command.Apu!.Description is not null);

            RuleFor(command => command.Apu!.Unit)
                .MaximumLength(Apu.UnitMaxLength)
                .When(command => command.Apu!.Unit is not null);

            RuleForEach(command => command.Apu!.Resources).ChildRules(resource =>
            {
                resource.RuleFor(line => line.ResourceId)
                    .NotEqual(Guid.Empty);

                resource.RuleFor(line => line.LineId)
                    .NotEqual(Guid.Empty)
                    .When(line => line.LineId.HasValue);

                resource.RuleFor(line => line.Consumption)
                    .GreaterThan(0m);

                resource.RuleFor(line => line.WastePercentage)
                    .InclusiveBetween(0m, 100m);
            });
        });
    }
}

/// <summary>
/// Edición unificada de ítem EDT + APU (`REQ-EDT-22`, `REQ-APUS-21`). `quantityWork` es obligatorio;
/// `code` es opcional; el bloque `apu` es opcional y parcial. Cuando `resources` viene, reemplaza la
/// composición completa: las líneas con `lineId` se actualizan, las nuevas se crean y las ausentes se
/// eliminan. Todo ocurre en una transacción con concurrencia optimista de ítem y APU.
/// </summary>
public sealed class UpdateWbsItemCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IApuRepository apuRepository,
    ApuDetailService apuDetailService,
    IValidator<UpdateWbsItemCommand> validator)
{
    public async Task<WbsItemApuResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid itemId,
        UpdateWbsItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var quantityWork = WbsRules.EnsureValidQuantity(command.QuantityWork);
        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);
        var requestedCode = command.Code is null
            ? null
            : WbsRules.EnsureValidCode(command.Code);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var item = await wbsRepository.GetItemAsync(projectId, itemId, cancellationToken)
            ?? throw new NotFoundAppException("El ítem no existe en el proyecto.", "WBS_NOT_FOUND");

        var codeChanged = requestedCode is not null &&
            !string.Equals(requestedCode, item.Code.Code, StringComparison.Ordinal);
        var quantityChanged = item.QuantityWork != quantityWork;

        // La edición que solo toca el APU no exige ni modifica la versión del ítem: un cambio ajeno en
        // el nodo (mover, reordenar, editar desde otra pestaña) no debe bloquear el guardado de un APU
        // cuyos datos y composición no cambiaron. El APU conserva su propia concurrencia optimista.
        if (codeChanged || quantityChanged)
        {
            WbsRules.EnsureCurrentRowVersion(item.RowVersion, requestedRowVersion);
            wbsRepository.SetOriginalRowVersion(item, requestedRowVersion);
        }

        if (codeChanged &&
            await wbsRepository.CodeExistsAsync(projectId, requestedCode!, item.CodeId, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var utcNow = DateTime.UtcNow;
        Apu? apu = null;

        try
        {
            if (codeChanged)
            {
                item.Code.ChangeCode(requestedCode!);
            }

            if (quantityChanged)
            {
                item.ChangeQuantity(quantityWork);
            }

            if (codeChanged || quantityChanged)
            {
                item.TouchAuthor(actorUserId, utcNow);
            }

            if (command.Apu is not null)
            {
                apu = await ApplyApuChangesAsync(
                    projectId,
                    item,
                    command.Apu,
                    actorUserId,
                    utcNow,
                    cancellationToken);
            }
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, WbsRules.ValidationError);
        }

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);

            if (apu is not null)
            {
                var payload = await apuRepository.GetCostPayloadAsync(
                    projectId,
                    apu.Id,
                    transactionCancellationToken);

                ApuDetailService.RecalculateStatus(apu, payload?.Lines ?? []);
                await wbsRepository.SaveChangesAsync(transactionCancellationToken);
            }
        }, cancellationToken);

        var detailApu = apu ?? await apuRepository.GetAsync(
            projectId,
            item.ApuId ?? Guid.Empty,
            cancellationToken)
            ?? throw new NotFoundAppException("El APU no existe en el proyecto.", ApuRules.ApuNotFound);

        var detail = await apuDetailService.BuildAsync(projectId, detailApu, cancellationToken);
        var summary = new ApuSummary(
            detailApu.Id,
            ApuStatusCodes.ToCode(detailApu.Status),
            detailApu.Description,
            detailApu.Unit,
            Convert.ToBase64String(detailApu.RowVersion));

        return WbsItemApuMapper.ToResponse(WbsMapper.ToItemNode(item, summary), detail);
    }

    private async Task<Apu> ApplyApuChangesAsync(
        Guid projectId,
        WbsItem item,
        UpdateWbsItemApuDraft draft,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!item.ApuId.HasValue)
        {
            throw new NotFoundAppException(
                "El APU no existe en el proyecto.",
                ApuRules.ApuNotFound);
        }

        var apu = await apuRepository.GetAsync(projectId, item.ApuId.Value, cancellationToken)
            ?? throw new NotFoundAppException(
                "El APU no existe en el proyecto.",
                ApuRules.ApuNotFound);

        var requestedRowVersion = ApuRules.ParseRowVersion(draft.ApuRowVersion);
        ApuRules.EnsureApuRowVersion(apu.RowVersion, requestedRowVersion);
        apuRepository.SetOriginalRowVersion(apu, requestedRowVersion);

        apu.ChangeDetails(draft.Description, draft.Unit, actorUserId, utcNow);

        if (draft.Resources is not null)
        {
            await ReplaceResourcesAsync(
                projectId,
                apu,
                draft.Resources,
                actorUserId,
                utcNow,
                cancellationToken);
        }

        return apu;
    }

    private async Task ReplaceResourcesAsync(
        Guid projectId,
        Apu apu,
        IReadOnlyList<WbsItemApuResourceUpdateDraft> resources,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        foreach (var resourceId in resources.Select(resource => resource.ResourceId).Distinct())
        {
            _ = await apuRepository.GetProjectResourceAsync(projectId, resourceId, cancellationToken)
                ?? throw new NotFoundAppException(
                    "El insumo no existe en el proyecto.",
                    ApuRules.ApuResourceNotFound);
        }

        var existingLines = apu.Lines.ToDictionary(line => line.Id);
        var retainedLineIds = new HashSet<Guid>();

        foreach (var draft in resources)
        {
            if (draft.LineId is { } lineId)
            {
                if (!existingLines.TryGetValue(lineId, out var line))
                {
                    throw new ValidationAppException(
                        "La línea indicada no existe en el APU.",
                        ApuRules.ValidationError);
                }

                if (line.ResourceId != draft.ResourceId)
                {
                    apu.RemoveLine(lineId, actorUserId, utcNow);
                    apuRepository.RemoveLine(line);
                    var replacement = apu.AddLine(
                        draft.ResourceId,
                        draft.Consumption,
                        draft.WastePercentage,
                        actorUserId,
                        utcNow);
                    apuRepository.AddLine(replacement);
                    continue;
                }

                apu.UpdateLine(lineId, draft.Consumption, draft.WastePercentage, actorUserId, utcNow);
                retainedLineIds.Add(lineId);
                continue;
            }

            var addedLine = apu.AddLine(
                draft.ResourceId,
                draft.Consumption,
                draft.WastePercentage,
                actorUserId,
                utcNow);
            apuRepository.AddLine(addedLine);
        }

        foreach (var line in existingLines.Values.Where(line => !retainedLineIds.Contains(line.Id)))
        {
            if (apu.FindLine(line.Id) is not null)
            {
                apu.RemoveLine(line.Id, actorUserId, utcNow);
                apuRepository.RemoveLine(line);
            }
        }
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
