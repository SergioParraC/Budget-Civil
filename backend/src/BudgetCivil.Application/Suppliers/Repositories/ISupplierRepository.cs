using BudgetCivil.Domain.Suppliers;

namespace BudgetCivil.Application.Suppliers.Repositories;

/// <summary>Filtros del listado completo y del selector (`REQ-PROVEEDORES-08`, `REQ-PROVEEDORES-09`).</summary>
public sealed record SupplierFilter(
    Guid? CompanyId,
    string? BusinessName,
    string? ContactName,
    Guid? CountryId,
    Guid? DepartmentId,
    Guid? MunicipalityId);

/// <summary>Ubicación administrativa resuelta de un proveedor o de un proyecto (`D-96`).</summary>
public sealed record SupplierLocation(
    Guid? CountryId,
    string? CountryName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? MunicipalityId,
    string? MunicipalityName);

/// <summary>Jerarquía territorial usada para validar la coherencia de la ubicación (`REQ-PROVEEDORES-03`).</summary>
public sealed record LocationHierarchy(
    Guid? DepartmentCountryId,
    Guid? MunicipalityDepartmentId,
    bool CountryExists,
    bool DepartmentExists,
    bool MunicipalityExists);

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken = default);

    Task<bool> BelongsToCompanyAsync(
        Guid supplierId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<LocationHierarchy> GetLocationHierarchyAsync(
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        CancellationToken cancellationToken = default);

    Task<SupplierLocation?> GetProjectLocationAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> ListAsync(
        SupplierFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(SupplierFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Selector ligero: solo proveedores activos (`REQ-PROVEEDORES-09`).</summary>
    Task<IReadOnlyList<Supplier>> ListActiveOptionsAsync(
        SupplierFilter filter,
        int limit,
        CancellationToken cancellationToken = default);
}
