using BudgetCivil.Domain.Suppliers;

namespace BudgetCivil.Application.Suppliers.Dtos;

/// <summary>Detalle completo del proveedor (`REQ-PROVEEDORES-02`, `REQ-PROVEEDORES-03`, `D-41`).</summary>
public sealed class SupplierDto
{
    public Guid SupplierId { get; init; }

    public Guid CompanyId { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string? ContactName { get; init; }

    public string? ContactPhone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public string? Observations { get; init; }

    public Guid? CountryId { get; init; }

    public string? CountryName { get; init; }

    public Guid? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string? MunicipalityName { get; init; }

    public bool IsActive { get; init; }

    public string RowVersion { get; init; } = string.Empty;

    public Guid CreatedBy { get; init; }

    public DateTime CreatedAt { get; init; }

    public Guid LastModifiedBy { get; init; }

    public DateTime LastModifiedAt { get; init; }
}

/// <summary>
/// Selector ligero: solo `supplierId` y `businessName` (`REQ-PROVEEDORES-09`, `D-101`). No expone
/// contacto ni ubicación.
/// </summary>
public sealed class SupplierOptionDto
{
    public Guid SupplierId { get; init; }

    public string BusinessName { get; init; } = string.Empty;
}

/// <summary>
/// Resumen del proveedor dentro de `ResourceDto` (`SC-PROVEEDORES-49`): razón social, contacto y
/// ubicación; las observaciones quedan en el detalle completo.
/// </summary>
public sealed class SupplierSummary
{
    public Guid SupplierId { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string? ContactName { get; init; }

    public string? ContactPhone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public Guid? CountryId { get; init; }

    public string? CountryName { get; init; }

    public Guid? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string? MunicipalityName { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>Ubicación administrativa comparada por la alerta (`D-96`).</summary>
public sealed class LocationDto
{
    public Guid? CountryId { get; init; }

    public string? CountryName { get; init; }

    public Guid? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string? MunicipalityName { get; init; }
}

/// <summary>Alerta de ubicación no bloqueante (`REQ-PROVEEDORES-10`).</summary>
public sealed class LocationAlertDto
{
    public Guid SupplierId { get; init; }

    public Guid ProjectId { get; init; }

    public bool CanCompare { get; init; }

    public bool IsSameLocation { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public LocationDto SupplierLocation { get; init; } = new();

    public LocationDto ProjectLocation { get; init; } = new();
}

public static class LocationAlertCodes
{
    public const string Match = "SUPPLIER_LOCATION_MATCH";

    public const string Mismatch = "SUPPLIER_LOCATION_MISMATCH";

    public const string Incomplete = "LOCATION_INCOMPLETE";
}

public static class SupplierMapper
{
    public static SupplierDto ToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            SupplierId = supplier.Id,
            CompanyId = supplier.CompanyId,
            BusinessName = supplier.BusinessName,
            ContactName = supplier.ContactName,
            ContactPhone = supplier.ContactPhone,
            Email = supplier.Email,
            Address = supplier.Address,
            Observations = supplier.Observations,
            CountryId = supplier.CountryId,
            CountryName = supplier.Country?.Name,
            DepartmentId = supplier.DepartmentId,
            DepartmentName = supplier.Department?.Name,
            MunicipalityId = supplier.MunicipalityId,
            MunicipalityName = supplier.Municipality?.Name,
            IsActive = supplier.IsActive,
            RowVersion = Convert.ToBase64String(supplier.RowVersion),
            CreatedBy = supplier.CreatedByUserId,
            CreatedAt = supplier.CreatedAtUtc,
            LastModifiedBy = supplier.LastModifiedByUserId,
            LastModifiedAt = supplier.LastModifiedAtUtc
        };
    }

    public static SupplierOptionDto ToOptionDto(Supplier supplier)
    {
        return new SupplierOptionDto
        {
            SupplierId = supplier.Id,
            BusinessName = supplier.BusinessName
        };
    }

    public static SupplierSummary ToSummary(Supplier supplier)
    {
        return new SupplierSummary
        {
            SupplierId = supplier.Id,
            BusinessName = supplier.BusinessName,
            ContactName = supplier.ContactName,
            ContactPhone = supplier.ContactPhone,
            Email = supplier.Email,
            Address = supplier.Address,
            CountryId = supplier.CountryId,
            CountryName = supplier.Country?.Name,
            DepartmentId = supplier.DepartmentId,
            DepartmentName = supplier.Department?.Name,
            MunicipalityId = supplier.MunicipalityId,
            MunicipalityName = supplier.Municipality?.Name,
            IsActive = supplier.IsActive
        };
    }

    public static LocationDto ToLocation(Supplier supplier)
    {
        return new LocationDto
        {
            CountryId = supplier.CountryId,
            CountryName = supplier.Country?.Name,
            DepartmentId = supplier.DepartmentId,
            DepartmentName = supplier.Department?.Name,
            MunicipalityId = supplier.MunicipalityId,
            MunicipalityName = supplier.Municipality?.Name
        };
    }
}
