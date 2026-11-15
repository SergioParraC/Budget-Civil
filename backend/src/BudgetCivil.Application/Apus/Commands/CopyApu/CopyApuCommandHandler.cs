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

namespace BudgetCivil.Application.Apus.Commands.CopyApu;

/// <summary>
/// Copia de un APU desde otro proyecto de la misma compañía (`REQ-APUS-09`, `REQ-APUS-10`,
/// `REQ-APUS-11`, `SC-APUS-42`…`55`, `D9`). Las Matrices se copian siempre como registros nuevos y todo
/// ocurre en una transacción.
///
/// <para>
/// **Uso conjunto con `GET /apus/copy/preview`.** La copia consume las resoluciones que el cliente preparó
/// a partir de la vista previa:
/// </para>
/// <list type="bullet">
/// <item>
/// Insumos `NONE` (sin coincidencia) y todas las líneas de Matriz: se copian automáticamente como
/// registros nuevos; no requieren entrada en `resolutions`.
/// </item>
/// <item>
/// Insumos `EXACT` o `SIMILAR`: exigen una resolución por `sourceResourceId`.
/// `USE_EXISTING` reutiliza el insumo del destino indicado en `existingResourceId` (debe pertenecer al
/// proyecto destino y, normalmente, ser uno de los `candidates` del preview); `KEEP_ORIGIN` crea un
/// insumo nuevo con los datos del origen y `origin_resource_id`. Si falta una resolución, responde
/// `400 APU_COMPOSITION_RESOLUTION_REQUIRED` y la transacción no crea el APU.
/// </item>
/// <item>
/// El bloque `target` aporta `code`, `wbsChapterId` y `quantityWork`; el ítem y el APU se crean en la
/// misma transacción. Un código duplicado responde `409 WBS_CODE_DUPLICATED`; el APU de origen de otra
/// compañía responde `404 APU_NOT_FOUND` sin revelar su existencia.
/// </item>
/// <item>
/// Autorización: `APUS_WRITE` con `ProjectMember` en el destino y lectura sobre el proyecto de origen
/// (`403 APU_WRITE_FORBIDDEN` o `403 APU_READ_FORBIDDEN`).
/// </item>
/// </list>
/// <para>
/// El ítem nuevo y el APU nacen sin aprobación, con el actor como último modificador y con el estado
/// recalculado según la composición y la completitud de las Matrices copiadas. Todo ocurre en una
/// transacción: un fallo no deja ítem, APU, Matriz ni insumos parciales.
/// </para>
/// </summary>
public sealed class CopyApuCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IWbsRepository wbsRepository,
    IApuResourceCopyWriter apuResourceCopyWriter,
    ApuDetailService apuDetailService,
    IValidator<CopyApuCommand> validator)
{
    private const string UseExisting = "USE_EXISTING";
    private const string KeepOrigin = "KEEP_ORIGIN";

    public async Task<WbsItemApuResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetProjectId,
        CopyApuCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var validationMessage = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(validationMessage, ApuRules.ValidationError);
        }

        var (actor, targetAccess) = await apuAccessService.EnsureWriteAsync(
            actorUserId,
            targetProjectId,
            cancellationToken);

        var source = await CopyApuPreviewCommandHandler.LoadSourceAsync(
            apuAccessService,
            apuRepository,
            actor,
            targetAccess.CompanyId,
            command.SourceApuId,
            cancellationToken);

        var code = WbsRules.EnsureValidCode(command.Target.Code);
        var quantityWork = WbsRules.EnsureValidQuantity(command.Target.QuantityWork);

        if (command.Target.WbsChapterId.HasValue)
        {
            var parent = await wbsRepository.GetChapterAsync(
                targetProjectId,
                command.Target.WbsChapterId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new ValidationAppException(
                    "El capítulo indicado no existe en el proyecto.",
                    WbsRules.ValidationError);
            }
        }

        if (await wbsRepository.CodeExistsAsync(targetProjectId, code, null, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var payload = await apuRepository.GetCostPayloadAsync(
            source.ProjectId,
            source.Id,
            cancellationToken)
            ?? throw new NotFoundAppException("El APU no existe.", ApuRules.ApuNotFound);

        var targetResources = await apuRepository.GetProjectSimpleResourcesAsync(
            targetProjectId,
            cancellationToken);

        var resolutions = command.Resolutions
            .GroupBy(resolution => resolution.SourceResourceId)
            .ToDictionary(group => group.Key, group => group.First());

        var plans = BuildPlans(payload.Lines, targetResources, resolutions);
        var utcNow = DateTime.UtcNow;

        var orderIndex = await wbsRepository.CountChildrenAsync(
            targetProjectId,
            command.Target.WbsChapterId,
            cancellationToken);

        Apu copy;
        WbsItem item;
        try
        {
            copy = Apu.Create(
                targetProjectId,
                source.Description,
                source.Unit,
                actorUserId,
                utcNow);

            item = WbsItem.Create(
                targetProjectId,
                command.Target.WbsChapterId,
                WbsCode.Create(targetProjectId, code, WbsNodeType.Item),
                quantityWork,
                orderIndex,
                actorUserId,
                utcNow);

            item.LinkApu(copy.Id);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ApuRules.ValidationError);
        }

        await CopyAsync(
            copy,
            item,
            plans,
            targetProjectId,
            actorUserId,
            utcNow,
            cancellationToken);

        var detail = await apuDetailService.BuildAsync(targetProjectId, copy, cancellationToken);
        var summary = new ApuSummary(
            copy.Id,
            ApuStatusCodes.ToCode(copy.Status),
            copy.Description,
            copy.Unit,
            Convert.ToBase64String(copy.RowVersion));

        return WbsItemApuMapper.ToResponse(WbsMapper.ToItemNode(item, summary), detail);
    }

    private async Task CopyAsync(
        Apu copy,
        WbsItem item,
        IReadOnlyList<CopyPlan> plans,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await apuRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await apuRepository.AddAsync(copy, transactionCancellationToken);
            await wbsRepository.AddItemAsync(item, transactionCancellationToken);

            var copiedResources = new Dictionary<Guid, Guid>();
            var copiedMatrices = new Dictionary<Guid, Guid>();

            foreach (var plan in plans)
            {
                var targetResourceId = plan.Decision switch
                {
                    CopyDecision.KeepOrigin => await CopySimpleAsync(
                        plan.Line.ResourceId,
                        copiedResources,
                        targetProjectId,
                        actorUserId,
                        utcNow,
                        transactionCancellationToken),
                    CopyDecision.UseExisting => plan.ExistingResourceId!.Value,
                    _ => await CopyMatrixAsync(
                        plan.Line.ResourceId,
                        copiedMatrices,
                        targetProjectId,
                        actorUserId,
                        utcNow,
                        transactionCancellationToken)
                };

                copy.AddLine(
                    targetResourceId,
                    plan.Line.Consumption,
                    plan.Line.WastePercentage,
                    actorUserId,
                    utcNow);
            }

            await wbsRepository.UpsertActivityAsync(
                targetProjectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await apuRepository.SaveChangesAsync(transactionCancellationToken);

            var newPayload = await apuRepository.GetCostPayloadAsync(
                targetProjectId,
                copy.Id,
                transactionCancellationToken);

            ApuDetailService.RecalculateStatus(copy, newPayload?.Lines ?? []);
            await apuRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }

    private async Task<Guid> CopySimpleAsync(
        Guid sourceResourceId,
        IDictionary<Guid, Guid> copiedResources,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (copiedResources.TryGetValue(sourceResourceId, out var existingCopy))
        {
            return existingCopy;
        }

        var sourceResource = await apuResourceCopyWriter.GetResourceAsync(
            sourceResourceId,
            cancellationToken)
            ?? throw new NotFoundAppException(
                "El insumo de origen no existe.",
                ApuRules.ApuResourceNotFound);

        var copy = await apuResourceCopyWriter.CopyResourceAsync(
            sourceResource,
            targetProjectId,
            actorUserId,
            utcNow,
            cancellationToken);

        await apuResourceCopyWriter.AddResourceAsync(copy, cancellationToken);
        copiedResources[sourceResourceId] = copy.Id;
        return copy.Id;
    }

    private async Task<Guid> CopyMatrixAsync(
        Guid sourceMatrixResourceId,
        IDictionary<Guid, Guid> copiedMatrices,
        Guid targetProjectId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (copiedMatrices.TryGetValue(sourceMatrixResourceId, out var existingCopy))
        {
            return existingCopy;
        }

        var copy = await apuResourceCopyWriter.CopyMatrixAsync(
            sourceMatrixResourceId,
            targetProjectId,
            actorUserId,
            utcNow,
            cancellationToken);

        copiedMatrices[sourceMatrixResourceId] = copy.MatrixResourceId;
        return copy.MatrixResourceId;
    }

    private static IReadOnlyList<CopyPlan> BuildPlans(
        IReadOnlyList<ApuCostLineSource> lines,
        IReadOnlyList<SimpleResourceSnapshot> targetResources,
        IReadOnlyDictionary<Guid, ApuCopyResolutionRequest> resolutions)
    {
        var plans = new List<CopyPlan>();

        foreach (var line in lines)
        {
            if (line.IsMatrix)
            {
                plans.Add(new CopyPlan(line, CopyDecision.Copy, null));
                continue;
            }

            var match = ApuCopyReconciliation.Classify(
                new SimpleResourceSnapshot(
                    line.ResourceId,
                    line.ResourceTypeCode,
                    line.Description,
                    line.Unit,
                    line.UnitPrice,
                    string.Empty),
                targetResources);

            if (match.Match == ApuCopyReconciliation.None)
            {
                plans.Add(new CopyPlan(line, CopyDecision.Copy, null));
                continue;
            }

            if (!resolutions.TryGetValue(line.ResourceId, out var resolution))
            {
                throw new ValidationAppException(
                    "Cada insumo simple con coincidencia requiere una decisión de copia.",
                    ApuRules.ApuCompositionResolutionRequired);
            }

            var decision = resolution.Decision?.Trim().ToUpperInvariant();
            if (decision == KeepOrigin)
            {
                plans.Add(new CopyPlan(line, CopyDecision.KeepOrigin, null));
                continue;
            }

            if (decision != UseExisting)
            {
                throw new ValidationAppException(
                    "La decisión de copia no es válida; usa USE_EXISTING o KEEP_ORIGIN.",
                    ApuRules.ValidationError);
            }

            if (resolution.ExistingResourceId is not { } existingId || existingId == Guid.Empty)
            {
                throw new ValidationAppException(
                    "La decisión de usar el insumo existente requiere existingResourceId.",
                    ApuRules.ValidationError);
            }

            if (!targetResources.Any(resource => resource.ResourceId == existingId))
            {
                throw new ValidationAppException(
                    "El insumo existente no pertenece al proyecto destino.",
                    ApuRules.ValidationError);
            }

            plans.Add(new CopyPlan(line, CopyDecision.UseExisting, existingId));
        }

        return plans;
    }

    private enum CopyDecision
    {
        Copy = 1,
        KeepOrigin = 2,
        UseExisting = 3
    }

    private sealed record CopyPlan(ApuCostLineSource Line, CopyDecision Decision, Guid? ExistingResourceId);
}
