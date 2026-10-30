using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Commands.ChangeProjectStatus;

public sealed class ChangeProjectStatusCommand
{
    public string? TargetStatus { get; init; }

    public string? Reason { get; init; }
}

public sealed class ChangeProjectStatusResponse
{
    public Guid ProjectId { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime ChangedAtUtc { get; init; }

    public Guid ChangedByUserId { get; init; }
}

public sealed class ChangeProjectStatusCommandHandler(
    IProjectRepository projectRepository,
    IProjectTransitionRepository projectTransitionRepository,
    ISecurityAuditService securityAuditService)
{
    public Task<ChangeProjectStatusResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        ChangeProjectStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(actorUserId, projectId, command, isReactivationEndpoint: false, cancellationToken);
    }

    public Task<ChangeProjectStatusResponse> ReactivateAsync(
        Guid actorUserId,
        Guid projectId,
        ChangeProjectStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(actorUserId, projectId, command, isReactivationEndpoint: true, cancellationToken);
    }

    private async Task<ChangeProjectStatusResponse> ExecuteInternalAsync(
        Guid actorUserId,
        Guid projectId,
        ChangeProjectStatusCommand command,
        bool isReactivationEndpoint,
        CancellationToken cancellationToken)
    {
        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanChangeStatus(actor);

        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");
        }

        EnsureCompanyScope(actor, project.CompanyId);

        var previousStatus = project.Status;
        var targetStatus = ResolveTargetStatus(command.TargetStatus, isReactivationEndpoint);

        ProjectTransitionKind transitionKind;
        try
        {
            transitionKind = ProjectStatusTransitionPolicy.EnsureTransitionAllowed(previousStatus, targetStatus, isReactivationEndpoint);
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("Invalid project status transition.", "PROJECT_STATE_TRANSITION_INVALID");
        }

        if (!ProjectStatusTransitionPolicy.CanRolePerformTransition(actor.Role.Code, previousStatus, targetStatus, isReactivationEndpoint))
        {
            throw new ForbiddenAppException("Actor cannot perform this transition.", "PROJECT_STATUS_FORBIDDEN");
        }


        var now = DateTime.UtcNow;
        project.Status = targetStatus;
        project.UpdatedAtUtc = now;

        await projectTransitionRepository.AddStatusTransitionAsync(
            new ProjectStatusTransition
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                PreviousStatus = previousStatus,
                NewStatus = targetStatus,
                ChangedByUserId = actor.Id,
                ChangedAtUtc = now,
                Reason = command.Reason?.Trim(),
                TransitionKind = transitionKind
            },
            cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                project.CompanyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECTS_STATUS_CHANGE",
                TargetType: nameof(Project),
                TargetId: project.Id,
                ReasonCode: "PROJECT_STATUS_CHANGED",
                Metadata: new Dictionary<string, string>
                {
                    ["previous_status"] = ProjectStatusTransitionPolicy.ToStatusCode(previousStatus),
                    ["new_status"] = ProjectStatusTransitionPolicy.ToStatusCode(targetStatus),
                    ["transition_kind"] = transitionKind.ToString()
                }),
            cancellationToken);

        return new ChangeProjectStatusResponse
        {
            ProjectId = project.Id,
            PreviousStatus = ProjectStatusTransitionPolicy.ToStatusCode(previousStatus),
            NewStatus = ProjectStatusTransitionPolicy.ToStatusCode(targetStatus),
            ChangedAtUtc = now,
            ChangedByUserId = actor.Id
        };
    }

    private static ProjectStatus ResolveTargetStatus(string? targetStatusCode, bool isReactivationEndpoint)
    {
        if (isReactivationEndpoint)
        {
            return ProjectStatus.FaltaInformacion;
        }

        if (!ProjectStatusTransitionPolicy.TryParseStatusCode(targetStatusCode, out var targetStatus))
        {
            throw new ValidationAppException("target_status is required and invalid.", "PROJECT_STATE_TRANSITION_INVALID");
        }

        return targetStatus;
    }

    private static UserAccount EnsureActorCanChangeStatus(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot change project status.", "PROJECT_STATUS_FORBIDDEN");
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
