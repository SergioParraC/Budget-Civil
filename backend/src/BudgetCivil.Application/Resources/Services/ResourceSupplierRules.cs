using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Repositories;

namespace BudgetCivil.Application.Resources.Services;

/// <summary>
/// Reglas de asociación del proveedor al insumo (`REQ-PROVEEDORES-04`, `REQ-PROVEEDORES-05`,
/// `REQ-PROVEEDORES-11`, `REQ-PROVEEDORES-13`, `D5`): debe existir, estar activo y pertenecer a la
/// compañía del proyecto.
/// </summary>
public static class ResourceSupplierRules
{
    /// <summary>
    /// `currentSupplierId` permite conservar un proveedor ya asociado aunque esté inactivo: una edición de
    /// otros campos no falla por ese motivo (`D5`).
    /// </summary>
    public static async Task EnsureAssignableAsync(
        ISupplierRepository supplierRepository,
        Guid? supplierId,
        Guid projectCompanyId,
        Guid? currentSupplierId,
        CancellationToken cancellationToken = default)
    {
        if (!supplierId.HasValue)
        {
            return;
        }

        if (currentSupplierId.HasValue && currentSupplierId.Value == supplierId.Value)
        {
            return;
        }

        var supplier = await supplierRepository.GetByIdAsync(supplierId.Value, cancellationToken)
            ?? throw new ValidationAppException("Supplier is invalid.", "SUPPLIER_NOT_FOUND");

        if (supplier.CompanyId != projectCompanyId)
        {
            throw new ValidationAppException(
                "Supplier does not belong to the project's company.",
                "SUPPLIER_COMPANY_MISMATCH");
        }

        if (!supplier.IsActive)
        {
            throw new ValidationAppException("Supplier is inactive.", "SUPPLIER_INACTIVE");
        }
    }

    /// <summary>La copia conserva el proveedor solo si pertenece a la compañía del proyecto destino (`SC-PROVEEDORES-47`, `SC-PROVEEDORES-48`).</summary>
    public static async Task EnsureCopyableAsync(
        ISupplierRepository supplierRepository,
        Guid? supplierId,
        Guid targetCompanyId,
        CancellationToken cancellationToken = default)
    {
        if (!supplierId.HasValue)
        {
            return;
        }

        if (await supplierRepository.BelongsToCompanyAsync(supplierId.Value, targetCompanyId, cancellationToken))
        {
            return;
        }

        throw new ValidationAppException(
            "Supplier does not belong to the target company.",
            "SUPPLIER_COMPANY_MISMATCH");
    }
}
