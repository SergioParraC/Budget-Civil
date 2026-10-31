namespace BudgetCivil.Domain.Memberships;

public static class CompanyMembershipPolicy
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string DirectorRoleCode = "DIRECTOR";
    public const string EstimatorRoleCode = "PRESUPUESTADOR";
    public const string AuditorRoleCode = "AUDITOR";

    private static readonly HashSet<string> AllowedCompanyRoles =
    [
        DirectorRoleCode,
        EstimatorRoleCode,
        AuditorRoleCode
    ];

    public static void EnsureRoleAllowedForCompanyAssignment(string roleCode)
    {
        if (!AllowedCompanyRoles.Contains(NormalizeRoleCode(roleCode)))
        {
            throw new InvalidOperationException("Role is not assignable to company membership.");
        }
    }

    public static void EnsureTargetCanBelongToCompany(string roleCode)
    {
        var normalizedRoleCode = NormalizeRoleCode(roleCode);

        if (normalizedRoleCode == AdministratorRoleCode)
        {
            throw new InvalidOperationException("Administradores cannot belong to companies.");
        }

        EnsureRoleAllowedForCompanyAssignment(normalizedRoleCode);
    }

    public static void EnsureActorCanAssign(Guid companyId, string actorRoleCode, Guid? actorCompanyId)
    {
        var normalizedActorRoleCode = NormalizeRoleCode(actorRoleCode);

        if (normalizedActorRoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (normalizedActorRoleCode == DirectorRoleCode && actorCompanyId == companyId)
        {
            return;
        }

        throw new InvalidOperationException("Actor cannot assign company memberships.");
    }

    public static void EnsureActorCanDeactivate(Guid companyId, string actorRoleCode, Guid? actorCompanyId)
    {
        EnsureActorCanAssign(companyId, actorRoleCode, actorCompanyId);
    }

    /// <summary>
    /// Reglas de qué roles puede gestionar cada actor en la compañía: el Administrador solo directores
    /// de proyectos; el Director de proyectos solo presupuestadores y auditores de su compañía.
    /// </summary>
    public static void EnsureActorCanManageTargetRole(string actorRoleCode, string targetRoleCode)
    {
        var normalizedActorRoleCode = NormalizeRoleCode(actorRoleCode);
        var normalizedTargetRoleCode = NormalizeRoleCode(targetRoleCode);

        if (normalizedActorRoleCode == AdministratorRoleCode)
        {
            if (normalizedTargetRoleCode != DirectorRoleCode)
            {
                throw new InvalidOperationException("Administrador can only manage Director de proyectos memberships.");
            }

            return;
        }

        if (normalizedActorRoleCode == DirectorRoleCode)
        {
            if (normalizedTargetRoleCode is not EstimatorRoleCode and not AuditorRoleCode)
            {
                throw new InvalidOperationException("Director de proyectos can only manage Presupuestador and Auditor memberships.");
            }

            return;
        }

        throw new InvalidOperationException("Actor cannot manage company membership roles.");
    }

    public static bool RequiresSingleActiveCompanyConstraint(string roleCode)
    {
        var normalizedRoleCode = NormalizeRoleCode(roleCode);
        return normalizedRoleCode is DirectorRoleCode or EstimatorRoleCode or AuditorRoleCode;
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        return roleCode.Trim().ToUpperInvariant();
    }
}
