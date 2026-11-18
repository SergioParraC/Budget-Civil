using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;

namespace BudgetCivil.Application.Suppliers.Services;

/// <summary>
/// Alcance del directorio de proveedores: Administrador lee cualquier compañía, Director, Presupuestador
/// y Auditor leen la propia; solo Director y Presupuestador escriben (`REQ-PROVEEDORES-07`, `D-34`,
/// `D-83`, `D-84`, matriz `1.3.2`). El permiso se exige en el endpoint.
/// </summary>
public sealed class SupplierAccessService(IActorAccessReader actorAccessReader)
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";
    private const string DirectorRoleCode = "DIRECTOR";
    private const string EstimatorRoleCode = "PRESUPUESTADOR";
    private const string AuditorRoleCode = "AUDITOR";

    /// <summary>`CompanyId` nulo significa «todas las compañías» y solo lo obtiene el Administrador.</summary>
    public async Task<SupplierScope> EnsureReadAsync(
        Guid actorUserId,
        Guid? requestedCompanyId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);

        if (actor.RoleCode == AdministratorRoleCode)
        {
            return new SupplierScope(actor, requestedCompanyId);
        }

        if (actor.RoleCode is not (DirectorRoleCode or EstimatorRoleCode or AuditorRoleCode))
        {
            throw new ForbiddenAppException("Actor cannot read suppliers.", "SUPPLIER_READ_FORBIDDEN");
        }

        var companyId = actor.CompanyId
            ?? throw new ForbiddenAppException("Actor has no company scope.", "SUPPLIER_READ_FORBIDDEN");

        if (requestedCompanyId.HasValue && requestedCompanyId.Value != companyId)
        {
            throw new ForbiddenAppException(
                "Actor cannot read suppliers from another company.",
                "SUPPLIER_READ_FORBIDDEN");
        }

        return new SupplierScope(actor, companyId);
    }

    /// <summary>Compañía del proveedor en el alta y la edición: sale del actor, nunca del cuerpo.</summary>
    public async Task<(ActorAccessSnapshot Actor, Guid CompanyId)> EnsureWriteAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorUserId, cancellationToken);

        if (actor.RoleCode == AdministratorRoleCode)
        {
            throw new ForbiddenAppException(
                "Administrator cannot write suppliers.",
                "SUPPLIER_WRITE_FORBIDDEN");
        }

        if (actor.RoleCode is not (DirectorRoleCode or EstimatorRoleCode))
        {
            throw new ForbiddenAppException("Actor cannot write suppliers.", "SUPPLIER_WRITE_FORBIDDEN");
        }

        var companyId = actor.CompanyId
            ?? throw new ForbiddenAppException("Actor has no company scope.", "SUPPLIER_WRITE_FORBIDDEN");

        return (actor, companyId);
    }

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

    /// <summary>
    /// Un proveedor de otra compañía se trata como inexistente para no revelar su existencia
    /// (`SC-PROVEEDORES-03`).
    /// </summary>
    public static void EnsureCompanyScope(Guid supplierCompanyId, Guid? scopeCompanyId)
    {
        if (!scopeCompanyId.HasValue)
        {
            return;
        }

        if (supplierCompanyId != scopeCompanyId.Value)
        {
            throw new NotFoundAppException("Supplier was not found.", "SUPPLIER_NOT_FOUND");
        }
    }
}

public sealed record SupplierScope(ActorAccessSnapshot Actor, Guid? CompanyId);
