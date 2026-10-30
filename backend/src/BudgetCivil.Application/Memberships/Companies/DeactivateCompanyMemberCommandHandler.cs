using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Companies;

public sealed class DeactivateCompanyMemberCommand
{
    public string? Reason { get; init; }
}

public sealed class DeactivateCompanyMemberResponse
{
    public Guid CompanyId { get; init; }

    public Guid UserId { get; init; }

    public bool IsActive { get; init; }

    public DateTime DeactivatedAtUtc { get; init; }
}

public sealed class DeactivateCompanyMemberCommandHandler(
    ICompanyMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<DeactivateCompanyMemberResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        Guid userId,
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

        var targetUser = await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new ConflictAppException("User was not found.", "USER_NOT_FOUND");

        if (targetUser.CompanyId != companyId)
        {
            throw new ConflictAppException("Company membership was not found.", "COMPANY_MEMBERSHIP_NOT_FOUND");
        }

        var utcNow = DateTime.UtcNow;
        var removed = await repository.TryUnassignCompanyAsync(userId, companyId, utcNow, cancellationToken);
        if (!removed)
        {
            throw new ConflictAppException("Company membership was not found.", "COMPANY_MEMBERSHIP_NOT_FOUND");
        }

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                companyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANY_MEMBERS_DEACTIVATE",
                TargetType: nameof(UserAccount),
                TargetId: userId,
                ReasonCode: "COMPANY_MEMBER_DEACTIVATED"),
            cancellationToken);

        return new DeactivateCompanyMemberResponse
        {
            CompanyId = companyId,
            UserId = userId,
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
