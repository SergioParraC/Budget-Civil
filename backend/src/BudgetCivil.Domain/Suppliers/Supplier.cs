using System.Text.RegularExpressions;
using BudgetCivil.Domain.Catalogs;

namespace BudgetCivil.Domain.Suppliers;

/// <summary>
/// Proveedor propio de una compañía y reutilizable en los insumos de todos sus proyectos
/// (`REQ-PROVEEDORES-01`, `D-27`, `D-36`). Solo la razón social es obligatoria.
/// </summary>
public sealed partial class Supplier
{
    public const int BusinessNameMaxLength = 200;
    public const int ContactNameMaxLength = 150;
    public const int ContactPhoneMaxLength = 30;
    public const int EmailMaxLength = 254;
    public const int AddressMaxLength = 300;
    public const int ObservationsMaxLength = 1000;

    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Observations { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? MunicipalityId { get; set; }

    /// <summary>Baja lógica (`REQ-PROVEEDORES-11`, `D-49`); no existe borrado físico.</summary>
    public bool IsActive { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid LastModifiedByUserId { get; set; }

    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public Country? Country { get; set; }

    public Department? Department { get; set; }

    public Municipality? Municipality { get; set; }

    public static Supplier Create(
        Guid companyId,
        string businessName,
        string? contactName,
        string? contactPhone,
        string? email,
        string? address,
        string? observations,
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (companyId == Guid.Empty)
        {
            throw new InvalidOperationException("Company is required.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor is required.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BusinessName = NormalizeBusinessName(businessName),
            ContactName = NormalizeOptional(contactName, ContactNameMaxLength, "contact_name"),
            ContactPhone = NormalizeOptional(contactPhone, ContactPhoneMaxLength, "contact_phone"),
            Email = NormalizeEmail(email),
            Address = NormalizeOptional(address, AddressMaxLength, "address"),
            Observations = NormalizeOptional(observations, ObservationsMaxLength, "observations"),
            CountryId = NormalizeReference(countryId),
            DepartmentId = NormalizeReference(departmentId),
            MunicipalityId = NormalizeReference(municipalityId),
            IsActive = true,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = utcNow,
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow
        };

        return supplier;
    }

    public void Update(
        string businessName,
        string? contactName,
        string? contactPhone,
        string? email,
        string? address,
        string? observations,
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor is required.");
        }

        BusinessName = NormalizeBusinessName(businessName);
        ContactName = NormalizeOptional(contactName, ContactNameMaxLength, "contact_name");
        ContactPhone = NormalizeOptional(contactPhone, ContactPhoneMaxLength, "contact_phone");
        Email = NormalizeEmail(email);
        Address = NormalizeOptional(address, AddressMaxLength, "address");
        Observations = NormalizeOptional(observations, ObservationsMaxLength, "observations");
        CountryId = NormalizeReference(countryId);
        DepartmentId = NormalizeReference(departmentId);
        MunicipalityId = NormalizeReference(municipalityId);
        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
    }

    /// <summary>Baja lógica (`SC-PROVEEDORES-40`): el registro se conserva y deja de ofrecerse.</summary>
    public void Deactivate(Guid actorUserId, DateTime utcNow)
    {
        IsActive = false;
        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
    }

    /// <summary>Reactivación (`SC-PROVEEDORES-43`).</summary>
    public void Activate(Guid actorUserId, DateTime utcNow)
    {
        IsActive = true;
        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
    }

    public static string NormalizeBusinessName(string? businessName)
    {
        var normalized = businessName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > BusinessNameMaxLength)
        {
            throw new InvalidOperationException("Supplier business name is invalid.");
        }

        return normalized;
    }

    /// <summary>El email es opcional; cuando se informa debe tener formato válido (`SC-PROVEEDORES-07`).</summary>
    public static string? NormalizeEmail(string? email)
    {
        var normalized = NormalizeOptional(email, EmailMaxLength, "email");
        if (normalized is null)
        {
            return null;
        }

        if (!EmailPattern().IsMatch(normalized))
        {
            throw new InvalidOperationException("Supplier email is invalid.");
        }

        return normalized;
    }

    /// <summary>
    /// Coherencia de la ubicación opcional (`REQ-PROVEEDORES-03`, `D-96`): la ubicación parcial se acepta,
    /// pero un nivel informado no puede contradecir a su padre. La existencia en el catálogo se valida en
    /// la capa de aplicación.
    /// </summary>
    public static void ValidateLocationCoherence(
        Guid? countryId,
        Guid? departmentId,
        Guid? municipalityId,
        Guid? departmentCountryId,
        Guid? municipalityDepartmentId)
    {
        if (departmentId.HasValue && countryId.HasValue && departmentCountryId != countryId)
        {
            throw new InvalidOperationException("Supplier department does not belong to the informed country.");
        }

        if (municipalityId.HasValue && departmentId.HasValue && municipalityDepartmentId != departmentId)
        {
            throw new InvalidOperationException("Supplier municipality does not belong to the informed department.");
        }
    }

    private static Guid? NormalizeReference(Guid? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Supplier location reference is invalid.");
        }

        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"Supplier {fieldName} is too long.");
        }

        return normalized;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
