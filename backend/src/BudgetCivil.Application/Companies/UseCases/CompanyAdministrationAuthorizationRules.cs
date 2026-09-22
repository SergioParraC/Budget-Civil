using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Companies.UseCases;

internal static class CompanyAdministrationAuthorizationRules
{
    public const string AdministratorRoleCode = "ADMINISTRADOR";
    public const string ProjectManagerRoleCode = "GERENTE_PROYECTOS";

    public static UserAccount EnsureActorCanCreateCompany(UserAccount? actor)
    {
        var resolvedActor = EnsureActorIsOperational(actor);

        if (resolvedActor.Role.Code == AdministratorRoleCode || resolvedActor.Role.Code == ProjectManagerRoleCode)
        {
            return resolvedActor;
        }

        throw new ForbiddenAppException("Actor is not allowed to create companies.", "COMPANY_CREATE_FORBIDDEN");
    }

    public static UserAccount EnsureActorCanUpdateCompany(UserAccount? actor, Guid companyId)
    {
        var resolvedActor = EnsureActorIsOperational(actor);

        if (resolvedActor.Role.Code == AdministratorRoleCode)
        {
            return resolvedActor;
        }

        if (resolvedActor.Role.Code == ProjectManagerRoleCode && resolvedActor.CompanyId == companyId)
        {
            return resolvedActor;
        }

        throw new ForbiddenAppException("Actor cannot update this company.", "COMPANY_UPDATE_FORBIDDEN");
    }

    public static UserAccount EnsureActorCanDeactivateCompany(UserAccount? actor)
    {
        var resolvedActor = EnsureActorIsOperational(actor);

        if (resolvedActor.Role.Code != AdministratorRoleCode)
        {
            throw new ForbiddenAppException("Only Administrador can deactivate companies.", "COMPANY_DEACTIVATE_FORBIDDEN");
        }

        return resolvedActor;
    }

    private static UserAccount EnsureActorIsOperational(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException(
                "Authenticated actor is not enabled for company administration.",
                "ACTOR_NOT_ENABLED");
        }

        return resolvedActor;
    }
}
