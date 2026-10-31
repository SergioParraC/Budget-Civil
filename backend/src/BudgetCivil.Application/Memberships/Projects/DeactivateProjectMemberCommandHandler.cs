using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Projects;

public sealed class DeactivateProjectMemberCommand
{
    public string? Reason { get; init; }
}

public sealed class DeactivateProjectMemberResponse
{
    public Guid MembershipId { get; init; }

    public bool IsActive { get; init; }

    public DateTime DeactivatedAtUtc { get; init; }
}

public sealed class DeactivateProjectMemberCommandHandler(
    IProjectMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<DeactivateProjectMemberResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid membershipId,
        DeactivateProjectMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorIsOperational(actor);

        var project = await repository.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var membership = await repository.GetMembershipByIdAsync(membershipId, cancellationToken);
        if (membership is null || membership.ProjectId != projectId)
        {
            throw new ConflictAppException("Project membership was not found.", "PROJECT_MEMBERSHIP_NOT_FOUND");
        }

        var targetUser = await repository.GetUserByIdAsync(membership.UserId, cancellationToken)
            ?? throw new ConflictAppException("Membership user was not found.", "USER_NOT_FOUND");

        try
        {
            ProjectMembershipPolicy.EnsureActorCanDeactivate(project.CompanyId, actor.Role.Code, actor.CompanyId, targetUser.Role.Code);
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenAppException("Actor cannot deactivate project memberships.", "PROJECT_MEMBER_DEACTIVATE_FORBIDDEN");
        }

        // D-98: la pertenencia no guarda historial; retirarla libera el par (project_id, user_id).
        await repository.RemoveMembershipAsync(membership, cancellationToken);

        var utcNow = DateTime.UtcNow;

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                project.CompanyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECT_MEMBERS_DEACTIVATE",
                TargetType: nameof(ProjectMember),
                TargetId: membership.Id,
                ReasonCode: "PROJECT_MEMBER_DEACTIVATED"),
            cancellationToken);

        return new DeactivateProjectMemberResponse
        {
            MembershipId = membership.Id,
            IsActive = false,
            DeactivatedAtUtc = utcNow
        };
    }

    private static UserAccount EnsureActorIsOperational(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        return resolvedActor;
    }
}
