namespace BudgetCivil.Domain.Memberships;

public static class ProjectMembershipPolicy
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string DirectorRoleCode = "DIRECTOR";
    public const string EstimatorRoleCode = "PRESUPUESTADOR";
    public const string AuditorRoleCode = "AUDITOR";

    private static readonly HashSet<string> AllowedProjectRoles =
    [
        DirectorRoleCode,
        EstimatorRoleCode,
        AuditorRoleCode
    ];

    public static void EnsureRoleAllowedForProjectAssignment(string roleCode)
    {
        if (!AllowedProjectRoles.Contains(NormalizeRoleCode(roleCode)))
        {
            throw new InvalidOperationException("Role is not assignable to project membership.");
        }
    }

    public static void EnsureActorCanAssign(
        Guid projectCompanyId,
        string actorRoleCode,
        Guid? actorCompanyId,
        string targetRoleCode)
    {
        var normalizedActorRoleCode = NormalizeRoleCode(actorRoleCode);
        var normalizedTargetRoleCode = NormalizeRoleCode(targetRoleCode);

        if (normalizedActorRoleCode == AdministratorRoleCode)
        {
            if (normalizedTargetRoleCode != DirectorRoleCode)
            {
                throw new InvalidOperationException("Administrador can only assign project managers to projects.");
            }

            return;
        }

        if (normalizedActorRoleCode == DirectorRoleCode)
        {
            EnsureActorInCompany(projectCompanyId, actorCompanyId);
            EnsureRoleAllowedForProjectAssignment(normalizedTargetRoleCode);
            return;
        }

        if (normalizedActorRoleCode == EstimatorRoleCode)
        {
            EnsureActorInCompany(projectCompanyId, actorCompanyId);

            if (normalizedTargetRoleCode is not EstimatorRoleCode and not AuditorRoleCode)
            {
                throw new InvalidOperationException("Presupuestador can only assign Presupuestador or Auditor in project.");
            }

            return;
        }

        throw new InvalidOperationException("Actor cannot assign project memberships.");
    }

    public static void EnsureActorCanDeactivate(
        Guid projectCompanyId,
        string actorRoleCode,
        Guid? actorCompanyId,
        string targetRoleCode)
    {
        var normalizedActorRoleCode = NormalizeRoleCode(actorRoleCode);
        var normalizedTargetRoleCode = NormalizeRoleCode(targetRoleCode);

        if (normalizedActorRoleCode == DirectorRoleCode)
        {
            EnsureActorInCompany(projectCompanyId, actorCompanyId);
            EnsureRoleAllowedForProjectAssignment(normalizedTargetRoleCode);
            return;
        }

        if (normalizedActorRoleCode == EstimatorRoleCode)
        {
            EnsureActorInCompany(projectCompanyId, actorCompanyId);

            if (normalizedTargetRoleCode is not EstimatorRoleCode and not AuditorRoleCode)
            {
                throw new InvalidOperationException("Presupuestador can only deactivate Presupuestador or Auditor in project.");
            }

            return;
        }

        throw new InvalidOperationException("Actor cannot deactivate project memberships.");
    }

    private static void EnsureActorInCompany(Guid companyId, Guid? actorCompanyId)
    {
        if (!actorCompanyId.HasValue || actorCompanyId.Value != companyId)
        {
            throw new InvalidOperationException("Actor cannot manage memberships for projects outside actor company.");
        }
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        return roleCode.Trim().ToUpperInvariant();
    }
}
