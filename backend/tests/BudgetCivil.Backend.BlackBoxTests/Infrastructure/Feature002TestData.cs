namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public static class Feature002TestData
{
    public static class Roles
    {
        public const string Administrador = "ADMINISTRADOR";
        public const string DirectorProyectos = "GERENTE_PROYECTOS";
        public const string Presupuestador = "PRESUPUESTADOR";
        public const string Auditor = "AUDITOR";
    }

    public static class PermissionKeys
    {
        public const string CatalogsRead = "CATALOGS_READ";
        public const string CompaniesCreate = "COMPANIES_CREATE";
        public const string CompaniesUpdate = "COMPANIES_UPDATE";
        public const string CompaniesDeactivate = "COMPANIES_DEACTIVATE";
        public const string CompaniesReactivate = "COMPANIES_REACTIVATE";
        public const string CompaniesRead = "COMPANIES_READ";
        public const string CompanyMembersAssign = "COMPANY_MEMBERS_ASSIGN";
        public const string CompanyMembersDeactivate = "COMPANY_MEMBERS_DEACTIVATE";
        public const string ProjectsCreate = "PROJECTS_CREATE";
        public const string ProjectsUpdate = "PROJECTS_UPDATE";
        public const string ProjectsStatusChange = "PROJECTS_STATUS_CHANGE";
        public const string ProjectsVisibilityChange = "PROJECTS_VISIBILITY_CHANGE";
        public const string ProjectsRead = "PROJECTS_READ";
        public const string ProjectMembersAssign = "PROJECT_MEMBERS_ASSIGN";
        public const string ProjectMembersDeactivate = "PROJECT_MEMBERS_DEACTIVATE";
    }

    public static string UniqueSuffix(int size = 10)
    {
        var normalizedSize = Math.Clamp(size, 4, 20);
        return Guid.NewGuid().ToString("N")[..normalizedSize].ToUpperInvariant();
    }

    public static string UniqueTaxId(string prefix = "F2")
    {
        var normalizedPrefix = string.IsNullOrWhiteSpace(prefix) ? "F2" : prefix.Trim().ToUpperInvariant();
        var candidate = $"{normalizedPrefix}{UniqueSuffix(12)}";
        return candidate[..Math.Min(candidate.Length, 20)];
    }
}
