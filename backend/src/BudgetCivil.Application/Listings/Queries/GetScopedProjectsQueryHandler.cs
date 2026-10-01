using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Repositories;
using BudgetCivil.Domain.Projects;

namespace BudgetCivil.Application.Listings.Queries;

public sealed class GetScopedProjectsQuery
{
    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("search")]
    public string? Search { get; init; }

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; } = 10;
}

public sealed class GetScopedProjectsResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<ScopedProjectListItem> Items { get; init; } = [];

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }
}

public sealed class ScopedProjectListItem
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; init; }
}

public sealed class GetScopedProjectsQueryHandler(
    IListingRepository listingRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";
    private const string ProjectManagerRoleCode = "GERENTE_PROYECTOS";
    private const string EstimatorRoleCode = "PRESUPUESTADOR";
    private const string AuditorRoleCode = "AUDITOR";

    public async Task<GetScopedProjectsResponse> ExecuteAsync(
        Guid actorUserId,
        GetScopedProjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = await listingRepository.GetActorSnapshotAsync(actorUserId, cancellationToken);
        var resolvedActor = EnsureActorCanReadProjects(actor);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.PageNumber, query.PageSize, cancellationToken);

        ProjectStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!ProjectStatusTransitionPolicy.TryParseStatusCode(query.Status, out var parsedStatus))
            {
                throw new ValidationAppException("status is invalid.", "VALIDATION_ERROR");
            }

            statusFilter = parsedStatus;
        }

        var scopedCompanyId = ResolveScopedCompanyId(resolvedActor, query.CompanyId);
        var includeInhabilitado = resolvedActor.RoleCode is AdministratorRoleCode or ProjectManagerRoleCode;

        var totalItems = await listingRepository.CountProjectsAsync(
            scopedCompanyId,
            statusFilter,
            query.Search,
            includeInhabilitado,
            cancellationToken);

        var rows = await listingRepository.GetProjectsAsync(
            scopedCompanyId,
            statusFilter,
            query.Search,
            pagination.PageNumber,
            pagination.PageSize,
            includeInhabilitado,
            cancellationToken);

        return new GetScopedProjectsResponse
        {
            Items = rows.Select(project => new ScopedProjectListItem
            {
                ProjectId = project.ProjectId,
                CompanyId = project.CompanyId,
                Name = project.Name,
                Status = ProjectStatusTransitionPolicy.ToStatusCode(project.Status),
                IsPublic = project.IsPublic
            }).ToArray(),
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    private static ListingActorSnapshot EnsureActorCanReadProjects(ListingActorSnapshot? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.RoleIsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.RoleCode is not AdministratorRoleCode and not ProjectManagerRoleCode and not EstimatorRoleCode and not AuditorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot read projects.", "PROJECT_READ_FORBIDDEN");
        }

        return resolvedActor;
    }

    private static Guid? ResolveScopedCompanyId(ListingActorSnapshot actor, Guid? requestedCompanyId)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            return requestedCompanyId;
        }

        if (!actor.CompanyId.HasValue)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        if (requestedCompanyId.HasValue && requestedCompanyId.Value != actor.CompanyId.Value)
        {
            throw new ForbiddenAppException("Actor cannot access projects from another company.", "PROJECT_SCOPE_FORBIDDEN");
        }

        return actor.CompanyId.Value;
    }
}
