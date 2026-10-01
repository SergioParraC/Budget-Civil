using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Companies;

public sealed class DeactivateCompanyMemberCommand
{
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

public sealed class DeactivateCompanyMemberResponse
{
    [JsonPropertyName("membership_id")]
    public Guid MembershipId { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("deactivated_at_utc")]
    public DateTime DeactivatedAtUtc { get; init; }
}

public sealed class DeactivateCompanyMemberCommandHandler(
    ICompanyMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<DeactivateCompanyMemberResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        Guid membershipId,
        DeactivateCompanyMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorIsOperational(actor);

        try
        {
            CompanyMembershipPolicy.EnsureActorCanDeactivate(companyId, actor.Role.Code, actor.CompanyId);
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenAppException("Actor cannot deactivate company memberships.", "COMPANY_MEMBER_DEACTIVATE_FORBIDDEN");
        }

        var membership = await repository.GetActiveMembershipByIdAsync(membershipId, cancellationToken);
        if (membership is null || membership.CompanyId != companyId)
        {
            throw new ConflictAppException("Company membership was not found.", "COMPANY_MEMBERSHIP_NOT_FOUND");
        }

        var targetUser = await repository.GetUserByIdAsync(membership.UserId, cancellationToken)
            ?? throw new ConflictAppException("Membership user was not found.", "USER_NOT_FOUND");

        var utcNow = DateTime.UtcNow;
        membership.IsActive = false;
        membership.EndedAtUtc = utcNow;

        if (targetUser.CompanyId == companyId)
        {
            targetUser.CompanyId = null;
        }

        targetUser.UpdatedAtUtc = utcNow;

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                companyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANY_MEMBERS_DEACTIVATE",
                TargetType: nameof(UserCompanyMembership),
                TargetId: membership.Id,
                ReasonCode: "COMPANY_MEMBER_DEACTIVATED"),
            cancellationToken);

        return new DeactivateCompanyMemberResponse
        {
            MembershipId = membership.Id,
            IsActive = membership.IsActive,
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
