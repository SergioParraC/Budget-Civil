namespace BudgetCivil.Domain.Projects;

public static class ProjectStatusTransitionPolicy
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string ProjectManagerRoleCode = "GERENTE_PROYECTOS";
    public const string EstimatorRoleCode = "PRESUPUESTADOR";

    public static ProjectTransitionKind EnsureTransitionAllowed(
        ProjectStatus currentStatus,
        ProjectStatus targetStatus,
        bool isReactivationEndpoint)
    {
        if (isReactivationEndpoint)
        {
            if (currentStatus != ProjectStatus.Inhabilitado || targetStatus != ProjectStatus.FaltaInformacion)
            {
                throw new InvalidOperationException("Invalid reactivation transition.");
            }

            return ProjectTransitionKind.Reactivate;
        }

        if (targetStatus == ProjectStatus.Inhabilitado)
        {
            if (currentStatus == ProjectStatus.Inhabilitado)
            {
                throw new InvalidOperationException("Project is already inhabilitado.");
            }

            return ProjectTransitionKind.Disable;
        }

        if (currentStatus == ProjectStatus.Inhabilitado)
        {
            throw new InvalidOperationException("Project cannot leave INHABILITADO without reactivation endpoint.");
        }

        if (currentStatus == ProjectStatus.PorCotizar && targetStatus == ProjectStatus.FaltaInformacion)
        {
            return ProjectTransitionKind.Ordinary;
        }

        if (currentStatus == ProjectStatus.FaltaInformacion && targetStatus == ProjectStatus.Cotizado)
        {
            return ProjectTransitionKind.Ordinary;
        }

        if (currentStatus == ProjectStatus.Cotizado && targetStatus == ProjectStatus.Finalizado)
        {
            return ProjectTransitionKind.Ordinary;
        }

        if (currentStatus == ProjectStatus.Finalizado && targetStatus == ProjectStatus.FaltaInformacion)
        {
            return ProjectTransitionKind.Reopen;
        }

        throw new InvalidOperationException("Invalid status transition.");
    }

    public static bool CanRolePerformTransition(
        string roleCode,
        ProjectStatus currentStatus,
        ProjectStatus targetStatus,
        bool isReactivationEndpoint)
    {
        if (isReactivationEndpoint)
        {
            return roleCode == ProjectManagerRoleCode || roleCode == AdministratorRoleCode;
        }

        if (targetStatus == ProjectStatus.Inhabilitado)
        {
            return roleCode == ProjectManagerRoleCode;
        }

        if (currentStatus == ProjectStatus.Finalizado && targetStatus == ProjectStatus.FaltaInformacion)
        {
            return roleCode == ProjectManagerRoleCode;
        }

        if (currentStatus == ProjectStatus.Cotizado && targetStatus == ProjectStatus.Finalizado)
        {
            return roleCode == ProjectManagerRoleCode;
        }

        if (targetStatus is ProjectStatus.FaltaInformacion or ProjectStatus.Cotizado)
        {
            return roleCode == ProjectManagerRoleCode || roleCode == EstimatorRoleCode;
        }

        return false;
    }

    public static bool TryParseStatusCode(string? statusCode, out ProjectStatus status)
    {
        status = ProjectStatus.PorCotizar;
        if (string.IsNullOrWhiteSpace(statusCode))
        {
            return false;
        }

        return statusCode.Trim().ToUpperInvariant() switch
        {
            "POR_COTIZAR" => Assign(ProjectStatus.PorCotizar, out status),
            "FALTA_INFORMACION" => Assign(ProjectStatus.FaltaInformacion, out status),
            "COTIZADO" => Assign(ProjectStatus.Cotizado, out status),
            "FINALIZADO" => Assign(ProjectStatus.Finalizado, out status),
            "INHABILITADO" => Assign(ProjectStatus.Inhabilitado, out status),
            _ => false
        };
    }

    public static string ToStatusCode(ProjectStatus status)
    {
        return status switch
        {
            ProjectStatus.PorCotizar => "POR_COTIZAR",
            ProjectStatus.FaltaInformacion => "FALTA_INFORMACION",
            ProjectStatus.Cotizado => "COTIZADO",
            ProjectStatus.Finalizado => "FINALIZADO",
            ProjectStatus.Inhabilitado => "INHABILITADO",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown project status.")
        };
    }

    private static bool Assign(ProjectStatus parsedStatus, out ProjectStatus status)
    {
        status = parsedStatus;
        return true;
    }
}
