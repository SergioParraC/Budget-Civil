using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Queries;

public sealed class GetProjectsQuery
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

public sealed class GetProjectsResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<ProjectListItem> Items { get; init; } = [];

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }
}

public sealed class ProjectListItem
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

public sealed class GetProjectsQueryHandler(
    IProjectRepository projectRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<GetProjectsResponse> ExecuteAsync(
        Guid actorUserId,
        GetProjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanReadProjects(actor);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        ProjectStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!ProjectStatusTransitionPolicy.TryParseStatusCode(query.Status, out var parsedStatus))
            {
                throw new ValidationAppException("status is invalid.", "VALIDATION_ERROR");
            }

            statusFilter = parsedStatus;
        }

        var scopedCompanyId = actor.Role.Code == ProjectStatusTransitionPolicy.AdministratorRoleCode
            ? query.CompanyId
            : actor.CompanyId;

        if (actor.Role.Code != ProjectStatusTransitionPolicy.AdministratorRoleCode && !scopedCompanyId.HasValue)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        var includeInhabilitado = actor.Role.Code is ProjectStatusTransitionPolicy.AdministratorRoleCode or ProjectStatusTransitionPolicy.ProjectManagerRoleCode;

        var totalItems = await projectRepository.CountProjectsAsync(
            scopedCompanyId,
            statusFilter,
            query.Search,
            includeInhabilitado,
            cancellationToken);

        var projects = await projectRepository.GetProjectsAsync(
            scopedCompanyId,
            statusFilter,
            query.Search,
            pagination.PageNumber,
            pagination.PageSize,
            includeInhabilitado,
            cancellationToken);

        return new GetProjectsResponse
        {
            Items = projects
                .Select(project => new ProjectListItem
                {
                    ProjectId = project.Id,
                    CompanyId = project.CompanyId,
                    Name = project.Name,
                    Status = ProjectStatusTransitionPolicy.ToStatusCode(project.Status),
                    IsPublic = project.IsPublic
                })
                .ToArray(),
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    private static UserAccount EnsureActorCanReadProjects(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.ProjectManagerRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode and not "AUDITOR")
        {
            throw new ForbiddenAppException("Actor cannot read projects.", "PROJECT_READ_FORBIDDEN");
        }

        return resolvedActor;
    }
}
