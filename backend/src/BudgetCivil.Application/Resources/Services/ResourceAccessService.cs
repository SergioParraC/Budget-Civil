using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Resources.Repositories;

namespace BudgetCivil.Application.Resources.Services;

/// <summary>
/// Reglas de alcance y autorización de `M-04` (`REQ-INSUMOS-13`, `REQ-INSUMOS-14`, matriz `1.3.2`).
/// El permiso se exige en el endpoint; aquí se resuelve la compañía y la pertenencia al proyecto.
/// </summary>
public sealed class ResourceAccessService(
    IActorAccessReader actorAccessReader,
    IResourceProjectAccessReader projectAccessReader)
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

    public async Task<(ActorAccessSnapshot Actor, ResourceProjectAccess Access)> EnsureReadAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanRead(actor, access);
        return (actor, access);
    }

    public async Task<(ActorAccessSnapshot Actor, ResourceProjectAccess Access)> EnsureWriteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);
        var access = await GetProjectAccessAsync(actorUserId, projectId, cancellationToken);
        EnsureCanWrite(actor, access);
        return (actor, access);
    }

    public async Task<ResourceProjectAccess> GetProjectAccessAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");
    }

    public Task<ResourceProjectAccess?> GetProjectAccessForActorAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return projectAccessReader.GetAccessAsync(projectId, actorUserId, cancellationToken);
    }

    public static void EnsureCanRead(ActorAccessSnapshot actor, ResourceProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "Actor cannot access resources from another company.",
                "RESOURCE_READ_FORBIDDEN");
        }

        if (actor.RoleCode == DirectorRoleCode)
        {
            return;
        }

        if (actor.RoleCode is EstimatorRoleCode or AuditorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException("Actor cannot read resources.", "RESOURCE_READ_FORBIDDEN");
    }

    public static void EnsureCanWrite(ActorAccessSnapshot actor, ResourceProjectAccess access)
    {
        if (actor.RoleCode == AdministratorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot write resources.", "RESOURCE_WRITE_FORBIDDEN");
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != access.CompanyId)
        {
            throw new ForbiddenAppException(
                "Actor cannot access resources from another company.",
                "RESOURCE_WRITE_FORBIDDEN");
        }

        if (actor.RoleCode == DirectorRoleCode)
        {
            return;
        }

        if (actor.RoleCode == EstimatorRoleCode && access.IsMember)
        {
            return;
        }

        throw new ForbiddenAppException("Actor cannot write resources.", "RESOURCE_WRITE_FORBIDDEN");
    }
}
