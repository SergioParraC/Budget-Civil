using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Domain.Suppliers;

namespace BudgetCivil.Application.Suppliers.Services;

/// <summary>
/// Coherencia y existencia de la ubicación opcional del proveedor (`REQ-PROVEEDORES-03`,
/// `SC-PROVEEDORES-10`, `SC-PROVEEDORES-12`, `D-96`).
/// </summary>
public static class SupplierLocationRules
{
    public static async Task EnsureLocationAsync(
        ISupplierRepository supplierRepository,
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        CancellationToken cancellationToken = default)
    {
        if (!countryId.HasValue && !departmentId.HasValue && !municipalityId.HasValue)
        {
            return;
        }

        var hierarchy = await supplierRepository.GetLocationHierarchyAsync(
            countryId,
            departmentId,
            municipalityId,
            cancellationToken);

        if (countryId.HasValue && !hierarchy.CountryExists)
        {
            throw new ValidationAppException("Country is invalid.", "VALIDATION_ERROR");
        }

        if (departmentId.HasValue && !hierarchy.DepartmentExists)
        {
            throw new ValidationAppException("Department is invalid.", "VALIDATION_ERROR");
        }

        if (municipalityId.HasValue && !hierarchy.MunicipalityExists)
        {
            throw new ValidationAppException("Municipality is invalid.", "VALIDATION_ERROR");
        }

        try
        {
            Supplier.ValidateLocationCoherence(
                countryId,
                departmentId,
                municipalityId,
                hierarchy.DepartmentCountryId,
                hierarchy.MunicipalityDepartmentId);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, "VALIDATION_ERROR");
        }
    }
}
