using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Dtos;

public sealed class ResourceDto
{
    public Guid ResourceId { get; init; }

    public Guid ProjectId { get; init; }

    public Guid ResourceTypeId { get; init; }

    public string ResourceTypeCode { get; init; } = string.Empty;

    public string ResourceTypeName { get; init; } = string.Empty;

    public Guid MunicipalityId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    public string? Brand { get; init; }

    public string? Distributor { get; init; }

    public string? UserKey { get; init; }

    public string? Details { get; init; }

    public string? ContactNumber { get; init; }

    public string QuotationStatus { get; init; } = string.Empty;

    public Guid? OriginResourceId { get; init; }

    public Guid LastModifiedBy { get; init; }

    public DateTime LastModifiedAt { get; init; }

    public string RowVersion { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ResourceSuggestionDto
{
    public Guid ResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? UserKey { get; init; }

    public decimal? UnitPrice { get; init; }

    public string QuotationStatus { get; init; } = string.Empty;
}

public static class ResourceMapper
{
    public static ResourceDto ToDto(Resource resource)
    {
        return new ResourceDto
        {
            ResourceId = resource.Id,
            ProjectId = resource.ProjectId,
            ResourceTypeId = resource.ResourceTypeId,
            ResourceTypeCode = resource.ResourceType?.Code ?? string.Empty,
            ResourceTypeName = resource.ResourceType?.Name ?? string.Empty,
            MunicipalityId = resource.MunicipalityId,
            Description = resource.Description,
            Unit = resource.Unit,
            UnitPrice = resource.UnitPrice,
            Brand = resource.Brand,
            Distributor = resource.Distributor,
            UserKey = resource.UserKey,
            Details = resource.Details,
            ContactNumber = resource.ContactNumber,
            QuotationStatus = QuotationStatusCodes.ToCode(resource.QuotationStatus),
            OriginResourceId = resource.OriginResourceId,
            LastModifiedBy = resource.LastModifiedByUserId,
            LastModifiedAt = resource.LastModifiedAtUtc,
            RowVersion = Convert.ToBase64String(resource.RowVersion),
            IsActive = resource.IsActive
        };
    }

    public static ResourceSuggestionDto ToSuggestionDto(Resource resource)
    {
        return new ResourceSuggestionDto
        {
            ResourceId = resource.Id,
            Description = resource.Description,
            UserKey = resource.UserKey,
            UnitPrice = resource.UnitPrice,
            QuotationStatus = QuotationStatusCodes.ToCode(resource.QuotationStatus)
        };
    }
}
