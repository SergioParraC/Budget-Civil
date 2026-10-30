using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Administration.UseCases;

internal static class AdministrationAuthorizationRules
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string DirectorRoleCode = "DIRECTOR";

    public static void EnsureActorCanCreateUsers(UserAccount? actor)
    {
        var resolvedActor = EnsureActorIsOperational(actor);

        if (resolvedActor.Role.Code != AdministratorRoleCode)
        {
            throw new ForbiddenAppException(
                "Administrative user creation is restricted to Administrador.",
                "ADMIN_ONLY_USER_CREATION");
        }
    }

    public static void EnsureActorCanManageTarget(UserAccount? actor, UserAccount? target)
    {
        var resolvedActor = EnsureActorIsOperational(actor);
        var resolvedTarget = target ?? throw new ForbiddenAppException("Target user was not found.", "TARGET_USER_NOT_FOUND");

        if (resolvedActor.Role.Code == AdministratorRoleCode)
        {
            return;
        }

        if (resolvedActor.Role.Code == DirectorRoleCode &&
            resolvedActor.CompanyId.HasValue &&
            resolvedTarget.CompanyId.HasValue &&
            resolvedActor.CompanyId == resolvedTarget.CompanyId)
        {
            return;
        }

        throw new ForbiddenAppException(
            "Target user is outside of the actor scope.",
            "TARGET_USER_OUT_OF_SCOPE");
    }

    public static void EnsureActorCanAssignCompany(UserAccount? actor, UserAccount? target, Guid destinationCompanyId)
    {
        var resolvedActor = EnsureActorIsOperational(actor);
        var resolvedTarget = target ?? throw new ForbiddenAppException("Target user was not found.", "TARGET_USER_NOT_FOUND");

        if (resolvedActor.Role.Code == AdministratorRoleCode)
        {
            return;
        }

        if (resolvedActor.Role.Code != DirectorRoleCode)
        {
            throw new ForbiddenAppException(
                "Only Administrador and Director de proyectos can assign company.",
                "COMPANY_ASSIGNMENT_FORBIDDEN");
        }

        if (!resolvedActor.CompanyId.HasValue)
        {
            throw new ForbiddenAppException(
                "Director de proyectos requires an active company context.",
                "MANAGER_COMPANY_CONTEXT_REQUIRED");
        }

        if (destinationCompanyId != resolvedActor.CompanyId.Value)
        {
            throw new ForbiddenAppException(
                "Director de proyectos can only assign users to their own company.",
                "MANAGER_COMPANY_SCOPE_VIOLATION");
        }

        if (resolvedTarget.CompanyId.HasValue && resolvedTarget.CompanyId != resolvedActor.CompanyId)
        {
            throw new ForbiddenAppException(
                "Target user belongs to a different company scope.",
                "TARGET_USER_OUT_OF_SCOPE");
        }
    }

    public static void EnsureManagerCannotElevateToAdministrator(UserAccount actor, string requestedRoleCode)
    {
        if (actor.Role.Code == DirectorRoleCode && requestedRoleCode == AdministratorRoleCode)
        {
            throw new ForbiddenAppException(
                "Director de proyectos cannot elevate users to Administrador.",
                "MANAGER_ROLE_ELEVATION_FORBIDDEN");
        }
    }

    private static UserAccount EnsureActorIsOperational(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException(
                "Authenticated actor is not enabled for administrative operations.",
                "ACTOR_NOT_ENABLED");
        }

        return resolvedActor;
    }
}
