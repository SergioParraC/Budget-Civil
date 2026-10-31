using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.LeaveCompany;

public sealed class LeaveCompanyResponse
{
    public Guid CompanyId { get; init; }

    public Guid UserId { get; init; }

    public DateTime LeftAtUtc { get; init; }

    /// <summary>Pertenecen a proyectos de la compañía que se retiraron al salir.</summary>
    public int ProjectsDetached { get; init; }

    /// <summary>
    /// El token vigente conserva el claim <c>company_id</c>; el cliente debe volver a iniciar sesión.
    /// </summary>
    public bool RequiresRelogin { get; init; } = true;
}

/// <summary>
/// Salida voluntaria de la compañía (solo Presupuestador y Auditor). Cierra el vínculo en
/// <c>users_company</c> conservándolo en el historial y retira las pertenencias a proyectos de esa
/// compañía; la cuenta sigue habilitada y queda disponible para otra compañía.
/// </summary>
public sealed class LeaveCompanyCommandHandler(
    ICompanyMembershipRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<LeaveCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken)
            ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!actor.AccountEnabled || actor.AccountLocked || !actor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (actor.Role.Code is not CompanyMembershipPolicy.EstimatorRoleCode and not CompanyMembershipPolicy.AuditorRoleCode)
        {
            throw new ForbiddenAppException(
                "Only Presupuestador and Auditor can leave their company.",
                "COMPANY_LEAVE_FORBIDDEN");
        }

        var membership = actor.ActiveMembership
            ?? throw new ConflictAppException("Company membership was not found.", "COMPANY_MEMBERSHIP_NOT_FOUND");

        var companyId = membership.CompanyId;
        var utcNow = DateTime.UtcNow;

        var closed = await repository.DeactivateAsync(actor.Id, companyId, utcNow, cancellationToken);
        if (!closed)
        {
            throw new ConflictAppException("Company membership was not found.", "COMPANY_MEMBERSHIP_NOT_FOUND");
        }

        var projectsDetached = await repository.RemoveProjectMembershipsAsync(actor.Id, companyId, cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                companyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANY_LEAVE_SELF",
                TargetType: nameof(UserAccount),
                TargetId: actor.Id,
                ReasonCode: "COMPANY_MEMBER_LEFT_SELF",
                Metadata: new Dictionary<string, string>
                {
                    ["company_id"] = companyId.ToString(),
                    ["projects_detached"] = projectsDetached.ToString()
                }),
            cancellationToken);

        return new LeaveCompanyResponse
        {
            CompanyId = companyId,
            UserId = actor.Id,
            LeftAtUtc = utcNow,
            ProjectsDetached = projectsDetached,
            RequiresRelogin = true
        };
    }
}
