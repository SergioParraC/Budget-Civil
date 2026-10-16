namespace BudgetCivil.Domain.Memberships;

public static class CompanyMembershipPolicy
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string ProjectManagerRoleCode = "GERENTE_PROYECTOS";
    public const string EstimatorRoleCode = "PRESUPUESTADOR";
    public const string AuditorRoleCode = "AUDITOR";

    private static readonly HashSet<string> AllowedCompanyRoles =
    [
        ProjectManagerRoleCode,
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

        if (normalizedActorRoleCode == ProjectManagerRoleCode && actorCompanyId == companyId)
        {
            return;
        }

        throw new InvalidOperationException("Actor cannot assign company memberships.");
    }

    public static void EnsureActorCanDeactivate(Guid companyId, string actorRoleCode, Guid? actorCompanyId)
    {
        EnsureActorCanAssign(companyId, actorRoleCode, actorCompanyId);
    }

    public static bool RequiresSingleActiveCompanyConstraint(string roleCode)
    {
        var normalizedRoleCode = NormalizeRoleCode(roleCode);
        return normalizedRoleCode is ProjectManagerRoleCode or EstimatorRoleCode or AuditorRoleCode;
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        return roleCode.Trim().ToUpperInvariant();
    }
}
