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

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("objective")]
    public string? Objective { get; init; }

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }

    [JsonPropertyName("base_currency_id")]
    public Guid BaseCurrencyId { get; init; }

    [JsonPropertyName("aiu_admin_percent")]
    public decimal? AiuAdminPercent { get; init; }

    [JsonPropertyName("aiu_unforeseen_percent")]
    public decimal? AiuUnforeseenPercent { get; init; }

    [JsonPropertyName("aiu_profit_percent")]
    public decimal? AiuProfitPercent { get; init; }

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
            Code = project.Code,
            Description = project.Description,
            Objective = project.Objective,
            CountryId = project.CountryId,
            DepartmentId = project.DepartmentId,
            MunicipalityId = project.MunicipalityId,
            BaseCurrencyId = project.BaseCurrencyId,
            AiuAdminPercent = project.AiuAdminPercent,
            AiuUnforeseenPercent = project.AiuUnforeseenPercent,
            AiuProfitPercent = project.AiuProfitPercent,
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
