using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Projects;

public sealed class AssignProjectMemberCommand
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("role_code")]
    public string RoleCode { get; init; } = string.Empty;
}

public sealed class AssignProjectMemberResponse
{
    [JsonPropertyName("membership_id")]
    public Guid MembershipId { get; init; }

    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }
}

public sealed class AssignProjectMemberCommandHandler(
    IProjectMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<AssignProjectMemberResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        AssignProjectMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorIsOperational(actor);

        var project = await repository.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var targetUser = await repository.GetUserByIdAsync(command.UserId, cancellationToken)
            ?? throw new ConflictAppException("User was not found.", "USER_NOT_FOUND");

        var requestedRoleCode = command.RoleCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestedRoleCode))
        {
            throw new ValidationAppException("role_code is required.", "VALIDATION_ERROR");
        }

        if (!string.Equals(requestedRoleCode, targetUser.Role.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("role_code does not match target user role.", "VALIDATION_ERROR");
        }

        try
        {
            ProjectMembershipPolicy.EnsureRoleAllowedForProjectAssignment(requestedRoleCode);
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("role_code is invalid for project assignment.", "VALIDATION_ERROR");
        }

        try
        {
            ProjectMembershipPolicy.EnsureActorCanAssign(project.CompanyId, actor.Role.Code, actor.CompanyId, requestedRoleCode);
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenAppException("Actor cannot assign project memberships.", "PROJECT_MEMBER_ASSIGN_FORBIDDEN");
        }

        var isAffiliatedToProjectCompany = await repository.IsUserAffiliatedToCompanyAsync(targetUser.Id, project.CompanyId, cancellationToken);
        if (!isAffiliatedToProjectCompany)
        {
            throw new ConflictAppException(
                "Target user must be affiliated to project company.",
                "PROJECT_MEMBERSHIP_COMPANY_AFFILIATION_REQUIRED");
        }

        var existingMembership = await repository.GetActiveMembershipByProjectAndUserAsync(project.Id, targetUser.Id, cancellationToken);
        if (existingMembership is not null)
        {
            throw new ConflictAppException("User already has an active membership in project.", "PROJECT_MEMBER_ALREADY_ACTIVE");
        }

        var membership = new ProjectUserMembership
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = targetUser.Id,
            IsActive = true,
            AssignedByUserId = actor.Id,
            AssignedAtUtc = DateTime.UtcNow,
            EndedAtUtc = null
        };

        await repository.AddMembershipAsync(membership, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                project.CompanyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECT_MEMBERS_ASSIGN",
                TargetType: nameof(ProjectUserMembership),
                TargetId: membership.Id,
                ReasonCode: "PROJECT_MEMBER_ASSIGNED"),
            cancellationToken);

        return new AssignProjectMemberResponse
        {
            MembershipId = membership.Id,
            ProjectId = membership.ProjectId,
            UserId = membership.UserId,
            IsActive = membership.IsActive
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
