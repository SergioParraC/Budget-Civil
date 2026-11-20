using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Domain.Apus;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.CopyApu;

public sealed class CopyApuPreviewCommand
{
    public Guid SourceApuId { get; init; }
}

public sealed class CopyApuCommand
{
    public Guid SourceApuId { get; init; }

    public CopyApuTargetCommand Target { get; init; } = new();

    public IReadOnlyList<ApuCopyResolutionRequest> Resolutions { get; init; } = [];
}

public sealed class CopyApuTargetCommand
{
    public string Code { get; init; } = string.Empty;

    public Guid? WbsChapterId { get; init; }

    public decimal QuantityWork { get; init; }
}

public sealed class CopyApuPreviewCommandValidator : AbstractValidator<CopyApuPreviewCommand>
{
    public CopyApuPreviewCommandValidator()
    {
        RuleFor(command => command.SourceApuId).NotEqual(Guid.Empty);
    }
}

public sealed class CopyApuCommandValidator : AbstractValidator<CopyApuCommand>
{
    public CopyApuCommandValidator()
    {
        RuleFor(command => command.SourceApuId).NotEqual(Guid.Empty);
        RuleFor(command => command.Target).NotNull();
        RuleFor(command => command.Target.Code).NotEmpty();
        RuleFor(command => command.Target.WbsChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.Target.WbsChapterId.HasValue);
        RuleFor(command => command.Target.QuantityWork).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>
/// Vista previa de reconciliación de la copia (`REQ-APUS-10`, `SC-APUS-48`): solo insumos simples, con
/// `EXACT`/`SIMILAR`/`NONE` y hasta diez candidatos por insumo.
///
/// <para>
/// **Uso conjunto con `POST /apus/copy`.** Este endpoint no copia nada: detecta conflictos y entrega los
/// datos para resolverlos. El flujo correcto es:
/// </para>
/// <list type="number">
/// <item>Llamar a `GET /apus/copy/preview` con `sourceApuId`.</item>
/// <item>
/// Para cada insumo con `requiresResolution = true` (`EXACT` o `SIMILAR`), comparar sus `candidates` con
/// el insumo de origen usando `unitPrice`, `unit`, `comparison.unitMatches`,
/// `comparison.unitPriceMatches` y `comparison.unitPriceDifference`; el candidato elegido se envía en
/// `resolutions` con `decision = USE_EXISTING` y `existingResourceId`.
/// </item>
/// <item>
/// Si el usuario prefiere conservar los datos del origen, enviar `decision = KEEP_ORIGIN`; la copia crea
/// un insumo nuevo en el destino con `origin_resource_id`.
/// </item>
/// <item>
/// Los insumos `NONE` y las líneas de Matriz no requieren resolución: se copian siempre como registros
/// nuevos. Si se omite una resolución `EXACT`/`SIMILAR`, `POST /apus/copy` responde
/// `400 APU_COMPOSITION_RESOLUTION_REQUIRED` y no crea nada.
/// </item>
/// </list>
/// <para>
/// Otros conflictos a resolver antes de copiar: el código del ítem destino debe estar libre
/// (`409 WBS_CODE_DUPLICATED`), el APU de origen debe ser de la misma compañía (`404 APU_NOT_FOUND` si no
/// se puede revelar) y el actor debe tener `APUS_WRITE` con `ProjectMember` en el proyecto destino y
/// lectura sobre el de origen.
/// </para>
/// </summary>
public sealed class CopyApuPreviewCommandHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    IValidator<CopyApuPreviewCommand> validator)
{
    public async Task<ApuCopyPreviewResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetProjectId,
        CopyApuPreviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" | ", result.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, ApuRules.ValidationError);
        }

        var (actor, targetAccess) = await apuAccessService.EnsureWriteAsync(
            actorUserId,
            targetProjectId,
            cancellationToken);

        var source = await LoadSourceAsync(
            apuAccessService,
            apuRepository,
            actor,
            targetAccess.CompanyId,
            command.SourceApuId,
            cancellationToken);

        var payload = await apuRepository.GetCostPayloadAsync(source.ProjectId, source.Id, cancellationToken);
        var targetResources = await apuRepository.GetProjectSimpleResourcesAsync(
            targetProjectId,
            cancellationToken);

        var items = (payload?.Lines ?? [])
            .Where(line => !line.IsMatrix)
            .GroupBy(line => line.ResourceId)
            .Select(group => group.First())
            .Select(line =>
            {
                var match = ApuCopyReconciliation.Classify(
                    new SimpleResourceSnapshot(
                        line.ResourceId,
                        line.ResourceTypeCode,
                        line.Description,
                        line.Unit,
                        line.UnitPrice,
                        string.Empty),
                    targetResources);

                return new ApuCopyPreviewItemDto
                {
                    SourceResourceId = line.ResourceId,
                    Description = line.Description,
                    ResourceTypeCode = line.ResourceTypeCode,
                    Unit = line.Unit,
                    UnitPrice = line.UnitPrice,
                    Match = match.Match,
                    RequiresResolution = !string.Equals(
                        match.Match,
                        ApuCopyReconciliation.None,
                        StringComparison.Ordinal),
                    Candidates = match.Candidates
                        .Select(candidate =>
                        {
                            var comparison = ApuCopyReconciliation.Compare(
                                line.Unit,
                                line.UnitPrice,
                                candidate.Unit,
                                candidate.UnitPrice);

                            return new ApuCopyCandidateDto
                            {
                                ResourceId = candidate.ResourceId,
                                Description = candidate.Description,
                                ResourceTypeCode = candidate.ResourceTypeCode,
                                Unit = candidate.Unit,
                                UnitPrice = candidate.UnitPrice,
                                QuotationStatus = candidate.QuotationStatus,
                                Comparison = new ApuCopyComparisonDto
                                {
                                    UnitMatches = comparison.UnitMatches,
                                    UnitPriceMatches = comparison.UnitPriceMatches,
                                    UnitPriceDifference = comparison.UnitPriceDifference
                                }
                            };
                        })
                        .ToList()
                };
            })
            .ToList();

        return new ApuCopyPreviewResponse
        {
            SourceApuId = source.Id,
            Items = items
        };
    }

    internal static async Task<Apu> LoadSourceAsync(
        ApuAccessService apuAccessService,
        IApuRepository apuRepository,
        ActorAccessSnapshot actor,
        Guid targetCompanyId,
        Guid sourceApuId,
        CancellationToken cancellationToken)
    {
        var source = await apuRepository.GetByIdAsync(sourceApuId, cancellationToken)
            ?? throw new NotFoundAppException("El APU no existe.", ApuRules.ApuNotFound);

        var sourceAccess = await apuAccessService.GetProjectAccessForActorAsync(
            actor.UserId,
            source.ProjectId,
            cancellationToken);

        if (sourceAccess is null || sourceAccess.CompanyId != targetCompanyId)
        {
            throw new NotFoundAppException("El APU no existe.", ApuRules.ApuNotFound);
        }

        ApuAccessService.EnsureCanRead(actor, sourceAccess);
        return source;
    }
}
