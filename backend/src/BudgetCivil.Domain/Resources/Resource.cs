using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Projects;

namespace BudgetCivil.Domain.Resources;

/// <summary>
/// Insumo propio de un proyecto (`RF-05`, `D-05`, `D-27`). La copia es independiente y conserva en
/// `OriginResourceId` la procedencia del insumo copiado (`R-17`, `D-29`).
/// </summary>
public sealed class Resource
{
    public const int DescriptionMaxLength = 255;
    public const int UnitMaxLength = 20;
    public const int BrandMaxLength = 100;
    public const int DistributorMaxLength = 150;
    public const int UserKeyMaxLength = 50;
    public const int ContactNumberMaxLength = 50;

    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid ResourceTypeId { get; set; }

    public Guid MunicipalityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? Brand { get; set; }

    public string? Distributor { get; set; }

    public string? UserKey { get; set; }

    public string? Details { get; set; }

    public string? ContactNumber { get; set; }

    public QuotationStatus QuotationStatus { get; set; } = QuotationStatus.FaltaInformacion;

    public Guid? OriginResourceId { get; set; }

    public Guid LastModifiedByUserId { get; set; }

    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = [];

    /// <summary>Baja lógica reservada por `D-49`; `E-3a` no expone el endpoint de baja.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public Project Project { get; set; } = null!;

    public ResourceType ResourceType { get; set; } = null!;

    public Municipality Municipality { get; set; } = null!;

    public Resource? OriginResource { get; set; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Unit) &&
        UnitPrice.HasValue &&
        !string.IsNullOrWhiteSpace(Brand);

    public static Resource Create(
        Guid projectId,
        Guid resourceTypeId,
        Guid municipalityId,
        string description,
        string? unit,
        decimal? unitPrice,
        string? brand,
        string? distributor,
        string? userKey,
        string? details,
        string? contactNumber,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        if (resourceTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("Resource type is required.");
        }

        if (municipalityId == Guid.Empty)
        {
            throw new InvalidOperationException("Municipality is required.");
        }

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ResourceTypeId = resourceTypeId,
            MunicipalityId = municipalityId,
            Description = NormalizeDescription(description),
            Unit = NormalizeOptional(unit, UnitMaxLength, "unit"),
            UnitPrice = NormalizeUnitPrice(unitPrice),
            Brand = NormalizeOptional(brand, BrandMaxLength, "brand"),
            Distributor = NormalizeOptional(distributor, DistributorMaxLength, "distributor"),
            UserKey = NormalizeOptional(userKey, UserKeyMaxLength, "user_key"),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            ContactNumber = NormalizeOptional(contactNumber, ContactNumberMaxLength, "contact_number"),
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow,
            IsActive = true
        };

        resource.QuotationStatus = resource.CalculateQuotationStatus();
        return resource;
    }

    public void Update(
        Guid resourceTypeId,
        Guid municipalityId,
        string description,
        string? unit,
        decimal? unitPrice,
        string? brand,
        string? distributor,
        string? userKey,
        string? details,
        string? contactNumber,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (resourceTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("Resource type is required.");
        }

        if (municipalityId == Guid.Empty)
        {
            throw new InvalidOperationException("Municipality is required.");
        }

        ResourceTypeId = resourceTypeId;
        MunicipalityId = municipalityId;
        Description = NormalizeDescription(description);
        Unit = NormalizeOptional(unit, UnitMaxLength, "unit");
        UnitPrice = NormalizeUnitPrice(unitPrice);
        Brand = NormalizeOptional(brand, BrandMaxLength, "brand");
        Distributor = NormalizeOptional(distributor, DistributorMaxLength, "distributor");
        UserKey = NormalizeOptional(userKey, UserKeyMaxLength, "user_key");
        Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        ContactNumber = NormalizeOptional(contactNumber, ContactNumberMaxLength, "contact_number");
        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
        QuotationStatus = CalculateQuotationStatus();
    }

    /// <summary>
    /// Crea una copia independiente en otro proyecto de la misma compañía. `OriginResourceId` registra
    /// la procedencia y no crea ningún vínculo de actualización (`R-17`, `D-29`).
    /// </summary>
    public Resource CopyTo(Guid targetProjectId, Guid actorUserId, DateTime utcNow)
    {
        if (targetProjectId == Guid.Empty)
        {
            throw new InvalidOperationException("Target project is required.");
        }

        var copy = new Resource
        {
            Id = Guid.NewGuid(),
            ProjectId = targetProjectId,
            ResourceTypeId = ResourceTypeId,
            MunicipalityId = MunicipalityId,
            Description = Description,
            Unit = Unit,
            UnitPrice = UnitPrice,
            Brand = Brand,
            Distributor = Distributor,
            UserKey = UserKey,
            Details = Details,
            ContactNumber = ContactNumber,
            OriginResourceId = Id,
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow,
            IsActive = true
        };

        copy.QuotationStatus = copy.CalculateQuotationStatus();
        return copy;
    }

    /// <summary>
    /// `FALTA_INFORMACION` si falta descripción, unidad, precio o marca; en caso contrario conserva
    /// `COTIZADO` si ya lo estaba y, si no, queda `POR_COTIZAR` (`REQ-INSUMOS-04`, `BR-009`).
    /// </summary>
    public QuotationStatus CalculateQuotationStatus()
    {
        if (!IsComplete)
        {
            return QuotationStatus.FaltaInformacion;
        }

        return QuotationStatus == QuotationStatus.Cotizado
            ? QuotationStatus.Cotizado
            : QuotationStatus.PorCotizar;
    }

    public static string NormalizeDescription(string? description)
    {
        var normalized = description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > DescriptionMaxLength)
        {
            throw new InvalidOperationException("Resource description is invalid.");
        }

        return normalized;
    }

    public static decimal? NormalizeUnitPrice(decimal? unitPrice)
    {
        if (unitPrice is < 0m)
        {
            throw new InvalidOperationException("Unit price cannot be negative.");
        }

        return unitPrice;
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
            throw new InvalidOperationException($"{fieldName} is too long.");
        }

        return normalized;
    }
}
