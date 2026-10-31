using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Companies;

public sealed class AssignCompanyMemberCommand
{
    public Guid UserId { get; init; }

    public string RoleCode { get; init; } = string.Empty;
}

public sealed class AssignCompanyMemberResponse
{
    public Guid CompanyId { get; init; }

    public Guid UserId { get; init; }

    public bool IsActive { get; init; }
}

public sealed class AssignCompanyMemberCommandHandler(
    ICompanyMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<AssignCompanyMemberResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        AssignCompanyMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorIsOperational(actor);

        var targetUser = await repository.GetUserByIdAsync(command.UserId, cancellationToken)
            ?? throw new ConflictAppException("User was not found.", "USER_NOT_FOUND");

        var company = await repository.GetCompanyByIdAsync(companyId, cancellationToken)
            ?? throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");

        if (!company.IsActive)
        {
            throw new ConflictAppException("Company is inactive.", "COMPANY_INACTIVE");
        }

        var requestedRoleCode = command.RoleCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestedRoleCode))
        {
            throw new ValidationAppException("role_code is required.", "VALIDATION_ERROR");
        }

        if (string.Equals(targetUser.Role.Code, CompanyMembershipPolicy.AdministratorRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictAppException("Administrador cannot be assigned to company membership.", "ADMIN_COMPANY_MEMBERSHIP_FORBIDDEN");
        }

        if (!string.Equals(requestedRoleCode, targetUser.Role.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("role_code does not match target user role.", "VALIDATION_ERROR");
        }

        try
        {
            CompanyMembershipPolicy.EnsureRoleAllowedForCompanyAssignment(requestedRoleCode);
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("role_code is invalid for company assignment.", "VALIDATION_ERROR");
        }

        try
        {
            CompanyMembershipPolicy.EnsureActorCanAssign(companyId, actor.Role.Code, actor.CompanyId);
            CompanyMembershipPolicy.EnsureActorCanManageTargetRole(actor.Role.Code, requestedRoleCode);
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenAppException("Actor cannot assign company memberships.", "COMPANY_MEMBER_ASSIGN_FORBIDDEN");
        }

        if (targetUser.CompanyId.HasValue)
        {
            if (targetUser.CompanyId.Value == companyId)
            {
                throw new ConflictAppException("User is already active in this company.", "COMPANY_MEMBER_ALREADY_ACTIVE");
            }

            throw new ConflictAppException(
                "User already has an active company membership.",
                "COMPANY_MEMBERSHIP_LIMIT_REACHED");
        }

        var utcNow = DateTime.UtcNow;
        var assigned = await repository.AssignAsync(targetUser.Id, companyId, utcNow, cancellationToken);
        if (!assigned)
        {
            // Otra solicitud asignó una compañía entre la lectura y la escritura (SC-IDENTIDAD-31).
            throw new ConflictAppException(
                "User already has an active company membership.",
                "COMPANY_MEMBERSHIP_LIMIT_REACHED");
        }

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                companyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANY_MEMBERS_ASSIGN",
                TargetType: nameof(UserAccount),
                TargetId: targetUser.Id,
                ReasonCode: "COMPANY_MEMBER_ASSIGNED"),
            cancellationToken);

        return new AssignCompanyMemberResponse
        {
            CompanyId = companyId,
            UserId = targetUser.Id,
            IsActive = true
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
