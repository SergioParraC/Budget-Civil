using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;

/// <summary>
/// Cambio masivo a `COTIZADO` por selección, filtro o totalidad en una sola transacción (`D-11`, `D-12`).
/// Resuelve el alcance en el servidor, reevalúa la completitud de cada insumo, omite los incompletos con
/// código y devuelve el resumen y los pendientes del proyecto (`REQ-COTIZACION-01`…`05`).
/// </summary>
public sealed class ChangeResourceQuotationStatusCommandHandler(
    ResourceAccessService resourceAccessService,
    IResourceQuotationRepository resourceQuotationRepository,
    ISecurityAuditService securityAuditService,
    IValidator<ChangeResourceQuotationStatusCommand> validator)
{
    public async Task<ChangeResourceQuotationStatusResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        ChangeResourceQuotationStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var (actor, access) = await resourceAccessService.EnsureWriteAsync(
            actorUserId,
            projectId,
            cancellationToken);

        ChangeResourceQuotationStatusCommand.TryParseScope(command.Scope, out var scope);
        var selection = scope == ResourceQuotationScope.Selection
            ? command.ResourceIds!.Distinct().ToList()
            : [];

        ChangeResourceQuotationStatusResponse? response = null;

        await resourceQuotationRepository.ExecuteInTransactionAsync(async token =>
        {
            var candidates = scope switch
            {
                ResourceQuotationScope.Selection => await resourceQuotationRepository.GetByIdsAsync(
                    projectId, selection, token),
                ResourceQuotationScope.Filter => await resourceQuotationRepository.GetByFilterAsync(
                    projectId, ToFilter(command.Filter!), token),
                _ => await resourceQuotationRepository.GetAllAsync(projectId, token)
            };

            if (scope == ResourceQuotationScope.Selection)
            {
                EnsureSelectionExists(selection, candidates);
            }

            var alreadyQuoted = candidates.Count(candidate =>
                candidate.QuotationStatus == QuotationStatus.Cotizado);

            var skipped = candidates
                .Where(candidate =>
                    candidate.QuotationStatus != QuotationStatus.Cotizado &&
                    !ResourceQuotationRules.CanQuote(candidate))
                .Select(candidate => new ResourceQuotationSkippedDto
                {
                    ResourceId = candidate.ResourceId,
                    Code = ResourceQuotationRules.IncompleteCode
                })
                .ToList();

            var toQuote = candidates
                .Where(candidate =>
                    candidate.QuotationStatus != QuotationStatus.Cotizado &&
                    ResourceQuotationRules.CanQuote(candidate))
                .Select(candidate => candidate.ResourceId)
                .ToList();

            var updated = toQuote.Count == 0
                ? 0
                : await resourceQuotationRepository.MarkQuotedAsync(
                    projectId,
                    toQuote,
                    actor.UserId,
                    DateTime.UtcNow,
                    token);

            var pendingResources = await resourceQuotationRepository.CountPendingAsync(projectId, token);

            response = new ChangeResourceQuotationStatusResponse
            {
                ProjectId = projectId,
                QuotationStatus = QuotationStatusCodes.Cotizado,
                Requested = candidates.Count,
                Updated = updated,
                AlreadyQuoted = alreadyQuoted,
                Skipped = skipped,
                PendingResources = pendingResources,
                AllQuoted = pendingResources == 0
            };

            await WriteAuditAsync(
                actor.UserId,
                access.CompanyId,
                projectId,
                command.Scope!,
                candidates.Count,
                updated,
                alreadyQuoted,
                skipped.Count,
                pendingResources,
                token);
        }, cancellationToken);

        return response!;
    }

    private static void EnsureSelectionExists(
        IReadOnlyCollection<Guid> selection,
        IReadOnlyList<ResourceQuotationCandidate> candidates)
    {
        var found = candidates.Select(candidate => candidate.ResourceId).ToHashSet();

        if (selection.Any(resourceId => !found.Contains(resourceId)))
        {
            throw new NotFoundAppException("Resource was not found.", "RESOURCE_NOT_FOUND");
        }
    }

    private static ResourceQuotationFilter ToFilter(ResourceQuotationFilterCommand filter)
    {
        return new ResourceQuotationFilter(
            filter.Description,
            filter.ResourceTypeId,
            ParseStatus(filter.QuotationStatus),
            filter.Brand,
            filter.MunicipalityId,
            filter.MinPrice,
            filter.MaxPrice);
    }

    private static QuotationStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return QuotationStatusCodes.TryParse(value, out var status)
            ? status
            : throw new ValidationAppException("quotationStatus is invalid.", "VALIDATION_ERROR");
    }

    private async Task WriteAuditAsync(
        Guid actorUserId,
        Guid companyId,
        Guid projectId,
        string scope,
        int requested,
        int updated,
        int alreadyQuoted,
        int skipped,
        int pendingResources,
        CancellationToken cancellationToken)
    {
        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actorUserId,
                companyId,
                SecurityAuditEventType.ResourceAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "RESOURCES_WRITE",
                TargetType: "Project",
                TargetId: projectId,
                ReasonCode: "RESOURCE_QUOTATION_CHANGED",
                Metadata: new Dictionary<string, string>
                {
                    ["scope"] = scope.Trim().ToUpperInvariant(),
                    ["requested"] = requested.ToString(),
                    ["updated"] = updated.ToString(),
                    ["already_quoted"] = alreadyQuoted.ToString(),
                    ["skipped"] = skipped.ToString(),
                    ["pending_resources"] = pendingResources.ToString()
                }),
            cancellationToken);
    }

    private async Task ValidateAsync(
        ChangeResourceQuotationStatusCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }
}
