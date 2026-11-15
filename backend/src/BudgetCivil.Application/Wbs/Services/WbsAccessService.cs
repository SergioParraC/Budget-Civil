using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Wbs.Repositories;

namespace BudgetCivil.Application.Wbs.Services;

/// <summary>
/// Reglas de alcance y autorización del módulo `M-03` (`REQ-EDT-13`, `REQ-EDT-14`, matriz `1.3.2`). El
/// permiso se exige en el endpoint; aquí se resuelve la compañía y la pertenencia al proyecto.
/// </summary>
public sealed class WbsAccessService(
    IActorAccessReader actorAccessReader,
    IWbsProjectAccessReader projectAccessReader)
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

    public async Task<(ActorAccessSnapshot Actor, WbsProjectAccess Access)> EnsureReadAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanRead(actor, access);
        return (actor, access);
    }

    public async Task<(ActorAccessSnapshot Actor, WbsProjectAccess Access)> EnsureWriteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanWrite(actor, access);
        return (actor, access);
    }

    /// <summary>
    /// La actividad exige el alcance de lectura, pero el Auditor no accede (`SC-EDT-76`, `D-120`).
    /// </summary>
    public async Task<(ActorAccessSnapshot Actor, WbsProjectAccess Access)> EnsureActivityReadAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var (actor, access) = await EnsureReadAsync(actorUserId, projectId, cancellationToken);

        if (actor.RoleCode == AuditorRoleCode)
        {
            throw new ForbiddenAppException(
                "El Auditor no consulta la actividad del EDT.",
                "WBS_READ_FORBIDDEN");
        }

        return (actor, access);
    }

    public async Task<(ActorAccessSnapshot Actor, WbsProjectAccess Access)> EnsureCodeValidationAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await EnsureWriteAsync(actorUserId, projectId, cancellationToken);
    }

    public Task<WbsProjectAccess?> GetProjectAccessForActorAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken);
    }

    public static void EnsureCanRead(ActorAccessSnapshot actor, WbsProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "El actor no puede acceder a la estructura de otra compañía.",
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
            "El actor no puede leer la estructura del proyecto.",
            "WBS_READ_FORBIDDEN");
    }

    public static void EnsureCanWrite(ActorAccessSnapshot actor, WbsProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            throw new ForbiddenAppException(
                "El Administrador solo lee la estructura del proyecto.",
                "WBS_WRITE_FORBIDDEN");
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "El actor no puede modificar la estructura de otra compañía.",
                "WBS_WRITE_FORBIDDEN");
        }

        if (actor.RoleCode is DirectorRoleCode or EstimatorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException(
            "El actor no puede escribir la estructura del proyecto.",
            "WBS_WRITE_FORBIDDEN");
    }

    private async Task<WbsProjectAccess> GetProjectAccessAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");
    }
}
