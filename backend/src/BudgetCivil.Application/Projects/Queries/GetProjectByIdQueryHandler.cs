using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Queries;

public sealed class GetProjectByIdResponse
{
    public Guid ProjectId { get; init; }

    public Guid CompanyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public string? Description { get; init; }

    public string? Objective { get; init; }

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }

    public Guid BaseCurrencyId { get; init; }

    public decimal? BudgetLimit { get; init; }

    public decimal? AiuAdminPercent { get; init; }

    public decimal? AiuUnforeseenPercent { get; init; }

    public decimal? AiuProfitPercent { get; init; }

    public string SignatureMode { get; init; } = string.Empty;

    public Guid? SignerUserId { get; init; }

    public bool RequiresAuditor { get; init; }

    public string Status { get; init; } = string.Empty;

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

        var configuration = project.Configuration
            ?? throw new ConflictAppException("Project configuration was not found.", "PROJECT_CONFIGURATION_NOT_FOUND");

        EnsureCompanyScope(actor, project.CompanyId);

        if (project.Status == ProjectStatus.Inhabilitado &&
            actor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode)
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
            BaseCurrencyId = configuration.BaseCurrencyId,
            BudgetLimit = configuration.BudgetLimit,
            AiuAdminPercent = configuration.AiuAdminPercent,
            AiuUnforeseenPercent = configuration.AiuUnforeseenPercent,
            AiuProfitPercent = configuration.AiuProfitPercent,
            SignatureMode = configuration.SignatureMode,
            SignerUserId = configuration.SignerUserId,
            RequiresAuditor = configuration.RequiresAuditor,
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

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode and not "AUDITOR")
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
