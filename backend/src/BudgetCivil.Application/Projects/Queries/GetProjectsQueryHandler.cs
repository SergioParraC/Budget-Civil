using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Queries;

public sealed class GetProjectsQuery
{
    public Guid? CompanyId { get; init; }

    public string? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class GetProjectsResponse
{
    public IReadOnlyList<ProjectListItem> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class ProjectListItem
{
    public Guid ProjectId { get; init; }

    public Guid CompanyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

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
            query.Page,
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

        var includeInhabilitado = actor.Role.Code is ProjectStatusTransitionPolicy.AdministratorRoleCode or ProjectStatusTransitionPolicy.DirectorRoleCode;

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
            pagination.Page,
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
            Page = pagination.Page,
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

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode and not "AUDITOR")
        {
            throw new ForbiddenAppException("Actor cannot read projects.", "PROJECT_READ_FORBIDDEN");
        }

        return resolvedActor;
    }
}
