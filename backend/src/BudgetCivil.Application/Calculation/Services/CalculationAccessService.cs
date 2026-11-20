using BudgetCivil.Application.Calculation.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;

namespace BudgetCivil.Application.Calculation.Services;

/// <summary>
/// Alcance de lectura del presupuesto con las mismas reglas del árbol (`REQ-CALCULO-04`, matriz `1.3.2`):
/// Administrador cualquiera, Director su compañía, Presupuestador y Auditor con membresía activa.
/// </summary>
public sealed class CalculationAccessService(
    IActorAccessReader actorAccessReader,
    ICalculationProjectAccessReader projectAccessReader)
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";
    private const string DirectorRoleCode = "DIRECTOR";
    private const string EstimatorRoleCode = "PRESUPUESTADOR";
    private const string AuditorRoleCode = "AUDITOR";

    public async Task<ActorAccessSnapshot> GetActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await actorAccessReader.GetSnapshotAsync(actorUserId, cancellationToken)
            ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!actor.AccountEnabled || actor.AccountLocked || !actor.RoleIsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        return actor;
    }

    public async Task<(ActorAccessSnapshot Actor, CalculationProjectAccess Access)> EnsureReadAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        EnsureCanRead(actor, access);
        return (actor, access);
    }

    public static void EnsureCanRead(ActorAccessSnapshot actor, CalculationProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "El actor no puede acceder al presupuesto de otra compañía.",
                "WBS_READ_FORBIDDEN");
        }

        if (actor.RoleCode == DirectorRoleCode)
        {
            return;
        }

        if (actor.RoleCode is EstimatorRoleCode or AuditorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException(
            "El actor no puede leer el presupuesto del proyecto.",
            "WBS_READ_FORBIDDEN");
    }
}
