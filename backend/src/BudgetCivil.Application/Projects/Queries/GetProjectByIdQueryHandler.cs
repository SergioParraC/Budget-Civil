using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Queries;

public sealed class GetProjectByIdResponse
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("objective")]
    public string? Objective { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; init; }
}

public sealed class GetProjectByIdQueryHandler(IProjectRepository projectRepository)
{
    public async Task<GetProjectByIdResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanReadProjects(actor);

        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");
        }

        EnsureCompanyScope(actor, project.CompanyId);

        if (project.Status == ProjectStatus.Inhabilitado &&
            actor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.ProjectManagerRoleCode)
        {
            throw new ForbiddenAppException("Project is not visible for this role.", "PROJECT_STATUS_HIDDEN");
        }

        return new GetProjectByIdResponse
        {
            ProjectId = project.Id,
            CompanyId = project.CompanyId,
            Name = project.Name,
            Description = project.Description,
            Objective = project.Objective,
            Status = ProjectStatusTransitionPolicy.ToStatusCode(project.Status),
            IsPublic = project.IsPublic
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

    private static void EnsureCompanyScope(UserAccount actor, Guid projectCompanyId)
    {
        if (actor.Role.Code == ProjectStatusTransitionPolicy.AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != projectCompanyId)
        {
            throw new ForbiddenAppException("Actor cannot access project from another company.", "PROJECT_SCOPE_FORBIDDEN");
        }
    }
}
