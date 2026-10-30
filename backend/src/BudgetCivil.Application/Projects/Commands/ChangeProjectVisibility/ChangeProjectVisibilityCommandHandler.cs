using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Commands.ChangeProjectVisibility;

public sealed class ChangeProjectVisibilityCommand
{
    public bool IsPublic { get; init; }

    public string? Reason { get; init; }
}

public sealed class ChangeProjectVisibilityResponse
{
    public Guid ProjectId { get; init; }

    public bool PreviousIsPublic { get; init; }

    public bool NewIsPublic { get; init; }

    public DateTime ChangedAtUtc { get; init; }
}

public sealed class ChangeProjectVisibilityCommandHandler(
    IProjectRepository projectRepository,
    IProjectTransitionRepository projectTransitionRepository,
    ISecurityAuditService securityAuditService)
{
    public async Task<ChangeProjectVisibilityResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        ChangeProjectVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanChangeVisibility(actor);

        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");
        }

        EnsureCompanyScope(actor, project.CompanyId);

        var previousIsPublic = project.IsPublic;
        var now = DateTime.UtcNow;

        project.IsPublic = command.IsPublic;
        project.UpdatedAtUtc = now;

        await projectTransitionRepository.AddVisibilityTransitionAsync(
            new ProjectVisibilityTransition
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                PreviousIsPublic = previousIsPublic,
                NewIsPublic = command.IsPublic,
                ChangedByUserId = actor.Id,
                ChangedAtUtc = now,
                Reason = command.Reason?.Trim()
            },
            cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                project.CompanyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECTS_VISIBILITY_CHANGE",
                TargetType: nameof(Project),
                TargetId: project.Id,
                ReasonCode: "PROJECT_VISIBILITY_CHANGED"),
            cancellationToken);

        return new ChangeProjectVisibilityResponse
        {
            ProjectId = project.Id,
            PreviousIsPublic = previousIsPublic,
            NewIsPublic = command.IsPublic,
            ChangedAtUtc = now
        };
    }

    private static UserAccount EnsureActorCanChangeVisibility(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot change project visibility.", "PROJECT_VISIBILITY_FORBIDDEN");
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
