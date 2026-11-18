using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;

namespace BudgetCivil.Application.Apus.Services;

/// <summary>
/// Reglas de alcance y autorización de `M-05` (`REQ-APUS-16`, `REQ-APUS-17`, `REQ-APUS-18`, `D6`). El
/// permiso se exige en el endpoint; aquí se resuelve la compañía y la pertenencia al proyecto.
/// </summary>
public sealed class ApuAccessService(
    IActorAccessReader actorAccessReader,
    IApuProjectAccessReader projectAccessReader)
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

    public async Task<(ActorAccessSnapshot Actor, ApuProjectAccess Access)> EnsureReadAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanRead(actor, access);
        return (actor, access);
    }

    public async Task<(ActorAccessSnapshot Actor, ApuProjectAccess Access)> EnsureWriteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanWrite(actor, access);
        return (actor, access);
    }

    public Task<ApuProjectAccess?> GetProjectAccessForActorAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken);
    }

    /// <summary>
    /// Lectura: Administrador cualquier proyecto; Director y Presupuestador cualquier proyecto de su
    /// compañía; Auditor solo sus proyectos asignados (`SC-APUS-76`…`80`).
    /// </summary>
    public static void EnsureCanRead(ActorAccessSnapshot actor, ApuProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "El actor no puede acceder a APUs de otra compañía.",
                ApuRules.ApuReadForbidden);
        }

        if (actor.RoleCode is DirectorRoleCode or EstimatorRoleCode)
        {
            return;
        }

        if (actor.RoleCode == AuditorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException(
            "El actor no puede leer los APUs del proyecto.",
            ApuRules.ApuReadForbidden);
    }

    /// <summary>
    /// Escritura: solo Director y Presupuestador con `ProjectMember` activo; Administrador y Auditor no
    /// escriben, y el dueño del proyecto sin fila en `project_members` tampoco (`SC-APUS-71`…`75`, `91`).
    /// </summary>
    public static void EnsureCanWrite(ActorAccessSnapshot actor, ApuProjectAccess access)
    {
        if (actor.RoleCode is AdministratorRoleCode or AuditorRoleCode)
        {
            throw new ForbiddenAppException(
                "El actor solo puede leer APUs y Matrices.",
                ApuRules.ApuWriteForbidden);
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "El actor no puede modificar APUs de otra compañía.",
                ApuRules.ApuWriteForbidden);
        }

        if (actor.RoleCode is DirectorRoleCode or EstimatorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException(
            "El actor no tiene pertenencia al proyecto para escribir APUs.",
            ApuRules.ApuWriteForbidden);
    }

    private async Task<ApuProjectAccess> GetProjectAccessAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");
    }
}
